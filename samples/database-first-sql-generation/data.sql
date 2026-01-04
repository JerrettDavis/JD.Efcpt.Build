SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Insert Categories
INSERT INTO dbo.Categories (Name, Description) VALUES
    ('Electronics', 'Electronic devices and accessories'),
    ('Books', 'Physical and digital books'),
    ('Clothing', 'Apparel and fashion items'),
    ('Home & Garden', 'Home improvement and gardening supplies');

-- Insert Products
INSERT INTO dbo.Products (CategoryId, Name, Description, Price, StockQuantity) VALUES
    (1, 'Laptop Pro 15', 'High-performance laptop with 15-inch display', 1299.99, 50),
    (1, 'Wireless Mouse', 'Ergonomic wireless mouse with USB receiver', 29.99, 200),
    (1, 'USB-C Hub', '7-in-1 USB-C hub with HDMI and SD card reader', 49.99, 100),
    (2, 'Clean Code', 'A Handbook of Agile Software Craftsmanship', 39.99, 75),
    (2, 'Design Patterns', 'Elements of Reusable Object-Oriented Software', 54.99, 60),
    (3, 'Cotton T-Shirt', 'Comfortable 100% cotton t-shirt', 19.99, 300),
    (3, 'Denim Jeans', 'Classic fit denim jeans', 49.99, 150),
    (4, 'Garden Tool Set', 'Complete 10-piece garden tool set', 89.99, 40);

-- Insert Customers
INSERT INTO dbo.Customers (FirstName, LastName, Email, Phone) VALUES
    ('John', 'Doe', 'john.doe@example.com', '555-0101'),
    ('Jane', 'Smith', 'jane.smith@example.com', '555-0102'),
    ('Bob', 'Johnson', 'bob.johnson@example.com', '555-0103'),
    ('Alice', 'Williams', 'alice.williams@example.com', '555-0104');

-- Insert Orders
INSERT INTO dbo.Orders (CustomerId, OrderDate, TotalAmount, Status, ShippingAddress) VALUES
    (1, '2025-01-01', 1329.98, 'Completed', '123 Main St, Seattle, WA 98101'),
    (2, '2025-01-02', 94.98, 'Shipped', '456 Oak Ave, Portland, OR 97201'),
    (3, '2025-01-03', 69.98, 'Processing', '789 Pine Rd, Austin, TX 78701'),
    (4, '2025-01-03', 129.97, 'Pending', '321 Elm St, Denver, CO 80201');

-- Insert OrderItems
INSERT INTO dbo.OrderItems (OrderId, ProductId, Quantity, UnitPrice) VALUES
    -- Order 1
    (1, 1, 1, 1299.99),  -- Laptop
    (1, 2, 1, 29.99),     -- Mouse
    -- Order 2
    (2, 4, 1, 39.99),     -- Clean Code
    (2, 5, 1, 54.99),     -- Design Patterns
    -- Order 3
    (3, 6, 2, 19.99),     -- 2x T-Shirt
    (3, 2, 1, 29.99),     -- Mouse
    -- Order 4
    (4, 7, 1, 49.99),     -- Jeans
    (4, 3, 1, 49.99),     -- USB-C Hub
    (4, 2, 1, 29.99);     -- Mouse

PRINT 'Sample data inserted successfully';
