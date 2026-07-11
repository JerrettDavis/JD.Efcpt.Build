/**
 * Pure secret-redaction for build output before it is pinned in the VS Code
 * Output Channel, echoed to the task terminal, or embedded in diagnostics.
 *
 * `dotnet build` output can contain a connection string (e.g. an efcpt CLI
 * invocation echoed by the pipeline). We mask the sensitive value of ADO.NET /
 * ODBC style key-value pairs (Password, Pwd, User ID, Uid) with `***`, mirroring
 * the server-side JD.Efcpt.Build.Core SecretRedaction helper. No dependency on
 * the `vscode` API, so this is unit-testable in plain Node/mocha.
 */

// Captures the key (incl. '=') so only the value up to the next ';', quote, or
// end-of-line is masked, leaving non-sensitive keys (Server, Database, ...) visible.
const SENSITIVE_KEY_VALUE = /(\b(?:password|pwd|user\s*id|uid)\s*=)([^;"'\r\n]*)/gi;

const MASK = '***';

/** Masks sensitive key-value pairs within an arbitrary string of build output. */
export function redactSecrets(text: string): string {
  if (!text) {
    return text;
  }
  return text.replace(SENSITIVE_KEY_VALUE, (_match, key: string) => `${key}${MASK}`);
}
