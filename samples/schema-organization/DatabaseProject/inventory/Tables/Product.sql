-- Inventory-related entity
CREATE TABLE [inventory].[Product]
(
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Name] NVARCHAR(100) NOT NULL,
    [Sku] NVARCHAR(50) NOT NULL,
    [Price] DECIMAL(18, 2) NOT NULL,
    [StockQuantity] INT NOT NULL DEFAULT 0
);
