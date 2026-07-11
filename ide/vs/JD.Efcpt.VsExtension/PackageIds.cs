namespace JD.Efcpt.VsExtension;

/// <summary>
/// Numeric command/menu IDs. Values match the <c>IDSymbol</c> entries under
/// <c>guidJdEfcptCommandSet</c> in JdEfcptCommandTable.vsct.
/// </summary>
internal static class PackageIds
{
    /// <summary>The "Entity Framework" submenu placed under the top-level Tools menu.</summary>
    public const int JdEfcptSubMenu = 0x1020;

    /// <summary>The single group inside <see cref="JdEfcptSubMenu"/> that holds this extension's buttons.</summary>
    public const int JdEfcptSubMenuGroup = 0x1021;

    /// <summary>"JD.Efcpt: Regenerate Models" command.</summary>
    public const int RegenerateModelsCommandId = 0x0100;

    /// <summary>"JD.Efcpt: Show Build Status" command.</summary>
    public const int ShowBuildStatusCommandId = 0x0101;
}
