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

CREATE TABLE [dbo].[OrderItems] (
    [OrderItemId] INT             IDENTITY (1, 1) NOT NULL,
    [OrderId]     INT             NOT NULL,
    [ProductId]   INT             NOT NULL,
    [Quantity]    INT             NOT NULL,
    [UnitPrice]   DECIMAL (18, 2) NOT NULL,
    [Subtotal]    AS              ([Quantity]*[UnitPrice]) PERSISTED,
    PRIMARY KEY CLUSTERED ([OrderItemId] ASC),
    CONSTRAINT [FK_OrderItems_Orders] FOREIGN KEY ([OrderId]) REFERENCES [dbo].[Orders] ([OrderId]),
    CONSTRAINT [FK_OrderItems_Products] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products] ([ProductId])
);


GO

CREATE NONCLUSTERED INDEX [IX_OrderItems_OrderId]
    ON [dbo].[OrderItems]([OrderId] ASC);


GO

CREATE NONCLUSTERED INDEX [IX_OrderItems_ProductId]
    ON [dbo].[OrderItems]([ProductId] ASC);


GO

