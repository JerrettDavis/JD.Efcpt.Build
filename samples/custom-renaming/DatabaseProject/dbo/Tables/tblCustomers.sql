-- Legacy table with "tbl" prefix and column prefixes
CREATE TABLE [dbo].[tblCustomers]
(
    [cust_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [cust_first_name] NVARCHAR(50) NOT NULL,
    [cust_last_name] NVARCHAR(50) NOT NULL,
    [cust_email] NVARCHAR(100) NOT NULL,
    [cust_phone] NVARCHAR(20) NULL,
    [cust_created_date] DATETIME2 NOT NULL DEFAULT GETDATE(),
    [cust_is_active] BIT NOT NULL DEFAULT 1
);
