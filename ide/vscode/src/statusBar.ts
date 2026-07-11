import * as vscode from 'vscode';
import { ParsedBuildProfile, formatIso8601Duration } from './buildProfile';

/**
 * Status bar indicator for JD.Efcpt.Build. Shows a spinner while a
 * regeneration build runs, then a check/error glyph with the resulting
 * model count once the build-profile.json for the run is available.
 */
export class JdEfcptStatusBar implements vscode.Disposable {
  private readonly item: vscode.StatusBarItem;
  private lastRunAt: Date | undefined;

  constructor() {
    this.item = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Left, 100);
    this.item.command = 'jdEfcpt.showBuildStatus';
    this.item.name = 'JD.Efcpt.Build';
    this.setIdle();
    this.item.show();
  }

  setIdle(): void {
    this.item.text = '$(sync) EF: idle';
    this.item.tooltip = 'JD.Efcpt.Build — click to show status';
  }

  setBuilding(): void {
    this.item.text = '$(sync~spin) EF: building…';
    this.item.tooltip = 'JD.Efcpt.Build regeneration in progress…';
  }

  setSuccess(profile: ParsedBuildProfile): void {
    this.lastRunAt = new Date();
    const count = profile.modelCount;
    this.item.text = `$(check) EF: ${count} model${count === 1 ? '' : 's'}`;
    this.item.tooltip = this.buildTooltip(profile);
  }

  setFailed(profile?: ParsedBuildProfile): void {
    this.lastRunAt = new Date();
    this.item.text = '$(error) EF: failed';
    this.item.tooltip = profile
      ? this.buildTooltip(profile)
      : 'JD.Efcpt.Build regeneration failed. Click for details.';
  }

  private buildTooltip(profile: ParsedBuildProfile): string {
    const lines = [
      `Status: ${profile.status}`,
      `Last run: ${this.lastRunAt ? this.lastRunAt.toLocaleString() : 'unknown'}`,
      `Duration: ${formatIso8601Duration(profile.duration)}`,
    ];
    if (profile.diagnostics.length > 0) {
      lines.push(`Diagnostics: ${profile.diagnostics.length}`);
    }
    if (!profile.schemaSupported) {
      lines.push(`Warning: unsupported schemaVersion ${profile.schemaVersion}`);
    }
    return lines.join('\n');
  }

  dispose(): void {
    this.item.dispose();
  }

  /** Test-only accessor for the current status bar text. */
  getText(): string {
    return this.item.text;
  }
}
