-- Sales-related entity
CREATE TABLE [sales].[OrderItem]
(
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [OrderId] INT NOT NULL,
    [ProductId] INT NOT NULL,
    [Quantity] INT NOT NULL,
    [UnitPrice] DECIMAL(18, 2) NOT NULL,

    CONSTRAINT [FK_OrderItem_Order] FOREIGN KEY ([OrderId])
        REFERENCES [sales].[Order] ([Id]),
    CONSTRAINT [FK_OrderItem_Product] FOREIGN KEY ([ProductId])
        REFERENCES [inventory].[Product] ([Id])
);
