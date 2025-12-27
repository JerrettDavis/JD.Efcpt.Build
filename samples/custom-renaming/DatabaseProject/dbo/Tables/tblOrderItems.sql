-- Legacy table with "tbl" prefix and column prefixes
CREATE TABLE [dbo].[tblOrderItems]
(
    [item_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [item_ord_id] INT NOT NULL,
    [item_product_name] NVARCHAR(100) NOT NULL,
    [item_qty] INT NOT NULL,
    [item_unit_price] DECIMAL(18, 2) NOT NULL,
    [item_discount] DECIMAL(5, 2) NOT NULL DEFAULT 0,

    CONSTRAINT [FK_tblOrderItems_tblOrders] FOREIGN KEY ([item_ord_id])
        REFERENCES [dbo].[tblOrders] ([ord_id])
);
