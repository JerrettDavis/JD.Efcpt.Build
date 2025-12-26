# Setup script for SQLite sample database
# This script creates a sample SQLite database with tables for demonstration

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$databaseDir = Join-Path $scriptDir "Database"
$dbPath = Join-Path $databaseDir "sample.db"

# Create Database directory if it doesn't exist
if (-not (Test-Path $databaseDir)) {
    New-Item -ItemType Directory -Path $databaseDir | Out-Null
    Write-Host "Created Database directory"
}

# Remove existing database if present
if (Test-Path $dbPath) {
    Remove-Item $dbPath -Force
    Write-Host "Removed existing database"
}

# Create the database using dotnet and inline C# (requires .NET SDK)
$csharpCode = @"
using Microsoft.Data.Sqlite;

var connectionString = @"Data Source=$($dbPath.Replace('\', '\\'))";
using var connection = new SqliteConnection(connectionString);
connection.Open();

using var command = connection.CreateCommand();
command.CommandText = @"
-- Categories table
CREATE TABLE categories (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    description TEXT,
    created_at TEXT DEFAULT CURRENT_TIMESTAMP
);

-- Products table
CREATE TABLE products (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    category_id INTEGER NOT NULL,
    name TEXT NOT NULL,
    description TEXT,
    price REAL NOT NULL,
    stock_quantity INTEGER DEFAULT 0,
    is_active INTEGER DEFAULT 1,
    created_at TEXT DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (category_id) REFERENCES categories(id)
);

-- Orders table
CREATE TABLE orders (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    customer_name TEXT NOT NULL,
    customer_email TEXT NOT NULL,
    order_date TEXT DEFAULT CURRENT_TIMESTAMP,
    status TEXT DEFAULT 'pending',
    total_amount REAL DEFAULT 0
);

-- Order items table
CREATE TABLE order_items (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    order_id INTEGER NOT NULL,
    product_id INTEGER NOT NULL,
    quantity INTEGER NOT NULL,
    unit_price REAL NOT NULL,
    FOREIGN KEY (order_id) REFERENCES orders(id),
    FOREIGN KEY (product_id) REFERENCES products(id)
);

-- Create indexes
CREATE INDEX idx_products_category ON products(category_id);
CREATE INDEX idx_products_active ON products(is_active);
CREATE INDEX idx_order_items_order ON order_items(order_id);
CREATE INDEX idx_order_items_product ON order_items(product_id);
CREATE INDEX idx_orders_customer_email ON orders(customer_email);
CREATE INDEX idx_orders_status ON orders(status);

-- Insert sample data
INSERT INTO categories (name, description) VALUES
    ('Electronics', 'Electronic devices and accessories'),
    ('Books', 'Physical and digital books'),
    ('Clothing', 'Apparel and fashion items');

INSERT INTO products (category_id, name, description, price, stock_quantity) VALUES
    (1, 'Laptop', 'High-performance laptop', 999.99, 50),
    (1, 'Wireless Mouse', 'Ergonomic wireless mouse', 29.99, 200),
    (2, 'Programming Guide', 'Complete guide to programming', 49.99, 100),
    (3, 'T-Shirt', 'Cotton t-shirt', 19.99, 500);
";
command.ExecuteNonQuery();

Console.WriteLine("Database created successfully!");
"@

# Create a temporary project to run the database creation
$tempDir = Join-Path $env:TEMP "sqlite-setup-$(Get-Random)"
New-Item -ItemType Directory -Path $tempDir | Out-Null

try {
    # Create a minimal console project
    Push-Location $tempDir

    $csprojContent = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Data.Sqlite" Version="9.0.1" />
  </ItemGroup>
</Project>
"@

    Set-Content -Path "Setup.csproj" -Value $csprojContent
    Set-Content -Path "Program.cs" -Value $csharpCode

    Write-Host "Creating SQLite database..."
    dotnet run --verbosity quiet

    if ($LASTEXITCODE -ne 0) {
        throw "Failed to create database"
    }

    Write-Host "Database created at: $dbPath" -ForegroundColor Green
}
finally {
    Pop-Location
    Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
}
