#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Replaces version placeholders in documentation files.

.DESCRIPTION
    This script replaces PACKAGE_VERSION placeholders in markdown and documentation
    files with the actual version number from GitVersion or provided as a parameter.

.PARAMETER Version
    The version string to use for replacement (e.g., "1.2.3")

.PARAMETER Path
    The root path to search for files (defaults to repository root)

.PARAMETER DryRun
    If specified, shows what would be replaced without making changes

.EXAMPLE
    ./replace-version.ps1 -Version "1.2.3"

.EXAMPLE
    ./replace-version.ps1 -Version "1.2.3" -Path "../docs" -DryRun
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$Version,
    
    [Parameter(Mandatory=$false)]
    [string]$Path = ".",
    
    [Parameter(Mandatory=$false)]
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

# Resolve the path to an absolute path for consistent handling
$Path = [System.IO.Path]::GetFullPath($Path)

Write-Host "Version Replacement Script" -ForegroundColor Cyan
Write-Host "=========================" -ForegroundColor Cyan
Write-Host "Version: $Version" -ForegroundColor Green
Write-Host "Path: $Path" -ForegroundColor Green
Write-Host "Dry Run: $DryRun" -ForegroundColor Green
Write-Host ""

# Define the patterns to replace
$patterns = @(
    # SDK version in Sdk attribute
    @{
        Pattern = 'Sdk="JD\.Efcpt\.Sdk/PACKAGE_VERSION"'
        Replacement = "Sdk=`"JD.Efcpt.Sdk/$Version`""
    },
    # PackageReference Version attribute
    @{
        Pattern = 'Version="PACKAGE_VERSION"'
        Replacement = "Version=`"$Version`""
    },
    # Inline text placeholder
    @{
        Pattern = '\bPACKAGE_VERSION\b'
        Replacement = $Version
    }
)

# Find all markdown files
$files = Get-ChildItem -Path $Path -Recurse -Include "*.md" -File | 
    Where-Object { $_.FullName -notmatch "[\\/]\.git[\\/]" -and $_.FullName -notmatch "[\\/]node_modules[\\/]" }

Write-Host "Found $($files.Count) markdown files to process" -ForegroundColor Yellow
Write-Host ""

$totalReplacements = 0

foreach ($file in $files) {
    # Use GetRelativePath for robust path handling
    $relativePath = [System.IO.Path]::GetRelativePath($Path, $file.FullName)
    $content = Get-Content -Path $file.FullName -Raw -ErrorAction Stop
    $fileReplacements = 0
    
    foreach ($patternInfo in $patterns) {
        $matches = [regex]::Matches($content, $patternInfo.Pattern)
        if ($matches.Count -gt 0) {
            $content = [regex]::Replace($content, $patternInfo.Pattern, $patternInfo.Replacement)
            $fileReplacements += $matches.Count
        }
    }
    
    if ($fileReplacements -gt 0) {
        Write-Host "  $relativePath" -ForegroundColor White
        Write-Host "    -> $fileReplacements replacement(s)" -ForegroundColor Gray
        
        if (-not $DryRun) {
            # Preserve the original file's newline behavior
            # Get-Content with -Raw preserves trailing newlines, so we use -NoNewline to avoid adding an extra one
            Set-Content -Path $file.FullName -Value $content -NoNewline -ErrorAction Stop
        }
        
        $totalReplacements += $fileReplacements
    }
}

Write-Host ""
if ($DryRun) {
    Write-Host "Dry run complete. Would have made $totalReplacements replacement(s) across $($files.Count) files." -ForegroundColor Yellow
} else {
    Write-Host "Successfully replaced $totalReplacements version placeholder(s)." -ForegroundColor Green
}

exit 0
