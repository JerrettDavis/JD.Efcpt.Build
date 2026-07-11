using System;

namespace JD.Efcpt.VsExtension;

/// <summary>
/// GUIDs identifying this extension's package and command set. These are intentionally distinct
/// from any GUIDs used by ErikEJ.EFCorePowerTools.Cli / EF Core Power Tools (or any other
/// extension), so JD.Efcpt.Build's menus and tool window cannot collide with theirs.
/// </summary>
internal static class PackageGuids
{
    /// <summary>String form of the <see cref="JdEfcptPackage"/> GUID, for use in attributes.</summary>
    public const string JdEfcptPackageString = "bd8d1ff2-daba-4b79-bac9-e0f905e352d8";

    /// <summary>String form of the <see cref="JdEfcptCommandSet"/> GUID, for use in attributes.</summary>
    public const string JdEfcptCommandSetString = "90ca1843-99b1-4955-b9e7-b59b12e40dd6";

    /// <summary>The <c>JD.Efcpt.VsExtension</c> package GUID. Matches <c>guidJdEfcptPackage</c> in JdEfcptCommandTable.vsct.</summary>
    public static readonly Guid JdEfcptPackage = new(JdEfcptPackageString);

    /// <summary>This extension's own command set GUID. Matches <c>guidJdEfcptCommandSet</c> in JdEfcptCommandTable.vsct.</summary>
    public static readonly Guid JdEfcptCommandSet = new(JdEfcptCommandSetString);
}
