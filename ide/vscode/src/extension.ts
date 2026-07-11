import * as vscode from 'vscode';
import * as fs from 'fs';
import * as path from 'path';
import { discoverJdEfcptProjects } from './projectDiscovery';
import { parseBuildProfile, ParsedBuildProfile, BuildProfileParseError } from './buildProfile';
import { runRegenerateTask } from './regenerate';
import { JdDiagnostic } from './jdDiagnostics';
import { JdEfcptStatusBar } from './statusBar';
import { JdEfcptStatusViewProvider } from './statusView';

const DIAGNOSTIC_COLLECTION_NAME = 'jdEfcpt';
const BUILD_PROFILE_GLOB = '**/obj/efcpt/build-profile.json';
const EXCLUDE_GLOB = '**/{bin,node_modules}/**';
const CSDEVKIT_EXTENSION_ID = 'ms-dotnettools.csdevkit';
const CSDEVKIT_PROMPT_SHOWN_KEY = 'jdEfcpt.csdevkitRecommendationShown';

let statusBar: JdEfcptStatusBar | undefined;
let statusViewProvider: JdEfcptStatusViewProvider | undefined;
let diagnosticCollection: vscode.DiagnosticCollection | undefined;
let outputChannel: vscode.OutputChannel | undefined;

/**
 * Test-only surface exposed via the activate() return value (accessible as
 * `vscode.extensions.getExtension(id).exports._test` from integration
 * tests). Lets tests exercise diagnostics/status-bar/tree-view wiring
 * directly instead of requiring a real `dotnet build` in CI.
 */
export interface JdEfcptTestApi {
  refreshFromProfile: (profilePath: string) => Promise<void>;
  applyJdDiagnostics: (projectPath: string, diagnostics: JdDiagnostic[]) => void;
  getStatusBarText: () => string | undefined;
  getStatusViewChildren: () => Array<{ label: string; description?: string }>;
}

export interface JdEfcptExtensionApi {
  _test: JdEfcptTestApi;
}

export async function activate(
  context: vscode.ExtensionContext
): Promise<JdEfcptExtensionApi> {
  outputChannel = vscode.window.createOutputChannel('JD.Efcpt.Build');
  diagnosticCollection = vscode.languages.createDiagnosticCollection(DIAGNOSTIC_COLLECTION_NAME);
  statusBar = new JdEfcptStatusBar();
  statusViewProvider = new JdEfcptStatusViewProvider();

  context.subscriptions.push(
    outputChannel,
    diagnosticCollection,
    statusBar,
    vscode.window.registerTreeDataProvider('jdEfcpt.statusView', statusViewProvider),
    vscode.commands.registerCommand('jdEfcpt.regenerateModels', regenerateModelsCommand),
    vscode.commands.registerCommand('jdEfcpt.showBuildStatus', showBuildStatusCommand)
  );

  // A plain `dotnet build` from a terminal (no extension involvement) also
  // refreshes build-profile.json when profiling is enabled — pick that up too.
  const watcher = vscode.workspace.createFileSystemWatcher(BUILD_PROFILE_GLOB);
  const refresh = (uri: vscode.Uri) =>
    refreshFromProfile(uri.fsPath).catch((err) => outputChannel?.appendLine(String(err)));
  context.subscriptions.push(
    watcher,
    watcher.onDidChange(refresh),
    watcher.onDidCreate(refresh)
  );

  // Fire-and-forget: this shows a UI notification whose promise only resolves on
  // user dismissal. Awaiting it here would block activation indefinitely in a
  // headless host (e.g. CI integration tests), so it must NOT be awaited.
  void recommendCSharpDevKitIfMissing(context).catch(() => undefined);

  const existing = await vscode.workspace.findFiles(BUILD_PROFILE_GLOB, EXCLUDE_GLOB, 1);
  if (existing.length > 0) {
    await refreshFromProfile(existing[0].fsPath).catch(() => undefined);
  }

  return {
    _test: {
      refreshFromProfile,
      applyJdDiagnostics,
      getStatusBarText: () => statusBar?.getText(),
      getStatusViewChildren: () =>
        (statusViewProvider?.getChildren() ?? []).map((node) => ({
          label: typeof node.label === 'string' ? node.label : String(node.label ?? ''),
          description: typeof node.description === 'string' ? node.description : undefined,
        })),
    },
  };
}

export function deactivate(): void {
  statusBar?.dispose();
  diagnosticCollection?.dispose();
  outputChannel?.dispose();
}

async function findCandidateProjects(): Promise<string[]> {
  const csprojUris = await vscode.workspace.findFiles('**/*.csproj', EXCLUDE_GLOB);
  return discoverJdEfcptProjects(
    csprojUris.map((u) => u.fsPath),
    (p) => fs.readFileSync(p, 'utf8')
  );
}

async function pickProject(): Promise<string | undefined> {
  const candidates = await findCandidateProjects();
  if (candidates.length === 0) {
    void vscode.window.showWarningMessage(
      'No .csproj referencing JD.Efcpt.Build was found in this workspace.'
    );
    return undefined;
  }
  if (candidates.length === 1) {
    return candidates[0];
  }
  const picked = await vscode.window.showQuickPick(
    candidates.map((p) => ({ label: path.basename(p), description: p })),
    { placeHolder: 'Select a project to regenerate models for' }
  );
  return picked?.description;
}

function getMtimeMs(filePath: string): number | undefined {
  try {
    return fs.statSync(filePath).mtimeMs;
  } catch {
    return undefined;
  }
}

async function regenerateModelsCommand(): Promise<void> {
  // The entire body (including project discovery) is guarded so any failure
  // surfaces via showErrorMessage + the output channel instead of an unhandled
  // rejection.
  try {
    const projectPath = await pickProject();
    if (!projectPath) {
      return;
    }

    const profilePath = path.join(path.dirname(projectPath), 'obj', 'efcpt', 'build-profile.json');
    // Capture the profile's timestamp BEFORE the build so we can tell whether
    // THIS run wrote it, rather than reusing a prior run's stale profile.
    const preBuildMtime = getMtimeMs(profilePath);

    statusBar?.setBuilding();

    const result = await runRegenerateTask(projectPath);
    outputChannel?.appendLine(result.output);

    // Only clear+repopulate JD diagnostics when the build actually ran and
    // produced output. A spawn failure (e.g. dotnet not on PATH) must not wipe
    // diagnostics captured from a previous, real run.
    if (!result.startFailed) {
      applyJdDiagnostics(projectPath, result.diagnostics);
    }

    // Only trust build-profile.json if THIS run wrote it (mtime strictly newer,
    // or the file is newly created). A stale profile from a prior successful
    // build must never be presented as a fresh success after a failed/aborted run.
    const postBuildMtime = getMtimeMs(profilePath);
    const profileIsFresh =
      postBuildMtime !== undefined &&
      (preBuildMtime === undefined || postBuildMtime > preBuildMtime);

    if (profileIsFresh) {
      await refreshFromProfile(profilePath);
    } else if (result.exitCode === 0 && !result.startFailed) {
      // Build succeeded but wrote no fresh profile (e.g. profiling disabled).
      // Show a neutral state rather than reusing a stale profile's model count.
      statusBar?.setIdle();
      statusViewProvider?.update(undefined);
    } else {
      statusBar?.setFailed();
      statusViewProvider?.update(undefined);
    }

    if (result.exitCode !== 0) {
      const lead = result.startFailed
        ? `JD.Efcpt.Build could not start the build for ${path.basename(projectPath)}`
        : `JD.Efcpt.Build regeneration failed for ${path.basename(projectPath)} (exit code ${result.exitCode})`;
      void vscode.window.showErrorMessage(
        `${lead}. See the JD.Efcpt.Build output channel and the Terminal panel for details.`
      );
    }
  } catch (err) {
    statusBar?.setFailed();
    outputChannel?.appendLine(`JD.Efcpt.Build regeneration error: ${(err as Error).message}`);
    void vscode.window.showErrorMessage(
      `JD.Efcpt.Build regeneration failed: ${(err as Error).message}`
    );
  }
}

async function showBuildStatusCommand(): Promise<void> {
  await vscode.commands.executeCommand('workbench.view.extension.jdEfcpt');
}

async function refreshFromProfile(profilePath: string): Promise<void> {
  let text: string;
  try {
    text = await fs.promises.readFile(profilePath, 'utf8');
  } catch (err) {
    outputChannel?.appendLine(
      `Could not read build-profile.json at ${profilePath}: ${(err as Error).message}`
    );
    return;
  }

  let profile: ParsedBuildProfile;
  try {
    profile = parseBuildProfile(text);
  } catch (err) {
    if (err instanceof BuildProfileParseError) {
      outputChannel?.appendLine(`Ignoring unreadable build-profile.json: ${err.message}`);
    }
    return;
  }

  if (!profile.schemaSupported) {
    outputChannel?.appendLine(
      `Warning: build-profile.json schemaVersion ${profile.schemaVersion} is newer than this extension supports (max supported major: 1).`
    );
  }

  statusViewProvider?.update(profile);
  if (profile.status === 'Success') {
    statusBar?.setSuccess(profile);
  } else {
    statusBar?.setFailed(profile);
  }
}

function applyJdDiagnostics(projectPath: string, diagnostics: JdDiagnostic[]): void {
  if (!diagnosticCollection) {
    return;
  }
  const uri = vscode.Uri.file(projectPath);
  const range = new vscode.Range(0, 0, 0, 0);
  const vsDiagnostics = diagnostics.map((d) => {
    const severity =
      d.severity === 'error' ? vscode.DiagnosticSeverity.Error : vscode.DiagnosticSeverity.Warning;
    const diagnostic = new vscode.Diagnostic(range, `${d.code}: ${d.message}`, severity);
    diagnostic.source = 'jdEfcpt';
    diagnostic.code = d.code;
    return diagnostic;
  });
  diagnosticCollection.set(uri, vsDiagnostics);
}

async function recommendCSharpDevKitIfMissing(context: vscode.ExtensionContext): Promise<void> {
  if (context.globalState.get<boolean>(CSDEVKIT_PROMPT_SHOWN_KEY)) {
    return;
  }
  // Soft-detect only: we tailor a one-time informational message, and never
  // call into C# Dev Kit's (private, undocumented) APIs.
  const csdevkit = vscode.extensions.getExtension(CSDEVKIT_EXTENSION_ID);
  if (csdevkit) {
    return;
  }
  await context.globalState.update(CSDEVKIT_PROMPT_SHOWN_KEY, true);
  const selection = await vscode.window.showInformationMessage(
    'JD.Efcpt.Build works great alongside the C# Dev Kit extension for .NET project management.',
    'Show C# Dev Kit'
  );
  if (selection === 'Show C# Dev Kit') {
    await vscode.commands.executeCommand('workbench.extensions.search', CSDEVKIT_EXTENSION_ID);
  }
}
