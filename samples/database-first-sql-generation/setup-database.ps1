#!/usr/bin/env pwsh
# Sets up LocalDB with EfcptSampleDb for the database-first SQL generation sample

$ErrorActionPreference = "Stop"

Write-Host "Setting up LocalDB with EfcptSampleDb..." -ForegroundColor Cyan

# Configuration
$instanceName = "mssqllocaldb"
$databaseName = "EfcptSampleDb"
$scriptDir = $PSScriptRoot

# Step 1: Check LocalDB installation
Write-Host "`n[1/5] Checking LocalDB installation..." -ForegroundColor Yellow
try {
    $null = sqllocaldb info 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Error "LocalDB is not installed. Please install SQL Server LocalDB"
        exit 1
    }
    Write-Host "  [OK] LocalDB is installed" -ForegroundColor Green
}
catch {
    Write-Error "Failed to check LocalDB installation: $_"
    exit 1
}

# Step 2: Create or start LocalDB instance
Write-Host "`n[2/5] Setting up LocalDB instance '$instanceName'..." -ForegroundColor Yellow
$instances = sqllocaldb info
if ($instances -contains $instanceName) {
    Write-Host "  [OK] Instance '$instanceName' exists" -ForegroundColor Green
    $state = sqllocaldb info $instanceName | Select-String "State:"
    if ($state -match "Stopped") {
        sqllocaldb start $instanceName | Out-Null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  [OK] Instance started" -ForegroundColor Green
        }
    }
    else {
        Write-Host "  [OK] Instance is running" -ForegroundColor Green
    }
}
else {
    sqllocaldb create $instanceName | Out-Null
    sqllocaldb start $instanceName | Out-Null
    Write-Host "  [OK] Instance created and started" -ForegroundColor Green
}

# Step 3: Create database
Write-Host "`n[3/5] Creating database '$databaseName'..." -ForegroundColor Yellow
$createDbQuery = "IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'$databaseName') CREATE DATABASE [$databaseName]"
sqlcmd -S "(localdb)\$instanceName" -Q $createDbQuery -b | Out-Null
if ($LASTEXITCODE -eq 0) {
    Write-Host "  [OK] Database ready" -ForegroundColor Green
}
else {
    Write-Error "Failed to create database"
    exit 1
}

# Step 4: Create schema
Write-Host "`n[4/5] Creating sample schema..." -ForegroundColor Yellow
$schemaFile = Join-Path $scriptDir "schema.sql"
sqlcmd -S "(localdb)\$instanceName" -d $databaseName -i $schemaFile -b | Out-Null
if ($LASTEXITCODE -eq 0) {
    Write-Host "  [OK] Tables created" -ForegroundColor Green
}
else {
    Write-Error "Failed to create schema"
    exit 1
}

# Step 5: Insert sample data
Write-Host "`n[5/5] Inserting sample data..." -ForegroundColor Yellow
$dataFile = Join-Path $scriptDir "data.sql"
sqlcmd -S "(localdb)\$instanceName" -d $databaseName -i $dataFile -b | Out-Null
if ($LASTEXITCODE -eq 0) {
    Write-Host "  [OK] Sample data inserted" -ForegroundColor Green
}
else {
    Write-Error "Failed to insert sample data"
    exit 1
}

# Summary
Write-Host "`n=====================================================================" -ForegroundColor Cyan
Write-Host "[OK] Database setup complete!" -ForegroundColor Green
Write-Host "=====================================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Database Details:" -ForegroundColor White
Write-Host "  Server:   (localdb)\$instanceName" -ForegroundColor Gray
Write-Host "  Database: $databaseName" -ForegroundColor Gray
Write-Host ""
Write-Host "Connection String:" -ForegroundColor White
Write-Host "  Server=(localdb)\$instanceName;Database=$databaseName;Trusted_Connection=True" -ForegroundColor Gray
Write-Host ""
Write-Host "Tables Created:" -ForegroundColor White
Write-Host "  - Categories (4 rows)" -ForegroundColor Gray
Write-Host "  - Products (8 rows)" -ForegroundColor Gray
Write-Host "  - Customers (4 rows)" -ForegroundColor Gray
Write-Host "  - Orders (4 rows)" -ForegroundColor Gray
Write-Host "  - OrderItems (9 rows)" -ForegroundColor Gray
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor White
Write-Host "  1. Build the DatabaseProject:" -ForegroundColor Gray
Write-Host "     dotnet build DatabaseProject" -ForegroundColor Cyan
Write-Host ""
Write-Host "  2. Build the DataAccessProject:" -ForegroundColor Gray
Write-Host "     dotnet build DataAccessProject" -ForegroundColor Cyan
Write-Host ""
