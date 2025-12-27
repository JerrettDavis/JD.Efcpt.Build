-- Sample database schema for ASP.NET Core application
-- This script initializes the MyAppDb database

USE [MyAppDb];
GO

-- Users table
CREATE TABLE [dbo].[Users] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Email] NVARCHAR(256) NOT NULL UNIQUE,
    [DisplayName] NVARCHAR(100) NOT NULL,
    [PasswordHash] NVARCHAR(MAX) NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [LastLoginAt] DATETIME2 NULL,
    [IsActive] BIT NOT NULL DEFAULT 1
);
GO

-- Roles table
CREATE TABLE [dbo].[Roles] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Name] NVARCHAR(50) NOT NULL UNIQUE,
    [Description] NVARCHAR(256) NULL
);
GO

-- UserRoles junction table
CREATE TABLE [dbo].[UserRoles] (
    [UserId] INT NOT NULL,
    [RoleId] INT NOT NULL,
    PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_UserRoles_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserRoles_Roles] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles]([Id]) ON DELETE CASCADE
);
GO

-- AuditLogs table
CREATE TABLE [dbo].[AuditLogs] (
    [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [UserId] INT NULL,
    [Action] NVARCHAR(100) NOT NULL,
    [EntityType] NVARCHAR(100) NULL,
    [EntityId] NVARCHAR(100) NULL,
    [OldValues] NVARCHAR(MAX) NULL,
    [NewValues] NVARCHAR(MAX) NULL,
    [Timestamp] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [IpAddress] NVARCHAR(45) NULL,
    CONSTRAINT [FK_AuditLogs_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id])
);
GO

-- Insert sample data
INSERT INTO [dbo].[Roles] ([Name], [Description]) VALUES
    ('Admin', 'Full system access'),
    ('User', 'Standard user access'),
    ('ReadOnly', 'Read-only access');
GO

INSERT INTO [dbo].[Users] ([Email], [DisplayName], [PasswordHash]) VALUES
    ('admin@example.com', 'Administrator', 'hashed_password_placeholder'),
    ('user@example.com', 'Regular User', 'hashed_password_placeholder');
GO

INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId]) VALUES
    (1, 1),  -- Admin has Admin role
    (2, 2);  -- User has User role
GO

PRINT 'MyAppDb database initialized successfully.';
GO
