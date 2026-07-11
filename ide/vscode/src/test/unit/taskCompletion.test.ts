import * as assert from 'assert';
import {
  awaitTaskCompletion,
  TaskCompletionDeps,
  TaskEndEvent,
  TASK_TIMEOUT_EXIT_CODE,
  TASK_UNKNOWN_EXIT_CODE,
} from '../../taskCompletion';

// Minimal fakes standing in for VS Code's Task / TaskExecution and the
// vscode.tasks API. The completion helper only relies on reference identity of
// the execution object, so plain objects suffice.
interface FakeTask {
  id: string;
}
interface FakeExecution {
  id: string;
}

/**
 * Builds a fake `tasks`-like dependency bundle plus a hook to fire
 * onDidEndTaskProcess events on demand, so tests can drive the completion path
 * deterministically without VS Code.
 */
function makeDeps(options: {
  execution: FakeExecution | Promise<FakeExecution>;
  timeoutMs?: number;
}): {
  deps: TaskCompletionDeps<FakeTask, FakeExecution>;
  fireEnd: (e: TaskEndEvent<FakeExecution>) => void;
  listenerDisposed: () => boolean;
  timeoutCleared: () => boolean;
} {
  let listener: ((e: TaskEndEvent<FakeExecution>) => void) | undefined;
  let pendingTimeout: (() => void) | undefined;
  let disposed = false;
  let cleared = false;

  const deps: TaskCompletionDeps<FakeTask, FakeExecution> = {
    executeTask: () => Promise.resolve(options.execution),
    onDidEndTaskProcess: (l) => {
      listener = l;
      return {
        dispose: () => {
          disposed = true;
        },
      };
    },
    timeoutMs: options.timeoutMs ?? 5000,
    // Deterministic scheduler: capture the callback so a test can trigger the
    // timeout explicitly rather than relying on wall-clock timing.
    setTimeoutFn: (cb) => {
      pendingTimeout = cb;
      return { token: true };
    },
    clearTimeoutFn: () => {
      cleared = true;
    },
  };

  return {
    deps,
    fireEnd: (e) => {
      // A sentinel event triggers the captured timeout callback instead.
      if ((e as { __timeout?: boolean }).__timeout) {
        pendingTimeout?.();
        return;
      }
      listener?.(e);
    },
    listenerDisposed: () => disposed,
    timeoutCleared: () => cleared,
  };
}

describe('awaitTaskCompletion', () => {
  it('resolves with the exit code when the matching end event fires', async () => {
    const execution: FakeExecution = { id: 'exec-1' };
    const { deps, fireEnd, listenerDisposed, timeoutCleared } = makeDeps({ execution });

    const promise = awaitTaskCompletion<FakeTask, FakeExecution>({ id: 'task-1' }, deps);

    // Let executeTask resolve so `execution` is known, then fire the event.
    await Promise.resolve();
    fireEnd({ execution, exitCode: 0 });

    assert.strictEqual(await promise, 0);
    assert.ok(listenerDisposed(), 'listener should be disposed');
    assert.ok(timeoutCleared(), 'timeout should be cleared');
  });

  it('propagates a non-zero exit code', async () => {
    const execution: FakeExecution = { id: 'exec-2' };
    const { deps, fireEnd } = makeDeps({ execution });

    const promise = awaitTaskCompletion<FakeTask, FakeExecution>({ id: 'task-2' }, deps);
    await Promise.resolve();
    fireEnd({ execution, exitCode: 1 });

    assert.strictEqual(await promise, 1);
  });

  it('ignores end events for other executions (does NOT hang on identity mismatch)', async () => {
    const execution: FakeExecution = { id: 'ours' };
    const { deps, fireEnd } = makeDeps({ execution });

    const promise = awaitTaskCompletion<FakeTask, FakeExecution>({ id: 'task-3' }, deps);
    await Promise.resolve();

    // A different execution's end event must be ignored...
    fireEnd({ execution: { id: 'someone-else' }, exitCode: 99 });
    // ...and only our execution's event resolves the promise.
    fireEnd({ execution, exitCode: 0 });

    assert.strictEqual(await promise, 0);
  });

  it('buffers an end event that arrives before executeTask resolves', async () => {
    let resolveExec!: (e: FakeExecution) => void;
    const execution: FakeExecution = { id: 'buffered' };
    const execPromise = new Promise<FakeExecution>((r) => (resolveExec = r));

    const { deps, fireEnd } = makeDeps({ execution: execPromise });
    const promise = awaitTaskCompletion<FakeTask, FakeExecution>({ id: 'task-4' }, deps);

    // Event fires BEFORE executeTask resolves — must be buffered, not dropped.
    fireEnd({ execution, exitCode: 0 });
    resolveExec(execution);

    assert.strictEqual(await promise, 0);
  });

  it('treats an undefined exit code as failure, not success', async () => {
    const execution: FakeExecution = { id: 'exec-5' };
    const { deps, fireEnd } = makeDeps({ execution });

    const promise = awaitTaskCompletion<FakeTask, FakeExecution>({ id: 'task-5' }, deps);
    await Promise.resolve();
    fireEnd({ execution, exitCode: undefined });

    assert.strictEqual(await promise, TASK_UNKNOWN_EXIT_CODE);
  });

  it('resolves as failure when the safety timeout elapses before any end event', async () => {
    const execution: FakeExecution = { id: 'exec-6' };
    const { deps, fireEnd, listenerDisposed } = makeDeps({ execution });

    const promise = awaitTaskCompletion<FakeTask, FakeExecution>({ id: 'task-6' }, deps);
    await Promise.resolve();
    // Trigger the timeout via the sentinel.
    fireEnd({ __timeout: true } as unknown as TaskEndEvent<FakeExecution>);

    assert.strictEqual(await promise, TASK_TIMEOUT_EXIT_CODE);
    assert.ok(listenerDisposed(), 'listener should be disposed on timeout');
  });

  it('rejects when executeTask itself rejects', async () => {
    const deps: TaskCompletionDeps<FakeTask, FakeExecution> = {
      executeTask: () => Promise.reject(new Error('cannot start task')),
      onDidEndTaskProcess: () => ({ dispose: () => undefined }),
      timeoutMs: 5000,
      setTimeoutFn: () => ({ token: true }),
      clearTimeoutFn: () => undefined,
    };

    await assert.rejects(
      () => awaitTaskCompletion<FakeTask, FakeExecution>({ id: 'task-7' }, deps),
      /cannot start task/
    );
  });
});
