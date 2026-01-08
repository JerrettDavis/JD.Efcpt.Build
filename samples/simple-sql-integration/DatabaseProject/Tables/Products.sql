CREATE TABLE [dbo].[Products]
(
    [ProductId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [ProductName] NVARCHAR(200) NOT NULL,
    [CategoryId] INT NULL,
    [UnitPrice] DECIMAL(18,2) NOT NULL DEFAULT 0,
    [UnitsInStock] INT NOT NULL DEFAULT 0,
    [Discontinued] BIT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [FK_Products_Categories] FOREIGN KEY ([CategoryId]) REFERENCES [dbo].[Categories]([CategoryId])
);
