-- Legacy table with "tbl" prefix and column prefixes
CREATE TABLE [dbo].[tblOrders]
(
    [ord_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [ord_cust_id] INT NOT NULL,
    [ord_date] DATETIME2 NOT NULL DEFAULT GETDATE(),
    [ord_total_amount] DECIMAL(18, 2) NOT NULL,
    [ord_status] NVARCHAR(20) NOT NULL DEFAULT 'Pending',
    [ord_notes] NVARCHAR(MAX) NULL,

    CONSTRAINT [FK_tblOrders_tblCustomers] FOREIGN KEY ([ord_cust_id])
        REFERENCES [dbo].[tblCustomers] ([cust_id])
);
