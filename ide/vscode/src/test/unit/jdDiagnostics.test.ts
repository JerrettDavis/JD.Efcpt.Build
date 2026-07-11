import * as assert from 'assert';
import { parseJdDiagnosticLine, parseJdDiagnostics } from '../../jdDiagnostics';

describe('jdDiagnostics', () => {
  describe('parseJdDiagnosticLine', () => {
    it('parses a warning line', () => {
      const result = parseJdDiagnosticLine(
        "warning JD0002: Connection string 'MyDatabase' not found in appsettings.json"
      );
      assert.deepStrictEqual(result, {
        severity: 'warning',
        code: 'JD0002',
        message: "Connection string 'MyDatabase' not found in appsettings.json",
      });
    });

    it('parses an error line', () => {
      const result = parseJdDiagnosticLine(
        "error JD0011: Failed to parse configuration file 'appsettings.json': Unexpected character encountered"
      );
      assert.deepStrictEqual(result, {
        severity: 'error',
        code: 'JD0011',
        message: "Failed to parse configuration file 'appsettings.json': Unexpected character encountered",
      });
    });

    it('parses when the diagnostic is prefixed by MSBuild project/target context', () => {
      const result = parseJdDiagnosticLine(
        '/path/Project.csproj : warning JD0001: Configuration file mismatch [/path/Project.csproj]'
      );
      assert.ok(result);
      assert.strictEqual(result?.code, 'JD0001');
      assert.strictEqual(result?.severity, 'warning');
    });

    it('returns null for a non-matching line', () => {
      assert.strictEqual(parseJdDiagnosticLine('Build succeeded.'), null);
    });

    it('returns null for a CS-prefixed compiler diagnostic (not a JD code)', () => {
      assert.strictEqual(
        parseJdDiagnosticLine('Program.cs(10,5): error CS0103: The name does not exist'),
        null
      );
    });

    it('returns null for a JD code with the wrong digit count', () => {
      assert.strictEqual(parseJdDiagnosticLine('warning JD12: short code'), null);
    });

    it('is case-sensitive on the JD prefix', () => {
      assert.strictEqual(parseJdDiagnosticLine('warning jd0001: lowercase prefix'), null);
    });
  });

  describe('parseJdDiagnostics', () => {
    it('extracts every JD diagnostic from multi-line build output, preserving order', () => {
      const output = [
        'Restoring packages...',
        'warning JD0002: Connection string missing',
        'Some other line',
        'error JD0011: Parse failed',
        'Build FAILED.',
      ].join('\n');

      const results = parseJdDiagnostics(output);
      assert.strictEqual(results.length, 2);
      assert.strictEqual(results[0].code, 'JD0002');
      assert.strictEqual(results[0].severity, 'warning');
      assert.strictEqual(results[1].code, 'JD0011');
      assert.strictEqual(results[1].severity, 'error');
    });

    it('handles CRLF line endings', () => {
      const output = 'warning JD0001: first\r\nerror JD0012: second\r\n';
      const results = parseJdDiagnostics(output);
      assert.strictEqual(results.length, 2);
    });

    it('returns an empty array for output with no JD diagnostics', () => {
      assert.deepStrictEqual(parseJdDiagnostics('Build succeeded.\n0 Warning(s)\n0 Error(s)'), []);
    });
  });
});
