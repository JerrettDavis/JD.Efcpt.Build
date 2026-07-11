import * as assert from 'assert';
import * as fs from 'fs';
import * as path from 'path';
import {
  parseBuildProfile,
  isSchemaSupported,
  formatIso8601Duration,
  BuildProfileParseError,
  SUPPORTED_SCHEMA_MAJOR,
} from '../../buildProfile';

const FIXTURE_DIR = path.join(__dirname, '..', '..', '..', 'test-fixtures');

function readFixture(name: string): string {
  return fs.readFileSync(path.join(FIXTURE_DIR, name), 'utf8');
}

describe('buildProfile', () => {
  describe('parseBuildProfile', () => {
    it('parses a valid v1 profile fixture', () => {
      const profile = parseBuildProfile(readFixture('build-profile.success.json'));
      assert.strictEqual(profile.schemaVersion, '1.0.0');
      assert.strictEqual(profile.schemaSupported, true);
      assert.strictEqual(profile.status, 'Success');
      assert.strictEqual(profile.modelCount, 2);
      assert.strictEqual(profile.artifacts.length, 3);
      assert.strictEqual(profile.diagnostics.length, 1);
      assert.strictEqual(profile.projectName, 'MyProject');
    });

    it('normalizes the diagnostic `level` field (real schema) into severity', () => {
      const profile = parseBuildProfile(readFixture('build-profile.success.json'));
      assert.strictEqual(profile.diagnostics[0].severity, 'warning');
      assert.strictEqual(profile.diagnostics[0].code, 'JD0001');

      const failed = parseBuildProfile(readFixture('build-profile.failed.json'));
      assert.strictEqual(failed.diagnostics[0].severity, 'error');
      assert.strictEqual(failed.diagnostics[0].code, 'JD0011');
    });

    it('falls back to `severity` when `level` is absent, defaulting to info', () => {
      const profile = parseBuildProfile(
        JSON.stringify({
          schemaVersion: '1.0.0',
          status: 'Success',
          diagnostics: [
            { severity: 'Error', code: 'JD9999', message: 'legacy field' },
            { code: 'JD0000', message: 'no severity at all' },
          ],
        })
      );
      assert.strictEqual(profile.diagnostics[0].severity, 'error');
      assert.strictEqual(profile.diagnostics[1].severity, 'info');
    });

    it('counts only artifacts of type GeneratedModel', () => {
      const profile = parseBuildProfile(readFixture('build-profile.success.json'));
      const nonModelArtifacts = profile.artifacts.filter((a) => a.type !== 'GeneratedModel');
      assert.strictEqual(nonModelArtifacts.length, 1);
      assert.strictEqual(profile.modelCount, 2);
    });

    it('parses a failed profile with diagnostics', () => {
      const profile = parseBuildProfile(readFixture('build-profile.failed.json'));
      assert.strictEqual(profile.status, 'Failed');
      assert.strictEqual(profile.modelCount, 0);
      assert.ok(profile.diagnostics.length >= 1);
    });

    it('flags an unsupported future MAJOR schema version without throwing', () => {
      const profile = parseBuildProfile(readFixture('build-profile.futureSchema.json'));
      assert.strictEqual(profile.schemaSupported, false);
      assert.strictEqual(profile.status, 'Success');
    });

    it('defaults artifacts/diagnostics to empty arrays when absent', () => {
      const profile = parseBuildProfile(
        JSON.stringify({ schemaVersion: '1.0.0', status: 'Success' })
      );
      assert.deepStrictEqual(profile.artifacts, []);
      assert.deepStrictEqual(profile.diagnostics, []);
      assert.strictEqual(profile.modelCount, 0);
    });

    it('throws BuildProfileParseError for invalid JSON', () => {
      assert.throws(() => parseBuildProfile('{ not json'), BuildProfileParseError);
    });

    it('throws BuildProfileParseError when schemaVersion is missing', () => {
      assert.throws(
        () => parseBuildProfile(JSON.stringify({ status: 'Success' })),
        BuildProfileParseError
      );
    });

    it('throws BuildProfileParseError when status is missing', () => {
      assert.throws(
        () => parseBuildProfile(JSON.stringify({ schemaVersion: '1.0.0' })),
        BuildProfileParseError
      );
    });
  });

  describe('isSchemaSupported', () => {
    it('supports the current major version', () => {
      assert.strictEqual(isSchemaSupported(`${SUPPORTED_SCHEMA_MAJOR}.0.0`), true);
    });

    it('supports older major versions', () => {
      assert.strictEqual(isSchemaSupported('0.9.0'), true);
    });

    it('rejects a newer major version', () => {
      assert.strictEqual(isSchemaSupported(`${SUPPORTED_SCHEMA_MAJOR + 1}.0.0`), false);
    });

    it('rejects an unparseable version string', () => {
      assert.strictEqual(isSchemaSupported('not-a-version'), false);
    });
  });

  describe('formatIso8601Duration', () => {
    it('formats minutes and seconds', () => {
      assert.strictEqual(formatIso8601Duration('PT1M30S'), '1m 30s');
    });

    it('formats hours, minutes, and seconds', () => {
      assert.strictEqual(formatIso8601Duration('PT1H2M3S'), '1h 2m 3s');
    });

    it('returns "unknown" for undefined input', () => {
      assert.strictEqual(formatIso8601Duration(undefined), 'unknown');
    });

    it('falls back to the raw string for unparseable durations', () => {
      assert.strictEqual(formatIso8601Duration('bogus'), 'bogus');
    });
  });
});
