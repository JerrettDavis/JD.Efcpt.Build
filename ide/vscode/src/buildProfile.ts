/**
 * Types and a pure reader/parser for the JD.Efcpt.Build profiling output
 * (obj/efcpt/build-profile.json). Schema documented in
 * docs/user-guide/build-profiling.md. This module has no dependency on the
 * `vscode` API so it can be unit tested in plain Node/mocha.
 */

/** Highest schema MAJOR version this extension understands. */
export const SUPPORTED_SCHEMA_MAJOR = 1;

export type BuildProfileStatus = 'Success' | 'Failed' | 'Skipped' | 'Canceled';

export interface BuildProfileArtifact {
  path: string;
  type: string;
  size: number;
}

export interface BuildProfileDiagnostic {
  severity?: string;
  code?: string;
  message?: string;
  [key: string]: unknown;
}

/** Raw shape of obj/efcpt/build-profile.json (subset this extension reads). */
export interface BuildProfileJson {
  schemaVersion: string;
  runId?: string;
  startTime?: string;
  endTime?: string;
  duration?: string;
  status: BuildProfileStatus | string;
  project?: {
    path?: string;
    name?: string;
    targetFramework?: string;
    configuration?: string;
  };
  artifacts?: BuildProfileArtifact[];
  diagnostics?: BuildProfileDiagnostic[];
  [key: string]: unknown;
}

export interface ParsedBuildProfile {
  schemaVersion: string;
  schemaSupported: boolean;
  status: string;
  startTime?: string;
  endTime?: string;
  duration?: string;
  modelCount: number;
  artifacts: BuildProfileArtifact[];
  diagnostics: BuildProfileDiagnostic[];
  projectName?: string;
}

export class BuildProfileParseError extends Error {}

/**
 * Parses raw JSON text into a ParsedBuildProfile. Throws BuildProfileParseError
 * on malformed JSON or a payload missing required fields.
 */
export function parseBuildProfile(jsonText: string): ParsedBuildProfile {
  let raw: BuildProfileJson;
  try {
    raw = JSON.parse(jsonText) as BuildProfileJson;
  } catch (err) {
    throw new BuildProfileParseError(
      `Failed to parse build-profile.json: ${(err as Error).message}`
    );
  }

  if (!raw || typeof raw !== 'object') {
    throw new BuildProfileParseError('build-profile.json did not contain a JSON object');
  }
  if (typeof raw.schemaVersion !== 'string' || raw.schemaVersion.length === 0) {
    throw new BuildProfileParseError('build-profile.json is missing "schemaVersion"');
  }
  if (typeof raw.status !== 'string' || raw.status.length === 0) {
    throw new BuildProfileParseError('build-profile.json is missing "status"');
  }

  const artifacts = Array.isArray(raw.artifacts) ? raw.artifacts : [];
  const diagnostics = Array.isArray(raw.diagnostics) ? raw.diagnostics : [];
  const modelCount = artifacts.filter((a) => a && a.type === 'GeneratedModel').length;

  return {
    schemaVersion: raw.schemaVersion,
    schemaSupported: isSchemaSupported(raw.schemaVersion),
    status: raw.status,
    startTime: raw.startTime,
    endTime: raw.endTime,
    duration: raw.duration,
    modelCount,
    artifacts,
    diagnostics,
    projectName: raw.project?.name,
  };
}

/** True when this extension's schema understanding covers the given version's MAJOR. */
export function isSchemaSupported(schemaVersion: string): boolean {
  const major = Number.parseInt(schemaVersion.split('.')[0] ?? '', 10);
  if (Number.isNaN(major)) {
    return false;
  }
  return major <= SUPPORTED_SCHEMA_MAJOR;
}

/** Formats an ISO-8601 duration (e.g. PT1M30S) as a short human string. Best-effort. */
export function formatIso8601Duration(duration: string | undefined): string {
  if (!duration) {
    return 'unknown';
  }
  const match = /^PT(?:(\d+(?:\.\d+)?)H)?(?:(\d+(?:\.\d+)?)M)?(?:(\d+(?:\.\d+)?)S)?$/.exec(
    duration
  );
  if (!match) {
    return duration;
  }
  const [, h, m, s] = match;
  const parts: string[] = [];
  if (h) parts.push(`${h}h`);
  if (m) parts.push(`${m}m`);
  if (s) parts.push(`${s}s`);
  return parts.length > 0 ? parts.join(' ') : '0s';
}
