import { defineConfig } from '@vscode/test-cli';

// @vscode/test-electron commonly HANGS in CI unless Electron is launched with
// --no-sandbox (the CI user can't create a sandbox), and a missing shared
// memory mount (/dev/shm) or GPU can wedge startup — hence --disable-dev-shm-usage
// and --disable-gpu. Combined with strict mocha timeouts, a stuck test fails
// fast instead of hanging the whole job.
export default defineConfig({
  files: 'out/test/integration/**/*.test.js',
  workspaceFolder: './test-fixtures/sample-workspace',
  launchArgs: ['--no-sandbox', '--disable-gpu', '--disable-dev-shm-usage'],
  mocha: {
    ui: 'tdd',
    // Per-test timeout: a wedged assertion fails in 20s rather than hanging.
    timeout: 20000,
    // Fail the run on the first stuck/failing test instead of pressing on.
    bail: true,
    // Cap slow-test reporting noise.
    slow: 10000,
  },
});
