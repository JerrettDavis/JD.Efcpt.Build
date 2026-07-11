import * as vscode from 'vscode';
import { ParsedBuildProfile } from './buildProfile';

class StatusNode extends vscode.TreeItem {
  constructor(label: string, description?: string, tooltip?: string) {
    super(label, vscode.TreeItemCollapsibleState.None);
    this.description = description;
    this.tooltip = tooltip ?? description;
  }
}

/**
 * TreeDataProvider backing the "Build Status" activity-bar view. Populated
 * from the most recently parsed obj/efcpt/build-profile.json (model count,
 * status, endTime, duration, diagnostics).
 */
export class JdEfcptStatusViewProvider implements vscode.TreeDataProvider<StatusNode> {
  private readonly _onDidChangeTreeData = new vscode.EventEmitter<void>();
  readonly onDidChangeTreeData = this._onDidChangeTreeData.event;

  private profile: ParsedBuildProfile | undefined;

  update(profile: ParsedBuildProfile | undefined): void {
    this.profile = profile;
    this._onDidChangeTreeData.fire();
  }

  getTreeItem(element: StatusNode): vscode.TreeItem {
    return element;
  }

  getChildren(): StatusNode[] {
    if (!this.profile) {
      return [
        new StatusNode(
          'No build profile yet',
          undefined,
          'Run "JD.Efcpt: Regenerate Models" to populate this view.'
        ),
      ];
    }

    const nodes: StatusNode[] = [
      new StatusNode('Status', this.profile.status),
      new StatusNode('Models generated', String(this.profile.modelCount)),
      new StatusNode('Last run', this.profile.endTime ?? 'unknown'),
      new StatusNode('Duration', this.profile.duration ?? 'unknown'),
    ];

    if (!this.profile.schemaSupported) {
      nodes.push(
        new StatusNode('Schema warning', `Unsupported schemaVersion ${this.profile.schemaVersion}`)
      );
    }

    if (this.profile.diagnostics.length === 0) {
      nodes.push(new StatusNode('Diagnostics', 'none'));
    } else {
      for (const diag of this.profile.diagnostics) {
        const label = diag.code
          ? `${diag.severity ?? 'diagnostic'} ${diag.code}`
          : diag.severity ?? 'diagnostic';
        nodes.push(new StatusNode(label, diag.message ?? ''));
      }
    }

    return nodes;
  }
}
