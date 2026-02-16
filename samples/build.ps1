#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Build script for samples solution with proper dependency management
.DESCRIPTION
    This script ensures the main JD.Efcpt.Build packages are built first,
    then builds the samples solution.
.PARAMETER Configuration
    Build configuration (Debug or Release). Default is Debug.
.PARAMETER SkipMainBuild
    Skip building the main solution packages
#>

param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    [switch]$SkipMainBuild
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$samplesRoot = $PSScriptRoot
$repoRoot = Split-Path $samplesRoot -Parent
$mainSolution = Join-Path $repoRoot "JD.Efcpt.Build.sln"
$packagesDir = Join-Path $repoRoot "packages"
$samplesSolution = Join-Path $samplesRoot "Samples.sln"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "JD.Efcpt.Build Samples Build Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Build main solution packages
if (-not $SkipMainBuild) {
    Write-Host "Step 1: Building main solution..." -ForegroundColor Yellow
    Push-Location $repoRoot
    try {
        dotnet build $mainSolution --configuration $Configuration
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to build main solution"
        }
        Write-Host "✓ Main solution built successfully" -ForegroundColor Green
        Write-Host ""
        
        Write-Host "Step 2: Packing main solution..." -ForegroundColor Yellow
        dotnet pack $mainSolution --configuration $Configuration --output $packagesDir --no-build
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to pack main solution"
        }
        Write-Host "✓ Main solution packed successfully" -ForegroundColor Green
        Write-Host ""
        
        Write-Host "Step 3: Fixing sample project issues..." -ForegroundColor Yellow
        & "$samplesRoot\fix-samples.ps1" -Quiet
        Write-Host "  ✓ Sample fixes applied" -ForegroundColor Green
        Write-Host ""
    }
    finally {
        Pop-Location
    }
}
else {
    Write-Host "Skipping main solution build" -ForegroundColor Gray
    Write-Host ""
}

# Step 2: Restore and build samples solution
Write-Host "Step 4: Restoring sample solution..." -ForegroundColor Yellow
Write-Host "Note: Using NuGet.config for package sources (local packages + nuget.org)" -ForegroundColor Gray
dotnet restore $samplesSolution
$restoreExitCode = $LASTEXITCODE

if ($restoreExitCode -ne 0) {
    Write-Host "⚠ Restore completed with errors" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Known issues:" -ForegroundColor Yellow
    Write-Host "  - sdk-zero-config: Requires SDK version in global.json" -ForegroundColor Gray
    Write-Host "  - Some samples: Target framework may not match EF Core package version" -ForegroundColor Gray
    Write-Host ""
}
else {
    Write-Host "✓ Sample solution restored" -ForegroundColor Green
    Write-Host ""
}

Write-Host "Step 5: Building sample solution..." -ForegroundColor Yellow
dotnet build $samplesSolution --configuration $Configuration --no-restore
$buildExitCode = $LASTEXITCODE

if ($buildExitCode -ne 0) {
    Write-Host "⚠ Build completed with errors" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Some samples may have pre-existing issues." -ForegroundColor Gray
    Write-Host "Check individual sample README files for requirements." -ForegroundColor Gray
    Write-Host ""
    exit 1
}
else {
    Write-Host "✓ Sample solution built successfully" -ForegroundColor Green
    Write-Host ""
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Build completed! 🎉" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
