/**
 * Pure parser for JD.Efcpt.Build task-level MSBuild diagnostic lines, e.g.:
 *   warning JD0002: Connection string 'MyDatabase' not found in appsettings.json
 *   error JD0011: Failed to parse configuration file 'appsettings.json'
 *
 * Codes are documented in docs/user-guide/error-codes.md. This module has no
 * dependency on the `vscode` API so it can be unit tested in plain Node/mocha;
 * mapping into a vscode.DiagnosticCollection happens in extension.ts/regenerate.ts.
 */

export type JdDiagnosticSeverity = 'warning' | 'error';

export interface JdDiagnostic {
  severity: JdDiagnosticSeverity;
  code: string;
  message: string;
}

/** Matches `warning JD0002: message` / `error JD0011: message` anywhere in a line. */
export const JD_DIAGNOSTIC_PATTERN = /\b(warning|error)\s+(JD\d{4}):\s*(.+)/;

/** Parses a single line of build output. Returns null when the line doesn't match. */
export function parseJdDiagnosticLine(line: string): JdDiagnostic | null {
  const match = JD_DIAGNOSTIC_PATTERN.exec(line);
  if (!match) {
    return null;
  }
  const [, severity, code, message] = match;
  return {
    severity: severity as JdDiagnosticSeverity,
    code,
    message: message.trim(),
  };
}

/** Parses multi-line build output, returning every JDxxxx diagnostic found, in order. */
export function parseJdDiagnostics(output: string): JdDiagnostic[] {
  const results: JdDiagnostic[] = [];
  for (const line of output.split(/\r?\n/)) {
    const diagnostic = parseJdDiagnosticLine(line);
    if (diagnostic) {
      results.push(diagnostic);
    }
  }
  return results;
}
