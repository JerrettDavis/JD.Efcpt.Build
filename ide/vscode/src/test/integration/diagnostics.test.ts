import * as assert from 'assert';
import * as fs from 'fs';
import * as path from 'path';
import * as vscode from 'vscode';
import type { JdEfcptExtensionApi } from '../../extension';

const EXTENSION_ID = 'JerrettDavis.jd-efcpt-build';

async function getApi(): Promise<JdEfcptExtensionApi> {
  const ext = vscode.extensions.getExtension<JdEfcptExtensionApi>(EXTENSION_ID);
  assert.ok(ext);
  return ext!.isActive ? ext!.exports : ext!.activate();
}

suite('JD.Efcpt.Build diagnostics + status wiring', () => {
  test('refreshFromProfile populates the status bar and status view from a fixture profile', async () => {
    const api = await getApi();
    const workspaceFolder = vscode.workspace.workspaceFolders?.[0];
    assert.ok(workspaceFolder, 'expected a workspace folder to be open for integration tests');

    const profileDir = path.join(workspaceFolder!.uri.fsPath, 'SampleApp', 'obj', 'efcpt');
    fs.mkdirSync(profileDir, { recursive: true });
    const profilePath = path.join(profileDir, 'build-profile.json');
    fs.writeFileSync(
      profilePath,
      JSON.stringify({
        schemaVersion: '1.0.0',
        status: 'Success',
        startTime: '2024-01-11T12:00:00Z',
        endTime: '2024-01-11T12:01:30Z',
        duration: 'PT1M30S',
        project: { name: 'SampleApp' },
        artifacts: [
          { path: 'Customer.g.cs', type: 'GeneratedModel', size: 100 },
          { path: 'Order.g.cs', type: 'GeneratedModel', size: 120 },
        ],
        diagnostics: [{ severity: 'warning', code: 'JD0001', message: 'Fixture diagnostic' }],
      }),
      'utf8'
    );

    try {
      await api._test.refreshFromProfile(profilePath);

      const statusText = api._test.getStatusBarText();
      assert.ok(statusText, 'status bar text missing after refresh');
      assert.match(statusText!, /2 models?/);

      const children = api._test.getStatusViewChildren();
      const labels = children.map((c) => c.label);
      assert.ok(labels.includes('Status'), 'status view missing Status row');
      const modelsRow = children.find((c) => c.label === 'Models generated');
      assert.strictEqual(modelsRow?.description, '2');
    } finally {
      fs.rmSync(path.join(workspaceFolder!.uri.fsPath, 'SampleApp', 'obj'), {
        recursive: true,
        force: true,
      });
    }
  });

  test('applyJdDiagnostics populates the DiagnosticCollection for the owning csproj', async () => {
    const api = await getApi();
    const workspaceFolder = vscode.workspace.workspaceFolders?.[0];
    assert.ok(workspaceFolder);
    const projectPath = path.join(workspaceFolder!.uri.fsPath, 'SampleApp', 'SampleApp.csproj');

    api._test.applyJdDiagnostics(projectPath, [
      { severity: 'warning', code: 'JD0002', message: 'Connection string missing' },
      { severity: 'error', code: 'JD0011', message: 'Failed to parse configuration file' },
    ]);

    const diagnostics = vscode.languages.getDiagnostics(vscode.Uri.file(projectPath));
    assert.strictEqual(diagnostics.length, 2);
    assert.ok(diagnostics.some((d) => d.severity === vscode.DiagnosticSeverity.Warning));
    assert.ok(diagnostics.some((d) => d.severity === vscode.DiagnosticSeverity.Error));
    assert.ok(diagnostics.every((d) => d.source === 'jdEfcpt'));

    // Clean up so this test doesn't leak state into other suites.
    api._test.applyJdDiagnostics(projectPath, []);
  });
});
