-- Sales-related entity
CREATE TABLE [sales].[Order]
(
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [CustomerId] INT NOT NULL,
    [OrderDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
    [TotalAmount] DECIMAL(18, 2) NOT NULL,
    [Status] NVARCHAR(20) NOT NULL DEFAULT 'Pending',

    CONSTRAINT [FK_Order_Customer] FOREIGN KEY ([CustomerId])
        REFERENCES [dbo].[Customer] ([Id])
);
