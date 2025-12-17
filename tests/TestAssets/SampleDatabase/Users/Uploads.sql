CREATE TABLE [Users].[Uploads]
(
    [UploadId] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Uploads] PRIMARY KEY,
    [AccountId] INT NOT NULL,
    [FileName] NVARCHAR(260) NOT NULL,
    [UploadedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [FK_Uploads_Accounts] FOREIGN KEY ([AccountId]) REFERENCES [Users].[Accounts]([AccountId])
);

