import * as vscode from 'vscode';
import * as cp from 'child_process';
import * as path from 'path';
import { JdDiagnostic, parseJdDiagnostics } from './jdDiagnostics';
import { redactSecrets } from './redact';
import { awaitTaskCompletion } from './taskCompletion';

export interface RegenerateResult {
  exitCode: number;
  projectPath: string;
  /** Redacted build output (safe to pin in the Output Channel / diagnostics). */
  output: string;
  diagnostics: JdDiagnostic[];
  /** True when `dotnet` could not even be started (e.g. ENOENT) — the build never ran. */
  startFailed: boolean;
}

const TASK_TYPE = 'jdEfcpt-regenerate';
const TASK_SOURCE = 'jdEfcpt';
const PROBLEM_MATCHER = '$jdEfcpt-msbuild';

/** Safety timeout so a task that never reports completion can't hang the command forever. */
const TASK_TIMEOUT_MS = 10 * 60 * 1000;

interface JdEfcptConfig {
  dotnetPath: string;
  enableProfiling: boolean;
  buildVerbosity: string;
}

function getConfig(): JdEfcptConfig {
  const config = vscode.workspace.getConfiguration('jdEfcpt');
  return {
    dotnetPath: config.get<string>('dotnetPath', 'dotnet'),
    enableProfiling: config.get<boolean>('enableProfiling', true),
    buildVerbosity: config.get<string>('buildVerbosity', 'minimal'),
  };
}

/**
 * Builds the `dotnet build <proj> -p:EfcptForceRegenerate=true ...` argv for
 * the given project, honoring jdEfcpt.enableProfiling/buildVerbosity config.
 * Pure aside from reading configuration, kept small and easy to eyeball.
 */
export function buildRegenerateArgs(projectPath: string, config: JdEfcptConfig = getConfig()): string[] {
  return [
    'build',
    projectPath,
    '-p:EfcptForceRegenerate=true',
    `-p:EfcptEnableProfiling=${config.enableProfiling ? 'true' : 'false'}`,
    `-p:EfcptLogVerbosity=${config.buildVerbosity}`,
  ];
}

/**
 * Runs the regeneration build as a VS Code Task, executed via
 * vscode.tasks.executeTask, with the $jdEfcpt-msbuild problem matcher
 * attached so compile diagnostics surface in the Problems panel.
 *
 * Implementation note: the task's execution is a CustomExecution backed by a
 * Pseudoterminal that spawns `dotnet` itself (rather than a plain
 * ShellExecution) because the public Tasks API does not expose captured
 * stdout/stderr for ShellExecution tasks, and this extension needs the full
 * output text to parse JDxxxx diagnostics per docs/user-guide/error-codes.md.
 * The pseudoterminal still streams output live to the integrated terminal,
 * so the user-visible behavior matches a normal task.
 *
 * Completion is detected by matching the TaskExecution *returned by*
 * executeTask against onDidEndTaskProcess events (VS Code does not guarantee
 * Task object identity across events), with a safety timeout so the command
 * can never hang indefinitely.
 */
export async function runRegenerateTask(projectPath: string): Promise<RegenerateResult> {
  const config = getConfig();
  const args = buildRegenerateArgs(projectPath, config);
  const projectName = path.basename(projectPath, path.extname(projectPath));

  let outputBuffer = '';
  let startFailed = false;
  const writeEmitter = new vscode.EventEmitter<string>();
  const closeEmitter = new vscode.EventEmitter<number>();

  const append = (text: string) => {
    const redacted = redactSecrets(text);
    outputBuffer += redacted;
    writeEmitter.fire(redacted.replace(/\r?\n/g, '\r\n'));
  };

  const pty: vscode.Pseudoterminal = {
    onDidWrite: writeEmitter.event,
    onDidClose: closeEmitter.event,
    open: () => {
      let child: cp.ChildProcess;
      try {
        child = cp.spawn(config.dotnetPath, args, { cwd: path.dirname(projectPath) });
      } catch (err) {
        startFailed = true;
        append(`Failed to start '${config.dotnetPath}': ${(err as Error).message}\r\n`);
        closeEmitter.fire(1);
        return;
      }

      const forward = (data: Buffer) => append(data.toString());
      child.stdout?.on('data', forward);
      child.stderr?.on('data', forward);
      // A null exit code means the process was killed by a signal — treat as failure.
      child.on('close', (code) => closeEmitter.fire(code ?? 1));
      child.on('error', (err) => {
        // Spawn failures (e.g. dotnet not on PATH / ENOENT) surface here rather
        // than throwing. Record the message in the captured output so the error
        // toast that points users at the Output Channel is not pointing at nothing.
        startFailed = true;
        append(`Failed to start '${config.dotnetPath}': ${err.message}\r\n`);
        closeEmitter.fire(1);
      });
    },
    close: () => {
      /* dotnet build runs to completion; nothing to tear down */
    },
  };

  const execution = new vscode.CustomExecution(async () => pty);
  const task = new vscode.Task(
    { type: TASK_TYPE, project: projectPath },
    vscode.TaskScope.Workspace,
    `Regenerate Models (${projectName})`,
    TASK_SOURCE,
    execution,
    [PROBLEM_MATCHER]
  );
  task.presentationOptions = {
    reveal: vscode.TaskRevealKind.Always,
    panel: vscode.TaskPanelKind.Dedicated,
    clear: true,
  };

  try {
    const exitCode = await awaitTaskCompletion(task, {
      executeTask: (t) => Promise.resolve(vscode.tasks.executeTask(t)),
      onDidEndTaskProcess: (listener) =>
        vscode.tasks.onDidEndTaskProcess((e) =>
          listener({ execution: e.execution, exitCode: e.exitCode })
        ),
      timeoutMs: TASK_TIMEOUT_MS,
    });

    return {
      exitCode,
      projectPath,
      output: outputBuffer,
      diagnostics: parseJdDiagnostics(outputBuffer),
      startFailed,
    };
  } finally {
    writeEmitter.dispose();
    closeEmitter.dispose();
  }
}
