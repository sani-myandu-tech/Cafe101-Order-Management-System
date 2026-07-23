using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;

namespace Cafe101
{
    public partial class Manager : Form
    {
        // ── Palette ───────────────────────────────────────────────
        static readonly Color Brown   = Color.FromArgb(111,  78,  55);
        static readonly Color BrownDk = Color.FromArgb( 74,  50,  37);
        static readonly Color Green   = Color.FromArgb( 76, 175,  80);
        static readonly Color Orange  = Color.FromArgb(230, 130,   0);
        static readonly Color Blue    = Color.FromArgb( 33, 150, 243);
        static readonly Color Purple  = Color.FromArgb(130,  70, 180);
        static readonly Color Red     = Color.FromArgb(210,  55,  45);
        static readonly Color Cream   = Color.FromArgb(248, 244, 238);

        // ── Fields ────────────────────────────────────────────────
        Label        _lblOrdersToday, _lblRevenue, _lblPending, _lblLowStock;
        DataGridView _dgLeft, _dgRight;
        Label        _lblLeftTitle, _lblRightTitle;
        Panel        _pnlContent, _pnlButtons, _pnlSearch, _pnlRightCell;
        Timer        _timer;
        string       _section = "overview";
        ToolTip      _tip = new ToolTip { AutoPopDelay = 5000, InitialDelay = 400 };

        // ─────────────────────────────────────────────────────────
        //  CONSTRUCTOR
        // ─────────────────────────────────────────────────────────
        public Manager()
        {
            InitializeComponent();
            BuildUI();
            this.Load += (s, e) => { LoadSection("overview"); RefreshStats(); };
        }

        // ─────────────────────────────────────────────────────────
        //  BUILD UI  — correct dock order: nav → stats → menu → search → [fill] → buttons
        // ─────────────────────────────────────────────────────────
        void BuildUI()
        {
            Text        = "Café 101  —  Manager  |  " + DatabaseHelper.CurrentUserName;
            WindowState = FormWindowState.Maximized;
            MinimumSize = new Size(1200, 700);
            BackColor   = Cream;
            Font        = new Font("Segoe UI", 10f);

            // WinForms docks Top panels in reverse-add order.
            // We add them last-first so they appear top-to-bottom in this order:
            //   1. Nav bar
            //   2. Stat cards
            //   3. Section button strip
            //   4. Search panel (hidden until needed)
            //   5. Action button panel (Bottom dock)
            //   6. Content panel (Fill)

            // ── 6. Content (Fill) — add first ──────────────────
            _pnlContent = new Panel { Dock = DockStyle.Fill, BackColor = Cream,
                Padding = new Padding(8, 4, 8, 4) };

            var split = new TableLayoutPanel {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2,
                BackColor = Color.Transparent
            };
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));
            split.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));
            split.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            _lblLeftTitle = new Label {
                Text = "Overview", Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = BrownDk, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(4, 0, 0, 0)
            };
            _lblRightTitle = new Label {
                Text = "", Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = BrownDk, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(4, 0, 0, 0)
            };

            _dgLeft  = MakeGrid();
            _dgRight = MakeGrid();

            _pnlRightCell = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            _pnlRightCell.Controls.Add(_dgRight);

            split.Controls.Add(_lblLeftTitle,  0, 0);
            split.Controls.Add(_lblRightTitle, 1, 0);
            split.Controls.Add(_dgLeft,        0, 1);
            split.Controls.Add(_pnlRightCell,  1, 1);

            _pnlContent.Controls.Add(split);
            Controls.Add(_pnlContent);

            // ── 5. Bottom action buttons — add second ──────────
            _pnlButtons = new Panel {
                Dock = DockStyle.Bottom, Height = 50,
                BackColor = Color.FromArgb(228, 218, 205),
                Padding = new Padding(6, 7, 6, 7)
            };
            Controls.Add(_pnlButtons);

            // ── 4. Search panel — add third (Top, hidden) ──────
            _pnlSearch = new Panel {
                Dock = DockStyle.Top, Height = 42,
                BackColor = Color.FromArgb(242, 238, 230),
                Padding = new Padding(8, 6, 8, 6),
                Visible = false
            };
            Controls.Add(_pnlSearch);

            // ── 3. Section button strip — add fourth (Top) ─────
            var pnlMenu = new Panel {
                Dock = DockStyle.Top, Height = 44,
                BackColor = Color.FromArgb(58, 38, 22)
            };
            string[] secs  = { "overview","orders","menu","inventory","suppliers","staff","reports" };
            string[] lbls  = { "📊  Overview","📋  Orders","🍽  Menu","📦  Inventory",
                               "🚚  Suppliers","👥  Staff","📈  Reports" };
            string[] tips  = { "Dashboard overview","Manage all orders","Manage menu items",
                               "View stock levels","Suppliers & POs","Manage staff","Sales reports" };
            int mx = 6;
            for (int i = 0; i < secs.Length; i++) {
                string sec = secs[i];
                var btn = new Button {
                    Text = lbls[i], Location = new Point(mx, 7), Size = new Size(118, 30),
                    BackColor = Color.FromArgb(80, 55, 32), ForeColor = Color.FromArgb(220, 200, 175),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold), Cursor = Cursors.Hand
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.Click += (s, e) => {
                    HighlightSectionBtn(pnlMenu, btn);
                    LoadSection(sec);
                };
                _tip.SetToolTip(btn, tips[i]);
                pnlMenu.Controls.Add(btn);
                mx += 122;
            }
            Controls.Add(pnlMenu);

            // ── 2. Stat cards — add fifth (Top) ────────────────
            var pnlCards = new TableLayoutPanel {
                Dock = DockStyle.Top, Height = 96,
                ColumnCount = 4, RowCount = 1,
                BackColor = Color.FromArgb(242, 238, 230),
                Padding = new Padding(8, 6, 8, 6)
            };
            for (int i = 0; i < 4; i++)
                pnlCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

            Color[] cc  = { Blue, Green, Orange, Red };
            string[] ct = { "Orders Today","Revenue Today","Pending Orders","Low Stock Items" };
            Label[]  vals = new Label[4];
            for (int i = 0; i < 4; i++) {
                int idx = i;
                var accent = cc[idx];
                var card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Margin = new Padding(4) };
                card.Paint += (s, e) => {
                    e.Graphics.FillRectangle(new SolidBrush(accent), 0, 0, 5, card.Height);
                    using (var p = new Pen(Color.FromArgb(215, 205, 192), 1f))
                        e.Graphics.DrawRectangle(p, 0, 0, card.Width - 1, card.Height - 1);
                };
                card.Controls.Add(new Label {
                    Text = ct[idx], Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(130, 110, 85), Location = new Point(12, 7), AutoSize = true
                });
                vals[idx] = new Label {
                    Text = "—", Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(40, 28, 16), Location = new Point(12, 26), AutoSize = true
                };
                card.Controls.Add(vals[idx]);
                pnlCards.Controls.Add(card, idx, 0);
            }
            _lblOrdersToday = vals[0]; _lblRevenue = vals[1];
            _lblPending = vals[2];     _lblLowStock = vals[3];
            Controls.Add(pnlCards);

            // ── 1. Nav bar — add last (Top) — appears at very top ─
            var nav = new Panel { Dock = DockStyle.Top, Height = 58, BackColor = BrownDk };

            nav.Controls.Add(new Label {
                Text = "☕  Café 101  —  Manager Dashboard",
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Color.White, AutoSize = true, Location = new Point(16, 14)
            });

            var btnHelp    = MakeNavBtn("❓  Help",      Color.FromArgb(55, 88, 145));
            var btnSignOut = MakeNavBtn("⬅  Sign Out",  Color.FromArgb(165, 48, 48));
            btnHelp.Click    += (s, e) => new HelpAbout().ShowDialog();
            btnSignOut.Click += (s, e) => { _timer?.Stop(); Hide(); new Form1().Show(); };
            _tip.SetToolTip(btnHelp,    "Help & About");
            _tip.SetToolTip(btnSignOut, "Sign out and return to login");

            var lblWelcome = new Label {
                Text = "Welcome,  " + DatabaseHelper.CurrentUserName,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 195, 155),
                AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            var lblRole = new Label {
                Text = "👤  Manager",
                Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(175, 148, 108),
                AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            nav.Controls.Add(lblWelcome);
            nav.Controls.Add(lblRole);
            nav.Controls.Add(btnHelp);
            nav.Controls.Add(btnSignOut);
            nav.Resize += (s, e) => {
                btnSignOut.Location = new Point(nav.Width - 134, 13);
                btnHelp.Location    = new Point(nav.Width - 272, 13);
                lblWelcome.Location = new Point(nav.Width - 272 - lblWelcome.Width - 18, 12);
                lblRole.Location    = new Point(nav.Width - 272 - lblRole.Width - 18, 33);
            };
            Controls.Add(nav);

            // ── Refresh timer ───────────────────────────────────
            _timer = new Timer { Interval = 60000 };
            _timer.Tick += (s, e) => RefreshStats();
            _timer.Start();
        }

        void HighlightSectionBtn(Panel strip, Button active)
        {
            foreach (Control c in strip.Controls)
                if (c is Button b) {
                    b.BackColor = Color.FromArgb(80, 55, 32);
                    b.ForeColor = Color.FromArgb(220, 200, 175);
                    b.Font      = new Font("Segoe UI", 9f, FontStyle.Regular);
                }
            active.BackColor = Brown;
            active.ForeColor = Color.White;
            active.Font      = new Font("Segoe UI", 9f, FontStyle.Bold);
        }

        // ─────────────────────────────────────────────────────────
        //  SECTION LOADER
        // ─────────────────────────────────────────────────────────
        void LoadSection(string section)
        {
            _section = section;
            ClearActionButtons();
            ClearSearchBoxes();

            // Restore both grids to their proper parents
            _dgLeft.Visible  = true;
            _dgLeft.Dock     = DockStyle.Fill;

            _pnlRightCell.Controls.Clear();
            _pnlRightCell.Controls.Add(_dgRight);
            _dgRight.Dock    = DockStyle.Fill;
            _dgRight.Visible = true;

            _dgLeft.DataSource  = null; _dgLeft.Columns.Clear();
            _dgRight.DataSource = null; _dgRight.Columns.Clear();
            _dgLeft.AutoGenerateColumns  = true;
            _dgRight.AutoGenerateColumns = true;

            try {
                switch (section) {

                case "overview":
                    _lblLeftTitle.Text  = "Today's Orders";
                    _lblRightTitle.Text = "Menu Stock Status";
                    FillGrid(_dgLeft,  DatabaseHelper.GetOrdersByStatus(null, DateTime.Today));
                    FillGrid(_dgRight, DatabaseHelper.GetMenuItems());
                    ColourStatus(_dgLeft);
                    ColourStock(_dgRight);
                    AddActionBtn("🖨  Print Summary", Purple, PrintDailySummary);
                    AddActionBtn("⟳  Refresh", Blue, () => LoadSection("overview"), 1);
                    break;

                case "orders":
                    _lblLeftTitle.Text  = "All Orders";
                    _lblRightTitle.Text = "Order Items  (click a row →)";
                    FillGrid(_dgLeft,
                        "SELECT o.OrderID AS [#], o.CustomerName AS [Customer], " +
                        "o.OrderType AS [Type], o.Status, " +
                        "ISNULL(o.PaymentMethod,'—') AS [Payment], " +
                        "o.TotalAmount AS [Total R], " +
                        "CONVERT(NVARCHAR,o.CreatedAt,103)+' '+CONVERT(NVARCHAR(5),o.CreatedAt,108) AS [Date] " +
                        "FROM Orders o ORDER BY o.CreatedAt DESC");
                    ColourStatus(_dgLeft);
                    _dgLeft.SelectionChanged += (s, e) => {
                        if (_dgLeft.SelectedRows.Count == 0) return;
                        try {
                            int oid = Convert.ToInt32(_dgLeft.SelectedRows[0].Cells["#"].Value);
                            FillGrid(_dgRight, DatabaseHelper.GetOrderItems(oid));
                        } catch { }
                    };
                    AddSearch("Search by customer name or status...", txt => {
                        FillGrid(_dgLeft,
                            "SELECT o.OrderID AS [#], o.CustomerName AS [Customer], " +
                            "o.Status, o.TotalAmount AS [Total R], " +
                            "CONVERT(NVARCHAR,o.CreatedAt,103) AS [Date] " +
                            "FROM Orders o WHERE o.CustomerName LIKE @s OR o.Status LIKE @s " +
                            "ORDER BY o.CreatedAt DESC",
                            new SqlParameter("@s", "%" + txt + "%"));
                        ColourStatus(_dgLeft);
                    });
                    AddActionBtn("✏  Update Status", Brown, () => {
                        if (_dgLeft.SelectedRows.Count == 0) { Msg("Select an order first."); return; }
                        int oid = Convert.ToInt32(_dgLeft.SelectedRows[0].Cells["#"].Value);
                        UpdateOrderStatusDialog(oid);
                        LoadSection("orders");
                    });
                    AddActionBtn("🖨  Print Receipt", Blue, () => {
                        if (_dgLeft.SelectedRows.Count == 0) { Msg("Select an order first."); return; }
                        int oid = Convert.ToInt32(_dgLeft.SelectedRows[0].Cells["#"].Value);
                        PrintOrderReceipt(oid);
                    }, 1);
                    AddActionBtn("🚚  Deliveries Only", Orange, () => {
                        _lblLeftTitle.Text = "Delivery Orders";
                        FillGrid(_dgLeft,
                            "SELECT o.OrderID AS [#], o.CustomerName AS [Customer], " +
                            "o.Status, ISNULL(o.Notes,'') AS [Address / Notes], " +
                            "o.TotalAmount AS [Total R], " +
                            "CONVERT(NVARCHAR,o.CreatedAt,103)+' '+CONVERT(NVARCHAR(5),o.CreatedAt,108) AS [Date] " +
                            "FROM Orders o WHERE o.OrderType='Delivery' ORDER BY o.CreatedAt DESC");
                        ColourDeliveryStatus(_dgLeft);
                    }, 2);
                    AddActionBtn("📋  All Orders", Color.FromArgb(80,80,80), () => LoadSection("orders"), 3);
                    break;

                case "menu":
                    _lblLeftTitle.Text  = "Menu Items";
                    _lblRightTitle.Text = "Add / Edit Item";
                    LoadMenuItems();
                    BuildMenuEditPanel();
                    AddSearch("Search menu items...", txt => LoadMenuItems(txt));
                    break;

                case "inventory":
                    _lblLeftTitle.Text  = "All Stock";
                    _lblRightTitle.Text = "Low Stock Alerts";
                    FillGrid(_dgLeft,
                        "SELECT c.Name AS Category, m.Name AS Item, " +
                        "m.StockQty AS [In Stock], m.MinStockQty AS [Min], " +
                        "CASE WHEN m.StockQty=0 THEN 'OUT OF STOCK' " +
                        "WHEN m.StockQty<=m.MinStockQty THEN 'LOW' ELSE 'OK' END AS Status " +
                        "FROM MenuItems m JOIN Categories c ON m.CategoryID=c.CategoryID " +
                        "WHERE m.IsAvailable=1 ORDER BY m.StockQty ASC");
                    ColourStock(_dgLeft);
                    FillGrid(_dgRight, DatabaseHelper.GetLowStockItems());
                    AddSearch("Search stock items...", txt => {
                        FillGrid(_dgLeft,
                            "SELECT c.Name AS Category, m.Name AS Item, " +
                            "m.StockQty AS [In Stock], m.MinStockQty AS [Min], " +
                            "CASE WHEN m.StockQty=0 THEN 'OUT OF STOCK' " +
                            "WHEN m.StockQty<=m.MinStockQty THEN 'LOW' ELSE 'OK' END AS Status " +
                            "FROM MenuItems m JOIN Categories c ON m.CategoryID=c.CategoryID " +
                            "WHERE m.IsAvailable=1 AND (m.Name LIKE @s OR c.Name LIKE @s) " +
                            "ORDER BY m.StockQty ASC",
                            new SqlParameter("@s", "%" + txt + "%"));
                        ColourStock(_dgLeft);
                    });
                    AddActionBtn("✏  Update Stock", Blue, () => {
                        if (_dgLeft.SelectedRows.Count == 0) { Msg("Select an item first."); return; }
                        string nm  = _dgLeft.SelectedRows[0].Cells["Item"].Value?.ToString();
                        string cur = _dgLeft.SelectedRows[0].Cells["In Stock"].Value?.ToString();
                        string inp = InputDialog.Show("New stock quantity for '" + nm + "':", "Update Stock", cur);
                        if (string.IsNullOrWhiteSpace(inp)) return;
                        if (!int.TryParse(inp, out int qty) || qty < 0) { Msg("Enter a valid non-negative number."); return; }
                        DatabaseHelper.ExecuteNonQuery("UPDATE MenuItems SET StockQty=@q WHERE Name=@n",
                            new SqlParameter("@q", qty), new SqlParameter("@n", nm));
                        LoadSection("inventory");
                    });
                    break;

                case "suppliers":
                    BuildSuppliersView();
                    break;

                case "staff":
                    _lblLeftTitle.Text  = "All Staff";
                    _lblRightTitle.Text = "Staff by Role";
                    FillGrid(_dgLeft, DatabaseHelper.GetAllStaff());
                    FillGrid(_dgRight,
                        "SELECT Role, COUNT(*) AS Total, " +
                        "SUM(CASE WHEN IsActive=1 THEN 1 ELSE 0 END) AS Active " +
                        "FROM Users GROUP BY Role ORDER BY Role");
                    AddSearch("Search staff by name, email or role...", txt => {
                        FillGrid(_dgLeft,
                            "SELECT UserID, FirstName+' '+LastName AS FullName, " +
                            "Email, Phone, Role, IsActive FROM Users " +
                            "WHERE FirstName+' '+LastName LIKE @s OR Email LIKE @s OR Role LIKE @s " +
                            "ORDER BY Role",
                            new SqlParameter("@s", "%" + txt + "%"));
                    });
                    AddActionBtn("➕ Add Staff",    Brown, () => { new Form2().ShowDialog(); LoadSection("staff"); });
                    AddActionBtn("✏ Edit Staff",    Blue,  () => {
                        if (_dgLeft.SelectedRows.Count == 0) { Msg("Select a staff member first."); return; }
                        ShowEditStaffDialog(Convert.ToInt32(_dgLeft.SelectedRows[0].Cells["UserID"].Value));
                        LoadSection("staff");
                    }, 1);
                    AddActionBtn("🚫 Deactivate",   Red,   () => {
                        if (_dgLeft.SelectedRows.Count == 0) { Msg("Select a staff member first."); return; }
                        string nm = _dgLeft.SelectedRows[0].Cells["FullName"].Value?.ToString();
                        if (MsgYN("Deactivate " + nm + "?\nThey will no longer be able to log in.")) {
                            DatabaseHelper.DeactivateStaff(Convert.ToInt32(_dgLeft.SelectedRows[0].Cells["UserID"].Value));
                            LoadSection("staff");
                        }
                    }, 2);
                    break;

                case "reports":
                    _lblLeftTitle.Text  = "7-Day Sales";
                    _lblRightTitle.Text = "Top 10 Best Sellers";
                    FillGrid(_dgLeft,  DatabaseHelper.GetWeeklySales());
                    FillGrid(_dgRight, DatabaseHelper.GetTopSellingItems(10));
                    _dgRight.DataBindingComplete -= FormatRevenueColumns;
                    _dgRight.DataBindingComplete += FormatRevenueColumns;
                    _dgLeft.DataBindingComplete  -= FormatRevenueColumns;
                    _dgLeft.DataBindingComplete  += FormatRevenueColumns;
                    break;
                }
                RefreshStats();
            }
            catch (Exception ex) {
                MessageBox.Show("Error loading section:\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ─────────────────────────────────────────────────────────
        //  GRID HELPERS
        // ─────────────────────────────────────────────────────────
        void FillGrid(DataGridView dg, string sql, params SqlParameter[] p)
        {
            dg.AutoGenerateColumns = true;
            dg.DataSource = DatabaseHelper.GetDataTable(sql, p);
        }

        void FillGrid(DataGridView dg, DataTable dt)
        {
            dg.AutoGenerateColumns = true;
            dg.DataSource = dt;
        }


        // ─────────────────────────────────────────────────────────
        //  SUPPLIERS — Landing page with two sub-sections
        // ─────────────────────────────────────────────────────────
        void BuildSuppliersView()
        {
            // Left grid = Purchase Orders or Suppliers list
            // Right cell = tab strip + detail grid for clicked row
            // Both live within the existing TableLayoutPanel — no layout disruption

            _dgLeft.Visible  = true;
            _dgLeft.Dock     = DockStyle.Fill;
            _dgRight.Visible = false;
            _pnlRightCell.Controls.Clear();

            // ── Right panel: tab strip + detail grid ─────────────
            var pnlRight = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };

            // Tab strip (Top)
            var tabStrip = new Panel { Dock = DockStyle.Top, Height = 40,
                BackColor = Color.FromArgb(228, 218, 205) };

            var btnPO = new Button {
                Text = "📋  Purchase Orders", Size = new Size(188, 30), Location = new Point(6, 5),
                BackColor = Brown, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), Cursor = Cursors.Hand };
            btnPO.FlatAppearance.BorderSize = 0;

            var btnSup = new Button {
                Text = "🏢  Manage Suppliers", Size = new Size(188, 30), Location = new Point(200, 5),
                BackColor = Color.FromArgb(150, 118, 88), ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular), Cursor = Cursors.Hand };
            btnSup.FlatAppearance.BorderSize = 0;

            tabStrip.Controls.Add(btnPO);
            tabStrip.Controls.Add(btnSup);
            pnlRight.Controls.Add(tabStrip);

            // Detail grid title
            var lblDetail = new Label {
                Dock = DockStyle.Top, Height = 26,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = BrownDk,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(4,0,0,0),
                BackColor = Color.FromArgb(248, 244, 238)
            };
            pnlRight.Controls.Add(lblDetail);

            // Detail grid (Fill — add first so tab and label dock above it)
            var dgDetail = MakeGrid();
            dgDetail.Dock = DockStyle.Fill;
            pnlRight.Controls.Add(dgDetail);

            _pnlRightCell.Controls.Add(pnlRight);

            // ── Search bar reuse ──────────────────────────────────
            bool showingPOs = true;
            var txtSrch = new TextBox {
                Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11f),
                BorderStyle = BorderStyle.FixedSingle,
                Text = "Search purchase orders...", ForeColor = Color.Gray
            };
            txtSrch.GotFocus  += (s, e) => { if (txtSrch.ForeColor == Color.Gray) { txtSrch.Text = ""; txtSrch.ForeColor = Color.Black; } };
            txtSrch.LostFocus += (s, e) => {
                if (string.IsNullOrWhiteSpace(txtSrch.Text)) {
                    txtSrch.ForeColor = Color.Gray;
                    txtSrch.Text = showingPOs ? "Search purchase orders..." : "Search suppliers...";
                }
            };
            _pnlSearch.Controls.Clear();
            _pnlSearch.Controls.Add(txtSrch);
            _pnlSearch.Visible = true;

            // ── Load data helpers ─────────────────────────────────
            Action loadPOs = () => {
                _lblLeftTitle.Text = "Purchase Orders";
                lblDetail.Text     = "  Details  (click a row)";
                string q = txtSrch.ForeColor == Color.Gray ? "" : txtSrch.Text.Trim();
                FillGrid(_dgLeft, DatabaseHelper.GetPurchaseOrders(q.Length >= 2 ? q : ""));
                ColourPOStatus(_dgLeft);
                dgDetail.DataSource = null; dgDetail.Columns.Clear();
            };

            Action loadSuppliers = () => {
                _lblLeftTitle.Text = "Suppliers";
                lblDetail.Text     = "  Purchase Orders for Supplier  (click a row)";
                string q = txtSrch.ForeColor == Color.Gray ? "" : txtSrch.Text.Trim();
                FillGrid(_dgLeft, DatabaseHelper.GetSuppliers(q.Length >= 2 ? q : ""));
                dgDetail.DataSource = null; dgDetail.Columns.Clear();
            };

            // ── Row click → show detail ───────────────────────────
            EventHandler selChanged = null;
            selChanged = (s, e) => {
                if (_dgLeft.SelectedRows.Count == 0) return;
                try {
                    if (showingPOs) {
                        var cell = _dgLeft.SelectedRows[0].Cells["PO #"];
                        if (cell?.Value == null) return;
                        int poId2 = Convert.ToInt32(cell.Value);
                        FillGrid(dgDetail, DatabaseHelper.GetDataTable(
                            "SELECT po.ItemName AS [Item], po.Quantity AS [Qty], " +
                            "ISNULL(CAST(po.UnitPrice AS NVARCHAR),'—') AS [Unit Price], " +
                            "CONVERT(NVARCHAR,po.ExpectedDate,103) AS [Expected Date], " +
                            "po.Status, s.CompanyName AS [Supplier], " +
                            "ISNULL(po.Notes,'') AS [Notes] " +
                            "FROM PurchaseOrders po JOIN Suppliers s ON po.SupplierID=s.SupplierID " +
                            "WHERE po.POID=@id", new SqlParameter("@id", poId2)));
                        ColourPOStatus(dgDetail);
                    } else {
                        var cell = _dgLeft.SelectedRows[0].Cells["SupplierID"];
                        if (cell?.Value == null) return;
                        int sid = Convert.ToInt32(cell.Value);
                        FillGrid(dgDetail, DatabaseHelper.GetDataTable(
                            "SELECT po.POID AS [PO #], po.ItemName AS [Item], " +
                            "po.Quantity AS [Qty], po.Status, " +
                            "CONVERT(NVARCHAR,po.ExpectedDate,103) AS [Expected], " +
                            "ISNULL(po.Notes,'') AS [Notes] " +
                            "FROM PurchaseOrders po WHERE po.SupplierID=@sid ORDER BY po.CreatedAt DESC",
                            new SqlParameter("@sid", sid)));
                        ColourPOStatus(dgDetail);
                    }
                } catch { }
            };
            _dgLeft.SelectionChanged += selChanged;

            // ── Search ────────────────────────────────────────────
            txtSrch.TextChanged += (s, e) => {
                if (txtSrch.ForeColor == Color.Gray) return;
                if (showingPOs) loadPOs(); else loadSuppliers();
            };

            // ── Switch view ───────────────────────────────────────
            Action<bool> switchV = (toPOs) => {
                showingPOs = toPOs;
                ClearActionButtons();
                txtSrch.ForeColor = Color.Gray;
                txtSrch.Text      = toPOs ? "Search purchase orders..." : "Search suppliers...";

                btnPO.BackColor  = toPOs ? Brown : Color.FromArgb(150, 118, 88);
                btnPO.Font       = new Font("Segoe UI", 9.5f, toPOs ? FontStyle.Bold : FontStyle.Regular);
                btnSup.BackColor = toPOs ? Color.FromArgb(150, 118, 88) : Brown;
                btnSup.Font      = new Font("Segoe UI", 9.5f, toPOs ? FontStyle.Regular : FontStyle.Bold);

                if (toPOs) {
                    loadPOs();
                    AddActionBtn("➕ New PO", Brown, () => { ShowPODialog(null); loadPOs(); });
                    AddActionBtn("✏ Edit PO", Orange, () => {
                        if (_dgLeft.SelectedRows.Count == 0) { Msg("Select a PO first."); return; }
                        try { ShowPODialog(Convert.ToInt32(_dgLeft.SelectedRows[0].Cells["PO #"].Value)); } catch { Msg("Could not read PO ID."); return; }
                        loadPOs();
                    }, 1);
                    AddActionBtn("✔ Receive PO", Green, () => {
                        if (_dgLeft.SelectedRows.Count == 0) { Msg("Select a PO first."); return; }
                        int id = Convert.ToInt32(_dgLeft.SelectedRows[0].Cells["PO #"].Value);
                        if (MsgYN("Mark as Received? Stock will be updated.")) { DatabaseHelper.ReceivePurchaseOrder(id); loadPOs(); }
                    }, 2);
                    AddActionBtn("❌ Cancel PO", Red, () => {
                        if (_dgLeft.SelectedRows.Count == 0) { Msg("Select a PO first."); return; }
                        int id = Convert.ToInt32(_dgLeft.SelectedRows[0].Cells["PO #"].Value);
                        if (MsgYN("Cancel this Purchase Order?")) { DatabaseHelper.CancelPurchaseOrder(id); loadPOs(); }
                    }, 3);
                } else {
                    loadSuppliers();
                    AddActionBtn("➕ Add Supplier", Brown, () => { ShowSupplierDialog(null); loadSuppliers(); });
                    AddActionBtn("✏ Edit Supplier", Blue, () => {
                        if (_dgLeft.SelectedRows.Count == 0) { Msg("Select a supplier first."); return; }
                        ShowSupplierDialog(Convert.ToInt32(_dgLeft.SelectedRows[0].Cells["SupplierID"].Value));
                        loadSuppliers();
                    }, 1);
                    AddActionBtn("🗑 Remove", Red, () => {
                        if (_dgLeft.SelectedRows.Count == 0) { Msg("Select a supplier first."); return; }
                        string nm = _dgLeft.SelectedRows[0].Cells["CompanyName"].Value?.ToString();
                        if (MsgYN("Deactivate supplier '" + nm + "'?")) {
                            DatabaseHelper.DeleteSupplier(Convert.ToInt32(_dgLeft.SelectedRows[0].Cells["SupplierID"].Value));
                            loadSuppliers();
                        }
                    }, 2);
                }
            };

            btnPO.Click  += (s, e) => switchV(true);
            btnSup.Click += (s, e) => switchV(false);
            switchV(true);
        }

        static void ColourPOStatus(DataGridView dg)
        {
            foreach (DataGridViewRow row in dg.Rows) {
                string st = "";
                foreach (DataGridViewCell c in row.Cells)
                    if (c.OwningColumn.HeaderText == "Status") { st = c.Value?.ToString() ?? ""; break; }
                row.DefaultCellStyle.BackColor =
                    st == "Received"  ? Color.FromArgb(210, 248, 210) :
                    st == "Cancelled" ? Color.FromArgb(255, 215, 215) :
                    st == "Pending"   ? Color.FromArgb(255, 246, 210) : Color.White;
                row.DefaultCellStyle.ForeColor = Color.FromArgb(30, 20, 10);
            }
        }

        static void AddSuppBtn(Panel bar, string text, Color bg, int slot, Action action)
        {
            var b = new Button { Text = text, Size = new Size(178, 36),
                Location = new Point(6 + slot * 186, 7),
                BackColor = bg, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0;
            b.Click += (s, e) => action();
            bar.Controls.Add(b);
        }

                void LoadMenuItems(string search = "")
        {
            string sql =
                "SELECT m.ItemID AS [ID], c.Name AS [Category], m.Name AS [Item], " +
                "m.Price AS [Price R], m.StockQty AS [Stock], " +
                "CASE WHEN m.StockQty=0 THEN 'Out of Stock' " +
                "WHEN m.StockQty<=m.MinStockQty THEN 'Low Stock' ELSE 'OK' END AS [Status], " +
                "CASE m.IsAvailable WHEN 1 THEN 'Yes' ELSE 'No' END AS [Available] " +
                "FROM MenuItems m JOIN Categories c ON m.CategoryID=c.CategoryID ";
            if (!string.IsNullOrWhiteSpace(search))
                sql += "WHERE m.Name LIKE @s OR c.Name LIKE @s ";
            sql += "ORDER BY c.Name, m.Name";
            FillGrid(_dgLeft, string.IsNullOrWhiteSpace(search) ? DatabaseHelper.GetDataTable(sql)
                : DatabaseHelper.GetDataTable(sql, new SqlParameter("@s", "%" + search + "%")));
            ColourStock(_dgLeft);
        }

        // ─────────────────────────────────────────────────────────
        //  MENU EDIT PANEL
        // ─────────────────────────────────────────────────────────
        void BuildMenuEditPanel()
        {
            _dgRight.Visible    = false;
            _lblRightTitle.Text = "Add / Edit Menu Item";
            _pnlRightCell.Controls.Clear();

            var pnl = new Panel {
                Dock = DockStyle.Fill, BackColor = Color.White,
                AutoScroll = true, Padding = new Padding(14, 10, 14, 10)
            };
            _pnlRightCell.Controls.Add(pnl);

            var inner = new FlowLayoutPanel {
                AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown, WrapContents = false,
                BackColor = Color.White, Location = new Point(14, 10)
            };
            pnl.Controls.Add(inner);
            pnl.Resize += (s, e) => inner.Width = Math.Max(100, pnl.ClientSize.Width - 28);
            inner.Width = 300;

            Control AddField(string lbl, Control ctrl) {
                var lb = new Label {
                    Text = lbl, AutoSize = false, Size = new Size(inner.Width, 20),
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(70, 50, 35), Margin = new Padding(0, 8, 0, 2)
                };
                ctrl.Width  = inner.Width;
                ctrl.Margin = new Padding(0, 0, 0, 4);
                pnl.Resize += (s, e) => { lb.Width = Math.Max(100, pnl.ClientSize.Width - 28); ctrl.Width = lb.Width; };
                inner.Controls.Add(lb);
                inner.Controls.Add(ctrl);
                return ctrl;
            }

            var cats   = DatabaseHelper.GetCategories();
            var cmbCat = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10f) };
            foreach (DataRow r in cats.Rows) cmbCat.Items.Add(new CatItem(r["Name"].ToString(), Convert.ToInt32(r["CategoryID"])));
            cmbCat.DisplayMember = "Name";
            if (cmbCat.Items.Count > 0) cmbCat.SelectedIndex = 0;
            _tip.SetToolTip(cmbCat, "Select the item's category");
            AddField("Category:", cmbCat);

            var tName  = (TextBox)AddField("Item Name: *",     new TextBox { Font = new Font("Segoe UI", 10f) });
            var tDesc  = (TextBox)AddField("Description:",     new TextBox { Font = new Font("Segoe UI", 10f) });
            var tPrice = (TextBox)AddField("Price (R): *",     new TextBox { Font = new Font("Segoe UI", 10f) });
            var tStock = (TextBox)AddField("Stock Qty: *",     new TextBox { Font = new Font("Segoe UI", 10f), Text = "0" });
            var tMin   = (TextBox)AddField("Min Stock Alert:", new TextBox { Font = new Font("Segoe UI", 10f), Text = "5" });
            _tip.SetToolTip(tName,  "Item name (required)");
            _tip.SetToolTip(tPrice, "Price in Rands e.g. 45.00 (required)");
            _tip.SetToolTip(tStock, "Current stock quantity");
            _tip.SetToolTip(tMin,   "System warns when stock drops below this value");

            var chkAvail = new CheckBox {
                Text = "Item Available for ordering", AutoSize = true,
                Font = new Font("Segoe UI", 9.5f), Checked = true, Margin = new Padding(0, 6, 0, 4)
            };
            inner.Controls.Add(chkAvail);

            var lblMsg = new Label {
                AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Visible = false, Margin = new Padding(0, 4, 0, 4)
            };
            inner.Controls.Add(lblMsg);

            int? editId = null;

            var btnLoad = MakeEditBtn("✏  LOAD SELECTED FOR EDITING", Color.FromArgb(215, 205, 192), BrownDk);
            _tip.SetToolTip(btnLoad, "Select a row in the left grid then click here to edit it");
            pnl.Resize += (s, e) => btnLoad.Width = Math.Max(100, pnl.ClientSize.Width - 28);
            inner.Controls.Add(btnLoad);

            var btnSave = MakeEditBtn("➕  ADD NEW ITEM", Green, Color.White);
            btnSave.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            btnSave.Height = 42;
            _tip.SetToolTip(btnSave, "Save this item to the menu");
            pnl.Resize += (s, e) => btnSave.Width = Math.Max(100, pnl.ClientSize.Width - 28);
            inner.Controls.Add(btnSave);

            var btnDel = MakeEditBtn("🗑  DELETE SELECTED", Red, Color.White);
            _tip.SetToolTip(btnDel, "Remove the selected item from the menu");
            pnl.Resize += (s, e) => btnDel.Width = Math.Max(100, pnl.ClientSize.Width - 28);
            inner.Controls.Add(btnDel);

            btnLoad.Click += (s, e) => {
                if (_dgLeft.SelectedRows.Count == 0) { ShowMsg(lblMsg, "Select a row in the left grid first.", true); return; }
                int iid = Convert.ToInt32(_dgLeft.SelectedRows[0].Cells["ID"].Value);
                var dt  = DatabaseHelper.GetDataTable(
                    "SELECT m.*, c.Name AS CatName FROM MenuItems m " +
                    "JOIN Categories c ON m.CategoryID=c.CategoryID WHERE m.ItemID=@id",
                    new SqlParameter("@id", iid));
                if (dt.Rows.Count == 0) return;
                var row = dt.Rows[0];
                editId = iid;
                tName.Text  = row["Name"].ToString();
                tDesc.Text  = row["Description"].ToString();
                tPrice.Text = row["Price"].ToString();
                tStock.Text = row["StockQty"].ToString();
                tMin.Text   = row["MinStockQty"].ToString();
                chkAvail.Checked = Convert.ToBoolean(row["IsAvailable"]);
                string cn = row["CatName"].ToString();
                for (int i = 0; i < cmbCat.Items.Count; i++)
                    if (((CatItem)cmbCat.Items[i]).Name == cn) { cmbCat.SelectedIndex = i; break; }
                btnSave.Text = "💾  SAVE CHANGES"; btnSave.BackColor = Blue;
                ShowMsg(lblMsg, "Editing: " + tName.Text, false);
            };

            btnSave.Click += (s, e) => {
                if (string.IsNullOrWhiteSpace(tName.Text))                       { ShowMsg(lblMsg, "Item name is required.", true); return; }
                if (!decimal.TryParse(tPrice.Text, out decimal price) || price < 0) { ShowMsg(lblMsg, "Enter a valid price (e.g. 45.00).", true); return; }
                if (!int.TryParse(tStock.Text, out int stock) || stock < 0)      { ShowMsg(lblMsg, "Enter a valid stock quantity.", true); return; }
                if (!int.TryParse(tMin.Text,   out int min)   || min < 0)        { ShowMsg(lblMsg, "Enter a valid minimum stock.", true); return; }
                var sel = (CatItem)cmbCat.SelectedItem;
                if (editId.HasValue)
                    DatabaseHelper.UpdateMenuItem(editId.Value, tName.Text.Trim(), tDesc.Text.Trim(), price, stock, min, chkAvail.Checked);
                else
                    DatabaseHelper.AddMenuItem(sel.Id, tName.Text.Trim(), tDesc.Text.Trim(), price, stock, min);
                ShowMsg(lblMsg, "✔  Saved: " + tName.Text, false);
                LoadMenuItems();
                editId = null; tName.Clear(); tDesc.Clear(); tPrice.Clear();
                tStock.Text = "0"; tMin.Text = "5";
                btnSave.Text = "➕  ADD NEW ITEM"; btnSave.BackColor = Green;
            };

            btnDel.Click += (s, e) => {
                if (_dgLeft.SelectedRows.Count == 0) { ShowMsg(lblMsg, "Select an item from the left grid.", true); return; }
                string nm = _dgLeft.SelectedRows[0].Cells["Item"].Value?.ToString();
                int iid   = Convert.ToInt32(_dgLeft.SelectedRows[0].Cells["ID"].Value);
                if (MsgYN("Remove '" + nm + "' from the menu?")) {
                    DatabaseHelper.DeleteMenuItem(iid); LoadMenuItems();
                    ShowMsg(lblMsg, "✔  Removed.", false);
                }
            };
        }

        class CatItem { public string Name; public int Id; public CatItem(string n, int i) { Name = n; Id = i; } public override string ToString() => Name; }

        // ─────────────────────────────────────────────────────────
        //  STATS REFRESH
        // ─────────────────────────────────────────────────────────
        void RefreshStats()
        {
            try {
                _lblOrdersToday.Text = DatabaseHelper.ExecuteScalar(
                    "SELECT COUNT(*) FROM Orders WHERE CAST(CreatedAt AS DATE)=CAST(GETDATE() AS DATE)").ToString();
                object rev = DatabaseHelper.ExecuteScalar(
                    "SELECT ISNULL(SUM(TotalAmount),0) FROM Orders " +
                    "WHERE CAST(CreatedAt AS DATE)=CAST(GETDATE() AS DATE) AND PaymentStatus='Paid'");
                _lblRevenue.Text  = "R " + Convert.ToDecimal(rev).ToString("N0");
                _lblPending.Text  = DatabaseHelper.ExecuteScalar(
                    "SELECT COUNT(*) FROM Orders WHERE Status IN ('Pending','Preparing')").ToString();
                _lblLowStock.Text = DatabaseHelper.ExecuteScalar(
                    "SELECT COUNT(*) FROM MenuItems WHERE StockQty<=MinStockQty AND IsAvailable=1").ToString();
            } catch { }
        }

        // ─────────────────────────────────────────────────────────
        //  PRINTING
        // ─────────────────────────────────────────────────────────
        string _printContent = "";

        void DoPrint(string content, string title)
        {
            _printContent = content;
            var pd = new PrintDocument { DocumentName = title };
            pd.PrintPage += (s, e) => {
                var g = e.Graphics;
                var fNorm = new Font("Courier New", 9f);
                var fBold = new Font("Courier New", 10f, FontStyle.Bold);
                float y = e.MarginBounds.Top, x = e.MarginBounds.Left;
                float lh = fNorm.GetHeight(g) + 1f;
                foreach (var line in _printContent.Split('\n')) {
                    if (y + lh > e.MarginBounds.Bottom) { e.HasMorePages = true; break; }
                    bool bold = line.StartsWith("═") || line.StartsWith("CAFÉ") || line.StartsWith("TOTAL");
                    g.DrawString(line.TrimEnd('\r'), bold ? fBold : fNorm, Brushes.Black, x, y);
                    y += lh;
                }
                fNorm.Dispose(); fBold.Dispose();
            };
            using (var dlg = new PrintDialog { Document = pd })
                if (dlg.ShowDialog() == DialogResult.OK) pd.Print();
        }

        void PrintOrderReceipt(int orderId)
        {
            try {
                var oDt = DatabaseHelper.GetDataTable("SELECT * FROM Orders WHERE OrderID=@id", new SqlParameter("@id", orderId));
                if (oDt.Rows.Count == 0) { Msg("Order not found."); return; }
                var o = oDt.Rows[0]; var items = DatabaseHelper.GetOrderItems(orderId);
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("═══════════════════════════════════");
                sb.AppendLine("            CAFÉ 101               ");
                sb.AppendLine("       Order / Delivery Receipt    ");
                sb.AppendLine("═══════════════════════════════════");
                sb.AppendLine($"Order #:    {orderId}");
                sb.AppendLine($"Customer:   {o["CustomerName"]}");
                sb.AppendLine($"Type:       {o["OrderType"]}");
                sb.AppendLine($"Status:     {o["Status"]}");
                sb.AppendLine($"Payment:    {o["PaymentMethod"]}");
                sb.AppendLine($"Date/Time:  {Convert.ToDateTime(o["CreatedAt"]):dd/MM/yyyy HH:mm}");
                sb.AppendLine($"Printed:    {DateTime.Now:dd/MM/yyyy HH:mm}");
                sb.AppendLine("───────────────────────────────────");
                sb.AppendLine($"{"Item",-22} {"Qty",4} {"Price",8} {"Total",9}");
                sb.AppendLine("───────────────────────────────────");
                foreach (DataRow r in items.Rows) {
                    string nm = r["ItemName"].ToString();
                    int    qty = Convert.ToInt32(r["Quantity"]);
                    decimal up = Convert.ToDecimal(r["UnitPrice"]);
                    decimal st = Convert.ToDecimal(r["Subtotal"]);
                    string nms = nm.Length > 22 ? nm.Substring(0, 21) + "." : nm;
                    sb.AppendLine($"{nms,-22} {qty,4} {"R"+up.ToString("N2"),8} {"R"+st.ToString("N2"),9}");
                }
                sb.AppendLine("───────────────────────────────────");
                sb.AppendLine($"{"Subtotal:",-30} {"R"+Convert.ToDecimal(o["Subtotal"]).ToString("N2"),6}");
                sb.AppendLine($"{"VAT (15%):",-30} {"R"+Convert.ToDecimal(o["VAT"]).ToString("N2"),6}");
                sb.AppendLine($"{"TOTAL DUE:",-30} {"R"+Convert.ToDecimal(o["TotalAmount"]).ToString("N2"),6}");
                sb.AppendLine("═══════════════════════════════════");
                sb.AppendLine("      Thank you for your order!    ");
                sb.AppendLine("          Café 101  ☕            ");
                sb.AppendLine("═══════════════════════════════════");
                DoPrint(sb.ToString(), $"Order #{orderId} Receipt");
            } catch (Exception ex) { Msg("Print error:\n" + ex.Message); }
        }

        void PrintDailySummary()
        {
            try {
                var sum = DatabaseHelper.GetDailySummary(DateTime.Today);
                var top = DatabaseHelper.GetTopSellingItems(5);
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("═══════════════════════════════════");
                sb.AppendLine("            CAFÉ 101               ");
                sb.AppendLine("       Daily Sales Summary         ");
                sb.AppendLine("═══════════════════════════════════");
                sb.AppendLine($"Date:    {DateTime.Today:dd MMMM yyyy}");
                sb.AppendLine($"Printed: {DateTime.Now:dd/MM/yyyy HH:mm}");
                sb.AppendLine("───────────────────────────────────");
                if (sum.Rows.Count > 0) {
                    var r = sum.Rows[0];
                    sb.AppendLine($"Total Orders:    {r["TotalOrders"]}");
                    sb.AppendLine($"Orders Served:   {r["Served"]}");
                    sb.AppendLine($"Active Orders:   {r["Active"]}");
                    sb.AppendLine($"Cancelled:       {r["Cancelled"]}");
                    sb.AppendLine($"TOTAL REVENUE:   R {Convert.ToDecimal(r["TotalRevenue"]):N2}");
                }
                sb.AppendLine("───────────────────────────────────");
                sb.AppendLine("Top 5 Items Today:");
                foreach (DataRow r in top.Rows) sb.AppendLine($"  {r["ItemName"],-24} x{r["TotalQty"]}");
                sb.AppendLine("═══════════════════════════════════");
                DoPrint(sb.ToString(), "Daily Summary");
            } catch (Exception ex) { Msg("Print error:\n" + ex.Message); }
        }

        void PrintWeeklyReport()
        {
            try {
                var data = DatabaseHelper.GetWeeklySales();
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("═══════════════════════════════════");
                sb.AppendLine("            CAFÉ 101               ");
                sb.AppendLine("        Weekly Sales Report        ");
                sb.AppendLine("═══════════════════════════════════");
                sb.AppendLine($"Period:  {DateTime.Today.AddDays(-6):dd MMM} – {DateTime.Today:dd MMM yyyy}");
                sb.AppendLine($"Printed: {DateTime.Now:dd/MM/yyyy HH:mm}");
                sb.AppendLine("───────────────────────────────────");
                sb.AppendLine($"{"Date",-14} {"Orders",7} {"Revenue",12}");
                sb.AppendLine("───────────────────────────────────");
                decimal total = 0;
                foreach (DataRow r in data.Rows) {
                    decimal rev = Convert.ToDecimal(r["Revenue"]); total += rev;
                    sb.AppendLine($"{Convert.ToDateTime(r["SaleDate"]):dd/MM/yyyy,-14} {r["Orders"],7} {"R"+rev.ToString("N2"),12}");
                }
                sb.AppendLine("───────────────────────────────────");
                sb.AppendLine($"{"WEEKLY TOTAL:",-22} {"R"+total.ToString("N2"),12}");
                sb.AppendLine("═══════════════════════════════════");
                DoPrint(sb.ToString(), "Weekly Sales Report");
            } catch (Exception ex) { Msg("Print error:\n" + ex.Message); }
        }

        void PrintMonthlyReport()
        {
            try {
                var data = DatabaseHelper.GetMonthlySales();
                var top  = DatabaseHelper.GetTopSellingItems(10);
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("═══════════════════════════════════");
                sb.AppendLine("            CAFÉ 101               ");
                sb.AppendLine("       Monthly Sales Report        ");
                sb.AppendLine("═══════════════════════════════════");
                sb.AppendLine($"Period:  {DateTime.Today.AddDays(-29):dd MMM} – {DateTime.Today:dd MMM yyyy}");
                sb.AppendLine($"Printed: {DateTime.Now:dd/MM/yyyy HH:mm}");
                sb.AppendLine("───────────────────────────────────");
                decimal total = 0;
                foreach (DataRow r in data.Rows) {
                    decimal rev = Convert.ToDecimal(r["Revenue"]); total += rev;
                    sb.AppendLine($"{Convert.ToDateTime(r["SaleDate"]):dd/MM/yyyy,-14} {r["Orders"],7} {"R"+rev.ToString("N2"),12}");
                }
                sb.AppendLine("───────────────────────────────────");
                sb.AppendLine($"{"MONTHLY TOTAL:",-22} {"R"+total.ToString("N2"),12}");
                sb.AppendLine("");
                sb.AppendLine("TOP 10 PRODUCTS:");
                foreach (DataRow r in top.Rows)
                    sb.AppendLine($"  {r["ItemName"],-24} Qty:{r["TotalQty"],4}  R{Convert.ToDecimal(r["TotalRevenue"]):N2}");
                sb.AppendLine("═══════════════════════════════════");
                DoPrint(sb.ToString(), "Monthly Sales Report");
            } catch (Exception ex) { Msg("Print error:\n" + ex.Message); }
        }

        // ─────────────────────────────────────────────────────────
        //  SEARCH BOX
        // ─────────────────────────────────────────────────────────
        void AddSearch(string placeholder, Action<string> onSearch)
        {
            _pnlSearch.Controls.Clear();
            _pnlSearch.Visible = true;
            var txt = new TextBox {
                Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11f),
                BorderStyle = BorderStyle.FixedSingle, Text = placeholder, ForeColor = Color.Gray
            };
            _tip.SetToolTip(txt, "Type to filter results in real time (2+ characters)");
            txt.GotFocus  += (s, e) => { if (txt.ForeColor == Color.Gray) { txt.Text = ""; txt.ForeColor = Color.Black; } };
            txt.LostFocus += (s, e) => { if (string.IsNullOrEmpty(txt.Text)) { txt.Text = placeholder; txt.ForeColor = Color.Gray; } };
            txt.TextChanged += (s, e) => {
                string q = txt.ForeColor == Color.Gray ? "" : txt.Text.Trim();
                if (q.Length == 0 || q.Length >= 2) onSearch(q);
            };
            _pnlSearch.Controls.Add(txt);
        }

        void ClearSearchBoxes() { _pnlSearch.Controls.Clear(); _pnlSearch.Visible = false; }

        // ─────────────────────────────────────────────────────────
        //  ACTION BUTTONS
        // ─────────────────────────────────────────────────────────
        void AddActionBtn(string text, Color colour, Action action, int slot = 0)
        {
            var btn = new Button {
                Text = text, Size = new Size(182, 36),
                Location = new Point(6 + slot * 190, 7),
                BackColor = colour, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (s, e) => action();
            _pnlButtons.Controls.Add(btn);
        }

        void AddActionBtnRight(string text, Color colour, Action action, int slot = 0)
        {
            var btn = new Button {
                Text = text, Size = new Size(162, 36),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = colour, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (s, e) => action();
            _pnlButtons.Controls.Add(btn);
            btn.Location = new Point(_pnlButtons.Width - 170 - slot * 170, 7);
            _pnlButtons.Resize += (s, e) => btn.Location = new Point(_pnlButtons.Width - 170 - slot * 170, 7);
        }

        void ClearActionButtons() { _pnlButtons.Controls.Clear(); }

        // ─────────────────────────────────────────────────────────
        //  DIALOGS
        // ─────────────────────────────────────────────────────────
        void UpdateOrderStatusDialog(int orderId)
        {
            var f = DlgForm("Update Order #" + orderId + " Status", 360, 210);
            f.Controls.Add(new Label { Text = "New status for Order #" + orderId + ":",
                Location = new Point(20, 16), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) });
            var cmb = new ComboBox { Location = new Point(20, 40), Size = new Size(310, 30),
                DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 11f) };
            cmb.Items.AddRange(new object[] { "Pending", "Preparing", "Ready", "Out for Delivery", "Delivered", "Served", "Cancelled" });
            cmb.SelectedIndex = 0;
            _tip.SetToolTip(cmb, "Select the new order status");
            f.Controls.Add(cmb);
            var btn = DlgBtn("✔  UPDATE STATUS", Brown, new Point(20, 88), new Size(310, 44));
            btn.Click += (s, e) => {
                if (cmb.SelectedItem == null) { MessageBox.Show("Please select a status."); return; }
                DatabaseHelper.UpdateOrderStatus(orderId, cmb.Text); f.Close();
            };
            f.Controls.Add(btn);
            f.ShowDialog();
        }

        void ShowPODialog(int? poId)
        {
            var f = DlgForm(poId.HasValue ? "Edit Purchase Order" : "New Purchase Order", 480, 460);
            var supps = DatabaseHelper.GetSuppliers();
            int y = 18; int lx = 18; int w = 424;

            f.Controls.Add(new Label { Text = "Supplier: *", Location = new Point(lx, y), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) }); y += 22;
            var cmbS = new ComboBox { Location = new Point(lx, y), Size = new Size(w, 30),
                DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10f) };
            foreach (DataRow r in supps.Rows) cmbS.Items.Add(new SuppItem(r["CompanyName"].ToString(), Convert.ToInt32(r["SupplierID"])));
            cmbS.DisplayMember = "Name"; if (cmbS.Items.Count > 0) cmbS.SelectedIndex = 0;
            f.Controls.Add(cmbS); y += 38;

            TextBox FT(string lbl, string tip = null) {
                f.Controls.Add(new Label { Text = lbl, Location = new Point(lx, y), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) }); y += 22;
                var t = new TextBox { Location = new Point(lx, y), Size = new Size(w, 30), Font = new Font("Segoe UI", 10f), BorderStyle = BorderStyle.FixedSingle };
                if (tip != null) _tip.SetToolTip(t, tip);
                f.Controls.Add(t); y += 38; return t;
            }

            var tItem = FT("Item Name: *",     "Name of the item being ordered");
            var tQty  = FT("Quantity: *",       "Number of units (whole number)");
            var tUp   = FT("Unit Price (R):",   "Optional — estimated unit cost");

            f.Controls.Add(new Label { Text = "Expected Delivery Date: *", Location = new Point(lx, y), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) }); y += 22;
            var dtp = new DateTimePicker { Location = new Point(lx, y), Size = new Size(w, 30), Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(3) };
            f.Controls.Add(dtp); y += 38;

            var tNotes = FT("Notes:", "Optional notes");
            var lblErr = new Label { Location = new Point(lx, y), AutoSize = true, ForeColor = Color.FromArgb(180, 50, 50), Font = new Font("Segoe UI", 9f, FontStyle.Bold), Visible = false };
            f.Controls.Add(lblErr);

            if (poId.HasValue) {
                var dt = DatabaseHelper.GetDataTable("SELECT * FROM PurchaseOrders WHERE POID=@id", new SqlParameter("@id", poId.Value));
                if (dt.Rows.Count > 0) {
                    var r = dt.Rows[0];
                    tItem.Text = r["ItemName"].ToString(); tQty.Text = r["Quantity"].ToString();
                    tUp.Text = r["UnitPrice"] is DBNull ? "" : r["UnitPrice"].ToString();
                    dtp.Value = Convert.ToDateTime(r["ExpectedDate"]); tNotes.Text = r["Notes"].ToString();
                    int sid = Convert.ToInt32(r["SupplierID"]);
                    for (int i = 0; i < cmbS.Items.Count; i++) if (((SuppItem)cmbS.Items[i]).Id == sid) { cmbS.SelectedIndex = i; break; }
                }
            }

            var btn = DlgBtn(poId.HasValue ? "💾  SAVE CHANGES" : "➕  CREATE ORDER", Brown, new Point(lx, y + 28), new Size(w, 44));
            btn.Click += (s, e) => {
                lblErr.Visible = false;
                // Supplier
                if (cmbS.SelectedItem == null) { lblErr.Text = "⚠  Please select a supplier."; lblErr.Visible = true; return; }
                // Item name
                if (string.IsNullOrWhiteSpace(tItem.Text)) { lblErr.Text = "⚠  Item name is required."; lblErr.Visible = true; tItem.Focus(); return; }
                if (tItem.Text.Trim().Length < 2) { lblErr.Text = "⚠  Item name must be at least 2 characters."; lblErr.Visible = true; tItem.Focus(); return; }
                // Quantity
                if (string.IsNullOrWhiteSpace(tQty.Text)) { lblErr.Text = "⚠  Quantity is required."; lblErr.Visible = true; tQty.Focus(); return; }
                if (!int.TryParse(tQty.Text, out int qty) || qty <= 0) { lblErr.Text = "⚠  Quantity must be a positive whole number (e.g. 10)."; lblErr.Visible = true; tQty.Focus(); return; }
                if (qty > 10000) { lblErr.Text = "⚠  Quantity seems too large. Please verify."; lblErr.Visible = true; tQty.Focus(); return; }
                // Unit price (optional but must be valid number if filled)
                decimal? up = null;
                if (!string.IsNullOrWhiteSpace(tUp.Text)) {
                    if (!decimal.TryParse(tUp.Text, out decimal upv) || upv < 0) { lblErr.Text = "⚠  Unit price must be a valid positive number (e.g. 45.00)."; lblErr.Visible = true; tUp.Focus(); return; }
                    up = upv;
                }
                // Delivery date must be today or future
                if (dtp.Value.Date < DateTime.Today) { lblErr.Text = "⚠  Expected delivery date cannot be in the past."; lblErr.Visible = true; return; }
                int sid2 = ((SuppItem)cmbS.SelectedItem).Id;
                if (poId.HasValue) DatabaseHelper.UpdatePurchaseOrder(poId.Value, sid2, tItem.Text.Trim(), qty, up, dtp.Value, tNotes.Text.Trim());
                else               DatabaseHelper.CreatePurchaseOrder(sid2, tItem.Text.Trim(), qty, up, dtp.Value, tNotes.Text.Trim());
                f.Close();
            };
            f.Controls.Add(btn);
            f.ClientSize = new Size(480, y + 112);
            f.ShowDialog();
        }
        class SuppItem { public string Name; public int Id; public SuppItem(string n, int i) { Name = n; Id = i; } public override string ToString() => Name; }

        void ShowSupplierDialog(int? suppId)
        {
            var f = DlgForm(suppId.HasValue ? "Edit Supplier" : "Add New Supplier", 480, 400);
            int y = 18; int lx = 18; int w = 424;
            var lblErr = new Label { AutoSize = true, ForeColor = Color.FromArgb(180, 50, 50), Font = new Font("Segoe UI", 9f, FontStyle.Bold), Visible = false };

            TextBox FT(string lbl, string tip = null) {
                f.Controls.Add(new Label { Text = lbl, Location = new Point(lx, y), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) }); y += 22;
                var t = new TextBox { Location = new Point(lx, y), Size = new Size(w, 30), Font = new Font("Segoe UI", 10f), BorderStyle = BorderStyle.FixedSingle };
                if (tip != null) _tip.SetToolTip(t, tip);
                f.Controls.Add(t); y += 38; return t;
            }

            var tCo = FT("Company Name: *", "Required"); var tCt = FT("Contact Name:");
            var tPh = FT("Phone:");          var tEm = FT("Email:");
            var tAd = FT("Address: *");
            // Phone: digits, spaces, hyphens, plus only
            tPh.KeyPress += (s, e) => { if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != ' ' && e.KeyChar != '-' && e.KeyChar != '+' && e.KeyChar != '(' && e.KeyChar != ')') e.Handled = true; };

            if (suppId.HasValue) {
                var dt = DatabaseHelper.GetDataTable("SELECT * FROM Suppliers WHERE SupplierID=@id", new SqlParameter("@id", suppId.Value));
                if (dt.Rows.Count > 0) {
                    var r = dt.Rows[0];
                    tCo.Text = r["CompanyName"].ToString(); tCt.Text = r["ContactName"].ToString();
                    tPh.Text = r["Phone"].ToString();       tEm.Text = r["Email"].ToString();
                    tAd.Text = r["Address"].ToString();
                }
            }

            lblErr.Location = new Point(lx, y); f.Controls.Add(lblErr);
            var btn = DlgBtn(suppId.HasValue ? "💾  SAVE CHANGES" : "➕  ADD SUPPLIER", Brown, new Point(lx, y + 10), new Size(w, 44));
            btn.Click += (s, e) => {
                lblErr.Visible = false;
                // Company name
                if (string.IsNullOrWhiteSpace(tCo.Text)) { lblErr.Text = "⚠  Company name is required."; lblErr.Visible = true; tCo.Focus(); return; }
                if (tCo.Text.Trim().Length < 2) { lblErr.Text = "⚠  Company name must be at least 2 characters."; lblErr.Visible = true; tCo.Focus(); return; }
                // Contact name (optional but if filled must be letters only)
                if (!string.IsNullOrWhiteSpace(tCt.Text)) {
                    if (!System.Text.RegularExpressions.Regex.IsMatch(tCt.Text.Trim(), @"^[A-Za-z\s\-']+$")) {
                        lblErr.Text = "⚠  Contact name may only contain letters, spaces or hyphens."; lblErr.Visible = true; tCt.Focus(); return; }
                }
                // Phone (optional but if filled must start with 0 and be 10 digits)
                if (!string.IsNullOrWhiteSpace(tPh.Text)) {
                    string dig = System.Text.RegularExpressions.Regex.Replace(tPh.Text.Trim(), @"[\s\-\(\)\+]", "");
                    if (!System.Text.RegularExpressions.Regex.IsMatch(dig, @"^\d+$")) {
                        lblErr.Text = "⚠  Phone must contain digits only."; lblErr.Visible = true; tPh.Focus(); return; }
                    if (dig.Length < 9 || dig.Length > 13) {
                        lblErr.Text = "⚠  Phone number length is invalid (9–13 digits)."; lblErr.Visible = true; tPh.Focus(); return; }
                    if (dig.StartsWith("0") && dig.Length != 10) {
                        lblErr.Text = "⚠  Local phone numbers must be exactly 10 digits (e.g. 0821234567)."; lblErr.Visible = true; tPh.Focus(); return; }
                }
                // Email (optional but if filled must be valid)
                if (!string.IsNullOrWhiteSpace(tEm.Text)) {
                    if (!System.Text.RegularExpressions.Regex.IsMatch(tEm.Text.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$")) {
                        lblErr.Text = "⚠  Enter a valid email address (e.g. supplier@company.co.za)."; lblErr.Visible = true; tEm.Focus(); return; }
                }
                // Address compulsory
                if (string.IsNullOrWhiteSpace(tAd.Text)) { lblErr.Text = "⚠  Address is required."; lblErr.Visible = true; tAd.Focus(); return; }
                if (tAd.Text.Trim().Length < 5) { lblErr.Text = "⚠  Please enter a full address (at least 5 characters)."; lblErr.Visible = true; tAd.Focus(); return; }

                if (suppId.HasValue) DatabaseHelper.UpdateSupplier(suppId.Value, tCo.Text.Trim(), tCt.Text.Trim(), tPh.Text.Trim(), tEm.Text.Trim(), tAd.Text.Trim());
                else                 DatabaseHelper.AddSupplier(tCo.Text.Trim(), tCt.Text.Trim(), tPh.Text.Trim(), tEm.Text.Trim(), tAd.Text.Trim());
                f.Close();
            };
            f.Controls.Add(btn);
            f.ClientSize = new Size(480, y + 104);
            f.ShowDialog();
        }

        void ShowEditStaffDialog(int userId)
        {
            var f = DlgForm("Edit Staff Member", 480, 460);
            int y = 18; int lx = 18; int w = 424;
            var lblErr = new Label { AutoSize = true, ForeColor = Color.FromArgb(180, 50, 50), Font = new Font("Segoe UI", 9f, FontStyle.Bold), Visible = false };

            TextBox FT(string lbl, string tip = null) {
                f.Controls.Add(new Label { Text = lbl, Location = new Point(lx, y), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) }); y += 22;
                var t = new TextBox { Location = new Point(lx, y), Size = new Size(w, 30), Font = new Font("Segoe UI", 10f), BorderStyle = BorderStyle.FixedSingle };
                if (tip != null) _tip.SetToolTip(t, tip);
                f.Controls.Add(t); y += 38; return t;
            }

            var tFn = FT("First Name: *"); var tLn = FT("Last Name: *");
            var tEm = FT("Email: *", "Valid email address"); var tPh = FT("Phone:");

            f.Controls.Add(new Label { Text = "Role: *", Location = new Point(lx, y), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) }); y += 22;
            var cmbR = new ComboBox { Location = new Point(lx, y), Size = new Size(w, 30), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10f) };
            cmbR.Items.AddRange(new object[] { "Cashier", "HeadChef", "Manager", "Owner" });
            f.Controls.Add(cmbR); y += 38;

            var chkA = new CheckBox { Text = "Account Active", Location = new Point(lx, y), AutoSize = true, Font = new Font("Segoe UI", 9.5f), Checked = true };
            f.Controls.Add(chkA); y += 34;

            var dt = DatabaseHelper.GetDataTable("SELECT * FROM Users WHERE UserID=@id", new SqlParameter("@id", userId));
            if (dt.Rows.Count > 0) {
                var r = dt.Rows[0];
                tFn.Text = r["FirstName"].ToString(); tLn.Text = r["LastName"].ToString();
                tEm.Text = r["Email"].ToString();     tPh.Text = r["Phone"].ToString();
                chkA.Checked = Convert.ToBoolean(r["IsActive"]);
                int idx = cmbR.Items.IndexOf(r["Role"].ToString()); if (idx >= 0) cmbR.SelectedIndex = idx;
            }

            lblErr.Location = new Point(lx, y); f.Controls.Add(lblErr); y += 24;
            var btn = DlgBtn("💾  SAVE CHANGES", Blue, new Point(lx, y), new Size(w, 44));
            btn.Click += (s, e) => {
                lblErr.Visible = false;
                if (string.IsNullOrWhiteSpace(tFn.Text) || string.IsNullOrWhiteSpace(tLn.Text)) { lblErr.Text = "First and last name are required."; lblErr.Visible = true; return; }
                if (!tEm.Text.Contains("@") || !tEm.Text.Contains("."))                          { lblErr.Text = "Enter a valid email address.";       lblErr.Visible = true; return; }
                if (cmbR.SelectedItem == null)                                                    { lblErr.Text = "Please select a role.";               lblErr.Visible = true; return; }
                DatabaseHelper.UpdateStaff(userId, tFn.Text.Trim(), tLn.Text.Trim(), tEm.Text.Trim(), tPh.Text.Trim(), cmbR.Text, chkA.Checked);
                f.Close();
            };
            f.Controls.Add(btn);
            f.ClientSize = new Size(480, y + 70);
            f.ShowDialog();
        }

        // ─────────────────────────────────────────────────────────
        //  COLOURING
        // ─────────────────────────────────────────────────────────
        static void FormatRevenueColumns(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            if (sender is DataGridView dg)
                foreach (DataGridViewColumn col in dg.Columns)
                    if (col.Name.IndexOf("Revenue", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        col.Name.IndexOf("Amount", StringComparison.OrdinalIgnoreCase) >= 0  ||
                        col.Name.IndexOf("Price",  StringComparison.OrdinalIgnoreCase) >= 0  ||
                        col.Name.IndexOf("Total",  StringComparison.OrdinalIgnoreCase) >= 0)
                        col.DefaultCellStyle.Format = "N2";
        }

        static void ColourStatus(DataGridView dg)
        {
            foreach (DataGridViewRow row in dg.Rows) {
                string st = "";
                foreach (DataGridViewCell c in row.Cells)
                    if (c.OwningColumn.HeaderText == "Status") { st = c.Value?.ToString() ?? ""; break; }
                row.DefaultCellStyle.BackColor =
                    st == "Served"          ? Color.FromArgb(210, 248, 210) :
                    st == "Delivered"       ? Color.FromArgb(190, 240, 210) :
                    st == "Ready"           ? Color.FromArgb(200, 225, 255) :
                    st == "Out for Delivery"? Color.FromArgb(255, 235, 190) :
                    st == "Preparing"       ? Color.FromArgb(255, 246, 210) :
                    st == "Cancelled"       ? Color.FromArgb(255, 215, 215) : Color.White;
                row.DefaultCellStyle.ForeColor = Color.FromArgb(30, 20, 10);
            }
        }

        static void ColourDeliveryStatus(DataGridView dg)
        {
            foreach (DataGridViewRow row in dg.Rows) {
                string st = "";
                foreach (DataGridViewCell c in row.Cells)
                    if (c.OwningColumn.HeaderText == "Status") { st = c.Value?.ToString() ?? ""; break; }
                row.DefaultCellStyle.BackColor =
                    st == "Delivered"        ? Color.FromArgb(190, 240, 210) :
                    st == "Out for Delivery" ? Color.FromArgb(255, 230, 170) :
                    st == "Ready"            ? Color.FromArgb(200, 225, 255) :
                    st == "Preparing"        ? Color.FromArgb(255, 246, 210) :
                    st == "Cancelled"        ? Color.FromArgb(255, 215, 215) : Color.White;
                // Bold font for active deliveries
                row.DefaultCellStyle.ForeColor = Color.FromArgb(30, 20, 10);
                if (st == "Out for Delivery")
                    row.DefaultCellStyle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            }
        }

        static void ColourStock(DataGridView dg)
        {
            foreach (DataGridViewRow row in dg.Rows) {
                string st = "";
                foreach (DataGridViewCell c in row.Cells)
                    if (c.OwningColumn.HeaderText == "Status") { st = c.Value?.ToString() ?? ""; break; }
                row.DefaultCellStyle.BackColor =
                    st.Contains("OUT") || st.Contains("Out") ? Color.FromArgb(255, 210, 210) :
                    st == "LOW"        || st.Contains("Low") ? Color.FromArgb(255, 245, 205) :
                                                               Color.FromArgb(220, 250, 220);
                row.DefaultCellStyle.ForeColor = Color.FromArgb(30, 20, 10);
            }
        }

        // ─────────────────────────────────────────────────────────
        //  UTILITY
        // ─────────────────────────────────────────────────────────
        DataGridView MakeGrid()
        {
            var dg = new DataGridView {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                ReadOnly = true, RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10f),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeight = 34, RowTemplate = { Height = 30 }
            };
            dg.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle {
                BackColor = Color.FromArgb(62, 40, 22), ForeColor = Color.FromArgb(255, 220, 160),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(4, 0, 0, 0)
            };
            dg.DefaultCellStyle.SelectionBackColor = Color.FromArgb(180, 120, 40);
            dg.DefaultCellStyle.SelectionForeColor = Color.White;
            dg.DefaultCellStyle.ForeColor          = Color.FromArgb(35, 24, 14);
            dg.DefaultCellStyle.BackColor          = Color.White;
            dg.DefaultCellStyle.Padding            = new Padding(4, 0, 0, 0);
            dg.EnableHeadersVisualStyles = false;
            dg.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 238, 228);
            dg.AlternatingRowsDefaultCellStyle.ForeColor = Color.FromArgb(35, 24, 14);
            dg.GridColor       = Color.FromArgb(210, 195, 175);
            dg.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dg.RowTemplate.Height = 32;
            return dg;
        }

        static Button MakeNavBtn(string text, Color bg)
        {
            var b = new Button { Text = text, Size = new Size(130, 32),
                BackColor = bg, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold), Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0; return b;
        }

        static Button MakeEditBtn(string text, Color bg, Color fg)
        {
            var b = new Button { Text = text, Height = 34, BackColor = bg, ForeColor = fg,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand, Margin = new Padding(0, 4, 0, 4) };
            b.FlatAppearance.BorderSize = 0; return b;
        }

        static void ShowMsg(Label lbl, string msg, bool err) {
            lbl.Text = msg;
            lbl.ForeColor = err ? Color.FromArgb(180, 50, 50) : Color.FromArgb(45, 130, 55);
            lbl.Visible = true;
        }
        static void Msg(string m) => MessageBox.Show(m, "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
        static bool MsgYN(string m) => MessageBox.Show(m, "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        static Form DlgForm(string t, int w, int h) => new Form {
            Text = t, Size = new Size(w, h), StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false,
            BackColor = Color.White, Font = new Font("Segoe UI", 10f)
        };
        static Button DlgBtn(string t, Color bg, Point loc, Size sz) {
            var b = new Button { Text = t, Location = loc, Size = sz, BackColor = bg,
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold), Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0; return b;
        }
    }
}
