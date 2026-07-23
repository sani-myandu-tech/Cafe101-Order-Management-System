using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace Cafe101
{
    public partial class Cashier : Form
    {
        // ── Colours ──────────────────────────────────────────────
        static readonly Color Brown     = Color.FromArgb(111,  78,  55);
        static readonly Color BrownDark = Color.FromArgb( 74,  50,  37);
        static readonly Color Green     = Color.FromArgb( 76, 175,  80);
        static readonly Color Orange    = Color.FromArgb(230, 130,   0);
        static readonly Color Blue      = Color.FromArgb( 33, 150, 243);
        static readonly Color SidebarBg = Color.FromArgb( 45,  30,  18);

        // ── Cart item ────────────────────────────────────────────
        class CartItem
        {
            public int     ItemID;
            public string  Name;
            public decimal UnitPrice;
            public int     Quantity;
            public decimal Subtotal => UnitPrice * Quantity;
        }

        // ── State ────────────────────────────────────────────────
        readonly List<CartItem> _cart        = new List<CartItem>();
        readonly List<DataRow>  _allProducts = new List<DataRow>();
        int _selectedCategory = 0;

        // ── Controls ─────────────────────────────────────────────
        Panel        _pnlLanding, _pnlMain;
        Panel        _pnlCategories, _pnlProducts;
        DataGridView _dgCart;
        Label        _lblSubtotal, _lblVat, _lblTotal, _lblStatus, _lblOrderCount;
        ComboBox     _cmbOrderType, _cmbPayment, _cmbCustDropdown;
        TextBox      _txtCustSearch, _txtNotes, _txtDeliveryAddress;
        TabControl   _tabMain;
        ToolTip      _tip = new ToolTip { AutoPopDelay = 5000, InitialDelay = 400 };
        List<(CheckBox cb, decimal price)> _sidesControls = new List<(CheckBox, decimal)>();

        class CustItem
        {
            public int Id; public string Display;
            public CustItem(int id, string d) { Id = id; Display = d; }
            public override string ToString() => Display;
        }

        // ═════════════════════════════════════════════════════════
        //  CONSTRUCTOR
        // ═════════════════════════════════════════════════════════
        public Cashier()
        {
            InitializeComponent();
            BuildUI();
            LoadProducts();
        }

        // ═════════════════════════════════════════════════════════
        //  BUILD UI
        // ═════════════════════════════════════════════════════════
        void BuildUI()
        {
            Text        = $"Café 101  —  {DatabaseHelper.CurrentUserName}";
            WindowState = FormWindowState.Maximized;
            MinimumSize = new Size(1200, 700);
            BackColor   = Color.FromArgb(240, 237, 232);
            Font        = new Font("Segoe UI", 10f);

            _pnlLanding = new Panel { Dock = DockStyle.Fill, BackColor = BrownDark };
            BuildLandingPage();
            Controls.Add(_pnlLanding);

            _pnlMain = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(240, 237, 232), Visible = false };
            BuildMainWorkspace();
            Controls.Add(_pnlMain);
        }

        // ═════════════════════════════════════════════════════════
        //  LANDING PAGE  — clean, no floating stat labels
        // ═════════════════════════════════════════════════════════
        void BuildLandingPage()
        {
            _pnlLanding.Controls.Clear();
            string hour = DateTime.Now.Hour < 12 ? "Good Morning" :
                          DateTime.Now.Hour < 17 ? "Good Afternoon" : "Good Evening";

            _pnlLanding.Paint += (s, e) => {
                using (var br = new LinearGradientBrush(new Point(0, 0), new Point(0, _pnlLanding.Height),
                    Color.FromArgb(14, 8, 3), Color.FromArgb(50, 30, 12)))
                    e.Graphics.FillRectangle(br, _pnlLanding.ClientRectangle);
            };

            // Sign out — top right
            var btnOut = new Button {
                Text = "⬅  Sign Out", Size = new Size(118, 32),
                BackColor = Color.FromArgb(130, 35, 35), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnOut.FlatAppearance.BorderSize = 0;
            btnOut.Click += (s, e) => { Hide(); new Form1().Show(); };
            _pnlLanding.Controls.Add(btnOut);
            _pnlLanding.Resize += (s, e) => btnOut.Location = new Point(_pnlLanding.Width - 132, 16);

            // Centre container
            var centre = new Panel { BackColor = Color.Transparent };
            _pnlLanding.Controls.Add(centre);
            _pnlLanding.Resize += (s, e) => {
                int cw = Math.Min(860, _pnlLanding.Width - 60);
                int ch = 440;
                centre.Size     = new Size(cw, ch);
                centre.Location = new Point((_pnlLanding.Width - cw) / 2,
                                            (_pnlLanding.Height - ch) / 2);
            };

            // Gold accent line at top
            var accentBar = new Panel { Size = new Size(52, 3), BackColor = Color.FromArgb(205, 160, 50) };
            centre.Controls.Add(accentBar);
            centre.Resize += (s, e) => accentBar.Location = new Point((centre.Width - accentBar.Width) / 2, 0);

            // Coffee icon
            centre.Controls.Add(new Label {
                Text = "☕", Font = new Font("Segoe UI", 36f),
                ForeColor = Color.FromArgb(205, 162, 88),
                Size = new Size(860, 52), Location = new Point(0, 10),
                TextAlign = ContentAlignment.MiddleCenter
            });

            // Café name
            centre.Controls.Add(new Label {
                Text = "CAFÉ  101",
                Font = new Font("Segoe UI", 28f, FontStyle.Bold), ForeColor = Color.White,
                Size = new Size(860, 44), Location = new Point(0, 62),
                TextAlign = ContentAlignment.MiddleCenter
            });

            // Tagline under name
            centre.Controls.Add(new Label {
                Text = "Point of Sale  ·  Customer Management",
                Font = new Font("Segoe UI", 10f), ForeColor = Color.FromArgb(130, 110, 82),
                Size = new Size(860, 22), Location = new Point(0, 108),
                TextAlign = ContentAlignment.MiddleCenter
            });

            // Greeting
            centre.Controls.Add(new Label {
                Text = $"{hour},  {DatabaseHelper.CurrentUserName}",
                Font = new Font("Segoe UI", 12f, FontStyle.Italic), ForeColor = Color.FromArgb(190, 160, 112),
                Size = new Size(860, 26), Location = new Point(0, 138),
                TextAlign = ContentAlignment.MiddleCenter
            });

            // Date / time
            centre.Controls.Add(new Label {
                Text = DateTime.Now.ToString("dddd, dd MMMM yyyy  ·  HH:mm"),
                Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(95, 80, 58),
                Size = new Size(860, 20), Location = new Point(0, 166),
                TextAlign = ContentAlignment.MiddleCenter
            });

            // Thin divider
            var div = new Panel { BackColor = Color.FromArgb(45, 200, 160, 75) };
            centre.Controls.Add(div);
            centre.Resize += (s, e) => {
                div.Size = new Size(centre.Width / 3, 1);
                div.Location = new Point((centre.Width - div.Width) / 2, 196);
            };

            // Instruction text
            centre.Controls.Add(new Label {
                Text = "Select an option to get started",
                Font = new Font("Segoe UI", 9.5f), ForeColor = Color.FromArgb(90, 75, 55),
                Size = new Size(860, 20), Location = new Point(0, 204),
                TextAlign = ContentAlignment.MiddleCenter
            });

            // ── Tiles — NO stat labels, clean and professional ────
            Panel MakeTile(string icon, string title, string desc, Color accent)
            {
                var tile = new Panel {
                    Size = new Size(258, 186), BackColor = Color.FromArgb(28, 17, 7),
                    Cursor = Cursors.Hand
                };
                tile.Paint += (s, e) => {
                    var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.FillRectangle(new SolidBrush(accent), 0, 0, tile.Width, 4);
                    using (var gb = new LinearGradientBrush(new Point(0, 0), new Point(0, 52),
                        Color.FromArgb(20, accent), Color.Transparent))
                        g.FillRectangle(gb, 0, 4, tile.Width, 48);
                    using (var p = new Pen(Color.FromArgb(50, accent), 1f))
                        g.DrawRectangle(p, 0, 0, tile.Width - 1, tile.Height - 1);
                };
                var lbIcon = new Label {
                    Text = icon, Font = new Font("Segoe UI", 28f),
                    ForeColor = accent, Size = new Size(258, 50),
                    Location = new Point(0, 10), TextAlign = ContentAlignment.MiddleCenter
                };
                var lbTitle = new Label {
                    Text = title, Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                    ForeColor = Color.White, Size = new Size(258, 26),
                    Location = new Point(0, 64), TextAlign = ContentAlignment.MiddleCenter
                };
                var lbDesc = new Label {
                    Text = desc, Font = new Font("Segoe UI", 9f),
                    ForeColor = Color.FromArgb(115, 98, 72), Size = new Size(238, 36),
                    Location = new Point(10, 96), TextAlign = ContentAlignment.MiddleCenter
                };
                var lbArr = new Label {
                    Text = "→  Open", Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(150, accent), Size = new Size(258, 22),
                    Location = new Point(0, 152), TextAlign = ContentAlignment.MiddleCenter
                };
                tile.Controls.AddRange(new Control[] { lbIcon, lbTitle, lbDesc, lbArr });
                void Ent(object ss, EventArgs ee) { tile.BackColor = Color.FromArgb(46, 28, 10); tile.Invalidate(); }
                void Lve(object ss, EventArgs ee) { tile.BackColor = Color.FromArgb(28, 17,  7); tile.Invalidate(); }
                tile.MouseEnter += Ent; tile.MouseLeave += Lve;
                foreach (Control c in tile.Controls) { c.MouseEnter += Ent; c.MouseLeave += Lve; }
                return tile;
            }

            var tPOS  = MakeTile("🛒",  "Place Order",      "Browse menu & process payments",    Color.FromArgb(68, 170, 78));
            var tCust = MakeTile("👥",  "Customers",        "Register, view & manage customers", Color.FromArgb(33, 148, 240));
            var tOrds = MakeTile("📋",  "Today's Orders",   "Track & update order status",       Orange);

            tPOS.Click  += (s, e) => OpenMain(0); foreach (Control c in tPOS.Controls)  c.Click += (s, e) => OpenMain(0);
            tCust.Click += (s, e) => OpenMain(1); foreach (Control c in tCust.Controls) c.Click += (s, e) => OpenMain(1);
            tOrds.Click += (s, e) => OpenMain(2); foreach (Control c in tOrds.Controls) c.Click += (s, e) => OpenMain(2);

            centre.Controls.Add(tPOS);
            centre.Controls.Add(tCust);
            centre.Controls.Add(tOrds);

            centre.Resize += (s, e) => {
                int gap = 20; int tw = 258; int total = 3 * tw + 2 * gap;
                int sx  = (centre.Width - total) / 2;
                tPOS.Location  = new Point(sx,              222);
                tCust.Location = new Point(sx + tw + gap,   222);
                tOrds.Location = new Point(sx + 2*(tw+gap), 222);
            };

            // Footer
            var footer = new Label {
                Text = $"Café 101  ·  POS System  ·  {DateTime.Now.Year}",
                Font = new Font("Segoe UI", 8f), ForeColor = Color.FromArgb(55, 45, 32),
                AutoSize = true
            };
            _pnlLanding.Controls.Add(footer);
            _pnlLanding.Resize += (s, e) =>
                footer.Location = new Point((_pnlLanding.Width - footer.Width) / 2, _pnlLanding.Height - 26);
        }

        void OpenMain(int tabIndex)
        {
            _pnlLanding.Visible = false;
            _pnlMain.Visible    = true;
            if (_tabMain != null && tabIndex < _tabMain.TabPages.Count)
                _tabMain.SelectedIndex = tabIndex;
        }

        // ═════════════════════════════════════════════════════════
        //  MAIN WORKSPACE
        // ═════════════════════════════════════════════════════════
        void BuildMainWorkspace()
        {
            // ── Nav bar — status message is baked inside, NO extra bar below ─
            // ── Nav bar — single height, status bar below it ──
            var nav = new Panel { Dock = DockStyle.Top, Height = 58, BackColor = BrownDark };

            // Café logo + name (left)
            nav.Controls.Add(new Label {
                Text = "☕  Café 101  —  Point of Sale",
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Color.White, AutoSize = true, Location = new Point(16, 13)
            });

            // "Welcome, Name" — professional greeting (right side)
            var lblUser = new Label {
                Text = $"Welcome,  {DatabaseHelper.CurrentUserName}",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(200, 175, 140),
                AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            nav.Controls.Add(lblUser);

            _lblOrderCount = new Label {
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(205, 165, 75),
                AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            nav.Controls.Add(_lblOrderCount);

            var btnHome    = NavBtn("🏠  Home",     Color.FromArgb(85,  62, 42));
            var btnHelp    = NavBtn("❓  Help",     Color.FromArgb(85,  62, 42));
            var btnSignOut = NavBtn("⬅  Sign Out", Color.FromArgb(160, 45, 45));

            btnHome.Click    += (s, e) => { _pnlMain.Visible = false; _pnlLanding.Visible = true; };
            btnHelp.Click    += (s, e) => new HelpAbout().ShowDialog();
            btnSignOut.Click += (s, e) => { Hide(); new Form1().Show(); };

            nav.Controls.Add(btnSignOut);
            nav.Controls.Add(btnHelp);
            nav.Controls.Add(btnHome);

            nav.Resize += (s, e) => {
                btnSignOut.Location = new Point(nav.Width - 128, 13);
                btnHelp.Location    = new Point(nav.Width - 246, 13);
                btnHome.Location    = new Point(nav.Width - 364, 13);
                lblUser.Location    = new Point(nav.Width - 364 - lblUser.Width - 24, 18);
                _lblOrderCount.Location = new Point(nav.Width - 364 - 160, 36);
            };
            _pnlMain.Controls.Add(nav);

            // ── Thin status bar — sits BELOW nav, never overlaps content ──
            var statusBar = new Panel { Dock = DockStyle.Top, Height = 24, BackColor = Color.FromArgb(38, 25, 12) };
            _lblStatus = new Label {
                Text = "Ready  —  click any product to add to cart",
                Font = new Font("Segoe UI", 8.5f), ForeColor = Color.FromArgb(140, 115, 82),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(14, 0, 0, 0)
            };
            statusBar.Controls.Add(_lblStatus);
            _pnlMain.Controls.Add(statusBar);

            // ── Tabs — directly below nav, no extra strip ─────────
            _tabMain = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold) };
            var tabPOS       = new TabPage("🛒  Point of Sale")  { BackColor = Color.FromArgb(240, 237, 232) };
            var tabCustomers = new TabPage("👥  Customers")      { BackColor = Color.White };
            var tabOrders    = new TabPage("📋  Today's Orders") { BackColor = Color.White };
            _tabMain.TabPages.Add(tabPOS);
            _tabMain.TabPages.Add(tabCustomers);
            _tabMain.TabPages.Add(tabOrders);
            _pnlMain.Controls.Add(_tabMain);

            BuildPOSTab(tabPOS);
            BuildCustomersTab(tabCustomers);
            BuildOrdersTab(tabOrders);
        }

        // ═════════════════════════════════════════════════════════
        //  POS TAB
        // ═════════════════════════════════════════════════════════
        void BuildPOSTab(TabPage tab)
        {
            // 2-column layout: [centre | cart]
            var layout = new TableLayoutPanel {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent, Padding = new Padding(8)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 390));
            tab.Controls.Add(layout);

            // Centre column: search bar → category strip → product grid
            var centre = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

            var search = new TextBox {
                Dock = DockStyle.Top, Height = 40, Font = new Font("Segoe UI", 12f),
                BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White,
                Text = "🔍   Search menu items...", ForeColor = Color.FromArgb(160, 140, 120)
            };
            _tip.SetToolTip(search, "Type to search menu items by name");
            search.GotFocus  += (s, e) => { if (search.Text.StartsWith("🔍")) { search.Text = ""; search.ForeColor = Color.Black; } };
            search.LostFocus += (s, e) => { if (string.IsNullOrEmpty(search.Text)) { search.Text = "🔍   Search menu items..."; search.ForeColor = Color.FromArgb(160, 140, 120); } };
            search.TextChanged += (s, e) => RenderProducts(search.Text.StartsWith("🔍") ? "" : search.Text);

            // Category strip — horizontal row of buttons below search
            _pnlCategories = new Panel {
                Dock = DockStyle.Top, Height = 46, BackColor = SidebarBg
            };

            _pnlProducts = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.Transparent };

            // Add in reverse dock order (Fill first, then Top items last = appear at top)
            centre.Controls.Add(_pnlProducts);
            centre.Controls.Add(_pnlCategories);
            centre.Controls.Add(search);
            layout.Controls.Add(centre, 0, 0);

            var cartOuter = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            BuildCartPanel(cartOuter);
            layout.Controls.Add(cartOuter, 1, 0);
        }

        // ── Cart panel ───────────────────────────────────────────
        void BuildCartPanel(Panel parent)
        {
            // Add in reverse dock order:
            // Fill controls first, then Top/Bottom so they claim space first
            // Footer (Bottom) → Grid (Fill) → Header (Top) added last = appears at top

            // ── Footer (Bottom — fixed height, scrollable so cart grid is never squeezed) ──
            // Outer scroll container
            var footerScroll = new Panel {
                Dock = DockStyle.Bottom, Height = 420,
                BackColor = Color.FromArgb(252, 249, 245),
                AutoScroll = true
            };
            footerScroll.Paint += (s, e) => { using (var p = new Pen(Color.FromArgb(220, 210, 200))) e.Graphics.DrawLine(p, 0, 0, footerScroll.Width, 0); };
            // Inner panel holds all controls at absolute positions — scroll works on this
            var footerInner = new Panel {
                Location = new Point(0, 0), Width = footerScroll.Width, Height = 560,
                BackColor = Color.FromArgb(252, 249, 245)
            };
            footerScroll.Controls.Add(footerInner);
            footerScroll.Resize += (s, e) => footerInner.Width = footerScroll.Width;
            BuildFooter(footerInner);
            parent.Controls.Add(footerScroll);

            // ── Grid (Fill — add second) ──────────────────────────
            _dgCart = new DataGridView {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                ReadOnly = true, RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10f),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeight = 32, RowTemplate = { Height = 32 }
            };
            _dgCart.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle {
                BackColor = Color.FromArgb(245, 240, 235), ForeColor = Color.FromArgb(80, 60, 40),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            _dgCart.EnableHeadersVisualStyles = false;
            _dgCart.DefaultCellStyle.SelectionBackColor = Color.FromArgb(42, 110, 95);
            _dgCart.DefaultCellStyle.SelectionForeColor = Color.White;

            _dgCart.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name",  HeaderText = "Item",  FillWeight = 38 });
            _dgCart.Columns.Add(new DataGridViewButtonColumn  { Name = "Minus", HeaderText = "",      Text = "−", UseColumnTextForButtonValue = true, FillWeight = 7 });
            _dgCart.Columns.Add(new DataGridViewTextBoxColumn { Name = "Qty",   HeaderText = "Qty",   FillWeight = 10 });
            _dgCart.Columns.Add(new DataGridViewButtonColumn  { Name = "Plus",  HeaderText = "",      Text = "+", UseColumnTextForButtonValue = true, FillWeight = 7 });
            _dgCart.Columns.Add(new DataGridViewTextBoxColumn { Name = "Price", HeaderText = "Price", FillWeight = 18 });
            _dgCart.Columns.Add(new DataGridViewTextBoxColumn { Name = "Total", HeaderText = "Total", FillWeight = 18 });
            _dgCart.Columns.Add(new DataGridViewButtonColumn  { Name = "Del",   HeaderText = "",      Text = "✕", UseColumnTextForButtonValue = true, FillWeight = 7 });

            _dgCart.CellClick += (s, e) => {
                if (e.RowIndex < 0 || e.RowIndex >= _cart.Count) return;
                int ci = e.ColumnIndex;
                if      (ci == _dgCart.Columns["Minus"].Index) ChangeQtyAt(e.RowIndex, -1);
                else if (ci == _dgCart.Columns["Plus"].Index)  ChangeQtyAt(e.RowIndex,  1);
                else if (ci == _dgCart.Columns["Del"].Index)   { _cart.RemoveAt(e.RowIndex); RefreshCart(); }
            };

            var ctx = new ContextMenuStrip();
            ctx.Items.Add("➕  Increase Qty").Click += (s, e) => { if (_dgCart.SelectedRows.Count > 0) ChangeQtyAt(_dgCart.SelectedRows[0].Index,  1); };
            ctx.Items.Add("➖  Decrease Qty").Click += (s, e) => { if (_dgCart.SelectedRows.Count > 0) ChangeQtyAt(_dgCart.SelectedRows[0].Index, -1); };
            ctx.Items.Add("🗑  Remove Item").Click  += (s, e) => { if (_dgCart.SelectedRows.Count > 0) { _cart.RemoveAt(_dgCart.SelectedRows[0].Index); RefreshCart(); } };
            _dgCart.ContextMenuStrip = ctx;
            parent.Controls.Add(_dgCart);

            // ── Header (Top — add LAST = docks above everything) ──
            var hdr = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = BrownDark };
            hdr.Controls.Add(new Label {
                Text = "🛒  Current Order", Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White, AutoSize = true, Location = new Point(12, 12)
            });
            parent.Controls.Add(hdr);
        }

        // ── Order footer ──────────────────────────────────────────
        void BuildFooter(Panel f)
        {
            int y = 8; int lx = 12; int fw = 364;

            // ── Customer ──────────────────────────────────────────
            f.Controls.Add(new Label { Text = "Customer:", Location = new Point(lx, y),
                AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(80, 60, 40) }); y += 18;

            _txtCustSearch = new TextBox { Location = new Point(lx, y), Size = new Size(fw, 26),
                Font = new Font("Segoe UI", 9.5f), BorderStyle = BorderStyle.FixedSingle,
                Text = "Search by name or phone...", ForeColor = Color.Gray };
            _txtCustSearch.GotFocus  += (s, e) => { if (_txtCustSearch.ForeColor == Color.Gray) { _txtCustSearch.Text = ""; _txtCustSearch.ForeColor = Color.Black; } };
            _txtCustSearch.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(_txtCustSearch.Text)) { _txtCustSearch.Text = "Search by name or phone..."; _txtCustSearch.ForeColor = Color.Gray; } };
            _txtCustSearch.TextChanged += (s, e) => FilterCustomerDropdown();
            f.Controls.Add(_txtCustSearch); y += 28;

            _cmbCustDropdown = new ComboBox { Location = new Point(lx, y), Size = new Size(fw, 26),
                DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5f) };
            LoadCustomerDropdown();
            f.Controls.Add(_cmbCustDropdown); y += 30;

            // ── Divider ───────────────────────────────────────────
            f.Controls.Add(new Panel { Location = new Point(lx, y), Size = new Size(fw, 1), BackColor = Color.FromArgb(215, 205, 195) }); y += 6;

            // ── Totals ────────────────────────────────────────────
            void TotalRow(string lbl, out Label val, bool big = false)
            {
                f.Controls.Add(new Label { Text = lbl, Location = new Point(lx, y), AutoSize = true,
                    Font = new Font("Segoe UI", big ? 11f : 9.5f),
                    ForeColor = big ? BrownDark : Color.FromArgb(100, 80, 60) });
                val = new Label { Text = "R 0.00", Location = new Point(fw - 74, y), AutoSize = true,
                    Font = new Font("Segoe UI", big ? 13f : 9.5f, FontStyle.Bold),
                    ForeColor = big ? Brown : Color.FromArgb(60, 60, 60) };
                f.Controls.Add(val);
                y += big ? 26 : 22;
            }
            TotalRow("Subtotal:",  out _lblSubtotal);
            TotalRow("VAT (15%):", out _lblVat);
            f.Controls.Add(new Panel { Location = new Point(lx, y), Size = new Size(fw, 1), BackColor = Color.FromArgb(195, 178, 158) }); y += 5;
            TotalRow("TOTAL DUE:", out _lblTotal, big: true); y += 4;
            f.Controls.Add(new Panel { Location = new Point(lx, y), Size = new Size(fw, 1), BackColor = Color.FromArgb(215, 205, 195) }); y += 8;

            // ── Sides / Add-ons ───────────────────────────────────
            f.Controls.Add(new Label { Text = "Sides / Add-ons (optional):",
                Location = new Point(lx, y), AutoSize = true,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.FromArgb(80, 60, 40) }); y += 20;

            string[]  sideNames  = { "Plain Fries", "Loaded Fries", "Coleslaw", "Side Salad", "Extra Sauce", "Soft Drink" };
            decimal[] sidePrices = { 25m, 40m, 18m, 22m, 8m, 20m };
            _sidesControls.Clear();
            int c1y = y, c2y = y;
            for (int i = 0; i < sideNames.Length; i++) {
                int ii = i;
                var cb = new CheckBox {
                    Text = $"{sideNames[i]} (+R{sidePrices[i]:N0})",
                    AutoSize = true, Font = new Font("Segoe UI", 8.5f), ForeColor = Color.FromArgb(60, 45, 30)
                };
                if (i < 3) { cb.Location = new Point(lx,       c1y); c1y += 20; }
                else       { cb.Location = new Point(lx + 188, c2y); c2y += 20; }
                cb.CheckedChanged += (s, e) => RefreshCart();
                f.Controls.Add(cb);
                _sidesControls.Add((cb, sidePrices[i]));
            }
            y = Math.Max(c1y, c2y) + 4;
            f.Controls.Add(new Panel { Location = new Point(lx, y), Size = new Size(fw, 1), BackColor = Color.FromArgb(215, 205, 195) }); y += 8;

            // ── Order Type + Payment ──────────────────────────────
            f.Controls.Add(new Label { Text = "Order Type:", Location = new Point(lx, y), AutoSize = true,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.FromArgb(80, 60, 40) });
            f.Controls.Add(new Label { Text = "Payment:", Location = new Point(lx + 188, y), AutoSize = true,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.FromArgb(80, 60, 40) }); y += 18;

            _cmbOrderType = new ComboBox { Location = new Point(lx, y), Size = new Size(170, 26),
                DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5f) };
            _cmbOrderType.Items.AddRange(new object[] { "Takeaway", "Delivery", "Dine-In" });
            _cmbOrderType.SelectedIndex = 0;

            _cmbPayment = new ComboBox { Location = new Point(lx + 188, y), Size = new Size(fw - 188, 26),
                DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5f) };
            _cmbPayment.Items.AddRange(new object[] { "Cash", "Card", "Mobile Payment" });
            _cmbPayment.SelectedIndex = 0;
            f.Controls.Add(_cmbOrderType); f.Controls.Add(_cmbPayment); y += 32;

            // ── Delivery address (City + Suburb) — hidden until Delivery selected ──
            int _dy   = y;
            int halfW = (fw - 6) / 2;
            var lblDelivTitle = new Label {
                Text = "🚚  Delivery Address: *",
                Location = new Point(lx, _dy), AutoSize = true,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(200, 100, 0), Visible = false
            };
            var lblCity   = new Label { Text = "City:",   Location = new Point(lx,               _dy + 18), AutoSize = true, Font = new Font("Segoe UI", 8f), ForeColor = Color.FromArgb(100,75,50), Visible = false };
            var lblSuburb = new Label { Text = "Suburb:", Location = new Point(lx + halfW + 6,   _dy + 18), AutoSize = true, Font = new Font("Segoe UI", 8f), ForeColor = Color.FromArgb(100,75,50), Visible = false };
            var txtCity   = new TextBox { Location = new Point(lx,             _dy + 32), Size = new Size(halfW, 26), Font = new Font("Segoe UI", 9.5f), BorderStyle = BorderStyle.FixedSingle, BackColor = Color.FromArgb(255,250,240), Visible = false };
            var txtSuburb = new TextBox { Location = new Point(lx + halfW + 6, _dy + 32), Size = new Size(halfW, 26), Font = new Font("Segoe UI", 9.5f), BorderStyle = BorderStyle.FixedSingle, BackColor = Color.FromArgb(255,250,240), Visible = false };
            f.Controls.Add(lblDelivTitle);
            f.Controls.Add(lblCity);   f.Controls.Add(lblSuburb);
            f.Controls.Add(txtCity);   f.Controls.Add(txtSuburb);
            _txtDeliveryAddress = txtCity; // primary; suburb read separately
            y += 68;

            string GetDeliveryAddress() {
                string c = txtCity.Text.Trim(), s = txtSuburb.Text.Trim();
                if (string.IsNullOrEmpty(c) && string.IsNullOrEmpty(s)) return "";
                return string.IsNullOrEmpty(s) ? c : string.IsNullOrEmpty(c) ? s : $"{c}, {s}";
            }

            // ── Notes — always below delivery block ───────────────
            int _ny = y;
            var lblNotes = new Label { Text = "Special Requests / Notes:",
                Location = new Point(lx, _ny), AutoSize = true,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(80, 60, 40) };
            _txtNotes = new TextBox { Location = new Point(lx, _ny + 16), Size = new Size(fw, 26),
                Font = new Font("Segoe UI", 9.5f), BorderStyle = BorderStyle.FixedSingle };
            f.Controls.Add(lblNotes);
            f.Controls.Add(_txtNotes);
            y += 50;

            // ── Place Order ───────────────────────────────────────
            var btnPlace = new Button { Text = "✔   PLACE ORDER",
                Location = new Point(lx, y), Size = new Size(fw, 44),
                BackColor = Green, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold), Cursor = Cursors.Hand };
            btnPlace.FlatAppearance.BorderSize = 0;
            btnPlace.Click += BtnPlaceOrder_Click;
            f.Controls.Add(btnPlace);
            y += 50;

            // ── Clear Order ───────────────────────────────────────
            var btnClear = new Button { Text = "🗑  Clear Order",
                Location = new Point(lx, y), Size = new Size(fw, 44),
                BackColor = Color.FromArgb(210, 70, 55), ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold), Cursor = Cursors.Hand };
            btnClear.FlatAppearance.BorderSize = 0;
            btnClear.Click += (s, e) => {
                _cart.Clear();
                foreach (var (cb, _) in _sidesControls) cb.Checked = false;
                RefreshCart(); ResetOrderForm();
                SetStatus("Order cleared — ready for next customer.", false);
            };
            f.Controls.Add(btnClear);

            // ── Toggle delivery visibility — zero position changes ──
            void RefreshDelivery()
            {
                bool isDel = _cmbOrderType?.Text == "Delivery";
                lblDelivTitle.Visible = isDel; lblCity.Visible = isDel; lblSuburb.Visible = isDel;
                txtCity.Visible = isDel; txtSuburb.Visible = isDel;
                if (isDel && string.IsNullOrWhiteSpace(txtCity.Text))
                    if (_cmbCustDropdown?.SelectedItem is CustItem ci && ci.Id > 0)
                        try {
                            var a = DatabaseHelper.ExecuteScalar(
                                "SELECT ISNULL(Address,'') FROM Customers WHERE CustomerID=@id",
                                new Microsoft.Data.SqlClient.SqlParameter("@id", ci.Id));
                            if (a != null && a.ToString().Trim().Length > 0) {
                                // Try to split "City, Suburb" if comma present
                                var parts = a.ToString().Split(',');
                                txtCity.Text   = parts[0].Trim();
                                txtSuburb.Text = parts.Length > 1 ? parts[1].Trim() : "";
                            }
                        } catch { }
            }
            _cmbOrderType.SelectedIndexChanged    += (s, e) => RefreshDelivery();
            _cmbCustDropdown.SelectedIndexChanged += (s, e) => {
                if (_cmbOrderType?.Text == "Delivery") { txtCity.Text = ""; txtSuburb.Text = ""; RefreshDelivery(); }
            };
            // Keep a hidden combined-address textbox updated for order placement logic
            EventHandler updateAddr = (s, e) => {
                if (_txtDeliveryAddress != null) {
                    string c = txtCity.Text.Trim(), sub = txtSuburb.Text.Trim();
                    // _txtDeliveryAddress IS txtCity; we store suburb in Tag for retrieval
                    txtCity.Tag = sub;
                }
            };
            txtCity.TextChanged   += updateAddr;
            txtSuburb.TextChanged += updateAddr;
        }

        // ═════════════════════════════════════════════════════════
        //  CUSTOMERS TAB
        // ═════════════════════════════════════════════════════════
        void BuildCustomersTab(TabPage tab)
        {
            var layout = new TableLayoutPanel {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3,
                BackColor = Color.Transparent, Padding = new Padding(12)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            tab.Controls.Add(layout);

            var txtSearch = new TextBox {
                Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11f),
                BorderStyle = BorderStyle.FixedSingle, Text = "Search customers by name, email or phone...",
                ForeColor = Color.Gray
            };
            _tip.SetToolTip(txtSearch, "Type 2+ characters to filter");
            txtSearch.GotFocus  += (s, e) => { if (txtSearch.ForeColor == Color.Gray) { txtSearch.Text = ""; txtSearch.ForeColor = Color.Black; } };
            txtSearch.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(txtSearch.Text)) { txtSearch.Text = "Search customers by name, email or phone..."; txtSearch.ForeColor = Color.Gray; } };
            layout.Controls.Add(txtSearch, 0, 0);

            var dg = new DataGridView {
                Dock = DockStyle.Fill, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
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
                Alignment = DataGridViewContentAlignment.MiddleLeft, Padding = new Padding(4,0,0,0)
            };
            dg.DefaultCellStyle.SelectionBackColor        = Color.FromArgb(180, 120, 40);
            dg.DefaultCellStyle.SelectionForeColor        = Color.White;
            dg.AlternatingRowsDefaultCellStyle.BackColor  = Color.FromArgb(250, 244, 235);
            dg.EnableHeadersVisualStyles = false;
            dg.DefaultCellStyle.SelectionBackColor = Color.FromArgb(255, 243, 230);
            dg.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(252, 249, 245);
            layout.Controls.Add(dg, 0, 1);

            Action loadCust = () => {
                string q = txtSearch.ForeColor == Color.Gray ? "" : txtSearch.Text.Trim();
                dg.DataSource = DatabaseHelper.GetCustomers(q.Length >= 2 ? q : "");
            };
            txtSearch.TextChanged += (s, e) => loadCust();
            loadCust();

            var btnRow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent, Padding = new Padding(0, 4, 0, 0) };
            layout.Controls.Add(btnRow, 0, 2);

            Button CB(string t, Color bg) {
                var b = new Button { Text = t, Size = new Size(160, 36),
                    BackColor = bg, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), Cursor = Cursors.Hand, Margin = new Padding(0, 0, 8, 0) };
                b.FlatAppearance.BorderSize = 0; return b;
            }

            var btnAdd  = CB("➕  Add Customer",  Brown);
            var btnEdit = CB("✏  Edit Customer",  Blue);
            var btnDel  = CB("🗑  Delete",         Color.FromArgb(200, 60, 60));
            btnRow.Controls.AddRange(new Control[] { btnAdd, btnEdit, btnDel });

            btnAdd.Click += (s, e) => ShowCustomerDialog(null, () => loadCust());
            btnEdit.Click += (s, e) => {
                if (dg.SelectedRows.Count == 0) { MessageBox.Show("Select a customer first."); return; }
                int id = Convert.ToInt32(dg.SelectedRows[0].Cells["CustomerID"].Value);
                ShowCustomerDialog(id, () => loadCust());
            };
            btnDel.Click += (s, e) => {
                if (dg.SelectedRows.Count == 0) { MessageBox.Show("Select a customer first."); return; }
                string nm = dg.SelectedRows[0].Cells["FullName"].Value?.ToString();
                if (MessageBox.Show($"Delete '{nm}'?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes) {
                    DatabaseHelper.DeleteCustomer(Convert.ToInt32(dg.SelectedRows[0].Cells["CustomerID"].Value));
                    loadCust();
                }
            };
        }

        void ShowCustomerDialog(int? custId, Action onSaved)
        {
            var f = new Form { Text = custId.HasValue ? "Edit Customer" : "New Customer",
                Size = new Size(420, 430), StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false,
                BackColor = Color.White, Font = new Font("Segoe UI", 10f) };
            int y = 16; int lx = 16; int w = 372;
            var lblErr = new Label { AutoSize = true, ForeColor = Color.FromArgb(180, 50, 50),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold), Visible = false };
            TextBox FT(string lbl) {
                f.Controls.Add(new Label { Text = lbl, Location = new Point(lx, y), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) }); y += 22;
                var t = new TextBox { Location = new Point(lx, y), Size = new Size(w, 30), Font = new Font("Segoe UI", 10f), BorderStyle = BorderStyle.FixedSingle };
                f.Controls.Add(t); y += 36; return t;
            }
            var tFn = FT("First Name: *"); var tLn = FT("Last Name: *");
            var tPh = FT("Phone: *");      var tEm = FT("Email:");
            // Only allow digits, spaces and hyphens in phone field
            tPh.KeyPress += (s, e) => {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != ' ' && e.KeyChar != '-') {
                    e.Handled = true;
                }
            };
            tPh.TextChanged += (s, e) => {
                // Show hint in red if doesn't start with 0
                string t = tPh.Text.Trim();
                tPh.BackColor = (!string.IsNullOrEmpty(t) && !t.StartsWith("0"))
                    ? Color.FromArgb(255, 235, 235) : Color.White;
            };
            var tAd = FT("Address: *");     var tNt = FT("Notes:");

            if (custId.HasValue) {
                var dt = DatabaseHelper.GetCustomers();
                foreach (DataRow r in dt.Rows) {
                    if (Convert.ToInt32(r["CustomerID"]) == custId.Value) {
                        tFn.Text = r["FirstName"].ToString(); tLn.Text = r["LastName"].ToString();
                        tPh.Text = r["Phone"].ToString();     tEm.Text = r["Email"].ToString();
                        tAd.Text = r["Address"].ToString();   tNt.Text = r["Notes"].ToString(); break;
                    }
                }
            }
            lblErr.Location = new Point(lx, y); f.Controls.Add(lblErr); y += 24;
            var btn = new Button { Text = custId.HasValue ? "💾  Save Changes" : "➕  Add Customer",
                Location = new Point(lx, y), Size = new Size(w, 42),
                BackColor = Brown, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold), Cursor = Cursors.Hand };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (s, e) => {
                lblErr.Visible = false;
                string fn = tFn.Text.Trim(), ln = tLn.Text.Trim();
                string ph = tPh.Text.Trim(), em = tEm.Text.Trim();

                // ── Name validation ──────────────────────────────
                if (string.IsNullOrWhiteSpace(fn)) {
                    lblErr.Text = "⚠  First name is required."; lblErr.Visible = true; tFn.Focus(); return; }
                if (fn.Length < 2) {
                    lblErr.Text = "⚠  First name must be at least 2 characters."; lblErr.Visible = true; tFn.Focus(); return; }
                if (!System.Text.RegularExpressions.Regex.IsMatch(fn, @"^[A-Za-z\s\-']+$")) {
                    lblErr.Text = "⚠  First name may only contain letters, spaces or hyphens."; lblErr.Visible = true; tFn.Focus(); return; }

                if (string.IsNullOrWhiteSpace(ln)) {
                    lblErr.Text = "⚠  Last name is required."; lblErr.Visible = true; tLn.Focus(); return; }
                if (ln.Length < 2) {
                    lblErr.Text = "⚠  Last name must be at least 2 characters."; lblErr.Visible = true; tLn.Focus(); return; }
                if (!System.Text.RegularExpressions.Regex.IsMatch(ln, @"^[A-Za-z\s\-']+$")) {
                    lblErr.Text = "⚠  Last name may only contain letters, spaces or hyphens."; lblErr.Visible = true; tLn.Focus(); return; }

                // ── Phone validation (SA format) ─────────────────
                if (string.IsNullOrWhiteSpace(ph)) {
                    lblErr.Text = "⚠  Phone number is required."; lblErr.Visible = true; tPh.Focus(); return; }
                string digitsOnly = System.Text.RegularExpressions.Regex.Replace(ph, @"[\s\-\(\)]", "");
                if (!digitsOnly.StartsWith("0")) {
                    lblErr.Text = "⚠  Phone number must start with 0 (e.g. 0821234567)."; lblErr.Visible = true; tPh.Focus(); return; }
                if (!System.Text.RegularExpressions.Regex.IsMatch(digitsOnly, @"^\d+$")) {
                    lblErr.Text = "⚠  Phone number must contain digits only."; lblErr.Visible = true; tPh.Focus(); return; }
                if (digitsOnly.Length < 10 || digitsOnly.Length > 11) {
                    lblErr.Text = "⚠  Phone must be 10 digits (e.g. 0821234567)."; lblErr.Visible = true; tPh.Focus(); return; }

                // ── Email validation (optional but if filled must be valid) ──
                if (!string.IsNullOrWhiteSpace(em)) {
                    if (!System.Text.RegularExpressions.Regex.IsMatch(em, @"^[^@\s]+@[^@\s]+\.[^@\s]+$")) {
                        lblErr.Text = "⚠  Enter a valid email address (e.g. name@example.com)."; lblErr.Visible = true; tEm.Focus(); return; }
                }

                // ── Address is compulsory ────────────────────────
                if (string.IsNullOrWhiteSpace(tAd.Text)) {
                    lblErr.Text = "⚠  Address is required."; lblErr.Visible = true; tAd.Focus(); return; }
                if (tAd.Text.Trim().Length < 5) {
                    lblErr.Text = "⚠  Please enter a full address (at least 5 characters)."; lblErr.Visible = true; tAd.Focus(); return; }

                if (custId.HasValue)
                    DatabaseHelper.UpdateCustomer(custId.Value, fn, ln, em, ph, tAd.Text.Trim(), tNt.Text.Trim());
                else
                    DatabaseHelper.AddCustomer(fn, ln, em, ph, tAd.Text.Trim(), tNt.Text.Trim());
                onSaved?.Invoke(); f.Close();
            };
            f.Controls.Add(btn);
            f.ClientSize = new Size(420, y + 60);
            f.ShowDialog();
        }

        // ═════════════════════════════════════════════════════════
        //  ORDERS TAB — fixed column name, visible forecolors
        // ═════════════════════════════════════════════════════════
        void BuildOrdersTab(TabPage tab)
        {
            var layout = new TableLayoutPanel {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2,
                BackColor = Color.Transparent, Padding = new Padding(12)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            tab.Controls.Add(layout);

            layout.Controls.Add(new Label { Text = "Today's Orders", Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = BrownDark,
                TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
            layout.Controls.Add(new Label { Text = "Order Items (click a row →)", Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = BrownDark,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(8, 0, 0, 0) }, 1, 0);

            var dgOrders = MakeGrid();
            var dgItems  = MakeGrid();

            // Readable forecolor for all cells
            dgOrders.DefaultCellStyle.ForeColor          = Color.FromArgb(40, 28, 16);
            dgOrders.DefaultCellStyle.SelectionBackColor = Color.FromArgb(42, 110, 95);
            dgOrders.DefaultCellStyle.SelectionForeColor = Color.White;
            dgItems.DefaultCellStyle.ForeColor           = Color.FromArgb(40, 28, 16);
            dgItems.DefaultCellStyle.SelectionBackColor  = Color.FromArgb(111, 78, 55);
            dgItems.DefaultCellStyle.SelectionForeColor  = Color.White;

            layout.Controls.Add(dgOrders, 0, 1);
            layout.Controls.Add(dgItems,  1, 1);

            Action reload = () => {
                var dt = DatabaseHelper.GetOrdersByStatus(null, DateTime.Today);
                dgOrders.DataSource = dt;
                // Rename columns for display
                if (dgOrders.Columns["#"] != null) dgOrders.Columns["#"].HeaderText = "Order #";
                ColourStatus(dgOrders);
            };
            reload();

            dgOrders.SelectionChanged += (s, e) => {
                if (dgOrders.SelectedRows.Count == 0) return;
                try {
                    // Try both column names
                    object idVal = null;
                    foreach (DataGridViewCell c in dgOrders.SelectedRows[0].Cells) {
                        string h = c.OwningColumn.HeaderText;
                        if (h == "#" || h == "Order #" || h == "OrderID") { idVal = c.Value; break; }
                    }
                    if (idVal != null)
                        dgItems.DataSource = DatabaseHelper.GetOrderItems(Convert.ToInt32(idVal));
                } catch { }
            };

            // Auto-refresh every 30 seconds — no manual button needed
            var autoTimer = new System.Windows.Forms.Timer { Interval = 30000 };
            autoTimer.Tick += (s, e) => reload();
            autoTimer.Start();
            tab.Disposed += (s, e) => autoTimer.Stop();
        }

        static void ColourStatus(DataGridView dgv)
        {
            foreach (DataGridViewRow row in dgv.Rows) {
                string st = "";
                foreach (DataGridViewCell c in row.Cells)
                    if (c.OwningColumn.HeaderText == "Status") { st = c.Value?.ToString() ?? ""; break; }
                row.DefaultCellStyle.BackColor =
                    st == "Served"    ? Color.FromArgb(200, 240, 200) :
                    st == "Ready"     ? Color.FromArgb(190, 220, 255) :
                    st == "Preparing" ? Color.FromArgb(255, 245, 200) :
                    st == "Cancelled" ? Color.FromArgb(255, 210, 210) : Color.White;
                row.DefaultCellStyle.ForeColor = Color.FromArgb(30, 20, 10);
            }
        }

        static DataGridView MakeGrid()
        {
            var dgv = new DataGridView {
                Dock = DockStyle.Fill, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                ReadOnly = true, RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10f),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeight = 34, RowTemplate = { Height = 30 }
            };
            dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle {
                BackColor = BrownDark, ForeColor = Color.White, Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };
            dgv.EnableHeadersVisualStyles = false;
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(42, 110, 95);
            dgv.DefaultCellStyle.SelectionForeColor = Color.White;
            dgv.DefaultCellStyle.ForeColor          = Color.FromArgb(40, 28, 16);
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(252, 249, 245);
            dgv.GridColor = Color.FromArgb(230, 220, 210);
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            return dgv;
        }

        // ═════════════════════════════════════════════════════════
        //  PRODUCTS — LOAD / RENDER / CATEGORIES
        // ═════════════════════════════════════════════════════════
        void LoadProducts()
        {
            _allProducts.Clear();
            try { var dt = DatabaseHelper.GetMenuItems(); foreach (DataRow r in dt.Rows) _allProducts.Add(r); } catch { }
            BuildCategoryButtons();
            RenderProducts("");
        }

        void BuildCategoryButtons()
        {
            if (_pnlCategories == null) return;
            _pnlCategories.Controls.Clear();

            var cats = new List<(int id, string name)> { (0, "All Items") };
            try { var dt = DatabaseHelper.GetCategories(); foreach (DataRow r in dt.Rows) cats.Add((Convert.ToInt32(r["CategoryID"]), r["Name"].ToString())); } catch { }

            // Horizontal strip — use a FlowLayoutPanel inside the categories panel
            var flow = new FlowLayoutPanel {
                Dock = DockStyle.Fill, BackColor = SidebarBg,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, AutoScroll = false,
                Padding = new Padding(4, 4, 4, 4)
            };

            foreach (var (id, name) in cats) {
                int catId = id; bool sel = catId == _selectedCategory;
                var btn = new Button {
                    Text = name,
                    Size = new Size(Math.Max(80, name.Length * 9 + 20), 36),
                    Margin = new Padding(2, 0, 2, 0),
                    BackColor = sel ? Brown : Color.FromArgb(55, 38, 22),
                    ForeColor = sel ? Color.White : Color.FromArgb(200, 175, 148),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9.5f, sel ? FontStyle.Bold : FontStyle.Regular),
                    Cursor = Cursors.Hand
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.Click += (s, e) => { _selectedCategory = catId; BuildCategoryButtons(); RenderProducts(""); };
                flow.Controls.Add(btn);
            }

            _pnlCategories.Controls.Add(flow);
        }

        void RenderProducts(string search)
        {
            if (_pnlProducts == null) return;
            _pnlProducts.Controls.Clear();
            var flow = new FlowLayoutPanel {
                Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.Transparent, Padding = new Padding(4)
            };
            _pnlProducts.Controls.Add(flow);

            var rows = _allProducts.Where(r => {
                bool catOk = true;
                if (_selectedCategory != 0) {
                    try {
                        int cid = Convert.ToInt32(DatabaseHelper.ExecuteScalar(
                            "SELECT CategoryID FROM MenuItems WHERE ItemID=@id",
                            new SqlParameter("@id", Convert.ToInt32(r["ItemID"]))));
                        catOk = cid == _selectedCategory;
                    } catch { catOk = false; }
                }
                bool srch = string.IsNullOrEmpty(search) ||
                            r["Name"].ToString().IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
                return catOk && srch;
            }).ToList();

            if (rows.Count == 0) {
                flow.Controls.Add(new Label {
                    Text = "No items found.", Font = new Font("Segoe UI", 12f),
                    ForeColor = Color.FromArgb(150, 130, 110), AutoSize = true, Margin = new Padding(20)
                }); return;
            }
            foreach (var row in rows) flow.Controls.Add(MakeProductCard(row));
        }

        // ═════════════════════════════════════════════════════════
        //  PRODUCT CARD — real photos + generated fallback art
        // ═════════════════════════════════════════════════════════
        static readonly Dictionary<string, string> _imageMap =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                // ── Drinks / Beverages ────────────────────────────────────
                {"Coke","coke.jpg"},{"Coca-Cola","coke.jpg"},{"Coca Cola","coke.jpg"},
                {"Sprite","sprite.jpg"},{"Sprite Flavours","sprite.jpg"},
                {"Red Bull","redbull.jpg"},{"Redbull","redbull.jpg"},
                {"Monster","monster.jpg"},{"Monster Energy","monster.jpg"},
                {"Aquelle","aquelle.jpg"},{"Water","aquelle.jpg"},
                {"Liquifruit","liquifruit.jpg"},
                {"Flying Fish","flyingfish.jpg"},{"Switch","switch.jpg"},
                {"Rooibos","reebost.jpg"},{"Reebost","reebost.jpg"},
                {"Cold Brew","coldbrew.jpg"},{"Cold Brew Coffee","coldbrew.jpg"},
                {"Flat White","flatwhite.jpg"},
                {"Hot Chocolate","hotchocolate.jpg"},{"Hot Choc","hotchocolate.jpg"},
                {"Orange Juice","orangejuice.jpg"},{"OJ","orangejuice.jpg"},
                {"Cappuccino","cappuccino.jpg"},
                {"Latte","latte.jpg"},
                {"Espresso","espresso.jpg"},
                {"Americano","americano.jpg"},
                {"Iced Coffee","icedcoffee.jpg"},
                {"Juice","orangejuice.jpg"},
                {"Tea","tea.jpg"},
                // ── Lunch ─────────────────────────────────────────────────
                {"Chips","plainfries.jpg"},{"Fries","plainfries.jpg"},{"Plain Fries","plainfries.jpg"},
                {"Loaded Fries","fullyloadedfries.jpg"},{"Fully Loaded Fries","fullyloadedfries.jpg"},
                {"Chip Sandwich","chipsandwich.jpg"},{"Chips Sandwich","chipsandwich.jpg"},
                {"Wing It","wingit.jpg"},{"Wings","wingit.jpg"},{"Chicken Wings","wingit.jpg"},
                {"Wrap It Up","wrapitup.jpg"},{"Wrap","wrapitup.jpg"},{"Chicken Wrap","wrapitup.jpg"},
                {"Lamb Slamwich","lambslamwich.jpg"},{"Slamwich","lambslamwich.jpg"},
                {"Caesar Salad","caesarsalad.jpg"},{"Salad","caesarsalad.jpg"},
                {"Toasted Sandwich","toastedsandwich.jpg"},{"Toastie","toastedsandwich.jpg"},
                {"Soup","soup.jpg"},
                // ── Breakfast ─────────────────────────────────────────────
                {"Avocado Toast","avocadotoast.jpg"},{"Avo Toast","avocadotoast.jpg"},
                {"Croissant","croissant.jpg"},
                {"Eggs Benedict","eggsbenedict.jpg"},{"Benny","eggsbenedict.jpg"},
                {"Full Breakfast","fullbreakfast.jpg"},{"Full English","fullbreakfast.jpg"},
                {"Pancakes","pancakes.jpg"},{"Waffle","waffle.jpg"},{"Waffles","waffle.jpg"},
                {"Toast","toast.jpg"},
                // ── Desserts ──────────────────────────────────────────────
                {"Brownie","brownie.jpg"},{"Chocolate Brownie","brownie.jpg"},
                {"Cheesecake","cheesecake.jpg"},
                {"Lemon Tart","lemontart.jpg"},{"Lemon Meringue","lemontart.jpg"},
                {"Banana Muffin","bananamuffin.jpg"},{"Muffin","bananamuffin.jpg"},
                {"Scone","scone.jpg"},
                {"Cake","cake.jpg"},{"Chocolate Cake","cake.jpg"},
                {"Cupcake","cupcake.jpg"},
            };

        // Tries the mapped filename with both .jpg and .jpeg, then does a
        // case-insensitive scan of the Resources folder as a final fallback.
        // Resolves the embedded resource name for a menu item, then crops it to w x h.
        // Images must be added to the project under a "Resources" folder with
        // Build Action = Embedded Resource.
        // Embedded resource names look like:  Cafe101.Resources.coldbrew.jpg
        Image LoadEmbeddedImage(string itemName, int w, int h)
        {
            // Build the stem list from the map — longest keys first so "cold brew"
            // is tested before "brew".
            string stem = null;

            // 1 — exact match
            if (_imageMap.TryGetValue(itemName, out string mf))
                stem = System.IO.Path.GetFileNameWithoutExtension(mf);

            // 2 — partial key match
            if (stem == null)
                foreach (var kv in _imageMap.OrderByDescending(x => x.Key.Length))
                    if (itemName.IndexOf(kv.Key, StringComparison.OrdinalIgnoreCase) >= 0)
                    { stem = System.IO.Path.GetFileNameWithoutExtension(kv.Value); break; }

            if (stem == null) return null;

            // Try to open the embedded stream — test .jpg, .jpeg, .png
            var asm = System.Reflection.Assembly.GetExecutingAssembly();
            string ns  = asm.GetName().Name;   // e.g. "Cafe101"
            System.IO.Stream stream = null;
            foreach (string ext in new[] { ".jpg", ".jpeg", ".png" })
            {
                string resName = $"{ns}.Resources.{stem}{ext}";
                stream = asm.GetManifestResourceStream(resName);
                if (stream != null) break;
            }
            if (stream == null) return null;

            using (stream)
            using (var src = Image.FromStream(stream, false, true))
            {
                int sw = src.Width, sh = src.Height;
                if (sw == 0 || sh == 0) return null;

                float sr = (float)sw / sh, tr = (float)w / h;
                Rectangle rc = sr > tr
                    ? new Rectangle((sw - (int)(sh * tr)) / 2, 0, (int)(sh * tr), sh)
                    : new Rectangle(0, (sh - (int)(sw / tr)) / 3, sw, (int)(sw / tr));

                var bmp = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.InterpolationMode  = InterpolationMode.HighQualityBicubic;
                    g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    g.SmoothingMode      = SmoothingMode.AntiAlias;
                    g.DrawImage(src, new Rectangle(0, 0, w, h), rc, GraphicsUnit.Pixel);
                }
                return bmp;
            }
        }

        // Draws a professional GDI+ food/drink illustration when no photo is available
        static Bitmap GenerateFoodArt(string itemName, string cat, Color accent, int w, int h)
        {
            var bmp = new Bitmap(w, h);
            using (var g = Graphics.FromImage(bmp)) {
                g.SmoothingMode      = SmoothingMode.AntiAlias;
                g.TextRenderingHint  = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

                // Soft background gradient per category
                Color bgTop = cat == "Drinks"    ? Color.FromArgb(228, 242, 252) :
                              cat == "Breakfast" ? Color.FromArgb(255, 248, 235) :
                              cat == "Desserts"  ? Color.FromArgb(252, 238, 252) :
                              cat == "Lunch"     ? Color.FromArgb(235, 252, 240) :
                                                   Color.FromArgb(248, 245, 240);
                Color bgBot = cat == "Drinks"    ? Color.FromArgb(205, 225, 248) :
                              cat == "Breakfast" ? Color.FromArgb(248, 232, 205) :
                              cat == "Desserts"  ? Color.FromArgb(242, 218, 248) :
                              cat == "Lunch"     ? Color.FromArgb(215, 245, 222) :
                                                   Color.FromArgb(235, 228, 218);
                using (var bgBr = new LinearGradientBrush(new Point(0,0), new Point(0,h), bgTop, bgBot))
                    g.FillRectangle(bgBr, 0, 0, w, h);

                string lo = itemName.ToLower();
                int cx = w / 2, cy = h / 2;

                // ── DRINKS ─────────────────────────────────────────
                if (lo.Contains("cappuccino") || lo.Contains("flat white") || lo.Contains("latte") ||
                    lo.Contains("espresso") || lo.Contains("americano")) {
                    DrawCoffeeCup(g, cx, cy, Color.FromArgb(95, 62, 35), Color.FromArgb(210, 170, 120),
                                  milkFoam: true, steamColor: Color.FromArgb(160, 190, 185, 180));
                } else if (lo.Contains("cold brew") || lo.Contains("iced coffee")) {
                    DrawIcedDrink(g, cx, cy, Color.FromArgb(60, 38, 18), Color.FromArgb(180, 140, 90));
                } else if (lo.Contains("hot choc") || lo.Contains("chocolate")) {
                    DrawCoffeeCup(g, cx, cy, Color.FromArgb(72, 40, 18), Color.FromArgb(220, 195, 158),
                                  milkFoam: true, steamColor: Color.FromArgb(150, 200, 195, 190));
                } else if (lo.Contains("juice") || lo.Contains("orange")) {
                    DrawTallGlass(g, cx, cy, Color.FromArgb(255, 170, 20), Color.FromArgb(255, 210, 80));
                } else if (lo.Contains("tea") || lo.Contains("rooibos")) {
                    DrawCoffeeCup(g, cx, cy, Color.FromArgb(160, 80, 40), Color.FromArgb(220, 195, 158),
                                  milkFoam: false, steamColor: Color.FromArgb(140, 200, 195, 190));
                } else if (lo.Contains("smoothie") || lo.Contains("shake") || lo.Contains("milkshake")) {
                    DrawTallGlass(g, cx, cy, Color.FromArgb(230, 120, 160), Color.FromArgb(255, 180, 200));
                } else if (cat == "Drinks") {
                    DrawTallGlass(g, cx, cy, Color.FromArgb(80, 160, 220), Color.FromArgb(140, 200, 245));

                // ── BREAKFAST ──────────────────────────────────────
                } else if (lo.Contains("egg") || lo.Contains("benedict") || lo.Contains("full breakfast")) {
                    DrawFriedEgg(g, cx, cy);
                } else if (lo.Contains("croissant")) {
                    DrawCroissant(g, cx, cy);
                } else if (lo.Contains("toast") || lo.Contains("avocado")) {
                    DrawToast(g, cx, cy, lo.Contains("avocado"));
                } else if (lo.Contains("pancake") || lo.Contains("waffle")) {
                    DrawPancakes(g, cx, cy);
                } else if (cat == "Breakfast") {
                    DrawFriedEgg(g, cx, cy);

                // ── LUNCH ──────────────────────────────────────────
                } else if (lo.Contains("salad") || lo.Contains("caesar")) {
                    DrawSaladBowl(g, cx, cy);
                } else if (lo.Contains("sandwich") || lo.Contains("slamwich")) {
                    DrawSandwich(g, cx, cy);
                } else if (lo.Contains("soup")) {
                    DrawSoupBowl(g, cx, cy);
                } else if (cat == "Lunch") {
                    DrawPlatedMeal(g, cx, cy, accent);

                // ── DESSERTS ───────────────────────────────────────
                } else if (lo.Contains("cheesecake") || lo.Contains("cake")) {
                    DrawCakeSlice(g, cx, cy, Color.FromArgb(245, 210, 165), Color.FromArgb(240, 80, 80));
                } else if (lo.Contains("brownie")) {
                    DrawBrownie(g, cx, cy);
                } else if (lo.Contains("tart") || lo.Contains("lemon")) {
                    DrawCakeSlice(g, cx, cy, Color.FromArgb(250, 235, 140), Color.FromArgb(200, 180, 60));
                } else if (lo.Contains("muffin") || lo.Contains("cupcake")) {
                    DrawMuffin(g, cx, cy);
                } else if (lo.Contains("scone")) {
                    DrawScone(g, cx, cy);
                } else {
                    DrawPlatedMeal(g, cx, cy, accent);
                }
            }
            return bmp;
        }

        // ── Drawing helpers ───────────────────────────────────────
        static void DrawCoffeeCup(Graphics g, int cx, int cy, Color cupColor, Color saucerColor,
                                   bool milkFoam, Color steamColor)
        {
            // Saucer
            using (var br = new SolidBrush(saucerColor))
                g.FillEllipse(br, cx - 30, cy + 22, 60, 13);
            using (var p = new Pen(Color.FromArgb(180, cupColor), 1f))
                g.DrawEllipse(p, cx - 30, cy + 22, 60, 13);

            // Cup body (trapezoid)
            var cupPts = new Point[] {
                new Point(cx - 22, cy - 10), new Point(cx + 22, cy - 10),
                new Point(cx + 18, cy + 26), new Point(cx - 18, cy + 26)
            };
            using (var br = new SolidBrush(cupColor))         g.FillPolygon(br, cupPts);
            using (var p  = new Pen(Color.FromArgb(60,0,0,0),1f)) g.DrawPolygon(p, cupPts);

            // Handle
            using (var p = new Pen(cupColor, 4f) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round })
                g.DrawArc(p, cx + 14, cy + 2, 16, 16, -80, 170);

            // Coffee surface or milk foam
            if (milkFoam) {
                using (var br = new SolidBrush(Color.FromArgb(245, 235, 218)))
                    g.FillEllipse(br, cx - 20, cy - 14, 40, 11);
                // Latte art dot
                using (var br = new SolidBrush(Color.FromArgb(150, 100, 55)))
                    g.FillEllipse(br, cx - 5, cy - 11, 10, 6);
            } else {
                using (var br = new SolidBrush(Color.FromArgb(110, 68, 30)))
                    g.FillEllipse(br, cx - 20, cy - 14, 40, 11);
            }

            // Steam wisps
            using (var p = new Pen(steamColor, 1.8f) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round }) {
                g.DrawCurve(p, new PointF[]{ new PointF(cx-9,cy-16), new PointF(cx-13,cy-24), new PointF(cx-7,cy-32) });
                g.DrawCurve(p, new PointF[]{ new PointF(cx,  cy-16), new PointF(cx-2, cy-26), new PointF(cx+4, cy-34) });
                g.DrawCurve(p, new PointF[]{ new PointF(cx+9,cy-16), new PointF(cx+11,cy-24), new PointF(cx+5, cy-32) });
            }
        }

        static void DrawIcedDrink(Graphics g, int cx, int cy, Color liquidColor, Color glassColor)
        {
            // Glass
            var pts = new PointF[] {
                new PointF(cx-18, cy-24), new PointF(cx+18, cy-24),
                new PointF(cx+14, cy+26), new PointF(cx-14, cy+26)
            };
            using (var br = new SolidBrush(Color.FromArgb(90, liquidColor)))  g.FillPolygon(br, pts);
            using (var p  = new Pen(Color.FromArgb(160, glassColor), 1.5f))   g.DrawPolygon(p, pts);
            // Liquid fill (bottom 70%)
            var fill = new PointF[] {
                new PointF(cx-15, cy-4), new PointF(cx+15, cy-4),
                new PointF(cx+14, cy+26), new PointF(cx-14, cy+26)
            };
            using (var br = new SolidBrush(Color.FromArgb(180, liquidColor))) g.FillPolygon(br, fill);
            // Ice cubes
            using (var br = new SolidBrush(Color.FromArgb(200, 235, 248, 255))) {
                g.FillRectangle(br, cx - 12, cy - 2, 10, 10);
                g.FillRectangle(br, cx + 2,  cy - 6, 10, 10);
            }
            // Straw
            using (var p = new Pen(Color.FromArgb(220, 80, 80), 3f))
                g.DrawLine(p, cx + 8, cy - 24, cx + 12, cy + 26);
        }

        static void DrawTallGlass(Graphics g, int cx, int cy, Color liquidColor, Color highlightColor)
        {
            var pts = new PointF[] {
                new PointF(cx-16, cy-26), new PointF(cx+16, cy-26),
                new PointF(cx+13, cy+26), new PointF(cx-13, cy+26)
            };
            using (var br = new LinearGradientBrush(new Point(cx-16,0), new Point(cx+16,0), liquidColor, highlightColor))
                g.FillPolygon(br, pts);
            using (var p = new Pen(Color.FromArgb(80, Color.White), 1.5f)) g.DrawPolygon(p, pts);
            // Highlight stripe
            using (var p = new Pen(Color.FromArgb(100, 255, 255, 255), 3f))
                g.DrawLine(p, cx - 12, cy - 22, cx - 10, cy + 22);
            // Straw
            using (var p = new Pen(Color.FromArgb(240, 180, 40, 40), 3f))
                g.DrawLine(p, cx + 8, cy - 32, cx + 10, cy + 26);
        }

        static void DrawFriedEgg(Graphics g, int cx, int cy)
        {
            // Plate
            using (var br = new SolidBrush(Color.FromArgb(240, 237, 230)))
                g.FillEllipse(br, cx - 32, cy - 4, 64, 40);
            using (var p = new Pen(Color.FromArgb(200, 190, 175), 1f))
                g.DrawEllipse(p, cx - 32, cy - 4, 64, 40);
            // Egg white
            using (var br = new SolidBrush(Color.White))
                g.FillEllipse(br, cx - 20, cy - 2, 40, 26);
            // Yolk
            using (var br = new SolidBrush(Color.FromArgb(255, 200, 35)))
                g.FillEllipse(br, cx - 9, cy + 3, 18, 14);
            using (var br = new SolidBrush(Color.FromArgb(80, 255, 235, 100)))
                g.FillEllipse(br, cx - 6, cy + 5, 7, 6);
            // Bacon strip
            using (var br = new SolidBrush(Color.FromArgb(180, 70, 55)))
                GdiHelper.FillRoundRect(g, cx - 26, cy + 26, 52, 7, 3, br);
            using (var br = new SolidBrush(Color.FromArgb(230, 170, 130)))
                GdiHelper.FillRoundRect(g, cx - 26, cy + 27, 52, 2, 1, br);
        }

        static void DrawCroissant(Graphics g, int cx, int cy)
        {
            using (var br = new SolidBrush(Color.FromArgb(210, 160, 65))) {
                // Main body arc
                g.FillPie(br, cx - 28, cy - 18, 56, 44, 200, 140);
            }
            // Left horn
            using (var br = new SolidBrush(Color.FromArgb(195, 140, 50)))
                g.FillEllipse(br, cx - 28, cy - 14, 18, 12);
            // Right horn
            using (var br = new SolidBrush(Color.FromArgb(195, 140, 50)))
                g.FillEllipse(br, cx + 10, cy - 14, 18, 12);
            // Shine
            using (var br = new SolidBrush(Color.FromArgb(80, 255, 235, 160)))
                g.FillEllipse(br, cx - 8, cy - 10, 16, 8);
        }

        static void DrawToast(Graphics g, int cx, int cy, bool withAvocado)
        {
            // Toast slice
            using (var br = new SolidBrush(Color.FromArgb(210, 168, 90)))
                GdiHelper.FillRoundRect(g, cx - 24, cy - 18, 48, 38, 5, br);
            // Crust darker edge
            using (var p = new Pen(Color.FromArgb(170, 120, 50), 3f))
                GdiHelper.DrawRoundRect(g, cx - 24, cy - 18, 48, 38, 5, p);
            if (withAvocado) {
                // Avocado spread (green)
                using (var br = new SolidBrush(Color.FromArgb(130, 185, 80)))
                    GdiHelper.FillRoundRect(g, cx - 20, cy - 14, 40, 30, 4, br);
                // Avocado pit
                using (var br = new SolidBrush(Color.FromArgb(130, 90, 40)))
                    g.FillEllipse(br, cx - 7, cy - 4, 14, 12);
            } else {
                // Butter gloss
                using (var br = new SolidBrush(Color.FromArgb(100, 255, 240, 150)))
                    GdiHelper.FillRoundRect(g, cx - 18, cy - 12, 36, 24, 3, br);
            }
        }

        static void DrawPancakes(Graphics g, int cx, int cy)
        {
            // Stack of 3 pancakes
            for (int i = 2; i >= 0; i--) {
                int yOff = cy + 8 - i * 12;
                using (var br = new SolidBrush(i == 0 ? Color.FromArgb(215, 170, 80) : Color.FromArgb(200, 152, 65)))
                    g.FillEllipse(br, cx - 24, yOff - 6, 48, 14);
                using (var p = new Pen(Color.FromArgb(170, 120, 45), 1f))
                    g.DrawEllipse(p, cx - 24, yOff - 6, 48, 14);
            }
            // Butter pat
            using (var br = new SolidBrush(Color.FromArgb(255, 230, 80)))
                g.FillEllipse(br, cx - 7, cy - 22, 14, 8);
            // Syrup drip
            using (var p = new Pen(Color.FromArgb(180, 150, 60, 10), 3f))
                g.DrawCurve(p, new PointF[]{ new PointF(cx+10,cy-14), new PointF(cx+14,cy-4), new PointF(cx+16,cy+8)});
        }

        static void DrawSaladBowl(Graphics g, int cx, int cy)
        {
            // Bowl
            using (var br = new SolidBrush(Color.FromArgb(240, 230, 210)))
                g.FillEllipse(br, cx - 30, cy + 2, 60, 28);
            using (var p = new Pen(Color.FromArgb(200, 185, 160), 1.5f))
                g.DrawEllipse(p, cx - 30, cy + 2, 60, 28);
            // Greens
            var leafData = new (int x, int y, int rw, int rh, Color c)[] {
                (cx-18, cy-14, 22, 14, Color.FromArgb(72, 158, 58)),
                (cx-2,  cy-18, 20, 13, Color.FromArgb(55, 140, 48)),
                (cx+8,  cy-12, 22, 14, Color.FromArgb(85, 170, 60)),
                (cx-10, cy-8,  18, 12, Color.FromArgb(65, 148, 52)),
                (cx+2,  cy-4,  20, 13, Color.FromArgb(90, 175, 65)),
            };
            foreach (var (lx, ly, rw, rh, lc) in leafData)
                using (var br = new SolidBrush(lc)) g.FillEllipse(br, lx - rw/2, ly - rh/2, rw, rh);
            // Tomato
            using (var br = new SolidBrush(Color.FromArgb(220, 60, 55)))
                g.FillEllipse(br, cx + 10, cy - 2, 12, 10);
            // Crouton
            using (var br = new SolidBrush(Color.FromArgb(210, 175, 90)))
                g.FillRectangle(br, cx - 16, cy - 2, 9, 9);
        }

        static void DrawSandwich(Graphics g, int cx, int cy)
        {
            // Bottom bread
            using (var br = new SolidBrush(Color.FromArgb(210, 168, 90)))
                GdiHelper.FillRoundRect(g, cx - 26, cy + 12, 52, 12, 4, br);
            // Filling layers
            using (var br = new SolidBrush(Color.FromArgb(80, 160, 60)))   // lettuce
                GdiHelper.FillRoundRect(g, cx - 24, cy + 4, 48, 10, 3, br);
            using (var br = new SolidBrush(Color.FromArgb(215, 85, 65)))   // tomato
                GdiHelper.FillRoundRect(g, cx - 22, cy - 2, 44, 8, 2, br);
            using (var br = new SolidBrush(Color.FromArgb(235, 200, 140))) // cheese
                GdiHelper.FillRoundRect(g, cx - 24, cy - 8, 48, 7, 2, br);
            using (var br = new SolidBrush(Color.FromArgb(180, 100, 70)))  // patty
                GdiHelper.FillRoundRect(g, cx - 26, cy - 16, 52, 9, 3, br);
            // Top bread
            using (var br = new SolidBrush(Color.FromArgb(215, 172, 92)))
                GdiHelper.FillRoundRect(g, cx - 26, cy - 26, 52, 12, 6, br);
            // Seeds on top
            using (var br = new SolidBrush(Color.FromArgb(240, 220, 140))) {
                g.FillEllipse(br, cx - 10, cy - 24, 5, 3);
                g.FillEllipse(br, cx,      cy - 25, 5, 3);
                g.FillEllipse(br, cx + 10, cy - 24, 5, 3);
            }
        }

        static void DrawSoupBowl(Graphics g, int cx, int cy)
        {
            using (var br = new SolidBrush(Color.FromArgb(240, 230, 210)))
                g.FillEllipse(br, cx - 30, cy - 4, 60, 36);
            using (var br = new SolidBrush(Color.FromArgb(200, 130, 55)))
                g.FillEllipse(br, cx - 24, cy - 1, 48, 26);
            using (var p = new Pen(Color.FromArgb(220, 160, 80), 1.5f) {DashStyle = DashStyle.Dot})
                g.DrawLine(p, cx - 20, cy + 8, cx + 20, cy + 8);
            // Steam
            using (var p = new Pen(Color.FromArgb(130, 200, 195, 190), 1.8f)) {
                g.DrawCurve(p, new PointF[]{new PointF(cx-8,cy-6),new PointF(cx-11,cy-14),new PointF(cx-6,cy-22)});
                g.DrawCurve(p, new PointF[]{new PointF(cx+6,cy-6),new PointF(cx+9, cy-14),new PointF(cx+4, cy-22)});
            }
        }

        static void DrawPlatedMeal(Graphics g, int cx, int cy, Color accent)
        {
            using (var br = new SolidBrush(Color.FromArgb(242, 237, 228)))
                g.FillEllipse(br, cx - 32, cy - 12, 64, 42);
            using (var p = new Pen(Color.FromArgb(210, 200, 185), 1.5f))
                g.DrawEllipse(p, cx - 32, cy - 12, 64, 42);
            using (var br = new SolidBrush(Color.FromArgb(60, accent)))
                g.FillEllipse(br, cx - 22, cy - 6, 44, 28);
            using (var br = new SolidBrush(accent))
                g.FillEllipse(br, cx - 14, cy - 2, 28, 18);
            using (var br = new SolidBrush(Color.FromArgb(100, 255, 255, 255)))
                g.FillEllipse(br, cx - 8, cy, 10, 7);
        }

        static void DrawCakeSlice(Graphics g, int cx, int cy, Color bodyColor, Color toppingColor)
        {
            // Plate
            using (var br = new SolidBrush(Color.FromArgb(240, 235, 225)))
                g.FillEllipse(br, cx - 32, cy + 10, 64, 22);
            // Slice body
            using (var br = new SolidBrush(bodyColor))
                g.FillPie(br, cx - 26, cy - 20, 52, 42, 250, 100);
            // Topping layer
            using (var br = new SolidBrush(toppingColor))
                g.FillPie(br, cx - 26, cy - 20, 52, 12, 250, 100);
            // Fork
            using (var p = new Pen(Color.FromArgb(180, 170, 160), 2f)) {
                g.DrawLine(p, cx + 22, cy + 20, cx + 28, cy - 4);
                g.DrawLine(p, cx + 24, cy + 20, cx + 30, cy - 4);
            }
        }

        static void DrawBrownie(Graphics g, int cx, int cy)
        {
            using (var br = new SolidBrush(Color.FromArgb(80, 48, 22)))
                GdiHelper.FillRoundRect(g, cx - 24, cy - 18, 48, 38, 5, br);
            // Crackled top
            using (var br = new SolidBrush(Color.FromArgb(62, 36, 14)))
                GdiHelper.FillRoundRect(g, cx - 22, cy - 16, 44, 14, 3, br);
            // Chocolate chips
            using (var br = new SolidBrush(Color.FromArgb(38, 20, 8))) {
                g.FillEllipse(br, cx - 12, cy - 12, 8, 6);
                g.FillEllipse(br, cx + 5,  cy - 10, 8, 6);
                g.FillEllipse(br, cx - 4,  cy - 4,  8, 6);
                g.FillEllipse(br, cx + 10, cy + 2,  7, 5);
            }
            // Shine
            using (var br = new SolidBrush(Color.FromArgb(50, 255, 220, 160)))
                g.FillEllipse(br, cx - 10, cy - 15, 18, 8);
        }

        static void DrawMuffin(Graphics g, int cx, int cy)
        {
            // Base (cup)
            using (var br = new SolidBrush(Color.FromArgb(200, 180, 140)))
                GdiHelper.FillRoundRect(g, cx - 18, cy + 2, 36, 20, 3, br);
            // Wrapper lines
            using (var p = new Pen(Color.FromArgb(170, 150, 110), 1f)) {
                g.DrawLine(p, cx - 10, cy + 2, cx - 10, cy + 22);
                g.DrawLine(p, cx,      cy + 2, cx,      cy + 22);
                g.DrawLine(p, cx + 10, cy + 2, cx + 10, cy + 22);
            }
            // Dome top
            using (var br = new SolidBrush(Color.FromArgb(210, 160, 70)))
                g.FillEllipse(br, cx - 20, cy - 16, 40, 24);
            // Blueberries / chips on dome
            using (var br = new SolidBrush(Color.FromArgb(80, 60, 150))) {
                g.FillEllipse(br, cx - 10, cy - 12, 8, 7);
                g.FillEllipse(br, cx + 4,  cy - 8,  8, 7);
                g.FillEllipse(br, cx - 3,  cy - 2,  7, 6);
            }
        }

        static void DrawScone( Graphics g, int cx, int cy)
        {
            using (var br = new SolidBrush(Color.FromArgb(225, 195, 145)))
                g.FillEllipse(br, cx - 24, cy - 12, 48, 34);
            using (var br = new SolidBrush(Color.FromArgb(200, 165, 110)))
                g.FillEllipse(br, cx - 24, cy - 12, 48, 16);
            // Clotted cream
            using (var br = new SolidBrush(Color.FromArgb(252, 248, 240)))
                g.FillEllipse(br, cx - 14, cy - 8, 28, 16);
            // Jam dot
            using (var br = new SolidBrush(Color.FromArgb(210, 45, 55)))
                g.FillEllipse(br, cx - 7, cy - 4, 14, 10);
        }


        private Panel MakeProductCard(DataRow row)
        {
            int id       = Convert.ToInt32(row["ItemID"]);
            string name  = row["Name"].ToString();
            string cat   = row["Category"].ToString();
            decimal price = Convert.ToDecimal(row["Price"]);
            int stock    = Convert.ToInt32(row["StockQty"]);
            bool avail   = stock > 0;

            // Category accent colour
            Color accent = cat == "Drinks"    ? Color.FromArgb(52, 152, 219) :
                           cat == "Breakfast" ? Color.FromArgb(230, 126, 34)  :
                           cat == "Desserts"  ? Color.FromArgb(155, 89,  182) :
                           cat == "Lunch"     ? Color.FromArgb(46,  204, 113) : Brown;

            // Load real photo once, cover-cropped to photo area dimensions
            const int PHOTO_W = 190, PHOTO_H = 110;
            Image photo = null;
            try { photo = LoadEmbeddedImage(name, PHOTO_W, PHOTO_H); } catch { photo = null; }

            var card = new Panel
            {
                Size      = new Size(190, 228),
                BackColor = avail ? Color.White : Color.FromArgb(245, 242, 240),
                Margin    = new Padding(7),
                Cursor    = avail ? Cursors.Hand : Cursors.Default
            };

            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // ── Photo area (top 110 px) ───────────────────────────────
                var photoRect = new Rectangle(0, 0, PHOTO_W, PHOTO_H);

                if (photo != null)
                {
                    // Draw the real cover-cropped photo — fills edge to edge, no padding
                    g.DrawImage(photo, photoRect);

                    // Subtle dark vignette at bottom of photo for text contrast below
                    using (var overlay = new LinearGradientBrush(
                        new Point(0, PHOTO_H - 28), new Point(0, PHOTO_H),
                        Color.Transparent, Color.FromArgb(70, 0, 0, 0)))
                        g.FillRectangle(overlay, 0, PHOTO_H - 28, PHOTO_W, 28);

                    // If unavailable, apply grey tint over the real photo
                    if (!avail)
                        g.FillRectangle(new SolidBrush(Color.FromArgb(115, 220, 216, 212)), photoRect);
                }
                else
                {
                    // ── Professional gradient placeholder (no emoji, no icons) ──
                    int r1 = (int)(accent.R * 0.15 + 238 * 0.85);
                    int g1 = (int)(accent.G * 0.15 + 233 * 0.85);
                    int b1 = (int)(accent.B * 0.15 + 228 * 0.85);
                    int r2 = (int)(accent.R * 0.28 + 215 * 0.72);
                    int g2 = (int)(accent.G * 0.28 + 208 * 0.72);
                    int b2 = (int)(accent.B * 0.28 + 203 * 0.72);
                    Color bgTop = Color.FromArgb(Math.Min(255,r1), Math.Min(255,g1), Math.Min(255,b1));
                    Color bgBot = Color.FromArgb(Math.Min(255,r2), Math.Min(255,g2), Math.Min(255,b2));

                    using (var bg = new LinearGradientBrush(new Point(0, 0), new Point(0, PHOTO_H), bgTop, bgBot))
                        g.FillRectangle(bg, photoRect);

                    // Thin accent bar at top of placeholder
                    g.FillRectangle(new SolidBrush(Color.FromArgb(avail ? 180 : 100, accent)), 0, 0, PHOTO_W, 4);

                    // Category name centred in placeholder — clean, minimal
                    string catLabel = cat == "Drinks"    ? "BEVERAGES"  :
                                      cat == "Breakfast" ? "BREAKFAST"  :
                                      cat == "Desserts"  ? "DESSERTS"   :
                                      cat == "Lunch"     ? "LUNCH"      : "MENU ITEM";
                    using (var lf = new Font("Segoe UI Semibold", 9f))
                    using (var lb = new SolidBrush(Color.FromArgb(avail ? 150 : 100, accent)))
                    {
                        var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        g.DrawString(catLabel, lf, lb, new RectangleF(0, 0, PHOTO_W, PHOTO_H), sf);
                    }

                }

                // ── Card border ──────────────────────────────────────────
                Color border = avail ? Color.FromArgb(215, 205, 195) : Color.FromArgb(200, 195, 190);
                using (var pen = new Pen(border, 1.5f))
                    g.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);

                // ── Accent strip at very top (4 px, same colour as category) ──
                g.FillRectangle(new SolidBrush(Color.FromArgb(avail ? 255 : 160, accent)), 0, 0, card.Width, 4);
            };

            // ── Item name ────────────────────────────────────────────────
            card.Controls.Add(new Label
            {
                Text      = name,
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                Location  = new Point(8, PHOTO_H + 6),
                Size      = new Size(174, 38),
                TextAlign = ContentAlignment.TopCenter,
                ForeColor = avail ? Color.FromArgb(55, 40, 25) : Color.FromArgb(155, 145, 135),
                BackColor = Color.Transparent
            });

            // ── Price ────────────────────────────────────────────────────
            card.Controls.Add(new Label
            {
                Text      = $"R {price:N2}",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                Location  = new Point(8, PHOTO_H + 48),
                Size      = new Size(174, 26),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = avail ? Brown : Color.FromArgb(155, 145, 135),
                BackColor = Color.Transparent
            });

            // ── Stock badge ──────────────────────────────────────────────
            card.Controls.Add(new Label
            {
                Text      = stock == 0 ? "OUT OF STOCK" : stock <= 5 ? $"⚠  Only {stock} left" : "✓  In stock",
                Font      = new Font("Segoe UI", 8.5f),
                Location  = new Point(8, PHOTO_H + 78),
                Size      = new Size(174, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = stock == 0 ? Color.FromArgb(200, 60,  60) :
                            stock <= 5 ? Color.FromArgb(220, 120,  0) :
                                         Color.FromArgb( 55, 148, 55),
                BackColor = Color.Transparent
            });

            // ── Hover & click ────────────────────────────────────────────
            if (avail)
            {
                EventHandler click = (s, e) => AddToCart(id, name, price, stock);
                card.Click += click;
                foreach (Control c in card.Controls) c.Click += click;
                card.MouseEnter += (s, e) => { card.BackColor = Color.FromArgb(255, 248, 240); card.Invalidate(); };
                card.MouseLeave += (s, e) => { card.BackColor = Color.White;                   card.Invalidate(); };
            }

            return card;
        }

        // DrawRoundedRect is in GdiExtensions static class below


        // ═════════════════════════════════════════════════════════
        //  CART LOGIC
        // ═════════════════════════════════════════════════════════
        void AddToCart(int itemId, string name, decimal price, int stock)
        {
            var ex = _cart.Find(c => c.ItemID == itemId);
            if (ex != null) {
                if (ex.Quantity >= stock) { SetStatus($"⚠  Max stock reached for {name}", true); return; }
                ex.Quantity++;
            } else _cart.Add(new CartItem { ItemID = itemId, Name = name, UnitPrice = price, Quantity = 1 });
            RefreshCart();
            SetStatus($"✔  {name} added to order", false);
        }

        void ChangeQtyAt(int idx, int delta)
        {
            if (idx < 0 || idx >= _cart.Count) return;
            _cart[idx].Quantity = Math.Max(1, _cart[idx].Quantity + delta);
            RefreshCart();
        }

        void RefreshCart()
        {
            if (_dgCart == null) return;
            _dgCart.Rows.Clear();
            decimal sub = 0;
            foreach (var item in _cart) {
                _dgCart.Rows.Add(item.Name, "−", item.Quantity, "+", $"R {item.UnitPrice:N2}", $"R {item.Subtotal:N2}", "✕");
                sub += item.Subtotal;
            }
            decimal sidesSub = 0;
            foreach (var (cb, pr) in _sidesControls) if (cb.Checked) sidesSub += pr;
            sub += sidesSub;
            decimal vat = sub * 0.15m;
            if (_lblSubtotal   != null) _lblSubtotal.Text   = $"R {sub:N2}";
            if (_lblVat        != null) _lblVat.Text        = $"R {vat:N2}";
            if (_lblTotal      != null) _lblTotal.Text      = $"R {sub + vat:N2}";
            if (_lblOrderCount != null) _lblOrderCount.Text = _cart.Count > 0 || sidesSub > 0 ? $"  {_cart.Count} item(s)" : "";
        }

        // ═════════════════════════════════════════════════════════
        //  PLACE ORDER
        // ═════════════════════════════════════════════════════════
        void BtnPlaceOrder_Click(object sender, EventArgs e)
        {
            bool hasSides = _sidesControls.Any(x => x.cb.Checked);
            if (_cart.Count == 0 && !hasSides) { SetStatus("⚠  Cart is empty — add items first", true); return; }
            if (_cmbPayment?.SelectedItem == null) { SetStatus("⚠  Please select a payment method", true); return; }

            string type    = _cmbOrderType?.Text?.Trim() ?? "Takeaway";
            string payment = _cmbPayment?.Text?.Trim()   ?? "Cash";

            // ── Delivery address validation (single check) ────────
            string deliveryCity   = _txtDeliveryAddress?.Text?.Trim() ?? "";
            string deliverySuburb = _txtDeliveryAddress?.Tag?.ToString()?.Trim() ?? "";
            string deliveryAddr   = string.IsNullOrEmpty(deliverySuburb)
                ? deliveryCity
                : string.IsNullOrEmpty(deliveryCity)
                    ? deliverySuburb
                    : $"{deliveryCity}, {deliverySuburb}";

            if (type == "Delivery") {
                if (string.IsNullOrEmpty(deliveryCity)) {
                    SetStatus("⚠  Enter the delivery city", true);
                    _txtDeliveryAddress?.Focus(); return;
                }
                if (string.IsNullOrEmpty(deliverySuburb)) {
                    SetStatus("⚠  Enter the delivery suburb", true); return;
                }
            }

            string customer = "";
            if (_cmbCustDropdown?.SelectedItem is CustItem ci && ci.Id > 0)
                customer = ci.Display.Contains("(") ? ci.Display.Substring(0, ci.Display.IndexOf("(")).Trim() : ci.Display;
            else if (_txtCustSearch?.ForeColor != Color.Gray && !string.IsNullOrWhiteSpace(_txtCustSearch?.Text))
                customer = _txtCustSearch.Text.Trim();
            if (string.IsNullOrWhiteSpace(customer)) customer = "Guest";

            decimal sidesSub = 0; var sideLines = new List<string>();
            foreach (var (cb, pr) in _sidesControls) if (cb.Checked) {
                sidesSub += pr;
                sideLines.Add(cb.Text.Contains("(") ? cb.Text.Substring(0, cb.Text.IndexOf("(")).Trim() : cb.Text);
            }

            decimal sub = _cart.Sum(i => i.Subtotal) + sidesSub;
            decimal vat = sub * 0.15m, total = sub + vat;
            string notes = (_txtNotes?.Text ?? "").Trim();
            // Prepend delivery address to notes
            if (type == "Delivery" && !string.IsNullOrEmpty(deliveryAddr))
                notes = "🚚 Deliver to: " + deliveryAddr + (notes.Length > 0 ? " | " + notes : "");
            if (sideLines.Count > 0) notes = (notes.Length > 0 ? notes + " | " : "") + "Sides: " + string.Join(", ", sideLines);

            try {
                int orderId = DatabaseHelper.CreateOrder(customer, DatabaseHelper.CurrentUserId,
                    type, null, payment, sub, vat, total, notes);
                foreach (var item in _cart)
                    DatabaseHelper.AddOrderItem(orderId, item.ItemID, item.Name, item.Quantity, item.UnitPrice);

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("═══════════════════════════════");
                sb.AppendLine("           CAFÉ 101");
                sb.AppendLine("      Point of Sale Receipt");
                sb.AppendLine("═══════════════════════════════");
                sb.AppendLine($"Order #:   {orderId}");
                sb.AppendLine($"Customer:  {customer}");
                sb.AppendLine($"Type:      {type}");
                if (type == "Delivery" && !string.IsNullOrEmpty(deliveryAddr))
                    sb.AppendLine($"Address:   {deliveryAddr}");
                sb.AppendLine($"Payment:   {payment}");
                sb.AppendLine($"Cashier:   {DatabaseHelper.CurrentUserName}");
                sb.AppendLine($"Date/Time: {DateTime.Now:dd/MM/yyyy HH:mm}");
                sb.AppendLine("───────────────────────────────");
                foreach (var item in _cart)
                    sb.AppendLine($"{item.Quantity}x  {item.Name,-20} R {item.Subtotal:N2}");
                if (sideLines.Count > 0) {
                    sb.AppendLine("───────────────────────────────");
                    sb.AppendLine("SIDES / ADD-ONS:");
                    foreach (var (cb, pr) in _sidesControls)
                        if (cb.Checked) {
                            string sn = cb.Text.Contains("(") ? cb.Text.Substring(0, cb.Text.IndexOf("(")).Trim() : cb.Text;
                            sb.AppendLine($"   {sn,-24} R {pr:N2}");
                        }
                }
                sb.AppendLine("───────────────────────────────");
                sb.AppendLine($"Subtotal:  R {sub:N2}");
                sb.AppendLine($"VAT (15%): R {vat:N2}");
                sb.AppendLine($"TOTAL:     R {total:N2}");
                sb.AppendLine("═══════════════════════════════");
                sb.AppendLine("    Thank you for visiting!");
                sb.AppendLine("         Café 101  ☕");
                sb.AppendLine("═══════════════════════════════");

                ShowReceiptDialog(sb.ToString(), orderId);
                SetStatus($"✔  Order #{orderId} placed — R {total:N2}", false);
                _cart.Clear();
                foreach (var (cb, _) in _sidesControls) cb.Checked = false;
                RefreshCart(); ResetOrderForm();
                // Auto-refresh Today's Orders tab
                if (_tabMain != null && _tabMain.TabPages.Count > 2) {
                    var ordersTab = _tabMain.TabPages[2];
                    // Re-load orders tab data by switching to it briefly if already visible
                    // The timer on the tab handles background refresh automatically
                }
            } catch (Exception ex) {
                SetStatus("⚠  Error: " + ex.Message, true);
            }
        }


        // ─────────────────────────────────────────────────────────
        //  RECEIPT DIALOG — shows receipt with Print button
        // ─────────────────────────────────────────────────────────
        void ShowReceiptDialog(string receiptText, int orderId)
        {
            var dlgForm = new Form {
                Text            = "Order #" + orderId + "  —  Receipt",
                Size            = new Size(480, 620),
                StartPosition   = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false,
                BackColor       = Color.White,
                Font            = new Font("Segoe UI", 10f)
            };

            var txt = new RichTextBox {
                Location    = new Point(12, 12), Size = new Size(440, 480),
                Font        = new Font("Courier New", 10f),
                BackColor   = Color.FromArgb(252, 250, 248),
                ForeColor   = Color.FromArgb(35, 22, 10),
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly    = true,
                ScrollBars  = RichTextBoxScrollBars.Vertical,
                Text        = receiptText
            };
            dlgForm.Controls.Add(txt);

            var btnPrint = new Button {
                Text      = "Print Receipt",
                Location  = new Point(12, 504), Size = new Size(208, 46),
                BackColor = Color.FromArgb(74, 50, 37), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btnPrint.FlatAppearance.BorderSize = 0;
            btnPrint.Click += (s, e) => PrintReceipt(receiptText, "Order #" + orderId);
            dlgForm.Controls.Add(btnPrint);

            var btnDone = new Button {
                Text      = "Done",
                Location  = new Point(244, 504), Size = new Size(208, 46),
                BackColor = Color.FromArgb(76, 175, 80), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btnDone.FlatAppearance.BorderSize = 0;
            btnDone.Click += (s, e) => dlgForm.Close();
            dlgForm.Controls.Add(btnDone);

            dlgForm.ShowDialog();
        }

        void PrintReceipt(string content, string docTitle)
        {
            var pd = new System.Drawing.Printing.PrintDocument { DocumentName = docTitle };
            string captured = content;
            pd.PrintPage += (s, e) => {
                using (var fN = new System.Drawing.Font("Courier New", 9f)) {
                    float py = e.MarginBounds.Top;
                    float px = e.MarginBounds.Left;
                    float lh = fN.GetHeight(e.Graphics) + 1f;
                    // Split on newline (char code 10) — no escape sequences needed
                    char nlChar = (char)10;
                    System.Text.StringBuilder sb2 = new System.Text.StringBuilder();
                    foreach (char ch in captured)
                        if (ch != (char)13) sb2.Append(ch);
                    string[] plines = sb2.ToString().Split(nlChar);
                    foreach (string pline in plines) {
                        if (py + lh > e.MarginBounds.Bottom) { e.HasMorePages = true; return; }
                        e.Graphics.DrawString(pline, fN, System.Drawing.Brushes.Black, px, py);
                        py += lh;
                    }
                }
            };
            using (var dlg = new System.Windows.Forms.PrintDialog { Document = pd })
                if (dlg.ShowDialog() == DialogResult.OK) pd.Print();
        }

                // ═════════════════════════════════════════════════════════
        //  CUSTOMER HELPERS
        // ═════════════════════════════════════════════════════════
        //  CUSTOMER HELPERS
        // ═════════════════════════════════════════════════════════
        void LoadCustomerDropdown(string filter = "")
        {
            if (_cmbCustDropdown == null) return;
            _cmbCustDropdown.Items.Clear();
            _cmbCustDropdown.Items.Add(new CustItem(0, "— No customer selected —"));
            try {
                var dt = DatabaseHelper.GetCustomers(filter);
                foreach (DataRow r in dt.Rows) {
                    int id = Convert.ToInt32(r["CustomerID"]);
                    string nm = r["FullName"].ToString();
                    string ph = r["Phone"].ToString();
                    _cmbCustDropdown.Items.Add(new CustItem(id,
                        string.IsNullOrEmpty(ph) ? nm : $"{nm} ({ph})"));
                }
            } catch { }
            if (_cmbCustDropdown.Items.Count > 0) _cmbCustDropdown.SelectedIndex = 0;
        }

        void FilterCustomerDropdown()
        {
            if (_txtCustSearch == null || _txtCustSearch.ForeColor == Color.Gray) return;
            string q = _txtCustSearch.Text.Trim();
            LoadCustomerDropdown(q.Length >= 2 ? q : "");
        }

        void ShowQuickAddCustomerDialog()
        {
            var f = new Form { Text = "Register New Customer", Size = new Size(420, 360),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false,
                BackColor = Color.White, Font = new Font("Segoe UI", 10f) };
            int y = 16; int lx = 16; int w = 372;
            var lblErr = new Label { AutoSize = true, ForeColor = Color.FromArgb(180, 50, 50),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold), Visible = false };
            TextBox FT(string lbl) {
                f.Controls.Add(new Label { Text = lbl, Location = new Point(lx, y), AutoSize = true,
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold) }); y += 22;
                var t = new TextBox { Location = new Point(lx, y), Size = new Size(w, 30),
                    Font = new Font("Segoe UI", 10f), BorderStyle = BorderStyle.FixedSingle };
                f.Controls.Add(t); y += 36; return t;
            }
            var tFn = FT("First Name: *"); var tLn = FT("Last Name: *");
            var tPh = FT("Phone: *");      var tEm = FT("Email:");
            tPh.KeyPress += (s, e) => {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != ' ' && e.KeyChar != '-') {
                    e.Handled = true;
                }
            };
            tPh.TextChanged += (s, e) => {
                string t = tPh.Text.Trim();
                tPh.BackColor = (!string.IsNullOrEmpty(t) && !t.StartsWith("0"))
                    ? Color.FromArgb(255, 235, 235) : Color.White;
            };
            lblErr.Location = new Point(lx, y); f.Controls.Add(lblErr); y += 24;
            var btn = new Button { Text = "➕  Add Customer", Location = new Point(lx, y), Size = new Size(w, 42),
                BackColor = Blue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold), Cursor = Cursors.Hand };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (s, e) => {
                lblErr.Visible = false;
                string fn2 = tFn.Text.Trim(), ln2 = tLn.Text.Trim();
                string ph2 = tPh.Text.Trim(), em2 = tEm.Text.Trim();

                if (string.IsNullOrWhiteSpace(fn2)) {
                    lblErr.Text = "⚠  First name is required."; lblErr.Visible = true; tFn.Focus(); return; }
                if (fn2.Length < 2) {
                    lblErr.Text = "⚠  First name must be at least 2 characters."; lblErr.Visible = true; tFn.Focus(); return; }
                if (!System.Text.RegularExpressions.Regex.IsMatch(fn2, @"^[A-Za-z\s\-']+$")) {
                    lblErr.Text = "⚠  First name: letters only."; lblErr.Visible = true; tFn.Focus(); return; }

                if (string.IsNullOrWhiteSpace(ln2)) {
                    lblErr.Text = "⚠  Last name is required."; lblErr.Visible = true; tLn.Focus(); return; }
                if (ln2.Length < 2) {
                    lblErr.Text = "⚠  Last name must be at least 2 characters."; lblErr.Visible = true; tLn.Focus(); return; }
                if (!System.Text.RegularExpressions.Regex.IsMatch(ln2, @"^[A-Za-z\s\-']+$")) {
                    lblErr.Text = "⚠  Last name: letters only."; lblErr.Visible = true; tLn.Focus(); return; }

                if (string.IsNullOrWhiteSpace(ph2)) {
                    lblErr.Text = "⚠  Phone number is required."; lblErr.Visible = true; tPh.Focus(); return; }
                string dig2 = System.Text.RegularExpressions.Regex.Replace(ph2, @"[\s\-\(\)]", "");
                if (!dig2.StartsWith("0")) {
                    lblErr.Text = "⚠  Phone must start with 0 (e.g. 0821234567)."; lblErr.Visible = true; tPh.Focus(); return; }
                if (!System.Text.RegularExpressions.Regex.IsMatch(dig2, @"^\d+$")) {
                    lblErr.Text = "⚠  Phone must contain digits only."; lblErr.Visible = true; tPh.Focus(); return; }
                if (dig2.Length < 10 || dig2.Length > 11) {
                    lblErr.Text = "⚠  Phone must be 10 digits (e.g. 0821234567)."; lblErr.Visible = true; tPh.Focus(); return; }

                if (!string.IsNullOrWhiteSpace(em2)) {
                    if (!System.Text.RegularExpressions.Regex.IsMatch(em2, @"^[^@\s]+@[^@\s]+\.[^@\s]+$")) {
                        lblErr.Text = "⚠  Enter a valid email (e.g. name@example.com)."; lblErr.Visible = true; tEm.Focus(); return; }
                }

                DatabaseHelper.AddCustomer(fn2, ln2, em2, ph2, "", "");
                LoadCustomerDropdown();
                string fn = tFn.Text.Trim() + " " + tLn.Text.Trim();
                for (int i = 0; i < _cmbCustDropdown.Items.Count; i++)
                    if (_cmbCustDropdown.Items[i].ToString().StartsWith(fn)) {
                        _cmbCustDropdown.SelectedIndex = i; break;
                    }
                f.Close();
            };
            f.Controls.Add(btn);
            f.ClientSize = new Size(420, y + 60);
            f.ShowDialog();
        }

        void ResetOrderForm()
        {
            if (_txtCustSearch != null) {
                _txtCustSearch.Text = "Search by name or phone...";
                _txtCustSearch.ForeColor = Color.Gray;
            }
            LoadCustomerDropdown();
            if (_txtNotes           != null) _txtNotes.Clear();
            if (_txtDeliveryAddress != null) {
                _txtDeliveryAddress.Clear();
                _txtDeliveryAddress.Tag = "";
                // Clear suburb — find sibling textbox via parent
                if (_txtDeliveryAddress.Parent != null)
                    foreach (Control ctl in _txtDeliveryAddress.Parent.Controls)
                        if (ctl is TextBox tb && tb != _txtDeliveryAddress && tb.BackColor == Color.FromArgb(255,250,240))
                            tb.Clear();
            }
            if (_cmbOrderType       != null) _cmbOrderType.SelectedIndex = 0;
            if (_cmbPayment   != null) _cmbPayment.SelectedIndex   = 0;
        }

        // ═════════════════════════════════════════════════════════
        //  UTILITIES
        // ═════════════════════════════════════════════════════════
        void SetStatus(string msg, bool isError)
        {
            if (_lblStatus == null || _lblStatus.IsDisposed) return;
            _lblStatus.Text      = msg;
            _lblStatus.ForeColor = isError ? Color.FromArgb(220, 80, 60) : Color.FromArgb(120, 195, 90);
        }

        void ShowHelp()
        {
            MessageBox.Show(
                "CAFÉ 101 — POS HELP\n\n" +
                "PLACING AN ORDER:\n" +
                "  • Click a product card to add it to the cart\n" +
                "  • Use  [ − ]  and  [ + ]  in the cart to adjust quantity\n" +
                "  • Click  ✕  to remove an item\n" +
                "  • Tick any Sides / Add-ons if needed\n" +
                "  • Search or select a registered customer (optional)\n" +
                "  • Click PLACE ORDER to confirm\n\n" +
                "CUSTOMERS TAB:\n  • Add, edit or delete customer records\n\n" +
                "TODAY'S ORDERS TAB:\n  • View all orders placed today\n\n" +
                "STATUS: Pending → Preparing → Ready → Served",
                "Help — POS", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        static Button NavBtn(string text, Color bg)
        {
            var b = new Button { Text = text, Size = new Size(112, 32),
                BackColor = bg, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold), Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0; return b;
        }

        // Designer stubs
        void label4_Click(object s, EventArgs e) { }
        void groupBox3_Enter(object s, EventArgs e) { }



    // ── Static GDI helper (not nested — avoids CS1109) ──────────

    } // end class Cashier

    static class GdiHelper
    {
        public static void FillRoundRect(Graphics g, int x, int y, int w, int h, int r, Brush br)
        {
            using (var path = new System.Drawing.Drawing2D.GraphicsPath()) {
                path.AddArc(x,       y,       r*2, r*2, 180, 90);
                path.AddArc(x+w-r*2, y,       r*2, r*2, 270, 90);
                path.AddArc(x+w-r*2, y+h-r*2, r*2, r*2,   0, 90);
                path.AddArc(x,       y+h-r*2, r*2, r*2,  90, 90);
                path.CloseFigure();
                g.FillPath(br, path);
            }
        }

        public static void DrawRoundRect(Graphics g, int x, int y, int w, int h, int r, Pen pen)
        {
            using (var path = new System.Drawing.Drawing2D.GraphicsPath()) {
                path.AddArc(x,       y,       r*2, r*2, 180, 90);
                path.AddArc(x+w-r*2, y,       r*2, r*2, 270, 90);
                path.AddArc(x+w-r*2, y+h-r*2, r*2, r*2,   0, 90);
                path.AddArc(x,       y+h-r*2, r*2, r*2,  90, 90);
                path.CloseFigure();
                g.DrawPath(pen, path);
            }
        }
    }

} // end namespace Cafe101
