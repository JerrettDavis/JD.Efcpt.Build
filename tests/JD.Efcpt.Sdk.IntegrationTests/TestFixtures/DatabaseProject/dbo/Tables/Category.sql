CREATE TABLE [dbo].[Category]
(
    [CategoryId] INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    [Name] NVARCHAR(100) NOT NULL,
    [ParentCategoryId] INT NULL,
    CONSTRAINT [FK_Category_ParentCategory] FOREIGN KEY ([ParentCategoryId]) REFERENCES [dbo].[Category]([CategoryId])
)
GO
