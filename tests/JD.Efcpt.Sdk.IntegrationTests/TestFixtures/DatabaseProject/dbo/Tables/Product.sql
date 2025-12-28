CREATE TABLE [dbo].[Product]
(
    [ProductId] INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    [Name] NVARCHAR(200) NOT NULL,
    [Description] NVARCHAR(MAX) NULL,
    [Price] DECIMAL(18,2) NOT NULL,
    [CategoryId] INT NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [IsActive] BIT NOT NULL DEFAULT 1
)
GO

CREATE INDEX [IX_Product_CategoryId] ON [dbo].[Product] ([CategoryId])
GO
