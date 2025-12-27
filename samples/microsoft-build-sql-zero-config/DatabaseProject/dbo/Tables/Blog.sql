CREATE TABLE [dbo].[Blog]
(
    [BlogId] INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    [Title] NVARCHAR(200) NOT NULL,
    [Description] NVARCHAR(MAX) NULL,
    [AuthorId] INT NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NULL,
    CONSTRAINT [FK_Blog_Author] FOREIGN KEY ([AuthorId]) REFERENCES [dbo].[Author]([AuthorId])
)
GO

CREATE INDEX [IX_Blog_AuthorId] ON [dbo].[Blog] ([AuthorId])
GO
