CREATE TABLE [dbo].[Posts]
(
    [PostId] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Posts] PRIMARY KEY,
    [BlogId] INT NOT NULL,
    [Title] NVARCHAR(200) NOT NULL,
    [Content] NVARCHAR(MAX) NULL,
    [PublishedAt] DATETIME2 NULL,
    CONSTRAINT [FK_Posts_Blogs] FOREIGN KEY ([BlogId]) REFERENCES [dbo].[Blogs]([BlogId])
);

