CREATE TABLE [dbo].[Author]
(
    [AuthorId] INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    [Name] NVARCHAR(100) NOT NULL,
    [Email] NVARCHAR(255) NOT NULL,
    [Bio] NVARCHAR(MAX) NULL
)
GO

CREATE UNIQUE INDEX [IX_Author_Email] ON [dbo].[Author] ([Email])
GO
