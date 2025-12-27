-- Inventory-related entity
CREATE TABLE [inventory].[Warehouse]
(
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Name] NVARCHAR(100) NOT NULL,
    [Location] NVARCHAR(200) NOT NULL,
    [Capacity] INT NOT NULL
);
