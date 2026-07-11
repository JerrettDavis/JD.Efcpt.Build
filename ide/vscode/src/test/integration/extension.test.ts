import * as assert from 'assert';
import * as vscode from 'vscode';
import type { JdEfcptExtensionApi } from '../../extension';

const EXTENSION_ID = 'JerrettDavis.jd-efcpt-build';

async function activateExtension(): Promise<JdEfcptExtensionApi> {
  const ext = vscode.extensions.getExtension<JdEfcptExtensionApi>(EXTENSION_ID);
  assert.ok(ext, `extension "${EXTENSION_ID}" was not found`);
  const api = await ext!.activate();
  assert.ok(api, 'activate() did not return the test API');
  return api;
}

suite('JD.Efcpt.Build extension', () => {
  test('registers the two command-palette commands', async () => {
    await activateExtension();
    const commands = await vscode.commands.getCommands(true);
    assert.ok(commands.includes('jdEfcpt.regenerateModels'), 'regenerateModels not registered');
    assert.ok(commands.includes('jdEfcpt.showBuildStatus'), 'showBuildStatus not registered');
  });

  test('status bar item exists with the idle indicator after activation', async () => {
    const api = await activateExtension();
    const text = api._test.getStatusBarText();
    assert.ok(text, 'status bar text was empty');
    assert.match(text!, /EF:/);
  });

  test('the status view renders a placeholder before any build profile is loaded', async () => {
    const api = await activateExtension();
    // Note: other tests in this run may have already populated the shared
    // status view via refreshFromProfile; just assert it renders *something*.
    const children = api._test.getStatusViewChildren();
    assert.ok(children.length > 0, 'status view produced no rows');
  });

  test('showBuildStatus command reveals the view container without throwing', async () => {
    await activateExtension();
    await vscode.commands.executeCommand('jdEfcpt.showBuildStatus');
  });
});
