CREATE TABLE [dbo].[Post]
(
    [PostId] INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    [BlogId] INT NOT NULL,
    [Title] NVARCHAR(200) NOT NULL,
    [Content] NVARCHAR(MAX) NOT NULL,
    [PublishedAt] DATETIME2 NULL,
    [IsPublished] BIT NOT NULL DEFAULT 0,
    CONSTRAINT [FK_Post_Blog] FOREIGN KEY ([BlogId]) REFERENCES [dbo].[Blog]([BlogId])
)
GO

CREATE INDEX [IX_Post_BlogId] ON [dbo].[Post] ([BlogId])
GO
