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
}
