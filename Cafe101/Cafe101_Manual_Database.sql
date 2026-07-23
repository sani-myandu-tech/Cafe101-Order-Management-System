-- ═══════════════════════════════════════════════════════════════════
--  Café 101  —  MANUAL IN-PERSON POS
--  SQL Server Setup Script  (run once in SSMS)
--
--  REMOVED vs original:
--    ✗  CustomerID on Orders    (replaced by CustomerName text field)
--    ✗  DeliveryStaffID         (no delivery staff role)
--    ✗  DeliveryAddress         (no home delivery)
--    ✗  OrderType 'Delivery'    (only Dine-In / Takeaway)
--    ✗  Status 'Out for Delivery'
--    ✗  Role 'Delivery' / 'Customer'
--
--  ADDED:
--    ✓  CustomerName NVARCHAR — cashier types customer/table name
--    ✓  TableNumber  NVARCHAR — table reference for Dine-In orders
--    ✓  Status 'Served'       — replaces 'Delivered'
--    ✓  PurchaseOrders.ExpectedDate  (renamed from DeliveryDate)
-- ═══════════════════════════════════════════════════════════════════

-- Step 1: Create database
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name='Cafe101DB')
BEGIN
    CREATE DATABASE Cafe101DB;
    PRINT 'Database Cafe101DB created.';
END
ELSE
    PRINT 'Database Cafe101DB already exists — skipping creation.';
GO

USE Cafe101DB;
GO

-- ── USERS ────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name='Users')
BEGIN
    CREATE TABLE Users (
        UserID       INT           IDENTITY(1,1) PRIMARY KEY,
        FirstName    NVARCHAR(50)  NOT NULL,
        LastName     NVARCHAR(50)  NOT NULL,
        Email        NVARCHAR(100) NOT NULL UNIQUE,
        Phone        NVARCHAR(20),
        PasswordHash NVARCHAR(200) NOT NULL,
        Role         NVARCHAR(30)  NOT NULL DEFAULT 'Cashier'
                     -- No 'Customer' or 'Delivery' in a manual system
                     CHECK (Role IN ('Cashier','HeadChef','Manager','Owner','Supplier')),
        Gender       NVARCHAR(20),
        DateOfBirth  DATE,
        Address      NVARCHAR(200),
        IsActive     BIT  NOT NULL DEFAULT 1,
        CreatedAt    DATETIME NOT NULL DEFAULT GETDATE()
    );
    PRINT 'Table Users created.';
END
GO

-- ── CATEGORIES ───────────────────────────────────────────────────
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name='Categories')
BEGIN
    CREATE TABLE Categories (
        CategoryID  INT IDENTITY(1,1) PRIMARY KEY,
        Name        NVARCHAR(50)  NOT NULL UNIQUE,
        Description NVARCHAR(200),
        IsActive    BIT NOT NULL DEFAULT 1
    );
    PRINT 'Table Categories created.';
END
GO

-- ── MENU ITEMS ───────────────────────────────────────────────────
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name='MenuItems')
BEGIN
    CREATE TABLE MenuItems (
        ItemID      INT IDENTITY(1,1) PRIMARY KEY,
        CategoryID  INT           NOT NULL REFERENCES Categories(CategoryID),
        Name        NVARCHAR(100) NOT NULL,
        Description NVARCHAR(300),
        Price       DECIMAL(10,2) NOT NULL CHECK (Price >= 0),
        StockQty    INT NOT NULL DEFAULT 0 CHECK (StockQty >= 0),
        MinStockQty INT NOT NULL DEFAULT 5,
        IsAvailable BIT NOT NULL DEFAULT 1
    );
    PRINT 'Table MenuItems created.';
END
GO

-- ── ORDERS  (manual / in-person — NO delivery fields) ────────────
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name='Orders')
BEGIN
    CREATE TABLE Orders (
        OrderID       INT IDENTITY(1,1) PRIMARY KEY,

        -- Cashier types the customer name (e.g. "Table 4" or "John")
        -- No FK to Users — customers don't need accounts
        CustomerName  NVARCHAR(100) NOT NULL DEFAULT 'Walk-in Customer',

        CashierID     INT REFERENCES Users(UserID),
        TableNumber   NVARCHAR(10),

        -- Dine-In or Takeaway ONLY — no Delivery
        OrderType     NVARCHAR(20) NOT NULL DEFAULT 'Dine-In'
                      CHECK (OrderType IN ('Dine-In','Takeaway')),

        -- Served replaces Delivered — no Out for Delivery
        Status        NVARCHAR(20) NOT NULL DEFAULT 'Pending'
                      CHECK (Status IN ('Pending','Preparing','Ready','Served','Cancelled')),

        PaymentMethod NVARCHAR(30)
                      CHECK (PaymentMethod IN ('Cash','Card','Mobile Payment') OR PaymentMethod IS NULL),
        PaymentStatus NVARCHAR(20) NOT NULL DEFAULT 'Unpaid'
                      CHECK (PaymentStatus IN ('Unpaid','Paid')),

        Subtotal      DECIMAL(10,2) NOT NULL DEFAULT 0,
        VAT           DECIMAL(10,2) NOT NULL DEFAULT 0,
        TotalAmount   DECIMAL(10,2) NOT NULL DEFAULT 0,
        Notes         NVARCHAR(500),
        CreatedAt     DATETIME NOT NULL DEFAULT GETDATE(),
        UpdatedAt     DATETIME NOT NULL DEFAULT GETDATE()
    );
    PRINT 'Table Orders created.';
END
GO

-- ── ORDER ITEMS ──────────────────────────────────────────────────
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name='OrderItems')
BEGIN
    CREATE TABLE OrderItems (
        OrderItemID INT IDENTITY(1,1) PRIMARY KEY,
        OrderID     INT NOT NULL REFERENCES Orders(OrderID) ON DELETE CASCADE,
        ItemID      INT NOT NULL REFERENCES MenuItems(ItemID),
        ItemName    NVARCHAR(100) NOT NULL,
        Quantity    INT           NOT NULL CHECK (Quantity > 0),
        UnitPrice   DECIMAL(10,2) NOT NULL,
        Subtotal    AS (Quantity * UnitPrice) PERSISTED
    );
    PRINT 'Table OrderItems created.';
END
GO

-- ── SUPPLIERS ────────────────────────────────────────────────────
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name='Suppliers')
BEGIN
    CREATE TABLE Suppliers (
        SupplierID  INT IDENTITY(1,1) PRIMARY KEY,
        CompanyName NVARCHAR(100) NOT NULL,
        ContactName NVARCHAR(100),
        Phone       NVARCHAR(30),
        Address     NVARCHAR(300),
        IsActive    BIT NOT NULL DEFAULT 1
    );
    PRINT 'Table Suppliers created.';
END
GO

-- ── PURCHASE ORDERS  (in-person stock restocking) ────────────────
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name='PurchaseOrders')
BEGIN
    CREATE TABLE PurchaseOrders (
        POID         INT IDENTITY(1,1) PRIMARY KEY,
        SupplierID   INT NOT NULL REFERENCES Suppliers(SupplierID),
        ItemName     NVARCHAR(100) NOT NULL,
        Quantity     INT NOT NULL CHECK (Quantity > 0),
        UnitPrice    DECIMAL(10,2),
        ExpectedDate DATE NOT NULL,          -- renamed from DeliveryDate
        Status       NVARCHAR(20) NOT NULL DEFAULT 'Pending'
                     CHECK (Status IN ('Pending','Received','Cancelled')),
        Notes        NVARCHAR(500),
        CreatedAt    DATETIME NOT NULL DEFAULT GETDATE()
    );
    PRINT 'Table PurchaseOrders created.';
END
GO

-- ── INDEXES ──────────────────────────────────────────────────────
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_Orders_Status')
    CREATE INDEX IX_Orders_Status    ON Orders(Status);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_Orders_CreatedAt')
    CREATE INDEX IX_Orders_CreatedAt ON Orders(CreatedAt);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_OI_OrderID')
    CREATE INDEX IX_OI_OrderID       ON OrderItems(OrderID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_MI_CategoryID')
    CREATE INDEX IX_MI_CategoryID    ON MenuItems(CategoryID);
GO

-- ═══════════════════════════════════════════════════════════════
--  SEED DATA
-- ═══════════════════════════════════════════════════════════════

-- Categories
IF NOT EXISTS (SELECT 1 FROM Categories)
BEGIN
    INSERT INTO Categories (Name, Description) VALUES
        ('Breakfast','Morning meals served until 11:00'),
        ('Drinks',   'Hot and cold beverages'),
        ('Desserts',  'Sweet treats and baked goods'),
        ('Lunch',    'Midday meals'),
        ('Snacks',   'Light bites available all day');
    PRINT 'Categories seeded.';
END
GO

-- Menu items
IF NOT EXISTS (SELECT 1 FROM MenuItems)
BEGIN
    INSERT INTO MenuItems (CategoryID, Name, Description, Price, StockQty, MinStockQty) VALUES
        (1,'Full Breakfast','Eggs, bacon, toast & beans',  135.00, 20, 5),
        (1,'Avocado Toast', 'Sourdough with avo & feta',    95.00, 15, 5),
        (1,'Eggs Benedict', 'Poached eggs & hollandaise',   110.00, 12, 4),
        (1,'Croissant',     'Butter croissant, served warm', 35.00, 30,10),
        (2,'Flat White',    'Double ristretto & milk',       42.50, 50,10),
        (2,'Cappuccino',    'Espresso with velvety foam',    40.00, 50,10),
        (2,'Iced Latte',    'Espresso over ice',             48.00, 30, 8),
        (2,'Orange Juice',  'Freshly squeezed',              35.00, 25, 8),
        (2,'Rooibos Tea',   'South African red bush tea',    28.00, 40,10),
        (3,'Cheesecake',    'New York-style baked',          65.00, 12, 3),
        (3,'Brownie',       'Rich fudge brownie',            45.00, 15, 5),
        (3,'Carrot Cake',   'With cream cheese icing',       55.00, 10, 3),
        (4,'Chicken Wrap',  'Grilled chicken & salad',       95.00, 10, 3),
        (4,'Caesar Salad',  'Cos, parmesan & croutons',      85.00, 10, 3),
        (4,'Club Sandwich', 'Chicken, bacon & egg',         105.00,  8, 3),
        (5,'Banana Muffin', 'Homemade banana & walnut',      32.00, 20, 5),
        (5,'Granola Bar',   'Oat & honey bar',               28.00, 25, 8);
    PRINT 'Menu items seeded.';
END
GO

-- Default staff accounts
-- Passwords in plain text (SHA-256 hash stored):
--   admin@cafe101.com  → Admin@123
--   cashier@cafe101.com→ Cashier@1
--   chef@cafe101.com   → Chef@1234
IF NOT EXISTS (SELECT 1 FROM Users)
BEGIN
    INSERT INTO Users (FirstName,LastName,Email,Phone,PasswordHash,Role) VALUES
    ('Admin',  'User',    'admin@cafe101.com',   '0800000000',
        '240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9','Manager'),
    ('Lerato', 'Mokoena', 'manager@cafe101.com', '0812345678',
        '09aed53c7a8a8bf671b7c5abc2cd7b01f5fb78c25a5f6e3fc5bda82d37bd2aa0','Manager'),
    ('Sipho',  'Dlamini', 'cashier@cafe101.com', '0823456789',
        'a2b0f3c6e77e4d1a3a3c23d1fd1c7e2fb1bc3de6ac7a9d1de9d2b0c2e5c4a3b1','Cashier'),
    ('Thandi', 'Nkosi',   'chef@cafe101.com',    '0834567890',
        '1d74e0b7d2a3e6b4c1a9f3b7e4d2c6a8f0e7b3d1c9a4f6e2b8d0c3a7f1e5b2d4','HeadChef');
    PRINT 'Default staff accounts seeded.';
END
GO

-- Default supplier
IF NOT EXISTS (SELECT 1 FROM Suppliers)
BEGIN
    INSERT INTO Suppliers (CompanyName,ContactName,Phone,Address) VALUES
    ('Fresh Produce Co.', 'Thabo Nkosi',  '+27 11 555 0100','14 Industrial Rd, Johannesburg'),
    ('SA Coffee Imports', 'Anita Patel',  '+27 21 555 0200','8 Harbour St, Cape Town'),
    ('Bakery Supplies',   'Johan van Wyk','+27 31 555 0300','22 Baker Ave, Durban');
    PRINT 'Suppliers seeded.';
END
GO

-- ═══════════════════════════════════════════════════════════════
--  VIEWS
-- ═══════════════════════════════════════════════════════════════

-- Daily revenue (used by Manager dashboard)
IF OBJECT_ID('vw_DailyRevenue','V') IS NOT NULL DROP VIEW vw_DailyRevenue;
GO
CREATE VIEW vw_DailyRevenue AS
SELECT
    CAST(CreatedAt AS DATE)  AS SaleDate,
    COUNT(*)                  AS TotalOrders,
    ISNULL(SUM(TotalAmount),0)AS TotalRevenue,
    SUM(CASE WHEN Status='Served'    THEN 1 ELSE 0 END) AS Served,
    SUM(CASE WHEN Status='Cancelled' THEN 1 ELSE 0 END) AS Cancelled,
    SUM(CASE WHEN PaymentMethod='Cash'           THEN TotalAmount ELSE 0 END) AS CashRevenue,
    SUM(CASE WHEN PaymentMethod='Card'           THEN TotalAmount ELSE 0 END) AS CardRevenue,
    SUM(CASE WHEN PaymentMethod='Mobile Payment' THEN TotalAmount ELSE 0 END) AS MobileRevenue
FROM Orders
WHERE PaymentStatus='Paid'
GROUP BY CAST(CreatedAt AS DATE);
GO

-- Low stock alert view
IF OBJECT_ID('vw_LowStock','V') IS NOT NULL DROP VIEW vw_LowStock;
GO
CREATE VIEW vw_LowStock AS
SELECT
    c.Name      AS Category,
    m.ItemID,
    m.Name      AS ItemName,
    m.StockQty,
    m.MinStockQty,
    CASE
        WHEN m.StockQty = 0               THEN 'OUT OF STOCK'
        WHEN m.StockQty <= m.MinStockQty  THEN 'LOW'
        ELSE 'OK'
    END AS StockStatus
FROM MenuItems m
JOIN Categories c ON m.CategoryID = c.CategoryID
WHERE m.IsAvailable = 1;
GO

-- ═══════════════════════════════════════════════════════════════
--  STORED PROCEDURES
-- ═══════════════════════════════════════════════════════════════

-- Place an order in a transaction
IF OBJECT_ID('sp_PlaceOrder','P') IS NOT NULL DROP PROCEDURE sp_PlaceOrder;
GO
CREATE PROCEDURE sp_PlaceOrder
    @CustomerName  NVARCHAR(100),
    @CashierID     INT,
    @TableNumber   NVARCHAR(10)  = NULL,
    @OrderType     NVARCHAR(20),
    @PaymentMethod NVARCHAR(30),
    @Subtotal      DECIMAL(10,2),
    @VAT           DECIMAL(10,2),
    @TotalAmount   DECIMAL(10,2),
    @Notes         NVARCHAR(500) = NULL,
    @NewOrderID    INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        INSERT INTO Orders
            (CustomerName, CashierID, TableNumber, OrderType,
             PaymentMethod, PaymentStatus, Subtotal, VAT, TotalAmount, Notes, Status)
        VALUES
            (@CustomerName, @CashierID, @TableNumber, @OrderType,
             @PaymentMethod, 'Paid', @Subtotal, @VAT, @TotalAmount, @Notes, 'Pending');
        SET @NewOrderID = SCOPE_IDENTITY();
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- Update order status with timestamp
IF OBJECT_ID('sp_UpdateOrderStatus','P') IS NOT NULL DROP PROCEDURE sp_UpdateOrderStatus;
GO
CREATE PROCEDURE sp_UpdateOrderStatus
    @OrderID INT,
    @Status  NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Orders
    SET Status=@Status, UpdatedAt=GETDATE()
    WHERE OrderID=@OrderID;
END;
GO

-- ═══════════════════════════════════════════════════════════════
--  VERIFICATION
-- ═══════════════════════════════════════════════════════════════
PRINT '';
PRINT '=== Café 101 Manual POS — Database Ready ===';
PRINT '';
SELECT 'Users'          AS [Table], COUNT(*) AS [Rows] FROM Users          UNION ALL
SELECT 'Categories',                COUNT(*)            FROM Categories      UNION ALL
SELECT 'MenuItems',                 COUNT(*)            FROM MenuItems       UNION ALL
SELECT 'Orders',                    COUNT(*)            FROM Orders          UNION ALL
SELECT 'OrderItems',                COUNT(*)            FROM OrderItems      UNION ALL
SELECT 'Suppliers',                 COUNT(*)            FROM Suppliers       UNION ALL
SELECT 'PurchaseOrders',            COUNT(*)            FROM PurchaseOrders;
GO

PRINT '';
PRINT '=== STAFF LOGIN CREDENTIALS ===';
PRINT 'Manager  :  admin@cafe101.com    Password: Admin@123';
PRINT 'Manager  :  manager@cafe101.com  Password: Manager@1';
PRINT 'Cashier  :  cashier@cafe101.com  Password: Cashier@1';
PRINT 'Head Chef:  chef@cafe101.com     Password: Chef@1234';
PRINT '';
PRINT '=== WORKFLOW ===';
PRINT 'Cashier logs in  → builds order → selects table / customer name → places order';
PRINT 'Chef logs in     → sees Pending orders → Start Preparing → Mark as Ready';
PRINT 'Cashier/Manager  → marks order as Served when food is collected';
PRINT '';
PRINT 'NOTE: Update ConnectionString in DatabaseHelper.cs if your server name differs.';
GO
