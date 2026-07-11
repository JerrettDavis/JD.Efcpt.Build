import * as assert from 'assert';
import { redactSecrets } from '../../redact';

describe('redactSecrets', () => {
  it('masks a Password=... value', () => {
    const result = redactSecrets('Server=.;Database=App;Password=secret;');
    assert.ok(!result.includes('secret'), 'password value leaked');
    assert.ok(result.includes('Password=***'));
    // Non-sensitive keys stay visible.
    assert.ok(result.includes('Server=.'));
    assert.ok(result.includes('Database=App'));
  });

  it('masks a bare "Password=secret" line', () => {
    assert.strictEqual(redactSecrets('Password=secret'), 'Password=***');
  });

  it('masks Pwd, User ID, and Uid variants case-insensitively', () => {
    assert.strictEqual(redactSecrets('pwd=hunter2;'), 'pwd=***;');
    assert.strictEqual(redactSecrets('User Id=admin;'), 'User Id=***;');
    assert.strictEqual(redactSecrets('UID=admin;'), 'UID=***;');
    assert.strictEqual(redactSecrets('PASSWORD=Secret;'), 'PASSWORD=***;');
  });

  it('masks a connection string embedded in an efcpt command line', () => {
    const line =
      'efcpt "Server=db;Database=App;User Id=sa;Password=P@ss;" mssql -i config.json';
    const result = redactSecrets(line);
    assert.ok(!result.includes('P@ss'), 'password leaked');
    assert.ok(!result.includes('sa;'), 'user id leaked');
    assert.ok(result.includes('mssql -i config.json'), 'non-secret args should remain');
  });

  it('masks a quoted value containing a semicolon (no regex bypass)', () => {
    // A naive value match that excludes quotes would match zero chars here and
    // leave the secret intact after a fake mask.
    assert.strictEqual(redactSecrets('Password="p@ss;word";'), 'Password=***;');
    assert.strictEqual(redactSecrets("Pwd='x;y';"), 'Pwd=***;');

    const mixed = redactSecrets('Server=db;Password="a;b";Database=App;');
    assert.ok(!mixed.includes('"a;b"'), 'quoted secret leaked');
    assert.ok(mixed.includes('Server=db'));
    assert.ok(mixed.includes('Database=App'));
  });

  it('masks cloud-provider credential keys', () => {
    assert.ok(!redactSecrets('AccessToken=abc123;').includes('abc123'));
    assert.ok(!redactSecrets('AccountKey=Zm9vYmFy;').includes('Zm9vYmFy'));
    assert.ok(
      !redactSecrets('SharedAccessSignature=sv=2020&sig=deadbeef;').includes('deadbeef')
    );
    assert.ok(!redactSecrets('SAS Token=tok999;').includes('tok999'));
  });

  it('leaves output with no secrets unchanged', () => {
    const line = 'Build succeeded.\n  2 model(s) generated\n';
    assert.strictEqual(redactSecrets(line), line);
  });

  it('returns empty string input unchanged', () => {
    assert.strictEqual(redactSecrets(''), '');
  });
});
