namespace JD.Efcpt.Build.Tests.Infrastructure;

internal static class TestScripts
{
    public static string CreateFakeMsBuild(TestFolder folder, string dacpacPath)
    {
        if (OperatingSystem.IsWindows())
        {
            var script = folder.WriteFile("build.cmd",
                $"""
                @echo off
                setlocal
                set "DEST={dacpacPath}"
                for %%A in ("{dacpacPath}") do set "DESTDIR=%%~dpA"
                if not exist "%DESTDIR%" mkdir "%DESTDIR%"
                echo rebuilt>"%DEST%"
                exit /b 0
                """);
            return script;
        }

        var sh = folder.WriteFile("build.sh",
            $"""
            #!/usr/bin/env bash
            mkdir -p "$(dirname "{dacpacPath}")"
            echo "rebuilt" > "{dacpacPath}"
            exit 0
            """);
        TestFileSystem.MakeExecutable(sh);
        return sh;
    }

    public static string CreateFakeEfcpt(TestFolder folder)
    {
        if (OperatingSystem.IsWindows())
        {
            var cmd = folder.WriteFile("fake-efcpt.cmd",
                """
                @echo off
                setlocal
                set "OUT="
                set "DAC="
                :parse
                if "%~1"=="" goto done
                if "%~1"=="--output" (
                  set "OUT=%~2"
                  shift
                ) else if "%~1"=="--dacpac" (
                  set "DAC=%~2"
                  shift
                )
                shift
                goto parse
                :done
                if "%OUT%"=="" exit /b 1
                if not exist "%OUT%" mkdir "%OUT%"
                echo // generated from %DAC%>"%OUT%\\SampleModel.cs"
                exit /b 0
                """);
            return cmd;
        }

        var sh = folder.WriteFile("fake-efcpt.sh",
            """
            #!/usr/bin/env bash
            while [[ $# -gt 0 ]]; do
              case "$1" in
                --output) OUT="$2"; shift 2;;
                --dacpac) DAC="$2"; shift 2;;
                *) shift;;
              esac
            done
            mkdir -p "$OUT"
            echo "// generated from $DAC" > "$OUT/SampleModel.cs"
            """);
        TestFileSystem.MakeExecutable(sh);
        return sh;
    }

    /// <summary>
    /// Writes a trivial, real, always-succeeding script standing in for a tool executable that
    /// exits 0 and does nothing else. On Windows this is a <c>.cmd</c> batch file (invoked via
    /// <c>cmd.exe /c</c> by <c>CommandNormalizationStrategy</c>); on Linux/macOS it is a shell
    /// script with a shebang and the executable bit set, launched directly by
    /// <see cref="System.Diagnostics.Process"/> without any wrapper.
    /// </summary>
    /// <param name="folder">The test folder to write the script into (under its "tools" dir).</param>
    /// <param name="baseName">The script's base name, without extension.</param>
    /// <returns>The full path to the created script.</returns>
    public static string CreateAlwaysSucceedsScript(TestFolder folder, string baseName)
    {
        var toolDir = folder.CreateDir("tools");

        if (OperatingSystem.IsWindows())
        {
            var path = Path.Combine(toolDir, $"{baseName}.cmd");
            File.WriteAllText(path, "@echo off\r\nexit /b 0\r\n");
            return path;
        }

        var shPath = Path.Combine(toolDir, $"{baseName}.sh");
        File.WriteAllText(shPath, "#!/bin/sh\nexit 0\n");
        TestFileSystem.MakeExecutable(shPath);
        return shPath;
    }

    /// <summary>
    /// Writes a trivial, real, always-succeeding script standing in for <c>dotnet</c> or a global
    /// tool executable that appends its own invocation (prefixed with <paramref name="label"/>,
    /// followed by its arguments) to <paramref name="captureFile"/> and exits 0. Standing in for a
    /// real executable lets tests assert exactly which commands were (or were not) invoked,
    /// without depending on a real dotnet tool installation being present on the test machine.
    /// On Windows this is a <c>.cmd</c> batch file (invoked via <c>cmd.exe /c</c> by
    /// <c>CommandNormalizationStrategy</c>); on Linux/macOS it is a shell script with a shebang
    /// and the executable bit set, launched directly by <see cref="System.Diagnostics.Process"/>
    /// without any wrapper.
    /// </summary>
    /// <param name="folder">The test folder to write the script into (under its "tools" dir).</param>
    /// <param name="baseName">The script's base name, without extension.</param>
    /// <param name="label">The label prefixed to each captured invocation line.</param>
    /// <param name="captureFile">The file the script appends its invocation line to.</param>
    /// <returns>The full path to the created script.</returns>
    public static string CreateCaptureScript(TestFolder folder, string baseName, string label, string captureFile)
    {
        var toolDir = folder.CreateDir("tools");

        if (OperatingSystem.IsWindows())
        {
            var path = Path.Combine(toolDir, $"{baseName}.cmd");
            File.WriteAllText(path, $"@echo off\r\necho {label} %* >> \"{captureFile}\"\r\nexit /b 0\r\n");
            return path;
        }

        var shPath = Path.Combine(toolDir, $"{baseName}.sh");
        File.WriteAllText(shPath, $"#!/bin/sh\necho {label} \"$@\" >> \"{captureFile}\"\nexit 0\n");
        TestFileSystem.MakeExecutable(shPath);
        return shPath;
    }
}
