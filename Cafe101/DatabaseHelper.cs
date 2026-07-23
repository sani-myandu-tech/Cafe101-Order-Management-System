using System;
using System.Configuration;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Windows.Forms;

namespace Cafe101
{
    public static class DatabaseHelper
    {
        private static readonly string ConnectionString = LoadConnectionString();

        private static string LoadConnectionString()
        {
            var fromConfig = ConfigurationManager.ConnectionStrings["Cafe101Db"]?.ConnectionString;
            if (!string.IsNullOrWhiteSpace(fromConfig))
                return fromConfig;

            throw new InvalidOperationException(
                "Database connection string 'Cafe101Db' is missing. " +
                "Copy App.config.example to App.config and fill in your connection details.");
        }

        public static int    CurrentUserId   { get; set; }
        public static string CurrentUserName { get; set; } = "";
        public static string CurrentUserRole { get; set; } = "";

        public static SqlConnection GetConnection() => new SqlConnection(ConnectionString);

        public static bool TestConnection()
        {
            try { using (var c = GetConnection()) { c.Open(); return true; } }
            catch { return false; }
        }

        public static int ExecuteNonQuery(string sql, params SqlParameter[] p)
        {
            using (var conn = GetConnection())
            using (var cmd  = new SqlCommand(sql, conn))
            {
                cmd.CommandTimeout = 30;
                if (p != null) cmd.Parameters.AddRange(p);
                conn.Open();
                return cmd.ExecuteNonQuery();
            }
        }

        public static object ExecuteScalar(string sql, params SqlParameter[] p)
        {
            using (var conn = GetConnection())
            using (var cmd  = new SqlCommand(sql, conn))
            {
                cmd.CommandTimeout = 30;
                if (p != null) cmd.Parameters.AddRange(p);
                conn.Open();
                return cmd.ExecuteScalar();
            }
        }

        public static DataTable GetDataTable(string sql, params SqlParameter[] p)
        {
            var dt = new DataTable();
            using (var conn = GetConnection())
            using (var cmd  = new SqlCommand(sql, conn))
            using (var da   = new SqlDataAdapter(cmd))
            {
                cmd.CommandTimeout = 30;
                if (p != null) cmd.Parameters.AddRange(p);
                conn.Open();
                da.Fill(dt);
            }
            return dt;
        }

        // ── SETUP ───────────────────────────────────────────────
        public static void CreateDatabaseIfNeeded()
        {
            string[] scripts = {
                @"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name='Users')
                  CREATE TABLE Users (
                      UserID INT IDENTITY(1,1) PRIMARY KEY,
                      FirstName NVARCHAR(50) NOT NULL,
                      LastName NVARCHAR(50) NOT NULL,
                      Email NVARCHAR(100) NOT NULL UNIQUE,
                      Phone NVARCHAR(20),
                      PasswordHash NVARCHAR(256) NOT NULL,
                      Role NVARCHAR(30) NOT NULL DEFAULT 'Cashier'
                           CHECK (Role IN ('Cashier','HeadChef','Manager','Owner')),
                      Gender NVARCHAR(20),
                      DateOfBirth DATE,
                      Address NVARCHAR(200),
                      IsActive BIT NOT NULL DEFAULT 1,
                      CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
                  );",
                @"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name='Customers')
                  CREATE TABLE Customers (
                      CustomerID INT IDENTITY(1,1) PRIMARY KEY,
                      FirstName NVARCHAR(50) NOT NULL,
                      LastName NVARCHAR(50) NOT NULL,
                      Email NVARCHAR(100),
                      Phone NVARCHAR(30),
                      Address NVARCHAR(300),
                      Notes NVARCHAR(300),
                      CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
                  );",
                @"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name='Categories')
                  CREATE TABLE Categories (
                      CategoryID INT IDENTITY(1,1) PRIMARY KEY,
                      Name NVARCHAR(50) NOT NULL UNIQUE,
                      Description NVARCHAR(200),
                      IsActive BIT NOT NULL DEFAULT 1
                  );",
                @"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name='MenuItems')
                  CREATE TABLE MenuItems (
                      ItemID INT IDENTITY(1,1) PRIMARY KEY,
                      CategoryID INT NOT NULL REFERENCES Categories(CategoryID),
                      Name NVARCHAR(100) NOT NULL,
                      Description NVARCHAR(300),
                      Price DECIMAL(10,2) NOT NULL CHECK (Price >= 0),
                      StockQty INT NOT NULL DEFAULT 0,
                      MinStockQty INT NOT NULL DEFAULT 5,
                      IsAvailable BIT NOT NULL DEFAULT 1
                  );",
                @"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name='Suppliers')
                  CREATE TABLE Suppliers (
                      SupplierID INT IDENTITY(1,1) PRIMARY KEY,
                      CompanyName NVARCHAR(100) NOT NULL,
                      ContactName NVARCHAR(100),
                      Phone NVARCHAR(30),
                      Email NVARCHAR(100),
                      Address NVARCHAR(300),
                      IsActive BIT NOT NULL DEFAULT 1,
                      CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
                  );",
                @"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name='Orders')
                  CREATE TABLE Orders (
                      OrderID INT IDENTITY(1,1) PRIMARY KEY,
                      CustomerName NVARCHAR(100) NOT NULL DEFAULT 'Walk-in Customer',
                      CashierID INT REFERENCES Users(UserID),
                      TableNumber NVARCHAR(10),
                      OrderType NVARCHAR(20) NOT NULL DEFAULT 'Takeaway'
                                CHECK (OrderType IN ('Dine-In','Takeaway','Delivery')),
                      Status NVARCHAR(20) NOT NULL DEFAULT 'Pending'
                             CHECK (Status IN ('Pending','Preparing','Ready','Served','Cancelled')),
                      PaymentMethod NVARCHAR(30),
                      PaymentStatus NVARCHAR(20) NOT NULL DEFAULT 'Unpaid'
                                    CHECK (PaymentStatus IN ('Unpaid','Paid')),
                      Subtotal DECIMAL(10,2) NOT NULL DEFAULT 0,
                      VAT DECIMAL(10,2) NOT NULL DEFAULT 0,
                      TotalAmount DECIMAL(10,2) NOT NULL DEFAULT 0,
                      Notes NVARCHAR(500),
                      CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
                      UpdatedAt DATETIME NOT NULL DEFAULT GETDATE()
                  );",
                @"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name='OrderItems')
                  CREATE TABLE OrderItems (
                      OrderItemID INT IDENTITY(1,1) PRIMARY KEY,
                      OrderID INT NOT NULL REFERENCES Orders(OrderID) ON DELETE CASCADE,
                      ItemID INT NOT NULL REFERENCES MenuItems(ItemID),
                      ItemName NVARCHAR(100) NOT NULL,
                      Quantity INT NOT NULL CHECK (Quantity > 0),
                      UnitPrice DECIMAL(10,2) NOT NULL,
                      Subtotal AS (CAST(Quantity AS DECIMAL(10,2)) * UnitPrice) PERSISTED
                  );",
                @"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name='PurchaseOrders')
                  CREATE TABLE PurchaseOrders (
                      POID INT IDENTITY(1,1) PRIMARY KEY,
                      SupplierID INT NOT NULL REFERENCES Suppliers(SupplierID),
                      ItemName NVARCHAR(100) NOT NULL,
                      Quantity INT NOT NULL CHECK (Quantity > 0),
                      UnitPrice DECIMAL(10,2),
                      ExpectedDate DATE NOT NULL,
                      Status NVARCHAR(20) NOT NULL DEFAULT 'Pending'
                             CHECK (Status IN ('Pending','Received','Cancelled')),
                      Notes NVARCHAR(500),
                      CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
                  );",
                @"IF NOT EXISTS (SELECT 1 FROM Categories)
                  INSERT INTO Categories (Name,Description) VALUES
                  ('Breakfast','Morning meals'),('Drinks','Hot and cold beverages'),
                  ('Desserts','Sweet treats'),('Lunch','Midday meals'),('Snacks','Light bites');",
                @"IF NOT EXISTS (SELECT 1 FROM MenuItems)
                  INSERT INTO MenuItems (CategoryID,Name,Description,Price,StockQty,MinStockQty) VALUES
                  (1,'Full Breakfast','Eggs, bacon, toast & beans',135.00,20,5),
                  (1,'Avocado Toast','Sourdough with avo & feta',95.00,15,5),
                  (1,'Croissant','Butter croissant',35.00,30,10),
                  (1,'Eggs Benedict','Poached eggs on English muffin',115.00,12,4),
                  (2,'Flat White','Double espresso & steamed milk',42.50,50,10),
                  (2,'Cappuccino','Espresso with rich foam',40.00,50,10),
                  (2,'Cold Brew','12-hour cold brew coffee',48.00,20,5),
                  (2,'Orange Juice','Freshly squeezed OJ',35.00,25,8),
                  (2,'Hot Chocolate','Rich Belgian hot chocolate',38.00,30,8),
                  (3,'Cheesecake','New York style slice',65.00,12,3),
                  (3,'Brownie','Rich fudge brownie',45.00,15,5),
                  (3,'Lemon Tart','Classic French lemon tart',55.00,10,3),
                  (4,'Chicken Wrap','Grilled chicken & garden salad',95.00,10,3),
                  (4,'Caesar Salad','Cos lettuce, croutons & dressing',85.00,10,3),
                  (4,'Toasted Sandwich','Club sandwich with fries',75.00,15,4),
                  (5,'Banana Muffin','Homemade banana muffin',32.00,20,5),
                  (5,'Scone','Served with jam & cream',28.00,18,5);",
                @"IF NOT EXISTS (SELECT 1 FROM Users WHERE Role='Manager')
                  INSERT INTO Users (FirstName,LastName,Email,Phone,PasswordHash,Role)
                  VALUES ('Admin','Manager','admin@cafe101.com','0000000000',
                  '240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9','Manager');",
                @"IF NOT EXISTS (SELECT 1 FROM Users WHERE Role='Cashier')
                  INSERT INTO Users (FirstName,LastName,Email,Phone,PasswordHash,Role)
                  VALUES ('Jane','Smith','cashier@cafe101.com','0112345678',
                  '240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9','Cashier');",
                @"IF NOT EXISTS (SELECT 1 FROM Users WHERE Role='HeadChef')
                  INSERT INTO Users (FirstName,LastName,Email,Phone,PasswordHash,Role)
                  VALUES ('Chef','Khumalo','chef@cafe101.com','0112345679',
                  '240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9','HeadChef');",
                @"IF NOT EXISTS (SELECT 1 FROM Users WHERE Role='Owner')
                  INSERT INTO Users (FirstName,LastName,Email,Phone,PasswordHash,Role)
                  VALUES ('Owner','Admin','owner@cafe101.com','0112345680',
                  '240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9','Owner');",
                @"IF NOT EXISTS (SELECT 1 FROM Suppliers)
                  INSERT INTO Suppliers (CompanyName,ContactName,Phone,Email,Address)
                  VALUES ('Fresh Produce Co.','Thabo Nkosi','+27 11 555 0100','thabo@freshproduce.co.za','14 Industrial Rd, Johannesburg'),
                  ('SA Coffee Wholesalers','Priya Pillay','+27 21 555 0200','priya@sacoffee.co.za','8 Roastery Lane, Cape Town');",
                @"IF NOT EXISTS (SELECT 1 FROM Customers)
                  INSERT INTO Customers (FirstName,LastName,Email,Phone,Notes)
                  VALUES ('John','Doe','john.doe@email.com','0811234567','Regular - prefers window seat'),
                  ('Sarah','Mokoena','sarah.m@email.com','0729876543','Lactose intolerant'),
                  ('Walk-in','Customer',NULL,NULL,'Default walk-in');"
            };
            try { foreach (var s in scripts) ExecuteNonQuery(s); }
            catch (Exception ex)
            {
                MessageBox.Show("Database setup note:\n" + ex.Message, "Setup",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // ── AUTH ─────────────────────────────────────────────────
        public static string Login(string email, string password)
        {
            var dt = GetDataTable(
                "SELECT UserID, FirstName+' '+LastName AS FullName, Role " +
                "FROM Users WHERE Email=@e AND PasswordHash=@p AND IsActive=1",
                new SqlParameter("@e", email),
                new SqlParameter("@p", HashPassword(password)));
            if (dt.Rows.Count == 0) return null;
            CurrentUserId   = (int)dt.Rows[0]["UserID"];
            CurrentUserName = dt.Rows[0]["FullName"].ToString();
            CurrentUserRole = dt.Rows[0]["Role"].ToString();
            return CurrentUserRole;
        }

        public static bool RegisterUser(string firstName, string lastName, string email,
            string phone, string password, string role, string gender, DateTime? dob, string address)
        {
            if (Convert.ToInt32(ExecuteScalar(
                "SELECT COUNT(*) FROM Users WHERE Email=@e",
                new SqlParameter("@e", email))) > 0) return false;
            ExecuteNonQuery(
                "INSERT INTO Users (FirstName,LastName,Email,Phone,PasswordHash,Role,Gender,DateOfBirth,Address) " +
                "VALUES (@fn,@ln,@em,@ph,@pw,@ro,@ge,@db,@ad)",
                new SqlParameter("@fn", firstName),
                new SqlParameter("@ln", lastName),
                new SqlParameter("@em", email),
                new SqlParameter("@ph", (object)phone   ?? DBNull.Value),
                new SqlParameter("@pw", HashPassword(password)),
                new SqlParameter("@ro", role),
                new SqlParameter("@ge", (object)gender  ?? DBNull.Value),
                new SqlParameter("@db", dob.HasValue ? (object)dob.Value : DBNull.Value),
                new SqlParameter("@ad", (object)address ?? DBNull.Value));
            return true;
        }

        public static bool UpdatePassword(string email, string newPassword)
        {
            return ExecuteNonQuery(
                "UPDATE Users SET PasswordHash=@p WHERE Email=@e",
                new SqlParameter("@p", HashPassword(newPassword)),
                new SqlParameter("@e", email)) > 0;
        }

        public static string HashPassword(string password)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                var sb = new System.Text.StringBuilder();
                foreach (var b in bytes) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        // ── STAFF ────────────────────────────────────────────────
        public static DataTable GetAllStaff() =>
            GetDataTable("SELECT UserID, FirstName+' '+LastName AS FullName, Email, Phone, Role, " +
                         "ISNULL(Gender,'') AS Gender, IsActive, CONVERT(NVARCHAR,CreatedAt,103) AS JoinDate " +
                         "FROM Users ORDER BY Role, FirstName");

        public static DataTable GetStaff(string role = null)
        {
            string sql = "SELECT UserID, FirstName+' '+LastName AS FullName, Email, Phone, Role, IsActive " +
                         "FROM Users WHERE IsActive=1 " + (role != null ? "AND Role=@r " : "") + "ORDER BY FirstName";
            return role != null ? GetDataTable(sql, new SqlParameter("@r", role)) : GetDataTable(sql);
        }

        public static void UpdateStaff(int userId, string firstName, string lastName,
            string email, string phone, string role, bool isActive) =>
            ExecuteNonQuery(
                "UPDATE Users SET FirstName=@fn,LastName=@ln,Email=@em,Phone=@ph,Role=@ro,IsActive=@ia WHERE UserID=@id",
                new SqlParameter("@fn", firstName), new SqlParameter("@ln", lastName),
                new SqlParameter("@em", email), new SqlParameter("@ph", (object)phone ?? DBNull.Value),
                new SqlParameter("@ro", role), new SqlParameter("@ia", isActive),
                new SqlParameter("@id", userId));

        public static void DeactivateStaff(int userId) =>
            ExecuteNonQuery("UPDATE Users SET IsActive=0 WHERE UserID=@id", new SqlParameter("@id", userId));

        // ── CUSTOMERS ────────────────────────────────────────────
        public static DataTable GetCustomers(string search = null)
        {
            string sql = "SELECT CustomerID, FirstName+' '+LastName AS FullName, FirstName, LastName, " +
                         "ISNULL(Email,'') AS Email, ISNULL(Phone,'') AS Phone, " +
                         "ISNULL(Address,'') AS Address, ISNULL(Notes,'') AS Notes, " +
                         "CONVERT(NVARCHAR,CreatedAt,103) AS Since FROM Customers WHERE 1=1 ";
            if (!string.IsNullOrWhiteSpace(search))
                sql += "AND (FirstName+' '+LastName LIKE @s OR Email LIKE @s OR Phone LIKE @s) ";
            sql += "ORDER BY FirstName, LastName";
            return string.IsNullOrWhiteSpace(search) ? GetDataTable(sql)
                : GetDataTable(sql, new SqlParameter("@s", "%" + search + "%"));
        }

        public static void AddCustomer(string firstName, string lastName, string email,
            string phone, string address, string notes) =>
            ExecuteNonQuery(
                "INSERT INTO Customers (FirstName,LastName,Email,Phone,Address,Notes) VALUES (@fn,@ln,@em,@ph,@ad,@nt)",
                new SqlParameter("@fn", firstName), new SqlParameter("@ln", lastName),
                new SqlParameter("@em", (object)email   ?? DBNull.Value),
                new SqlParameter("@ph", (object)phone   ?? DBNull.Value),
                new SqlParameter("@ad", (object)address ?? DBNull.Value),
                new SqlParameter("@nt", (object)notes   ?? DBNull.Value));

        public static void UpdateCustomer(int id, string firstName, string lastName,
            string email, string phone, string address, string notes) =>
            ExecuteNonQuery(
                "UPDATE Customers SET FirstName=@fn,LastName=@ln,Email=@em,Phone=@ph,Address=@ad,Notes=@nt WHERE CustomerID=@id",
                new SqlParameter("@fn", firstName), new SqlParameter("@ln", lastName),
                new SqlParameter("@em", (object)email   ?? DBNull.Value),
                new SqlParameter("@ph", (object)phone   ?? DBNull.Value),
                new SqlParameter("@ad", (object)address ?? DBNull.Value),
                new SqlParameter("@nt", (object)notes   ?? DBNull.Value),
                new SqlParameter("@id", id));

        public static void DeleteCustomer(int id) =>
            ExecuteNonQuery("DELETE FROM Customers WHERE CustomerID=@id", new SqlParameter("@id", id));

        // ── MENU ─────────────────────────────────────────────────
        public static DataTable GetMenuItems(int? categoryId = null)
        {
            string sql = "SELECT m.ItemID, c.Name AS Category, m.Name, m.Description, " +
                         "m.Price, m.StockQty, m.MinStockQty FROM MenuItems m " +
                         "JOIN Categories c ON m.CategoryID=c.CategoryID WHERE m.IsAvailable=1 " +
                         (categoryId.HasValue ? "AND m.CategoryID=@cid " : "") + "ORDER BY c.Name, m.Name";
            return categoryId.HasValue ? GetDataTable(sql, new SqlParameter("@cid", categoryId.Value)) : GetDataTable(sql);
        }

        public static DataTable GetCategories() =>
            GetDataTable("SELECT CategoryID, Name FROM Categories WHERE IsActive=1 ORDER BY Name");

        public static void AddMenuItem(int catId, string name, string desc, decimal price, int stock, int minStock) =>
            ExecuteNonQuery(
                "INSERT INTO MenuItems (CategoryID,Name,Description,Price,StockQty,MinStockQty) VALUES (@cid,@nm,@ds,@pr,@sq,@ms)",
                new SqlParameter("@cid", catId), new SqlParameter("@nm", name),
                new SqlParameter("@ds", (object)desc ?? DBNull.Value),
                new SqlParameter("@pr", price), new SqlParameter("@sq", stock), new SqlParameter("@ms", minStock));

        public static void UpdateMenuItem(int itemId, string name, string desc,
            decimal price, int stock, int minStock, bool available) =>
            ExecuteNonQuery(
                "UPDATE MenuItems SET Name=@nm,Description=@ds,Price=@pr,StockQty=@sq,MinStockQty=@ms,IsAvailable=@av WHERE ItemID=@id",
                new SqlParameter("@nm", name), new SqlParameter("@ds", (object)desc ?? DBNull.Value),
                new SqlParameter("@pr", price), new SqlParameter("@sq", stock),
                new SqlParameter("@ms", minStock), new SqlParameter("@av", available),
                new SqlParameter("@id", itemId));

        public static void DeleteMenuItem(int itemId) =>
            ExecuteNonQuery("UPDATE MenuItems SET IsAvailable=0 WHERE ItemID=@id", new SqlParameter("@id", itemId));

        // ── ORDERS ───────────────────────────────────────────────
        public static int CreateOrder(string customerName, int cashierId, string orderType,
            string tableNumber, string paymentMethod, decimal subtotal, decimal vat, decimal total, string notes)
        {
            return Convert.ToInt32(ExecuteScalar(
                "INSERT INTO Orders (CustomerName,CashierID,TableNumber,OrderType,PaymentMethod," +
                "PaymentStatus,Subtotal,VAT,TotalAmount,Notes,Status) " +
                "OUTPUT INSERTED.OrderID " +
                "VALUES (@cn,@cid,@tn,@ot,@pm,'Paid',@sub,@vat,@tot,@nt,'Pending')",
                new SqlParameter("@cn",  customerName),
                new SqlParameter("@cid", cashierId),
                new SqlParameter("@tn",  string.IsNullOrWhiteSpace(tableNumber) ? (object)DBNull.Value : tableNumber),
                new SqlParameter("@ot",  orderType),
                new SqlParameter("@pm",  (object)paymentMethod ?? DBNull.Value),
                new SqlParameter("@sub", subtotal),
                new SqlParameter("@vat", vat),
                new SqlParameter("@tot", total),
                new SqlParameter("@nt",  (object)notes ?? DBNull.Value)));
        }

        public static void AddOrderItem(int orderId, int itemId, string itemName, int qty, decimal unitPrice)
        {
            ExecuteNonQuery(
                "INSERT INTO OrderItems (OrderID,ItemID,ItemName,Quantity,UnitPrice) VALUES (@oid,@iid,@nm,@qty,@up)",
                new SqlParameter("@oid", orderId), new SqlParameter("@iid", itemId),
                new SqlParameter("@nm",  itemName), new SqlParameter("@qty", qty),
                new SqlParameter("@up",  unitPrice));
            ExecuteNonQuery(
                "UPDATE MenuItems SET StockQty=StockQty-@q WHERE ItemID=@id AND StockQty>=@q",
                new SqlParameter("@q", qty), new SqlParameter("@id", itemId));
        }

        public static void UpdateOrderStatus(int orderId, string status) =>
            ExecuteNonQuery("UPDATE Orders SET Status=@s,UpdatedAt=GETDATE() WHERE OrderID=@id",
                new SqlParameter("@s", status), new SqlParameter("@id", orderId));

        public static DataTable GetOrdersByStatus(string status = null, DateTime? dateFrom = null)
        {
            string sql = "SELECT o.OrderID, o.CustomerName AS [Customer], " +
                         "o.OrderType AS [Type], o.Status, ISNULL(o.PaymentMethod,'—') AS [Payment], " +
                         "o.TotalAmount AS [Total R], CONVERT(NVARCHAR,o.CreatedAt,103)+' '+CONVERT(NVARCHAR(5),o.CreatedAt,108) AS [Date/Time] " +
                         "FROM Orders o WHERE 1=1 ";
            var ps = new System.Collections.Generic.List<SqlParameter>();
            if (status != null)    { sql += "AND o.Status=@st "; ps.Add(new SqlParameter("@st", status)); }
            if (dateFrom.HasValue) { sql += "AND CAST(o.CreatedAt AS DATE)>=@df "; ps.Add(new SqlParameter("@df", dateFrom.Value.Date)); }
            sql += "ORDER BY o.CreatedAt DESC";
            return GetDataTable(sql, ps.ToArray());
        }

        public static DataTable GetOrderItems(int orderId) =>
            GetDataTable("SELECT ItemName, Quantity, UnitPrice, Subtotal FROM OrderItems WHERE OrderID=@id",
                new SqlParameter("@id", orderId));

        public static DataTable GetKitchenOrders() =>
            GetDataTable(
                "SELECT o.OrderID, o.CustomerName AS Customer, o.OrderType AS Type, o.Status, " +
                "ISNULL(o.Notes,'') AS Notes, DATEDIFF(MINUTE,o.CreatedAt,GETDATE()) AS MinutesAgo " +
                "FROM Orders o WHERE o.Status IN ('Pending','Preparing') ORDER BY o.CreatedAt ASC");

        // ── SUPPLIERS ────────────────────────────────────────────
        public static DataTable GetSuppliers(string search = null)
        {
            string sql = "SELECT SupplierID, CompanyName, ISNULL(ContactName,'') AS ContactName, " +
                         "ISNULL(Phone,'') AS Phone, ISNULL(Email,'') AS Email, ISNULL(Address,'') AS Address " +
                         "FROM Suppliers WHERE IsActive=1 ";
            if (!string.IsNullOrWhiteSpace(search))
                sql += "AND (CompanyName LIKE @s OR ContactName LIKE @s OR Phone LIKE @s) ";
            sql += "ORDER BY CompanyName";
            return string.IsNullOrWhiteSpace(search) ? GetDataTable(sql)
                : GetDataTable(sql, new SqlParameter("@s", "%" + search + "%"));
        }

        public static void AddSupplier(string company, string contact, string phone, string email, string address) =>
            ExecuteNonQuery(
                "INSERT INTO Suppliers (CompanyName,ContactName,Phone,Email,Address) VALUES (@cn,@ct,@ph,@em,@ad)",
                new SqlParameter("@cn", company), new SqlParameter("@ct", (object)contact ?? DBNull.Value),
                new SqlParameter("@ph", (object)phone   ?? DBNull.Value),
                new SqlParameter("@em", (object)email   ?? DBNull.Value),
                new SqlParameter("@ad", (object)address ?? DBNull.Value));

        public static void UpdateSupplier(int id, string company, string contact, string phone, string email, string address) =>
            ExecuteNonQuery(
                "UPDATE Suppliers SET CompanyName=@cn,ContactName=@ct,Phone=@ph,Email=@em,Address=@ad WHERE SupplierID=@id",
                new SqlParameter("@cn", company), new SqlParameter("@ct", (object)contact ?? DBNull.Value),
                new SqlParameter("@ph", (object)phone   ?? DBNull.Value),
                new SqlParameter("@em", (object)email   ?? DBNull.Value),
                new SqlParameter("@ad", (object)address ?? DBNull.Value),
                new SqlParameter("@id", id));

        public static void DeleteSupplier(int id) =>
            ExecuteNonQuery("UPDATE Suppliers SET IsActive=0 WHERE SupplierID=@id", new SqlParameter("@id", id));

        // ── PURCHASE ORDERS ──────────────────────────────────────
        public static DataTable GetPurchaseOrders(string search = null)
        {
            string sql = "SELECT po.POID AS [PO #], s.CompanyName AS Supplier, po.ItemName AS Item, " +
                         "po.Quantity AS Qty, po.UnitPrice AS [Unit Price], po.ExpectedDate AS [Expected], " +
                         "po.Status, ISNULL(po.Notes,'') AS Notes, CONVERT(NVARCHAR,po.CreatedAt,103) AS [Created] " +
                         "FROM PurchaseOrders po JOIN Suppliers s ON po.SupplierID=s.SupplierID WHERE 1=1 ";
            if (!string.IsNullOrWhiteSpace(search))
                sql += "AND (po.ItemName LIKE @s OR s.CompanyName LIKE @s OR po.Status LIKE @s) ";
            sql += "ORDER BY po.CreatedAt DESC";
            return string.IsNullOrWhiteSpace(search) ? GetDataTable(sql)
                : GetDataTable(sql, new SqlParameter("@s", "%" + search + "%"));
        }

        public static void CreatePurchaseOrder(int supplierId, string itemName, int qty,
            decimal? unitPrice, DateTime expectedDate, string notes) =>
            ExecuteNonQuery(
                "INSERT INTO PurchaseOrders (SupplierID,ItemName,Quantity,UnitPrice,ExpectedDate,Notes) VALUES (@sid,@in,@qty,@up,@ed,@nt)",
                new SqlParameter("@sid", supplierId), new SqlParameter("@in", itemName),
                new SqlParameter("@qty", qty),
                new SqlParameter("@up",  unitPrice.HasValue ? (object)unitPrice.Value : DBNull.Value),
                new SqlParameter("@ed",  expectedDate.Date),
                new SqlParameter("@nt",  (object)notes ?? DBNull.Value));

        public static void UpdatePurchaseOrder(int poId, int supplierId, string itemName, int qty,
            decimal? unitPrice, DateTime expectedDate, string notes) =>
            ExecuteNonQuery(
                "UPDATE PurchaseOrders SET SupplierID=@sid,ItemName=@in,Quantity=@qty,UnitPrice=@up,ExpectedDate=@ed,Notes=@nt WHERE POID=@id",
                new SqlParameter("@sid", supplierId), new SqlParameter("@in", itemName),
                new SqlParameter("@qty", qty),
                new SqlParameter("@up",  unitPrice.HasValue ? (object)unitPrice.Value : DBNull.Value),
                new SqlParameter("@ed",  expectedDate.Date),
                new SqlParameter("@nt",  (object)notes ?? DBNull.Value),
                new SqlParameter("@id",  poId));

        public static void CancelPurchaseOrder(int poId) =>
            ExecuteNonQuery("UPDATE PurchaseOrders SET Status='Cancelled' WHERE POID=@id", new SqlParameter("@id", poId));

        public static void ReceivePurchaseOrder(int poId)
        {
            ExecuteNonQuery("UPDATE PurchaseOrders SET Status='Received' WHERE POID=@id", new SqlParameter("@id", poId));
            var row = GetDataTable("SELECT ItemName, Quantity FROM PurchaseOrders WHERE POID=@id", new SqlParameter("@id", poId));
            if (row.Rows.Count > 0)
                ExecuteNonQuery("UPDATE MenuItems SET StockQty=StockQty+@q WHERE Name=@nm",
                    new SqlParameter("@q", row.Rows[0]["Quantity"]),
                    new SqlParameter("@nm", row.Rows[0]["ItemName"]));
        }

        // ── REPORTS ──────────────────────────────────────────────
        public static DataTable GetDailySummary(DateTime date) =>
            GetDataTable(
                "SELECT COUNT(*) AS TotalOrders, ISNULL(SUM(TotalAmount),0) AS TotalRevenue, " +
                "SUM(CASE WHEN Status='Served' THEN 1 ELSE 0 END) AS Served, " +
                "SUM(CASE WHEN Status='Cancelled' THEN 1 ELSE 0 END) AS Cancelled, " +
                "SUM(CASE WHEN Status IN ('Pending','Preparing','Ready') THEN 1 ELSE 0 END) AS Active " +
                "FROM Orders WHERE CAST(CreatedAt AS DATE)=@d AND PaymentStatus='Paid'",
                new SqlParameter("@d", date.Date));

        public static DataTable GetWeeklySales() =>
            GetDataTable(
                "SELECT CAST(CreatedAt AS DATE) AS SaleDate, COUNT(*) AS Orders, ISNULL(SUM(TotalAmount),0) AS Revenue " +
                "FROM Orders WHERE CreatedAt>=DATEADD(DAY,-6,CAST(GETDATE() AS DATE)) AND PaymentStatus='Paid' " +
                "GROUP BY CAST(CreatedAt AS DATE) ORDER BY SaleDate");

        public static DataTable GetMonthlySales() =>
            GetDataTable(
                "SELECT CAST(CreatedAt AS DATE) AS SaleDate, COUNT(*) AS Orders, ISNULL(SUM(TotalAmount),0) AS Revenue " +
                "FROM Orders WHERE CreatedAt>=DATEADD(DAY,-29,CAST(GETDATE() AS DATE)) AND PaymentStatus='Paid' " +
                "GROUP BY CAST(CreatedAt AS DATE) ORDER BY SaleDate");

        public static DataTable GetTopSellingItems(int topN = 10) =>
            GetDataTable(
                "SELECT TOP(@n) oi.ItemName, SUM(oi.Quantity) AS TotalQty, SUM(oi.Subtotal) AS TotalRevenue " +
                "FROM OrderItems oi JOIN Orders o ON oi.OrderID=o.OrderID WHERE o.PaymentStatus='Paid' " +
                "GROUP BY oi.ItemName ORDER BY TotalQty DESC",
                new SqlParameter("@n", topN));

        public static DataTable GetLeastSellingItems(int topN = 5) =>
            GetDataTable(
                "SELECT TOP(@n) m.Name AS ItemName, ISNULL(SUM(oi.Quantity),0) AS TotalQty " +
                "FROM MenuItems m LEFT JOIN OrderItems oi ON m.ItemID=oi.ItemID " +
                "LEFT JOIN Orders o ON oi.OrderID=o.OrderID AND o.PaymentStatus='Paid' " +
                "WHERE m.IsAvailable=1 GROUP BY m.Name ORDER BY TotalQty ASC",
                new SqlParameter("@n", topN));

        public static DataTable GetLowStockItems() =>
            GetDataTable(
                "SELECT c.Name AS Category, m.ItemID, m.Name, m.StockQty, m.MinStockQty " +
                "FROM MenuItems m JOIN Categories c ON m.CategoryID=c.CategoryID " +
                "WHERE m.StockQty<=m.MinStockQty AND m.IsAvailable=1 ORDER BY m.StockQty ASC");

        public static DataTable GetRevenueByPaymentMethod(DateTime from, DateTime to) =>
            GetDataTable(
                "SELECT PaymentMethod, COUNT(*) AS Orders, SUM(TotalAmount) AS Revenue " +
                "FROM Orders WHERE CAST(CreatedAt AS DATE) BETWEEN @f AND @t AND PaymentStatus='Paid' " +
                "GROUP BY PaymentMethod",
                new SqlParameter("@f", from.Date), new SqlParameter("@t", to.Date));

        // ── OWNER DASHBOARD QUERIES ───────────────────────────────
        public static decimal GetDailySalesTotal() =>
            Convert.ToDecimal(ExecuteScalar(
                "SELECT ISNULL(SUM(TotalAmount),0) FROM Orders WHERE CAST(CreatedAt AS DATE)=CAST(GETDATE() AS DATE) AND PaymentStatus='Paid'") ?? 0);

        public static decimal GetWeeklySalesTotal() =>
            Convert.ToDecimal(ExecuteScalar(
                "SELECT ISNULL(SUM(TotalAmount),0) FROM Orders WHERE CreatedAt>=DATEADD(DAY,-6,CAST(GETDATE() AS DATE)) AND PaymentStatus='Paid'") ?? 0);

        public static decimal GetMonthlySalesTotal() =>
            Convert.ToDecimal(ExecuteScalar(
                "SELECT ISNULL(SUM(TotalAmount),0) FROM Orders WHERE CreatedAt>=DATEADD(DAY,-29,CAST(GETDATE() AS DATE)) AND PaymentStatus='Paid'") ?? 0);

        public static decimal GetAllTimeSalesTotal() =>
            Convert.ToDecimal(ExecuteScalar(
                "SELECT ISNULL(SUM(TotalAmount),0) FROM Orders WHERE PaymentStatus='Paid'") ?? 0);

        public static int GetNewCustomersThisWeek() =>
            Convert.ToInt32(ExecuteScalar(
                "SELECT COUNT(*) FROM Customers WHERE CreatedAt>=DATEADD(DAY,-6,CAST(GETDATE() AS DATE))") ?? 0);

        public static int GetNewCustomersThisMonth() =>
            Convert.ToInt32(ExecuteScalar(
                "SELECT COUNT(*) FROM Customers WHERE CreatedAt>=DATEADD(DAY,-29,CAST(GETDATE() AS DATE))") ?? 0);

        public static int GetTotalCustomers() =>
            Convert.ToInt32(ExecuteScalar("SELECT COUNT(*) FROM Customers") ?? 0);

        public static string GetMostPurchasedProduct()
        {
            var r = ExecuteScalar(
                "SELECT TOP 1 oi.ItemName FROM OrderItems oi JOIN Orders o ON oi.OrderID=o.OrderID " +
                "WHERE o.PaymentStatus='Paid' GROUP BY oi.ItemName ORDER BY SUM(oi.Quantity) DESC");
            return r?.ToString() ?? "N/A";
        }

        public static int GetTotalOrders() =>
            Convert.ToInt32(ExecuteScalar("SELECT COUNT(*) FROM Orders") ?? 0);

        public static int GetCompletedOrders() =>
            Convert.ToInt32(ExecuteScalar("SELECT COUNT(*) FROM Orders WHERE Status='Served'") ?? 0);

        public static int GetPendingOrders() =>
            Convert.ToInt32(ExecuteScalar("SELECT COUNT(*) FROM Orders WHERE Status IN ('Pending','Preparing','Ready')") ?? 0);

        public static int GetCancelledOrders() =>
            Convert.ToInt32(ExecuteScalar("SELECT COUNT(*) FROM Orders WHERE Status='Cancelled'") ?? 0);

        public static int GetTodaysOrderCount() =>
            Convert.ToInt32(ExecuteScalar(
                "SELECT COUNT(*) FROM Orders WHERE CAST(CreatedAt AS DATE)=CAST(GETDATE() AS DATE)") ?? 0);

        public static int GetTodaysCustomersServed() =>
            Convert.ToInt32(ExecuteScalar(
                "SELECT COUNT(DISTINCT CustomerName) FROM Orders WHERE CAST(CreatedAt AS DATE)=CAST(GETDATE() AS DATE) AND PaymentStatus='Paid'") ?? 0);

        public static int GetTodaysPendingOrders() =>
            Convert.ToInt32(ExecuteScalar(
                "SELECT COUNT(*) FROM Orders WHERE CAST(CreatedAt AS DATE)=CAST(GETDATE() AS DATE) AND Status IN ('Pending','Preparing','Ready')") ?? 0);

        // ── DATE-RANGE QUERIES (used by OwnerDashboard) ──────────

        public static DataTable GetSalesInRange(DateTime from, DateTime to) =>
            GetDataTable(
                "SELECT CAST(CreatedAt AS DATE) AS SaleDate, COUNT(*) AS Orders, " +
                "ISNULL(SUM(TotalAmount),0) AS Revenue " +
                "FROM Orders WHERE CAST(CreatedAt AS DATE) BETWEEN @from AND @to " +
                "AND PaymentStatus='Paid' " +
                "GROUP BY CAST(CreatedAt AS DATE) ORDER BY SaleDate",
                new SqlParameter("@from", from.Date),
                new SqlParameter("@to",   to.Date));

        public static int GetTotalOrdersInRange(DateTime from, DateTime to) =>
            Convert.ToInt32(ExecuteScalar(
                "SELECT COUNT(*) FROM Orders " +
                "WHERE CAST(CreatedAt AS DATE) BETWEEN @from AND @to",
                new SqlParameter("@from", from.Date),
                new SqlParameter("@to",   to.Date)) ?? 0);

        public static int GetCompletedOrdersInRange(DateTime from, DateTime to) =>
            Convert.ToInt32(ExecuteScalar(
                "SELECT COUNT(*) FROM Orders WHERE Status='Served' " +
                "AND CAST(CreatedAt AS DATE) BETWEEN @from AND @to",
                new SqlParameter("@from", from.Date),
                new SqlParameter("@to",   to.Date)) ?? 0);

        public static int GetCancelledOrdersInRange(DateTime from, DateTime to) =>
            Convert.ToInt32(ExecuteScalar(
                "SELECT COUNT(*) FROM Orders WHERE Status='Cancelled' " +
                "AND CAST(CreatedAt AS DATE) BETWEEN @from AND @to",
                new SqlParameter("@from", from.Date),
                new SqlParameter("@to",   to.Date)) ?? 0);
    }
}