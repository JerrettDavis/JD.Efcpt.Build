/**
 * Pure, vscode-free task-completion detection used by regenerate.ts.
 *
 * VS Code does NOT guarantee Task object identity across events, so completion
 * must be matched against the TaskExecution returned by `executeTask(...)`, not
 * against the Task object passed in. This module encapsulates that logic behind
 * injectable dependencies so it can be unit-tested with a fake `tasks` API
 * (fake executeTask + a manually-fired onDidEndTaskProcess) — asserting it
 * resolves with the exit code and never hangs.
 */

export interface TaskEndEvent<TExec> {
  execution: TExec;
  exitCode: number | undefined;
}

export interface Disposable {
  dispose(): void;
}

export interface TaskCompletionDeps<TTask, TExec> {
  /** Mirrors vscode.tasks.executeTask — resolves with the running TaskExecution. */
  executeTask: (task: TTask) => Promise<TExec>;
  /** Mirrors vscode.tasks.onDidEndTaskProcess — returns a disposable subscription. */
  onDidEndTaskProcess: (listener: (e: TaskEndEvent<TExec>) => void) => Disposable;
  /** Safety timeout in milliseconds after which the task is treated as failed. */
  timeoutMs: number;
  setTimeoutFn?: (cb: () => void, ms: number) => unknown;
  clearTimeoutFn?: (handle: unknown) => void;
}

/** Exit code reported when the safety timeout elapses before the task ends. */
export const TASK_TIMEOUT_EXIT_CODE = -1;

/**
 * Exit code reported when the task ends but the event carried no exit code.
 * Treated as a failure (non-zero) so an abnormal termination is never read as
 * success.
 */
export const TASK_UNKNOWN_EXIT_CODE = 1;

/**
 * Executes a task and resolves with its exit code once the matching
 * onDidEndTaskProcess event fires. Always disposes the listener and clears the
 * timeout on every resolution/rejection path. Rejects only if executeTask
 * itself rejects.
 */
export function awaitTaskCompletion<TTask, TExec>(
  task: TTask,
  deps: TaskCompletionDeps<TTask, TExec>
): Promise<number> {
  const setT = deps.setTimeoutFn ?? ((cb, ms) => setTimeout(cb, ms));
  const clearT = deps.clearTimeoutFn ?? ((h) => clearTimeout(h as ReturnType<typeof setTimeout>));

  return new Promise<number>((resolve, reject) => {
    let execution: TExec | undefined;
    let settled = false;
    // Events can arrive before executeTask resolves (fast tasks); buffer until
    // we know the execution identity to compare against.
    const buffered: Array<TaskEndEvent<TExec>> = [];

    const cleanup = () => {
      listener.dispose();
      clearT(timer);
    };

    const settle = (code: number) => {
      if (settled) {
        return;
      }
      settled = true;
      cleanup();
      resolve(code);
    };

    const tryMatch = (e: TaskEndEvent<TExec>): boolean => {
      if (execution !== undefined && e.execution === execution) {
        settle(e.exitCode ?? TASK_UNKNOWN_EXIT_CODE);
        return true;
      }
      return false;
    };

    const listener = deps.onDidEndTaskProcess((e) => {
      if (settled) {
        return;
      }
      if (execution === undefined) {
        buffered.push(e);
      } else {
        tryMatch(e);
      }
    });

    const timer = setT(() => settle(TASK_TIMEOUT_EXIT_CODE), deps.timeoutMs);

    deps.executeTask(task).then(
      (exec) => {
        execution = exec;
        for (const e of buffered) {
          if (settled) {
            break;
          }
          tryMatch(e);
        }
      },
      (err) => {
        if (settled) {
          return;
        }
        settled = true;
        cleanup();
        reject(err instanceof Error ? err : new Error(String(err)));
      }
    );
  });
}
