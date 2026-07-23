using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace Cafe101
{
    public class OwnerDashboard : Form
    {
        static readonly Color BrownDk = Color.FromArgb( 74,  50,  37);
        static readonly Color Brown   = Color.FromArgb(111,  78,  55);
        static readonly Color Cream   = Color.FromArgb(248, 244, 238);
        static readonly Color Green   = Color.FromArgb( 46, 160,  80);
        static readonly Color Blue    = Color.FromArgb( 33, 150, 243);
        static readonly Color Orange  = Color.FromArgb(230, 120,   0);
        static readonly Color Purple  = Color.FromArgb(130,  70, 180);
        static readonly Color Red     = Color.FromArgb(200,  50,  50);
        static readonly Color Gold    = Color.FromArgb(200, 155,  40);

        ToolTip  _tip          = new ToolTip { AutoPopDelay = 5000, InitialDelay = 400 };
        string   _printContent = "";

        // ── Date range state (owner can change these) ─────────────
        DateTime _fromDate = DateTime.Today.AddDays(-29);
        DateTime _toDate   = DateTime.Today;
        Panel    _scrollBody;   // kept so Refresh can repopulate

        public OwnerDashboard()
        {
            Text          = "Café 101  —  Owner Analytics Dashboard";
            WindowState   = FormWindowState.Maximized;
            MinimumSize   = new Size(1100, 700);
            BackColor     = Cream;
            Font          = new Font("Segoe UI", 10f);
            StartPosition = FormStartPosition.CenterScreen;
            // Build everything now — no Load event needed
            BuildAll();
        }

        void BuildAll()
        {
            SuspendLayout();

            _scrollBody = new Panel {
                Dock = DockStyle.Fill, AutoScroll = true,
                BackColor = Cream, Padding = new Padding(12, 8, 12, 12)
            };
            Controls.Add(_scrollBody);

            var nav = new Panel { Dock = DockStyle.Top, Height = 58, BackColor = BrownDk };
            nav.Controls.Add(new Label {
                Text = "☕  Café 101  —  Owner Dashboard",
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Color.White, AutoSize = true, Location = new Point(16, 14)
            });
            var lblWelcome = new Label {
                Text = "Welcome,  " + DatabaseHelper.CurrentUserName,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 195, 155),
                AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            var lblUser = new Label {
                Text = "👤  Owner",
                Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(195, 168, 138),
                AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            var btnHelp    = NavBtn("❓  Help",     Color.FromArgb(55, 88, 145));
            var btnRefresh = NavBtn("⟳  Refresh",  Color.FromArgb(38, 118, 58));
            var btnSignOut = NavBtn("⬅  Sign Out", Color.FromArgb(148, 42, 42));
            btnHelp.Click    += (s, e) => new HelpAbout().ShowDialog();
            btnRefresh.Click += (s, e) => { _scrollBody.Controls.Clear(); PopulateBody(_scrollBody); };
            btnSignOut.Click += (s, e) => { Hide(); new Form1().Show(); };
            nav.Controls.Add(lblWelcome);
            nav.Controls.Add(lblUser);
            nav.Controls.Add(btnHelp);
            nav.Controls.Add(btnRefresh);
            nav.Controls.Add(btnSignOut);
            nav.Resize += (s, e) => {
                btnSignOut.Location = new Point(nav.Width - 134, 13);
                btnRefresh.Location = new Point(nav.Width - 272, 13);
                btnHelp.Location    = new Point(nav.Width - 410, 13);
                lblWelcome.Location = new Point(nav.Width - 410 - lblWelcome.Width - 18, 12);
                lblUser.Location    = new Point(nav.Width - 410 - lblUser.Width - 18, 32);
            };
            Controls.Add(nav);

            ResumeLayout(false);
            PopulateBody(_scrollBody);
        }

        // ─────────────────────────────────────────────────────────
        //  POPULATE BODY  — uses only Dock-based layout, no width math
        // ─────────────────────────────────────────────────────────
        void PopulateBody(Panel scroll)
        {
            scroll.SuspendLayout();
            scroll.Controls.Clear();

            // ── ROW 5: Customers + Least sellers ─────────────────
            AddSectionLabel(scroll, "📉  Recent Customers  &  Least Selling Items");
            var r5 = new TableLayoutPanel {
                Dock = DockStyle.Top, Height = 310,
                ColumnCount = 2, BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 8)
            };
            r5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
            r5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));
            var custPanel = GridPanel("👥  Recent Customers");
            var custGrid  = MakeGrid();
            custGrid.DataSource = DatabaseHelper.GetDataTable(
                "SELECT TOP 20 FirstName+' '+LastName AS [Name], " +
                "ISNULL(Phone,'—') AS [Phone], ISNULL(Email,'—') AS [Email], " +
                "CONVERT(NVARCHAR,CreatedAt,103) AS [Registered] " +
                "FROM Customers ORDER BY CreatedAt DESC");
            custPanel.Controls.Add(custGrid);
            var leastPanel = GridPanel("📉  Least Selling Items");
            var leastGrid  = MakeGrid();
            leastGrid.DataSource = DatabaseHelper.GetLeastSellingItems(10);
            leastPanel.Controls.Add(leastGrid);
            r5.Controls.Add(custPanel,  0, 0);
            r5.Controls.Add(leastPanel, 1, 0);
            scroll.Controls.Add(r5);

            // ── ROW 4: Chart + Top 5 ─────────────────────────────
            AddSectionLabel(scroll, "📈  Sales Chart  &  Top 5 Products");
            var r4 = new TableLayoutPanel {
                Dock = DockStyle.Top, Height = 350,
                ColumnCount = 2, BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 8)
            };
            r4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62f));
            r4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38f));
            var chartPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            chartPanel.Paint += (s, e) => DrawBorder(e.Graphics, chartPanel);
            chartPanel.Controls.Add(BuildSalesChart(_fromDate, _toDate));
            var top5Panel = GridPanel("🏆  Top 5 Best Sellers");
            var top5Grid  = MakeGrid();
            top5Grid.DataSource = DatabaseHelper.GetTopSellingItems(5);
            top5Grid.DataBindingComplete += (s, e) => {
                if (top5Grid.Columns["TotalRevenue"] != null)
                    top5Grid.Columns["TotalRevenue"].DefaultCellStyle.Format = "N2";
            };
            top5Panel.Controls.Add(top5Grid);
            r4.Controls.Add(chartPanel, 0, 0);
            r4.Controls.Add(top5Panel,  1, 0);
            scroll.Controls.Add(r4);

            // ── ROW 3: Customer analytics ─────────────────────────
            AddSectionLabel(scroll, "👥  Customer Analytics");
            var r3 = CardRow();
            r3.Controls.Add(KpiCard("New This Week",   DatabaseHelper.GetNewCustomersThisWeek().ToString(),  Blue));
            r3.Controls.Add(KpiCard("New This Month",  DatabaseHelper.GetNewCustomersThisMonth().ToString(), Green));
            r3.Controls.Add(KpiCard("Total Customers", DatabaseHelper.GetTotalCustomers().ToString(),        Purple));
            r3.Controls.Add(KpiCard("Top Product",     DatabaseHelper.GetMostPurchasedProduct(),             Gold));
            scroll.Controls.Add(r3);

            // ── ROW 2: Order analytics — filtered by selected range ──
            AddSectionLabel(scroll, "📋  Order Analytics  —  Selected Period");
            var r2 = CardRow();
            r2.Controls.Add(KpiCard("Total Orders",     DatabaseHelper.GetTotalOrdersInRange(_fromDate, _toDate).ToString(),     Brown));
            r2.Controls.Add(KpiCard("Completed",        DatabaseHelper.GetCompletedOrdersInRange(_fromDate, _toDate).ToString(), Green));
            r2.Controls.Add(KpiCard("Pending / Active", DatabaseHelper.GetPendingOrders().ToString(),   Orange));
            r2.Controls.Add(KpiCard("Cancelled",        DatabaseHelper.GetCancelledOrdersInRange(_fromDate, _toDate).ToString(), Red));
            scroll.Controls.Add(r2);

            // ── ROW 1: Sales KPIs — filtered by selected range ────
            AddSectionLabel(scroll, "📊  Sales Overview  —  Selected Period");
            var r1 = CardRow();
            var rangeData   = DatabaseHelper.GetSalesInRange(_fromDate, _toDate);
            decimal rangeTot = 0; int rangeOrds = 0;
            foreach (DataRow rr in rangeData.Rows) {
                rangeTot  += Convert.ToDecimal(rr["Revenue"]);
                rangeOrds += Convert.ToInt32(rr["Orders"]);
            }
            r1.Controls.Add(KpiCard("Daily Sales",      "R " + DatabaseHelper.GetDailySalesTotal().ToString("N2"),   Blue));
            r1.Controls.Add(KpiCard("Period Sales",     "R " + rangeTot.ToString("N2"),  Green));
            r1.Controls.Add(KpiCard("Period Orders",    rangeOrds.ToString(),            Orange));
            r1.Controls.Add(KpiCard("All-Time Sales",   "R " + DatabaseHelper.GetAllTimeSalesTotal().ToString("N2"), Purple));
            scroll.Controls.Add(r1);

            // ── DATE RANGE SELECTOR (added last = appears at top) ──
            var rangeBar = new Panel {
                Dock = DockStyle.Top, Height = 52,
                BackColor = Color.FromArgb(235, 225, 210),
                Padding = new Padding(10, 8, 10, 8)
            };

            rangeBar.Controls.Add(new Label {
                Text = "From:", AutoSize = true, Location = new Point(10, 16),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = BrownDk
            });
            var dtpFrom = new DateTimePicker {
                Location = new Point(58, 12), Size = new Size(140, 28),
                Format = DateTimePickerFormat.Short, Value = _fromDate,
                Font = new Font("Segoe UI", 9.5f)
            };
            rangeBar.Controls.Add(dtpFrom);

            rangeBar.Controls.Add(new Label {
                Text = "To:", AutoSize = true, Location = new Point(212, 16),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = BrownDk
            });
            var dtpTo = new DateTimePicker {
                Location = new Point(238, 12), Size = new Size(140, 28),
                Format = DateTimePickerFormat.Short, Value = _toDate,
                Font = new Font("Segoe UI", 9.5f)
            };
            rangeBar.Controls.Add(dtpTo);

            // Preset buttons
            var btnToday = new Button {
                Text = "Today", Size = new Size(96, 28), BackColor = Brown, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Cursor = Cursors.Hand
            };
            btnToday.FlatAppearance.BorderSize = 0;
            btnToday.Click += (s, e) => {
                _fromDate = DateTime.Today; _toDate = DateTime.Today;
                _scrollBody.Controls.Clear(); PopulateBody(_scrollBody);
            };

            var btnWeek = new Button {
                Text = "This Week", Size = new Size(96, 28), BackColor = Brown, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Cursor = Cursors.Hand
            };
            btnWeek.FlatAppearance.BorderSize = 0;
            btnWeek.Click += (s, e) => {
                _fromDate = DateTime.Today.AddDays(-6); _toDate = DateTime.Today;
                _scrollBody.Controls.Clear(); PopulateBody(_scrollBody);
            };

            var btnMonth = new Button {
                Text = "This Month", Size = new Size(96, 28), BackColor = Brown, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Cursor = Cursors.Hand
            };
            btnMonth.FlatAppearance.BorderSize = 0;
            btnMonth.Click += (s, e) => {
                _fromDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                _toDate   = DateTime.Today;
                _scrollBody.Controls.Clear(); PopulateBody(_scrollBody);
            };

            var btnYear = new Button {
                Text = "This Year", Size = new Size(96, 28), BackColor = Brown, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Cursor = Cursors.Hand
            };
            btnYear.FlatAppearance.BorderSize = 0;
            btnYear.Click += (s, e) => {
                _fromDate = new DateTime(DateTime.Today.Year, 1, 1);
                _toDate   = DateTime.Today;
                _scrollBody.Controls.Clear(); PopulateBody(_scrollBody);
            };

            int px = 392;
            foreach (var pb in new[] { btnToday, btnWeek, btnMonth, btnYear }) {
                pb.Location = new Point(px, 12); rangeBar.Controls.Add(pb); px += 104;
            }

            var btnApply = new Button {
                Text = "✔  Apply", Location = new Point(px, 12), Size = new Size(100, 28),
                BackColor = Color.FromArgb(38, 118, 58), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnApply.FlatAppearance.BorderSize = 0;
            btnApply.Click += (s, e) => {
                if (dtpFrom.Value > dtpTo.Value) {
                    MessageBox.Show("'From' date cannot be after 'To' date.", "Invalid Range",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning); return;
                }
                _fromDate = dtpFrom.Value.Date;
                _toDate   = dtpTo.Value.Date;
                _scrollBody.Controls.Clear();
                PopulateBody(_scrollBody);
            };
            rangeBar.Controls.Add(btnApply);

            var lblRange = new Label {
                Text = $"Showing:  {_fromDate:dd MMM yyyy}  →  {_toDate:dd MMM yyyy}",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(110, 85, 55),
                AutoSize = true
            };
            rangeBar.Controls.Add(lblRange);
            rangeBar.Resize += (s, e) =>
                lblRange.Location = new Point(rangeBar.Width - lblRange.Width - 14, 18);

            scroll.Controls.Add(rangeBar);

            // ── TOOLBAR — report buttons download selected range ──
            var toolbar = new Panel {
                Dock = DockStyle.Top, Height = 48,
                BackColor = Color.FromArgb(228, 218, 205),
                Padding = new Padding(8, 8, 8, 8)
            };
            var btns = new (string t, Action a)[] {
                ("🖨  Weekly Report",   () => PrintWeeklyReport(_fromDate, _toDate)),
                ("🖨  Monthly Report",  () => PrintMonthlyReport(_fromDate, _toDate)),
                ("🖨  Customer Report", () => PrintCustomerReport()),
                ("🖨  Product Report",  () => PrintProductReport(_fromDate, _toDate)),
            };
            int bx = 0;
            foreach (var (t, a) in btns) {
                var b = ActionBtn(t); b.Location = new Point(bx, 0);
                b.Click += (s, e) => a(); toolbar.Controls.Add(b); bx += 188;
            }
            scroll.Controls.Add(toolbar);

            scroll.ResumeLayout(true);
        }

        // ─────────────────────────────────────────────────────────
        //  CHART
        // ─────────────────────────────────────────────────────────
        Chart BuildSalesChart(DateTime from, DateTime to)
        {
            var chart = new Chart { Dock = DockStyle.Fill, BackColor = Color.White };
            var area = new ChartArea("SA") {
                BackColor = Color.White,
                AxisX = {
                    Title = "Date", TitleFont = new Font("Segoe UI", 8f, FontStyle.Bold),
                    LabelStyle = { Font = new Font("Segoe UI", 8f), Format = "dd/MM" },
                    MajorGrid = { LineColor = Color.FromArgb(225, 215, 202) }
                },
                AxisY = {
                    Title = "Revenue (R)", TitleFont = new Font("Segoe UI", 8f, FontStyle.Bold),
                    LabelStyle = { Font = new Font("Segoe UI", 8f), Format = "N0" },
                    MajorGrid = { LineColor = Color.FromArgb(225, 215, 202) }
                }
            };
            area.AxisY2.Enabled = AxisEnabled.True;
            area.AxisY2.Title   = "Orders";
            area.AxisY2.TitleFont = new Font("Segoe UI", 8f, FontStyle.Bold);
            area.AxisY2.LabelStyle.Font = new Font("Segoe UI", 8f);
            area.AxisY2.MajorGrid.Enabled = false;
            chart.ChartAreas.Add(area);
            chart.Legends.Add(new Legend { Font = new Font("Segoe UI", 9f), BackColor = Color.Transparent, Docking = Docking.Top });
            string rangeLabel = from == to
                ? from.ToString("dd MMM yyyy")
                : $"{from:dd MMM yyyy}  →  {to:dd MMM yyyy}";
            chart.Titles.Add(new Title($"Sales Overview  —  {rangeLabel}") {
                Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = BrownDk });

            var revS = new Series("Revenue") {
                ChartType = SeriesChartType.Area, XValueType = ChartValueType.Date,
                Color = Color.FromArgb(80, 33, 150, 243), BorderColor = Color.FromArgb(33, 150, 243), BorderWidth = 2
            };
            var ordS = new Series("Orders") {
                ChartType = SeriesChartType.Line, XValueType = ChartValueType.Date,
                Color = Color.FromArgb(46, 160, 80), BorderWidth = 2,
                MarkerStyle = MarkerStyle.Circle, MarkerSize = 6, YAxisType = AxisType.Secondary
            };

            var data = DatabaseHelper.GetSalesInRange(from, to);
            foreach (DataRow r in data.Rows) {
                DateTime dt = Convert.ToDateTime(r["SaleDate"]);
                revS.Points.AddXY(dt.ToOADate(), (double)Convert.ToDecimal(r["Revenue"]));
                ordS.Points.AddXY(dt.ToOADate(), Convert.ToInt32(r["Orders"]));
            }
            if (data.Rows.Count == 0) {
                revS.Points.AddXY(from.ToOADate(), 0);
                revS.Points.AddXY(to.ToOADate(), 0);
            }
            chart.Series.Add(revS); chart.Series.Add(ordS);
            return chart;
        }

        // ─────────────────────────────────────────────────────────
        //  PRINTING
        // ─────────────────────────────────────────────────────────
        void DoPrint(string content, string title)
        {
            _printContent = content;
            var pd = new PrintDocument { DocumentName = title };
            pd.PrintPage += (s, e) => {
                var fN = new Font("Courier New", 9f);
                var fB = new Font("Courier New", 10f, FontStyle.Bold);
                float y = e.MarginBounds.Top, x = e.MarginBounds.Left, lh = fN.GetHeight(e.Graphics) + 1f;
                foreach (var line in _printContent.Split('\n')) {
                    if (y + lh > e.MarginBounds.Bottom) { e.HasMorePages = true; break; }
                    bool bold = line.StartsWith("═") || line.StartsWith("CAFÉ") || line.StartsWith("TOTAL");
                    e.Graphics.DrawString(line.TrimEnd('\r'), bold ? fB : fN, Brushes.Black, x, y);
                    y += lh;
                }
                fN.Dispose(); fB.Dispose();
            };
            using (var dlg = new PrintDialog { Document = pd })
                if (dlg.ShowDialog() == DialogResult.OK) pd.Print();
        }

        void PrintWeeklyReport(DateTime from, DateTime to)
        {
            var data = DatabaseHelper.GetSalesInRange(from, to);
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("               CAFÉ 101               ");
            sb.AppendLine("            Sales Report              ");
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine($"Period:    {from:dd MMM yyyy}  –  {to:dd MMM yyyy}");
            sb.AppendLine($"Generated: {DateTime.Now:dd/MM/yyyy HH:mm}");
            sb.AppendLine("───────────────────────────────────────");
            sb.AppendLine($"{"Date",-14} {"Orders",8} {"Revenue",14}");
            sb.AppendLine("───────────────────────────────────────");
            decimal tot = 0; int tOrd = 0;
            foreach (DataRow r in data.Rows) {
                decimal rev = Convert.ToDecimal(r["Revenue"]); int ord = Convert.ToInt32(r["Orders"]);
                tot += rev; tOrd += ord;
                sb.AppendLine($"{Convert.ToDateTime(r["SaleDate"]):dd/MM/yyyy,-14} {ord,8} {"R"+rev.ToString("N2"),14}");
            }
            sb.AppendLine("───────────────────────────────────────");
            sb.AppendLine($"{"TOTALS:",-14} {tOrd,8} {"R"+tot.ToString("N2"),14}");
            sb.AppendLine("═══════════════════════════════════════");
            DoPrint(sb.ToString(), $"Sales Report {from:dd MMM} – {to:dd MMM yyyy}");
        }

        void PrintMonthlyReport(DateTime from, DateTime to)
        {
            var data = DatabaseHelper.GetSalesInRange(from, to);
            var top  = DatabaseHelper.GetTopSellingItems(10);
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("               CAFÉ 101               ");
            sb.AppendLine("         Period Sales Report          ");
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine($"Period:    {from:dd MMM yyyy}  –  {to:dd MMM yyyy}");
            sb.AppendLine($"Generated: {DateTime.Now:dd/MM/yyyy HH:mm}");
            sb.AppendLine("───────────────────────────────────────");
            sb.AppendLine($"{"Date",-14} {"Orders",8} {"Revenue",14}");
            sb.AppendLine("───────────────────────────────────────");
            decimal tot = 0; int tOrd = 0;
            foreach (DataRow r in data.Rows) {
                decimal rev = Convert.ToDecimal(r["Revenue"]); int ord = Convert.ToInt32(r["Orders"]);
                tot += rev; tOrd += ord;
                sb.AppendLine($"{Convert.ToDateTime(r["SaleDate"]):dd/MM/yyyy,-14} {ord,8} {"R"+rev.ToString("N2"),14}");
            }
            sb.AppendLine("───────────────────────────────────────");
            sb.AppendLine($"{"PERIOD TOTAL:",-22} {tOrd,8} {"R"+tot.ToString("N2"),14}");
            sb.AppendLine();
            sb.AppendLine("TOP 10 PRODUCTS (ALL TIME):");
            sb.AppendLine($"{"#",-4} {"Product",-24} {"Qty",6} {"Revenue",12}");
            sb.AppendLine("───────────────────────────────────────");
            int rank = 1;
            foreach (DataRow r in top.Rows)
                sb.AppendLine($"{rank++,-4} {r["ItemName"],-24} {r["TotalQty"],6} {"R"+Convert.ToDecimal(r["TotalRevenue"]).ToString("N2"),12}");
            sb.AppendLine("═══════════════════════════════════════");
            DoPrint(sb.ToString(), $"Period Sales Report {from:dd MMM} – {to:dd MMM yyyy}");
        }

        void PrintCustomerReport()
        {
            var data = DatabaseHelper.GetCustomers();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("               CAFÉ 101               ");
            sb.AppendLine("            Customer Report           ");
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine($"Generated:       {DateTime.Now:dd MMM yyyy HH:mm}");
            sb.AppendLine($"Total Customers: {data.Rows.Count}");
            sb.AppendLine($"New This Week:   {DatabaseHelper.GetNewCustomersThisWeek()}");
            sb.AppendLine($"New This Month:  {DatabaseHelper.GetNewCustomersThisMonth()}");
            sb.AppendLine("───────────────────────────────────────");
            sb.AppendLine($"{"Name",-24} {"Phone",-14} {"Since",-12}");
            sb.AppendLine("───────────────────────────────────────");
            foreach (DataRow r in data.Rows) {
                string nm = r["FullName"].ToString();
                if (nm.Length > 23) nm = nm.Substring(0, 21) + "..";
                sb.AppendLine($"{nm,-24} {r["Phone"],-14} {r["Since"],-12}");
            }
            sb.AppendLine("═══════════════════════════════════════");
            DoPrint(sb.ToString(), "Customer Report");
        }

        void PrintProductReport(DateTime from, DateTime to)
        {
            var top   = DatabaseHelper.GetTopSellingItems(20);
            var least = DatabaseHelper.GetLeastSellingItems(10);
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("               CAFÉ 101               ");
            sb.AppendLine("      Product Performance Report      ");
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine($"Period:       {from:dd MMM yyyy}  –  {to:dd MMM yyyy}");
            sb.AppendLine($"Generated:    {DateTime.Now:dd MMM yyyy HH:mm}");
            sb.AppendLine($"Most Popular: {DatabaseHelper.GetMostPurchasedProduct()}");
            sb.AppendLine("───────────────────────────────────────");
            sb.AppendLine("TOP 20 SELLING PRODUCTS:");
            sb.AppendLine($"{"#",-4} {"Product",-26} {"Qty",9} {"Revenue",12}");
            sb.AppendLine("───────────────────────────────────────");
            int rank = 1;
            foreach (DataRow r in top.Rows)
                sb.AppendLine($"{rank++,-4} {r["ItemName"],-26} {r["TotalQty"],9} {"R"+Convert.ToDecimal(r["TotalRevenue"]).ToString("N2"),12}");
            sb.AppendLine();
            sb.AppendLine("LEAST SELLING PRODUCTS:");
            foreach (DataRow r in least.Rows)
                sb.AppendLine($"  {r["ItemName"],-30} Qty:{r["TotalQty"],4}");
            sb.AppendLine("═══════════════════════════════════════");
            DoPrint(sb.ToString(), $"Product Report {from:dd MMM} – {to:dd MMM yyyy}");
        }

        // ─────────────────────────────────────────────────────────
        //  WIDGET HELPERS
        // ─────────────────────────────────────────────────────────
        void AddSectionLabel(Panel parent, string text)
        {
            var lbl = new Label {
                Text = text, Dock = DockStyle.Top, Height = 32,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = BrownDk, TextAlign = ContentAlignment.BottomLeft,
                Padding = new Padding(2, 0, 0, 2),
                Margin = new Padding(0, 10, 0, 2)
            };
            lbl.Paint += (s, e) =>
                e.Graphics.DrawLine(new Pen(Color.FromArgb(205, 190, 170), 1.5f),
                    0, lbl.Height - 1, lbl.Width, lbl.Height - 1);
            parent.Controls.Add(lbl);
        }

        TableLayoutPanel CardRow()
        {
            var row = new TableLayoutPanel {
                Dock = DockStyle.Top, Height = 96,
                ColumnCount = 4, BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 4)
            };
            for (int i = 0; i < 4; i++)
                row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            return row;
        }

        Panel KpiCard(string title, string value, Color accent)
        {
            var card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Margin = new Padding(3) };
            card.Paint += (s, e) => {
                e.Graphics.FillRectangle(new SolidBrush(accent), 0, 0, 5, card.Height);
                using (var p = new Pen(Color.FromArgb(215, 205, 192), 1f))
                    e.Graphics.DrawRectangle(p, 0, 0, card.Width - 1, card.Height - 1);
            };
            card.Controls.Add(new Label {
                Text = title, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(128, 108, 84), Location = new Point(12, 8), AutoSize = true
            });
            card.Controls.Add(new Label {
                Text = value, Font = new Font("Segoe UI", value.Length > 14 ? 11f : 17f, FontStyle.Bold),
                ForeColor = Color.FromArgb(36, 24, 12), Location = new Point(12, 26), AutoSize = true
            });
            return card;
        }

        Panel GridPanel(string title)
        {
            var p = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            p.Paint += (s, e) => DrawBorder(e.Graphics, p);
            p.Controls.Add(new Label {
                Text = title, Dock = DockStyle.Top, Height = 32,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = BrownDk,
                Padding = new Padding(10, 0, 0, 0),
                BackColor = Color.FromArgb(250, 246, 240),
                TextAlign = ContentAlignment.MiddleLeft
            });
            return p;
        }

        DataGridView MakeGrid()
        {
            var dg = new DataGridView {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                ReadOnly = true, RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9.5f),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeight = 32, RowTemplate = { Height = 28 }
            };
            dg.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle {
                BackColor = BrownDk, ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            dg.DefaultCellStyle.ForeColor          = Color.FromArgb(35, 24, 12);
            dg.DefaultCellStyle.SelectionBackColor = Color.FromArgb(255, 243, 228);
            dg.DefaultCellStyle.SelectionForeColor = Color.FromArgb(55, 35, 16);
            dg.EnableHeadersVisualStyles           = false;
            dg.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(252, 249, 244);
            dg.GridColor = Color.FromArgb(228, 218, 205);
            dg.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            return dg;
        }

        static void DrawBorder(Graphics g, Panel p)
        {
            using (var pen = new Pen(Color.FromArgb(215, 204, 190), 1f))
                g.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
        }

        Button NavBtn(string text, Color bg)
        {
            var b = new Button {
                Text = text, Size = new Size(128, 32), BackColor = bg,
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold), Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0; return b;
        }

        Button ActionBtn(string text)
        {
            var b = new Button {
                Text = text, Size = new Size(182, 32), BackColor = Brown,
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold), Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0; return b;
        }
    }
}
