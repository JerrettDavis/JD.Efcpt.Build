/*
 * ============================================================================
 * AUTO-GENERATED FILE - DO NOT EDIT DIRECTLY
 * ============================================================================
 *
 * This file was automatically generated from database: EfcptSampleDb
 * Generator: JD.Efcpt.Build (Database-First SqlProj Generation)
 *
 * IMPORTANT:
 * - Changes to this file may be overwritten during the next generation.
 * - To preserve custom changes, configure the generation process
 *   or create separate files that will not be regenerated.
 * - To extend the database with custom scripts or seeded data,
 *   add them to the SQL project separately.
 *
 * For more information:
 * https://github.com/jerrettdavis/JD.Efcpt.Build
 * ============================================================================
 */

CREATE TABLE [dbo].[Products] (
    [ProductId]     INT             IDENTITY (1, 1) NOT NULL,
    [CategoryId]    INT             NOT NULL,
    [Name]          NVARCHAR (200)  NOT NULL,
    [Description]   NVARCHAR (1000) NULL,
    [Price]         DECIMAL (18, 2) NOT NULL,
    [StockQuantity] INT             DEFAULT ((0)) NOT NULL,
    [IsActive]      BIT             DEFAULT ((1)) NOT NULL,
    [CreatedAt]     DATETIME2 (7)   DEFAULT (getutcdate()) NOT NULL,
    [ModifiedAt]    DATETIME2 (7)   NULL,
    PRIMARY KEY CLUSTERED ([ProductId] ASC),
    CONSTRAINT [FK_Products_Categories] FOREIGN KEY ([CategoryId]) REFERENCES [dbo].[Categories] ([CategoryId])
);


GO

CREATE NONCLUSTERED INDEX [IX_Products_CategoryId]
    ON [dbo].[Products]([CategoryId] ASC);


GO

