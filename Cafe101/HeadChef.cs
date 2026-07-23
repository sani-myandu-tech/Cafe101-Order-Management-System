using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace Cafe101
{
    public partial class HeadChef : Form
    {
        static readonly Color BgDark      = Color.FromArgb( 18,  12,   6);
        static readonly Color CardDark    = Color.FromArgb( 32,  22,  12);
        static readonly Color Gold        = Color.FromArgb(255, 193,   7);
        static readonly Color GreenAcc    = Color.FromArgb( 76, 175,  80);
        static readonly Color OrangeAcc   = Color.FromArgb(255, 152,   0);
        static readonly Color BlueAcc     = Color.FromArgb( 33, 150, 243);
        static readonly Color RedAcc      = Color.FromArgb(244,  67,  54);
        static readonly Color TextMuted   = Color.FromArgb(160, 130, 100);
        static readonly Color TextBright  = Color.FromArgb(240, 225, 205);

        DataGridView _dgOrders, _dgItems;
        Label        _lblSelected, _lblClock, _lblStats;
        Panel        _pnlActions;
        Timer        _refreshTimer, _clockTimer;
        int          _selectedOrderId = -1;

        public HeadChef()
        {
            InitializeComponent();
            BuildUI();
            LoadOrders();
            StartTimers();
        }

        // ═════════════════════════════════════════════════════════
        //  BUILD UI — nav added LAST so it docks to the very top
        // ═════════════════════════════════════════════════════════
        void BuildUI()
        {
            Text        = $"Café 101  —  Kitchen Display  |  {DatabaseHelper.CurrentUserName}";
            WindowState = FormWindowState.Maximized;
            MinimumSize = new Size(1200, 700);
            BackColor   = BgDark;
            Font        = new Font("Segoe UI", 10f);

            // ── 4. Split content (Fill — add first) ─────────────
            var split = new SplitContainer {
                Dock = DockStyle.Fill, SplitterWidth = 5,
                BackColor = Color.FromArgb(40, 28, 14), Orientation = Orientation.Vertical
            };
            split.SplitterDistance = 900;
            BuildOrdersPanel(split.Panel1);
            BuildDetailPanel(split.Panel2);
            Controls.Add(split);

            // ── 3. Legend bar (Top — add third) ─────────────────
            var legend = new Panel { Dock = DockStyle.Top, Height = 32, BackColor = Color.FromArgb(22, 14, 6) };
            int lx = 14;
            foreach (var (col, label) in new (Color c, string l)[] {
                (Color.FromArgb(255, 100, 100), "● Urgent (>15 min)"),
                (Color.FromArgb(255, 180,  50), "● Due (>10 min)"),
                (Color.FromArgb(255, 210, 120), "● Pending"),
                (GreenAcc,                       "● Preparing"),
            }) {
                legend.Controls.Add(new Label { Text = label, ForeColor = col, AutoSize = true,
                    Location = new Point(lx, 7), Font = new Font("Segoe UI", 9.5f) });
                lx += 190;
            }
            Controls.Add(legend);

            // ── 2. Stats bar (Top — add second) ─────────────────
            var statsBar = new Panel { Dock = DockStyle.Top, Height = 38, BackColor = Color.FromArgb(26, 18, 8) };
            _lblStats = new Label {
                Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = OrangeAcc,
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(14, 0, 0, 0)
            };
            var btnRefresh = new Button {
                Text = "🔄  Refresh", Size = new Size(112, 28),
                BackColor = Color.FromArgb(60, 42, 22), ForeColor = Color.FromArgb(200, 170, 120),
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand, Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += (s, e) => LoadOrders();
            statsBar.Controls.Add(_lblStats);
            statsBar.Controls.Add(btnRefresh);
            statsBar.Resize += (s, e) => btnRefresh.Location = new Point(statsBar.Width - 126, 5);
            Controls.Add(statsBar);

            // ── 1. Nav bar (Top — add LAST = appears at top) ─────
            var nav = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.FromArgb(10, 6, 2) };
            nav.Controls.Add(new Label {
                Text = "🍳  KITCHEN DISPLAY SYSTEM  —  CAFÉ 101",
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 200, 120),
                AutoSize = true, Location = new Point(16, 16)
            });
            var lblWelcome = new Label {
                Text = "Welcome,  " + DatabaseHelper.CurrentUserName,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 195, 130),
                AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            var lblUser = new Label {
                Text = "👤  Head Chef",
                Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(175, 152, 118),
                AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _lblClock = new Label {
                Font = new Font("Segoe UI Mono", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 210, 100),
                AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            var btnHelp    = NavBtn("❓  Help",     Color.FromArgb(55, 40, 20));
            var btnSignOut = NavBtn("⬅  Sign Out", Color.FromArgb(145, 45, 38));
            btnHelp.Click    += (s, e) => new HelpAbout().ShowDialog();
            btnSignOut.Click += (s, e) => { _refreshTimer?.Stop(); _clockTimer?.Stop(); Hide(); new Form1().Show(); };
            nav.Controls.Add(lblWelcome);
            nav.Controls.Add(lblUser);
            nav.Controls.Add(_lblClock);
            nav.Controls.Add(btnHelp);
            nav.Controls.Add(btnSignOut);
            nav.Resize += (s, e) => {
                btnSignOut.Location = new Point(nav.Width - 132, 14);
                btnHelp.Location    = new Point(nav.Width - 248, 14);
                _lblClock.Location  = new Point(nav.Width - 420, 20);
                lblWelcome.Location = new Point(nav.Width - 420 - lblWelcome.Width - 16, 12);
                lblUser.Location    = new Point(nav.Width - 420 - lblUser.Width - 16, 32);
            };
            Controls.Add(nav);
        }

        // ═════════════════════════════════════════════════════════
        //  ORDERS PANEL (left)
        // ═════════════════════════════════════════════════════════
        void BuildOrdersPanel(SplitterPanel panel)
        {
            var hdr = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.FromArgb(28, 18, 8) };
            hdr.Controls.Add(new Label {
                Text = "  📋  ACTIVE ORDERS  —  Pending & Preparing",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = TextBright, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            });
            panel.Controls.Add(hdr);

            _dgOrders = new DataGridView {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                ReadOnly = true, RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.FromArgb(28, 18, 8),
                GridColor = Color.FromArgb(50, 36, 18),
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10.5f),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeight = 38, RowTemplate = { Height = 40 }
            };
            _dgOrders.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle {
                BackColor = Color.FromArgb(50, 34, 14),
                ForeColor = Color.FromArgb(200, 170, 110),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleLeft
            };
            _dgOrders.DefaultCellStyle = new DataGridViewCellStyle {
                BackColor = Color.FromArgb(28, 18, 8), ForeColor = TextBright,
                SelectionBackColor = Color.FromArgb(111, 78, 55),
                SelectionForeColor = Color.White, Padding = new Padding(4, 0, 0, 0)
            };
            _dgOrders.EnableHeadersVisualStyles = false;
            _dgOrders.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            _dgOrders.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(34, 22, 10);

            // Columns match GetKitchenOrders():
            // OrderID, Customer, Type, Status, Notes, MinutesAgo
            _dgOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColID",       HeaderText = "Order #",  FillWeight = 10 });
            _dgOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColCustomer", HeaderText = "Customer", FillWeight = 22 });
            _dgOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColType",     HeaderText = "Type",     FillWeight = 14 });
            _dgOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColStatus",   HeaderText = "Status",   FillWeight = 14 });
            _dgOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColMins",     HeaderText = "⏱ Min",   FillWeight = 9  });
            _dgOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColItems",    HeaderText = "Items",    FillWeight = 31 });

            _dgOrders.SelectionChanged += DgOrders_SelectionChanged;
            panel.Controls.Add(_dgOrders);
        }

        // ═════════════════════════════════════════════════════════
        //  DETAIL PANEL (right)
        // ═════════════════════════════════════════════════════════
        void BuildDetailPanel(SplitterPanel panel)
        {
            panel.BackColor = Color.FromArgb(20, 12, 4);

            var hdr = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.FromArgb(28, 18, 8) };
            _lblSelected = new Label {
                Text = "  Select an order from the left panel",
                Font = new Font("Segoe UI", 11f), ForeColor = TextMuted,
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0)
            };
            hdr.Controls.Add(_lblSelected);
            panel.Controls.Add(hdr);

            var itemsHdr = new Panel { Dock = DockStyle.Top, Height = 34, BackColor = Color.FromArgb(24, 16, 6) };
            itemsHdr.Controls.Add(new Label {
                Text = "  🍽  ORDER ITEMS", Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(180, 150, 100),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
            });
            panel.Controls.Add(itemsHdr);

            _dgItems = new DataGridView {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                ReadOnly = true, RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = CardDark, GridColor = Color.FromArgb(50, 36, 18),
                BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 11f),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeight = 36, RowTemplate = { Height = 38 }
            };
            _dgItems.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle {
                BackColor = Color.FromArgb(40, 28, 10), ForeColor = Color.FromArgb(180, 150, 100),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };
            _dgItems.DefaultCellStyle = new DataGridViewCellStyle {
                BackColor = CardDark, ForeColor = TextBright,
                SelectionBackColor = Color.FromArgb(80, 60, 30), SelectionForeColor = Color.White
            };
            _dgItems.EnableHeadersVisualStyles = false;
            _dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ItemName",  HeaderText = "Item",       FillWeight = 50 });
            _dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Quantity",  HeaderText = "Qty",        FillWeight = 15 });
            _dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitPrice", HeaderText = "Unit Price", FillWeight = 35 });
            panel.Controls.Add(_dgItems);

            // Action buttons
            _pnlActions = new Panel { Dock = DockStyle.Bottom, Height = 178, BackColor = Color.FromArgb(14, 8, 2) };
            var btnStart  = KitchenBtn("▶   START PREPARING", OrangeAcc,  8);
            var btnReady  = KitchenBtn("✔   MARK AS READY",   GreenAcc,  62);
            var btnServed = KitchenBtn("☑   MARK AS SERVED",  BlueAcc,  116);
            btnStart.Click  += (s, e) => UpdateStatus("Preparing");
            btnReady.Click  += (s, e) => UpdateStatus("Ready");
            btnServed.Click += (s, e) => UpdateStatus("Served");
            _pnlActions.Controls.AddRange(new Control[] { btnStart, btnReady, btnServed });
            panel.Controls.Add(_pnlActions);
        }

        Button KitchenBtn(string text, Color colour, int top)
        {
            var btn = new Button {
                Text = text, Location = new Point(10, top), Size = new Size(100, 46),
                BackColor = Color.FromArgb(28, 18, 8), ForeColor = colour,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                Cursor = Cursors.Hand, Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            btn.FlatAppearance.BorderSize  = 1;
            btn.FlatAppearance.BorderColor = colour;
            _pnlActions.Resize += (s, e) => btn.Width = _pnlActions.Width - 20;
            return btn;
        }

        // ═════════════════════════════════════════════════════════
        //  DATA  — uses exact column names from GetKitchenOrders()
        //  Columns: OrderID, Customer, Type, Status, Notes, MinutesAgo
        // ═════════════════════════════════════════════════════════
        void LoadOrders()
        {
            try {
                var dt = DatabaseHelper.GetKitchenOrders();
                _dgOrders.Rows.Clear();
                int pending = 0, preparing = 0, urgent = 0;

                foreach (DataRow r in dt.Rows) {
                    int    orderId  = Convert.ToInt32(r["OrderID"]);
                    string customer = r["Customer"].ToString();
                    string type     = r["Type"].ToString();
                    string status   = r["Status"].ToString();
                    int    mins     = Convert.ToInt32(r["MinutesAgo"]);
                    string notes    = r["Notes"].ToString();

                    if (status == "Pending")   pending++;
                    if (status == "Preparing") preparing++;
                    if (mins > 15)             urgent++;

                    // Build items summary
                    string itemsSummary = "—";
                    try {
                        var items = DatabaseHelper.GetOrderItems(orderId);
                        var parts = new System.Collections.Generic.List<string>();
                        foreach (DataRow ir in items.Rows)
                            parts.Add($"{ir["Quantity"]}× {ir["ItemName"]}");
                        if (parts.Count > 0) itemsSummary = string.Join(", ", parts);
                    } catch { }

                    // Append notes to items if present
                    if (!string.IsNullOrWhiteSpace(notes))
                        itemsSummary += $"  📝 {notes}";

                    int ri = _dgOrders.Rows.Add($"#{orderId}", customer, type, status, $"{mins} min", itemsSummary);

                    // Colour by urgency
                    Color fg = mins > 15            ? Color.FromArgb(255, 100,  80) :
                               mins > 10            ? Color.FromArgb(255, 180,  60) :
                               status == "Preparing"? Color.FromArgb(100, 220, 100) :
                                                      Color.FromArgb(255, 210, 130);
                    Color bg = status == "Preparing"? Color.FromArgb(38,  28,  10) :
                               mins > 15            ? Color.FromArgb(50,  16,  10) :
                                                      Color.FromArgb(28,  18,   8);

                    _dgOrders.Rows[ri].DefaultCellStyle = new DataGridViewCellStyle {
                        BackColor = bg, ForeColor = fg,
                        SelectionBackColor = Color.FromArgb(111, 78, 55),
                        SelectionForeColor = Color.White,
                        Font = mins > 15 ? new Font("Segoe UI", 10.5f, FontStyle.Bold) : new Font("Segoe UI", 10.5f),
                        Padding = new Padding(4, 0, 0, 0)
                    };
                }

                _lblStats.Text = $"  Pending: {pending}     Preparing: {preparing}     " +
                                 $"Urgent (>15 min): {urgent}     Last refresh: {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex) {
                MessageBox.Show("Error loading kitchen orders:\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void DgOrders_SelectionChanged(object sender, EventArgs e)
        {
            if (_dgOrders.SelectedRows.Count == 0) return;
            var row = _dgOrders.SelectedRows[0];
            string orderStr = row.Cells["ColID"].Value?.ToString() ?? "";
            _selectedOrderId = int.TryParse(orderStr.Replace("#", ""), out int oid) ? oid : -1;
            string status = row.Cells["ColStatus"].Value?.ToString() ?? "";
            _lblSelected.Text =
                $"  Order {orderStr}  |  {row.Cells["ColCustomer"].Value}  " +
                $"|  {row.Cells["ColType"].Value}  |  Status: {status}  |  {row.Cells["ColMins"].Value}";
            _lblSelected.ForeColor = TextBright;
            LoadItems(_selectedOrderId);
        }

        void LoadItems(int orderId)
        {
            _dgItems.Rows.Clear();
            if (orderId < 0) return;
            try {
                var dt = DatabaseHelper.GetOrderItems(orderId);
                foreach (DataRow r in dt.Rows)
                    _dgItems.Rows.Add(r["ItemName"], r["Quantity"], $"R {Convert.ToDecimal(r["UnitPrice"]):N2}");
            }
            catch (Exception ex) {
                MessageBox.Show("Error loading items:\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void UpdateStatus(string newStatus)
        {
            if (_selectedOrderId < 0) {
                MessageBox.Show("Please select an order first.", "No Order Selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Information); return;
            }
            try {
                DatabaseHelper.UpdateOrderStatus(_selectedOrderId, newStatus);
                _lblSelected.Text = $"  Order #{_selectedOrderId} → Updated to: {newStatus}  ✔";
                _lblSelected.ForeColor = newStatus == "Ready"  ? GreenAcc :
                                         newStatus == "Served" ? BlueAcc  : OrangeAcc;
                _dgItems.Rows.Clear();
                _selectedOrderId = -1;
                LoadOrders();
            }
            catch (Exception ex) {
                MessageBox.Show("Error updating order:\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void StartTimers()
        {
            _clockTimer = new Timer { Interval = 1000 };
            _clockTimer.Tick += (s, e) => {
                if (_lblClock != null && !_lblClock.IsDisposed)
                    _lblClock.Text = DateTime.Now.ToString("HH:mm:ss");
            };
            _clockTimer.Start();

            _refreshTimer = new Timer { Interval = 30000 };
            _refreshTimer.Tick += (s, e) => LoadOrders();
            _refreshTimer.Start();
        }

        void ShowHelp()
        {
            MessageBox.Show(
                "CAFÉ 101 — KITCHEN DISPLAY HELP\n\n" +
                "COLOUR GUIDE:\n" +
                "  Red text   = Order waiting > 15 minutes (URGENT)\n" +
                "  Amber text = Order waiting > 10 minutes\n" +
                "  Yellow     = Pending order\n" +
                "  Green text = Order being prepared\n\n" +
                "WORKFLOW:\n" +
                "  1. Click an order row to select it\n" +
                "  2. Click  ▶ START PREPARING  when you begin cooking\n" +
                "  3. Click  ✔ MARK AS READY    when food is plated\n" +
                "  4. Click  ☑ MARK AS SERVED   when customer collects\n\n" +
                "Auto-refreshes every 30 seconds.\n" +
                "Click Refresh to update manually.",
                "Help — Kitchen Display", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _refreshTimer?.Stop(); _refreshTimer?.Dispose();
            _clockTimer?.Stop();   _clockTimer?.Dispose();
            base.OnFormClosed(e);
        }

        static Button NavBtn(string text, Color bg)
        {
            var b = new Button {
                Text = text, Size = new Size(112, 32),
                BackColor = bg, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold), Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0; return b;
        }
    }
}
