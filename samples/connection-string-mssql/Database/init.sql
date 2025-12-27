-- Sample Northwind-style database schema for demonstration
-- This script initializes the database with tables for code generation

USE [Northwind];
GO

-- Categories table
CREATE TABLE [dbo].[Categories] (
    [CategoryId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [CategoryName] NVARCHAR(100) NOT NULL,
    [Description] NVARCHAR(MAX) NULL
);
GO

-- Suppliers table
CREATE TABLE [dbo].[Suppliers] (
    [SupplierId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [CompanyName] NVARCHAR(100) NOT NULL,
    [ContactName] NVARCHAR(100) NULL,
    [ContactTitle] NVARCHAR(50) NULL,
    [Address] NVARCHAR(200) NULL,
    [City] NVARCHAR(50) NULL,
    [Region] NVARCHAR(50) NULL,
    [PostalCode] NVARCHAR(20) NULL,
    [Country] NVARCHAR(50) NULL,
    [Phone] NVARCHAR(30) NULL
);
GO

-- Products table
CREATE TABLE [dbo].[Products] (
    [ProductId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [ProductName] NVARCHAR(100) NOT NULL,
    [SupplierId] INT NULL,
    [CategoryId] INT NULL,
    [QuantityPerUnit] NVARCHAR(50) NULL,
    [UnitPrice] DECIMAL(18,2) NULL,
    [UnitsInStock] SMALLINT NULL,
    [UnitsOnOrder] SMALLINT NULL,
    [ReorderLevel] SMALLINT NULL,
    [Discontinued] BIT NOT NULL DEFAULT 0,
    CONSTRAINT [FK_Products_Categories] FOREIGN KEY ([CategoryId]) REFERENCES [dbo].[Categories]([CategoryId]),
    CONSTRAINT [FK_Products_Suppliers] FOREIGN KEY ([SupplierId]) REFERENCES [dbo].[Suppliers]([SupplierId])
);
GO

-- Customers table
CREATE TABLE [dbo].[Customers] (
    [CustomerId] NCHAR(5) NOT NULL PRIMARY KEY,
    [CompanyName] NVARCHAR(100) NOT NULL,
    [ContactName] NVARCHAR(100) NULL,
    [ContactTitle] NVARCHAR(50) NULL,
    [Address] NVARCHAR(200) NULL,
    [City] NVARCHAR(50) NULL,
    [Region] NVARCHAR(50) NULL,
    [PostalCode] NVARCHAR(20) NULL,
    [Country] NVARCHAR(50) NULL,
    [Phone] NVARCHAR(30) NULL
);
GO

-- Orders table
CREATE TABLE [dbo].[Orders] (
    [OrderId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [CustomerId] NCHAR(5) NULL,
    [OrderDate] DATETIME NULL,
    [RequiredDate] DATETIME NULL,
    [ShippedDate] DATETIME NULL,
    [ShipAddress] NVARCHAR(200) NULL,
    [ShipCity] NVARCHAR(50) NULL,
    [ShipRegion] NVARCHAR(50) NULL,
    [ShipPostalCode] NVARCHAR(20) NULL,
    [ShipCountry] NVARCHAR(50) NULL,
    CONSTRAINT [FK_Orders_Customers] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers]([CustomerId])
);
GO

-- Order Details table
CREATE TABLE [dbo].[OrderDetails] (
    [OrderId] INT NOT NULL,
    [ProductId] INT NOT NULL,
    [UnitPrice] DECIMAL(18,2) NOT NULL,
    [Quantity] SMALLINT NOT NULL,
    [Discount] REAL NOT NULL DEFAULT 0,
    PRIMARY KEY ([OrderId], [ProductId]),
    CONSTRAINT [FK_OrderDetails_Orders] FOREIGN KEY ([OrderId]) REFERENCES [dbo].[Orders]([OrderId]),
    CONSTRAINT [FK_OrderDetails_Products] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products]([ProductId])
);
GO

-- Insert sample data
INSERT INTO [dbo].[Categories] ([CategoryName], [Description]) VALUES
    ('Beverages', 'Soft drinks, coffees, teas, beers, and ales'),
    ('Condiments', 'Sweet and savory sauces, relishes, spreads, and seasonings'),
    ('Confections', 'Desserts, candies, and sweet breads');
GO

INSERT INTO [dbo].[Suppliers] ([CompanyName], [ContactName], [City], [Country]) VALUES
    ('Exotic Liquids', 'Charlotte Cooper', 'London', 'UK'),
    ('New Orleans Cajun Delights', 'Shelley Burke', 'New Orleans', 'USA');
GO

INSERT INTO [dbo].[Customers] ([CustomerId], [CompanyName], [ContactName], [City], [Country]) VALUES
    ('ALFKI', 'Alfreds Futterkiste', 'Maria Anders', 'Berlin', 'Germany'),
    ('ANATR', 'Ana Trujillo Emparedados', 'Ana Trujillo', 'Mexico City', 'Mexico');
GO

PRINT 'Northwind sample database initialized successfully.';
GO
