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

CREATE TABLE [dbo].[Categories] (
    [CategoryId]  INT            IDENTITY (1, 1) NOT NULL,
    [Name]        NVARCHAR (100) NOT NULL,
    [Description] NVARCHAR (500) NULL,
    [CreatedAt]   DATETIME2 (7)  DEFAULT (getutcdate()) NOT NULL,
    [ModifiedAt]  DATETIME2 (7)  NULL,
    PRIMARY KEY CLUSTERED ([CategoryId] ASC)
);


GO

