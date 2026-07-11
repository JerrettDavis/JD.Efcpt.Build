import * as vscode from 'vscode';
import * as cp from 'child_process';
import * as path from 'path';
import { JdDiagnostic, parseJdDiagnostics } from './jdDiagnostics';

export interface RegenerateResult {
  exitCode: number;
  projectPath: string;
  output: string;
  diagnostics: JdDiagnostic[];
}

const TASK_TYPE = 'jdEfcpt-regenerate';
const TASK_SOURCE = 'jdEfcpt';
const PROBLEM_MATCHER = '$jdEfcpt-msbuild';

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
    buildVerbosity: config.get<string>('buildVerbosity', 'detailed'),
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
 */
export async function runRegenerateTask(projectPath: string): Promise<RegenerateResult> {
  const config = getConfig();
  const args = buildRegenerateArgs(projectPath, config);
  const projectName = path.basename(projectPath, path.extname(projectPath));

  let outputBuffer = '';
  const writeEmitter = new vscode.EventEmitter<string>();
  const closeEmitter = new vscode.EventEmitter<number>();

  const pty: vscode.Pseudoterminal = {
    onDidWrite: writeEmitter.event,
    onDidClose: closeEmitter.event,
    open: () => {
      let child: cp.ChildProcess;
      try {
        child = cp.spawn(config.dotnetPath, args, { cwd: path.dirname(projectPath) });
      } catch (err) {
        writeEmitter.fire(`${(err as Error).message}\r\n`);
        closeEmitter.fire(1);
        return;
      }

      const forward = (data: Buffer) => {
        const text = data.toString();
        outputBuffer += text;
        writeEmitter.fire(text.replace(/\r?\n/g, '\r\n'));
      };
      child.stdout?.on('data', forward);
      child.stderr?.on('data', forward);
      child.on('close', (code) => closeEmitter.fire(code ?? 0));
      child.on('error', (err) => {
        writeEmitter.fire(`${err.message}\r\n`);
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

  const exitCode = await new Promise<number>((resolve) => {
    const listener = vscode.tasks.onDidEndTaskProcess((e) => {
      if (e.execution.task === task) {
        listener.dispose();
        resolve(e.exitCode ?? 0);
      }
    });
    void vscode.tasks.executeTask(task);
  });

  return {
    exitCode,
    projectPath,
    output: outputBuffer,
    diagnostics: parseJdDiagnostics(outputBuffer),
  };
}
