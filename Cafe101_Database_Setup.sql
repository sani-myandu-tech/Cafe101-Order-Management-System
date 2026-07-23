-- ════════════════════════════════════════════════════════════════
--  Café 101 — Complete Database Reset & Setup Script
--  Run this in SSMS against your ist3dy database.
--  WARNING: This drops all existing Café 101 tables and recreates
--  them from scratch with sample data.
-- ════════════════════════════════════════════════════════════════

USE ist3dy;
GO

-- ── Drop tables in dependency order ──────────────────────────────
IF OBJECT_ID('OrderItems',     'U') IS NOT NULL DROP TABLE OrderItems;
IF OBJECT_ID('Orders',         'U') IS NOT NULL DROP TABLE Orders;
IF OBJECT_ID('PurchaseOrders', 'U') IS NOT NULL DROP TABLE PurchaseOrders;
IF OBJECT_ID('MenuItems',      'U') IS NOT NULL DROP TABLE MenuItems;
IF OBJECT_ID('Categories',     'U') IS NOT NULL DROP TABLE Categories;
IF OBJECT_ID('Suppliers',      'U') IS NOT NULL DROP TABLE Suppliers;
IF OBJECT_ID('Customers',      'U') IS NOT NULL DROP TABLE Customers;
IF OBJECT_ID('Users',          'U') IS NOT NULL DROP TABLE Users;
GO

-- ── Users (staff accounts) ───────────────────────────────────────
CREATE TABLE Users (
    UserID       INT IDENTITY(1,1) PRIMARY KEY,
    FirstName    NVARCHAR(50)  NOT NULL,
    LastName     NVARCHAR(50)  NOT NULL,
    Email        NVARCHAR(100) NOT NULL UNIQUE,
    Phone        NVARCHAR(20),
    PasswordHash NVARCHAR(256) NOT NULL,
    Role         NVARCHAR(30)  NOT NULL DEFAULT 'Cashier'
                 CHECK (Role IN ('Cashier','HeadChef','Manager','Owner')),
    Gender       NVARCHAR(20),
    DateOfBirth  DATE,
    Address      NVARCHAR(200),
    IsActive     BIT NOT NULL DEFAULT 1,
    CreatedAt    DATETIME NOT NULL DEFAULT GETDATE()
);
GO

-- ── Customers (walk-in / loyalty customers) ───────────────────────
CREATE TABLE Customers (
    CustomerID   INT IDENTITY(1,1) PRIMARY KEY,
    FirstName    NVARCHAR(50)  NOT NULL,
    LastName     NVARCHAR(50)  NOT NULL,
    Email        NVARCHAR(100),
    Phone        NVARCHAR(30),
    Address      NVARCHAR(300),
    Notes        NVARCHAR(300),
    CreatedAt    DATETIME NOT NULL DEFAULT GETDATE()
);
GO

-- ── Categories ───────────────────────────────────────────────────
CREATE TABLE Categories (
    CategoryID  INT IDENTITY(1,1) PRIMARY KEY,
    Name        NVARCHAR(50)  NOT NULL UNIQUE,
    Description NVARCHAR(200),
    IsActive    BIT NOT NULL DEFAULT 1
);
GO

-- ── MenuItems ────────────────────────────────────────────────────
CREATE TABLE MenuItems (
    ItemID      INT IDENTITY(1,1) PRIMARY KEY,
    CategoryID  INT NOT NULL REFERENCES Categories(CategoryID),
    Name        NVARCHAR(100) NOT NULL,
    Description NVARCHAR(300),
    Price       DECIMAL(10,2) NOT NULL CHECK (Price >= 0),
    StockQty    INT NOT NULL DEFAULT 0,
    MinStockQty INT NOT NULL DEFAULT 5,
    IsAvailable BIT NOT NULL DEFAULT 1
);
GO

-- ── Suppliers ────────────────────────────────────────────────────
CREATE TABLE Suppliers (
    SupplierID  INT IDENTITY(1,1) PRIMARY KEY,
    CompanyName NVARCHAR(100) NOT NULL,
    ContactName NVARCHAR(100),
    Phone       NVARCHAR(30),
    Email       NVARCHAR(100),
    Address     NVARCHAR(300),
    IsActive    BIT NOT NULL DEFAULT 1,
    CreatedAt   DATETIME NOT NULL DEFAULT GETDATE()
);
GO

-- ── Orders ───────────────────────────────────────────────────────
CREATE TABLE Orders (
    OrderID       INT IDENTITY(1,1) PRIMARY KEY,
    CustomerName  NVARCHAR(100) NOT NULL DEFAULT 'Walk-in Customer',
    CashierID     INT REFERENCES Users(UserID),
    TableNumber   NVARCHAR(10),
    OrderType     NVARCHAR(20) NOT NULL DEFAULT 'Dine-In'
                  CHECK (OrderType IN ('Dine-In','Takeaway')),
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
GO

-- ── OrderItems ───────────────────────────────────────────────────
CREATE TABLE OrderItems (
    OrderItemID INT IDENTITY(1,1) PRIMARY KEY,
    OrderID     INT NOT NULL REFERENCES Orders(OrderID) ON DELETE CASCADE,
    ItemID      INT NOT NULL REFERENCES MenuItems(ItemID),
    ItemName    NVARCHAR(100) NOT NULL,
    Quantity    INT NOT NULL CHECK (Quantity > 0),
    UnitPrice   DECIMAL(10,2) NOT NULL,
    Subtotal    AS (CAST(Quantity AS DECIMAL(10,2)) * UnitPrice) PERSISTED
);
GO

-- ── PurchaseOrders ───────────────────────────────────────────────
CREATE TABLE PurchaseOrders (
    POID         INT IDENTITY(1,1) PRIMARY KEY,
    SupplierID   INT NOT NULL REFERENCES Suppliers(SupplierID),
    ItemName     NVARCHAR(100) NOT NULL,
    Quantity     INT NOT NULL CHECK (Quantity > 0),
    UnitPrice    DECIMAL(10,2),
    ExpectedDate DATE NOT NULL,
    Status       NVARCHAR(20) NOT NULL DEFAULT 'Pending'
                 CHECK (Status IN ('Pending','Received','Cancelled')),
    Notes        NVARCHAR(500),
    CreatedAt    DATETIME NOT NULL DEFAULT GETDATE()
);
GO

-- ════════════════════════════════════════════════════════════════
--  SEED DATA
-- ════════════════════════════════════════════════════════════════

-- ── Staff accounts (password for all: Admin@123) ─────────────────
-- Hash = SHA-256 of "Admin@123"
INSERT INTO Users (FirstName,LastName,Email,Phone,PasswordHash,Role) VALUES
    ('Admin',  'Manager',  'admin@cafe101.com',   '0000000000', '240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9', 'Manager'),
    ('Jane',   'Smith',    'cashier@cafe101.com', '0112345678', '240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9', 'Cashier'),
    ('Chef',   'Khumalo',  'chef@cafe101.com',    '0112345679', '240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9', 'HeadChef'),
    ('Thandi', 'Nkosi',    'cashier2@cafe101.com','0113456780', '240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9', 'Cashier');
GO

-- ── Customers ────────────────────────────────────────────────────
INSERT INTO Customers (FirstName,LastName,Email,Phone,Notes) VALUES
    ('John',     'Doe',     'john.doe@email.com',  '0811234567', 'Regular - prefers window seat'),
    ('Sarah',    'Mokoena', 'sarah.m@email.com',   '0729876543', 'Lactose intolerant - no dairy'),
    ('Michael',  'van Wyk', 'mike.vw@email.com',   '0834567890', 'Corporate account - Acme Corp'),
    ('Walk-in',  'Customer', NULL,                  NULL,         'Default walk-in customer');
GO

-- ── Categories ───────────────────────────────────────────────────
INSERT INTO Categories (Name, Description) VALUES
    ('Breakfast', 'Morning meals served until 11:00'),
    ('Drinks',    'Hot and cold beverages'),
    ('Desserts',  'Sweet treats and baked goods'),
    ('Lunch',     'Midday meals'),
    ('Snacks',    'Light bites all day');
GO

-- ── Menu Items ───────────────────────────────────────────────────
INSERT INTO MenuItems (CategoryID, Name, Description, Price, StockQty, MinStockQty) VALUES
    (1, 'Full Breakfast',  'Eggs, bacon, toast & beans',       135.00, 20, 5),
    (1, 'Avocado Toast',   'Sourdough with avo & feta',         95.00, 15, 5),
    (1, 'Croissant',       'Butter croissant',                  35.00, 30, 10),
    (1, 'Eggs Benedict',   'Poached eggs on English muffin',   115.00, 12, 4),
    (2, 'Flat White',      'Double espresso & steamed milk',    42.50, 50, 10),
    (2, 'Cappuccino',      'Espresso with rich foam',           40.00, 50, 10),
    (2, 'Cold Brew',       '12-hour cold brew coffee',          48.00, 20, 5),
    (2, 'Orange Juice',    'Freshly squeezed OJ',               35.00, 25, 8),
    (2, 'Hot Chocolate',   'Rich Belgian hot chocolate',        38.00, 30, 8),
    (3, 'Cheesecake',      'New York style slice',              65.00, 12, 3),
    (3, 'Brownie',         'Rich fudge brownie',                45.00, 15, 5),
    (3, 'Lemon Tart',      'Classic French lemon tart',         55.00, 10, 3),
    (4, 'Chicken Wrap',    'Grilled chicken & garden salad',    95.00, 10, 3),
    (4, 'Caesar Salad',    'Cos lettuce, croutons & dressing',  85.00, 10, 3),
    (4, 'Toasted Sandwich','Club sandwich with fries',          75.00, 15, 4),
    (5, 'Banana Muffin',   'Homemade banana muffin',            32.00, 20, 5),
    (5, 'Scone',           'Served with jam & cream',           28.00, 18, 5);
GO

-- ── Suppliers ────────────────────────────────────────────────────
INSERT INTO Suppliers (CompanyName,ContactName,Phone,Email,Address) VALUES
    ('Fresh Produce Co.',    'Thabo Nkosi',    '+27 11 555 0100', 'thabo@freshproduce.co.za',  '14 Industrial Rd, Johannesburg'),
    ('SA Coffee Wholesalers','Priya Pillay',   '+27 21 555 0200', 'priya@sacoffee.co.za',       '8 Roastery Lane, Cape Town'),
    ('Bakery Supplies Ltd',  'Gerrit du Plessis','+27 31 555 0300','gerrit@bakerysup.co.za',    '22 Flour Street, Durban'),
    ('Dairy Direct',         'Nomsa Dlamini',  '+27 11 555 0400', 'nomsa@dairydirect.co.za',   '5 Milk Way, Pretoria');
GO

-- ── Sample Purchase Orders ───────────────────────────────────────
INSERT INTO PurchaseOrders (SupplierID,ItemName,Quantity,UnitPrice,ExpectedDate,Status,Notes) VALUES
    (1, 'Avocado',       50, 3.50,  DATEADD(DAY, 2,  GETDATE()), 'Pending',  'Ripe, ready to serve'),
    (2, 'Coffee Beans',  10, 85.00, DATEADD(DAY, 3,  GETDATE()), 'Pending',  'Single-origin Ethiopian'),
    (3, 'Croissants',    60, 4.20,  DATEADD(DAY, 1,  GETDATE()), 'Pending',  'Pre-baked, frozen'),
    (4, 'Full Cream Milk',20, 18.00, DATEADD(DAY,-1, GETDATE()), 'Received', 'Delivered on time');
GO

-- ── Sample historical Orders (for reports) ───────────────────────
DECLARE @cashierId INT = (SELECT TOP 1 UserID FROM Users WHERE Role='Cashier');

INSERT INTO Orders (CustomerName,CashierID,TableNumber,OrderType,Status,PaymentMethod,PaymentStatus,Subtotal,VAT,TotalAmount,CreatedAt,UpdatedAt)
VALUES
    ('John Doe',        @cashierId,'3','Dine-In', 'Served',   'Card',          'Paid', 135.00, 20.25, 155.25, DATEADD(HOUR,-2, GETDATE()), DATEADD(HOUR,-1,GETDATE())),
    ('Walk-in Customer',@cashierId,'5','Dine-In', 'Served',   'Cash',          'Paid',  82.50, 12.38,  94.88, DATEADD(HOUR,-3, GETDATE()), DATEADD(HOUR,-2,GETDATE())),
    ('Sarah Mokoena',   @cashierId,NULL,'Takeaway','Served',  'Mobile Payment','Paid',  95.00, 14.25, 109.25, DATEADD(HOUR,-4, GETDATE()), DATEADD(HOUR,-3,GETDATE())),
    ('Table 2',         @cashierId,'2','Dine-In', 'Pending',  'Cash',          'Paid', 175.00, 26.25, 201.25, DATEADD(MINUTE,-15,GETDATE()),GETDATE()),
    ('Table 7',         @cashierId,'7','Dine-In', 'Preparing','Card',          'Paid', 230.50, 34.58, 265.08, DATEADD(MINUTE,-8, GETDATE()),GETDATE());
GO

-- Add order items for the sample orders
DECLARE @o1 INT = (SELECT TOP 1 OrderID FROM Orders ORDER BY CreatedAt ASC);
DECLARE @o2 INT = (SELECT TOP 1 OrderID FROM Orders ORDER BY CreatedAt ASC OFFSET 1 ROWS);
DECLARE @o3 INT = (SELECT TOP 1 OrderID FROM Orders ORDER BY CreatedAt ASC OFFSET 2 ROWS);
DECLARE @o4 INT = (SELECT TOP 1 OrderID FROM Orders ORDER BY CreatedAt DESC OFFSET 1 ROWS);
DECLARE @o5 INT = (SELECT TOP 1 OrderID FROM Orders ORDER BY CreatedAt DESC);

INSERT INTO OrderItems (OrderID,ItemID,ItemName,Quantity,UnitPrice) VALUES
    (@o1, 1, 'Full Breakfast', 1, 135.00),
    (@o2, 5, 'Flat White',     1,  42.50),(@o2, 10, 'Cheesecake', 1, 65.00),
    (@o3, 13,'Chicken Wrap',   1,  95.00),
    (@o4, 1, 'Full Breakfast', 1, 135.00),(@o4, 5, 'Flat White', 2, 42.50),
    (@o5, 2, 'Avocado Toast',  2,  95.00),(@o5, 8, 'Orange Juice',2,35.00),(@o5, 11,'Brownie',1,45.00);
GO

PRINT '✔  Café 101 database created and populated successfully.';
PRINT '   Default password for all accounts: Admin@123';
PRINT '';
PRINT '   Accounts:';
PRINT '   admin@cafe101.com    - Manager';
PRINT '   cashier@cafe101.com  - Cashier';
PRINT '   chef@cafe101.com     - HeadChef';
PRINT '   cashier2@cafe101.com - Cashier';
GO
