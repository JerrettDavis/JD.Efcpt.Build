#!/usr/bin/env dotnet-script
/*
 * EFCPT Sample Build Script
 * 
 * This script rebuilds the JD.Efcpt.Build package and the sample project.
 * 
 * Usage:
 *   dotnet script build.csx
 *   OR
 *   .\build.csx (if dotnet-script is installed globally)
 */

using System;
using System.Diagnostics;
using System.IO;

var rootDir = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "..", ".."));
var artifactsDir = Path.Combine(rootDir, "artifacts");
var sampleDir = Path.Combine(rootDir, "samples", "split-data-and-models-between-multiple-projects");
var tasksProject = Path.Combine(rootDir, "src", "JD.Efcpt.Build.Tasks", "JD.Efcpt.Build.Tasks.csproj");
var buildProject = Path.Combine(rootDir, "src", "JD.Efcpt.Build", "JD.Efcpt.Build.csproj");
var nugetCachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages", "jd.efcpt.build");

Console.WriteLine("=== EFCPT Sample Build Script ===");
Console.WriteLine($"Root: {rootDir}");
Console.WriteLine();

// Step 1: Clean NuGet cache
Console.WriteLine("Step 1: Cleaning NuGet cache...");
if (Directory.Exists(nugetCachePath))
{
    try
    {
        Directory.Delete(nugetCachePath, true);
        Console.WriteLine($"  ✓ Removed: {nugetCachePath}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  ⚠ Warning: Could not remove cache: {ex.Message}");
    }
}
else
{
    Console.WriteLine("  ✓ Cache already clean");
}
Console.WriteLine();

// Step 2: Build JD.Efcpt.Build.Tasks
Console.WriteLine("Step 2: Building JD.Efcpt.Build.Tasks...");
RunCommand("dotnet", $"build \"{tasksProject}\" -c Release --no-incremental", rootDir);
Console.WriteLine();

// Step 3: Build JD.Efcpt.Build
Console.WriteLine("Step 3: Building JD.Efcpt.Build...");
RunCommand("dotnet", $"build \"{buildProject}\" -c Release --no-incremental", rootDir);
Console.WriteLine();

// Step 4: Pack JD.Efcpt.Build
Console.WriteLine("Step 4: Packing JD.Efcpt.Build NuGet package...");
Directory.CreateDirectory(artifactsDir);
RunCommand("dotnet", $"pack \"{buildProject}\" -c Release --no-build --output \"{artifactsDir}\"", rootDir);
Console.WriteLine();

// Step 5: Clean sample output
Console.WriteLine("Step 5: Cleaning sample output...");
var sampleEfcptDir = Path.Combine(sampleDir, "EntityFrameworkCoreProject", "obj", "efcpt");
if (Directory.Exists(sampleEfcptDir))
{
    Directory.Delete(sampleEfcptDir, true);
    Console.WriteLine($"  ✓ Removed: {sampleEfcptDir}");
}
RunCommand("dotnet", "clean", sampleDir);
Console.WriteLine();

// Step 6: Restore sample
Console.WriteLine("Step 6: Restoring sample dependencies...");
RunCommand("dotnet", "restore --force", sampleDir);
Console.WriteLine();

// Step 7: Build sample
Console.WriteLine("Step 7: Building sample...");
RunCommand("dotnet", "build -v n", sampleDir);
Console.WriteLine();

Console.WriteLine("=== Build Complete ===");

void RunCommand(string command, string args, string workingDir)
{
    var psi = new ProcessStartInfo
    {
        FileName = command,
        Arguments = args,
        WorkingDirectory = workingDir,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false
    };

    Console.WriteLine($"  > {command} {args}");
    
    using var process = Process.Start(psi);
    if (process == null)
    {
        throw new InvalidOperationException($"Failed to start: {command}");
    }

    var stdout = process.StandardOutput.ReadToEnd();
    var stderr = process.StandardError.ReadToEnd();
    
    process.WaitForExit();

    if (!string.IsNullOrWhiteSpace(stdout))
    {
        Console.WriteLine(stdout);
    }
    
    if (!string.IsNullOrWhiteSpace(stderr))
    {
        Console.Error.WriteLine(stderr);
    }

    if (process.ExitCode != 0)
    {
        Console.WriteLine($"  ✗ Command failed with exit code {process.ExitCode}");
        Environment.Exit(process.ExitCode);
    }
    
    Console.WriteLine($"  ✓ Success");
}

