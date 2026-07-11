/**
 * Discovers workspace .csproj files that reference the JD.Efcpt.Build NuGet
 * package. Pure functions only — no dependency on the `vscode` API — so file
 * discovery (vscode.workspace.findFiles) and file reads are injected by the
 * caller (extension.ts) and this module stays unit-testable in plain mocha.
 */

/** Matches a PackageReference (or PackageVersion) element for JD.Efcpt.Build, self-closing or paired. */
const PACKAGE_REFERENCE_PATTERN =
  /<PackageReference\s+[^>]*Include\s*=\s*"JD\.Efcpt\.Build"[^>]*\/?>/i;

/**
 * True when the given .csproj file content contains a PackageReference to
 * JD.Efcpt.Build (case-insensitive, tolerant of attribute ordering).
 */
export function hasJdEfcptPackageReference(csprojContent: string): boolean {
  return PACKAGE_REFERENCE_PATTERN.test(csprojContent);
}

/**
 * Filters a list of .csproj file paths down to those referencing
 * JD.Efcpt.Build, using the supplied `readFile` function to load content.
 * Files that fail to read (missing, permissions, etc.) are skipped rather
 * than throwing, since workspace discovery can race with file deletion.
 */
export function discoverJdEfcptProjects(
  csprojPaths: readonly string[],
  readFile: (path: string) => string
): string[] {
  const matches: string[] = [];
  for (const path of csprojPaths) {
    let content: string;
    try {
      content = readFile(path);
    } catch {
      continue;
    }
    if (hasJdEfcptPackageReference(content)) {
      matches.push(path);
    }
  }
  return matches;
}
