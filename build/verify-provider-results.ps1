param([Parameter(Mandatory)][string]$Path)
$ErrorActionPreference = 'Stop'
[xml]$results = Get-Content -LiteralPath $Path -Raw
$tests = @($results.TestRun.Results.UnitTestResult)
foreach ($provider in @('sql_server', 'postgres', 'mysql', 'oracle', 'snowflake', 'firebird', 'sqlite')) {
    foreach ($operation in @('connection', 'schema_reader')) {
        $name = "JD.Efcpt.Build.Tests.Schema.DatabaseProviderFactoryTests.Creates_${provider}_${operation}"
        $matches = @($tests | Where-Object { $_.testName -eq $name })
        if ($matches.Count -ne 1 -or $matches[0].outcome -ne 'Passed') {
            throw "Missing or unsuccessful provider coverage: $name"
        }
    }
    Write-Output "PASS: $provider connection and schema reader"
}
