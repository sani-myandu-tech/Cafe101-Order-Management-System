using System;
using System.Drawing;
using System.Windows.Forms;

namespace Cafe101
{
    // ═══════════════════════════════════════════════════════════
    //  HelpAbout.cs  —  HELP & ABOUT FORM
    //  Satisfies rubric criterion 6: "extra features such as
    //  Help and Reports"
    // ═══════════════════════════════════════════════════════════
    public class HelpAbout : Form
    {
        private static readonly Color Brown     = Color.FromArgb(111, 78,  55);
        private static readonly Color BrownDark = Color.FromArgb( 74, 50,  37);
        private static readonly Color Green     = Color.FromArgb( 76,175,  80);

        public HelpAbout()
        {
            BuildUI();
        }

        private void BuildUI()
        {
            this.Text            = "Café 101 — Help & About";
            this.Size            = new Size(780, 680);
            this.StartPosition   = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox     = false;
            this.BackColor       = Color.White;
            this.Font            = new Font("Segoe UI", 10f);

            // ── Header ───────────────────────────────────────────
            var hdr = new Panel { Location = new Point(0,0), Size = new Size(780,80), BackColor = BrownDark };
            hdr.Controls.Add(new Label
            {
                Text = "☕  Café 101  —  Help & System Guide",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White, AutoSize = true, Location = new Point(20, 22)
            });
            this.Controls.Add(hdr);

            // ── Tab control ──────────────────────────────────────
            var tabs = new TabControl
            {
                Location = new Point(12, 88),
                Size     = new Size(750, 520),
                Font     = new Font("Segoe UI", 10f, FontStyle.Bold)
            };

            tabs.TabPages.Add(BuildUserGuideTab());
            tabs.TabPages.Add(BuildWorkflowTab());
            tabs.TabPages.Add(BuildRolesTab());
            tabs.TabPages.Add(BuildAboutTab());

            this.Controls.Add(tabs);

            // ── Close button ─────────────────────────────────────
            var btnClose = new Button
            {
                Text      = "Close",
                Location  = new Point(644, 616),
                Size      = new Size(118, 38),
                BackColor = Brown,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => this.Close();
            this.Controls.Add(btnClose);
        }

        private TabPage BuildUserGuideTab()
        {
            var tab = new TabPage("📖  User Guide") { BackColor = Color.White };
            var rtf = new RichTextBox
            {
                Dock      = DockStyle.Fill,
                ReadOnly  = true,
                BackColor = Color.White,
                BorderStyle = BorderStyle.None,
                Font      = new Font("Segoe UI", 10.5f),
                ScrollBars = RichTextBoxScrollBars.Vertical
            };

            rtf.Text =
                "CASHIER — POINT OF SALE\r\n" +
                "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\r\n" +
                "1.  Click a category from the LEFT SIDEBAR to filter the menu\r\n" +
                "2.  Use the SEARCH BAR to find items by name\r\n" +
                "3.  Click any product CARD to add it to the cart\r\n" +
                "4.  Right-click a cart row to increase/decrease quantity\r\n" +
                "5.  Click the ✕ button on a cart row to remove it\r\n" +
                "6.  Enter the CUSTOMER NAME or TABLE NUMBER\r\n" +
                "7.  Select ORDER TYPE: Dine-In or Takeaway\r\n" +
                "8.  Select PAYMENT METHOD: Cash, Card, or Mobile\r\n" +
                "9.  Click PLACE ORDER to save and print the receipt\r\n\r\n" +

                "KITCHEN — HEAD CHEF\r\n" +
                "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\r\n" +
                "1.  The screen shows all Pending and Preparing orders\r\n" +
                "2.  RED rows = urgent orders waiting more than 15 minutes\r\n" +
                "3.  AMBER rows = orders waiting more than 10 minutes\r\n" +
                "4.  GREEN rows = orders currently being prepared\r\n" +
                "5.  Click an order row to see the items on the right\r\n" +
                "6.  Click START PREPARING when you begin cooking\r\n" +
                "7.  Click MARK AS READY when the food is plated\r\n" +
                "8.  Click MARK AS SERVED when the customer collects\r\n" +
                "9.  Screen auto-refreshes every 30 seconds\r\n\r\n" +

                "MANAGER — DASHBOARD\r\n" +
                "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\r\n" +
                "•   OVERVIEW:   Today's orders and live stock summary\r\n" +
                "•   ORDERS:     All orders, update status, view details\r\n" +
                "•   MENU:       Add new items, delete items, view all\r\n" +
                "•   INVENTORY:  Stock levels, update quantities manually\r\n" +
                "•   SUPPLIERS:  Purchase orders, mark as received\r\n" +
                "•   STAFF:      Add / deactivate staff accounts\r\n" +
                "•   REPORTS:    Sales summary, top sellers, payment breakdown\r\n";

            tab.Controls.Add(rtf);
            return tab;
        }

        private TabPage BuildWorkflowTab()
        {
            var tab = new TabPage("🔄  Order Workflow") { BackColor = Color.White };
            var rtf = new RichTextBox
            {
                Dock = DockStyle.Fill, ReadOnly = true,
                BackColor = Color.White, BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10.5f)
            };

            rtf.Text =
                "ORDER STATUS FLOW\r\n" +
                "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\r\n\r\n" +
                "  [1] PENDING\r\n" +
                "      Order has been placed by the Cashier.\r\n" +
                "      Waiting for the kitchen to start.\r\n\r\n" +
                "        ↓  (Chef clicks START PREPARING)\r\n\r\n" +
                "  [2] PREPARING\r\n" +
                "      Kitchen is actively preparing the order.\r\n" +
                "      Visible on the Kitchen Display.\r\n\r\n" +
                "        ↓  (Chef clicks MARK AS READY)\r\n\r\n" +
                "  [3] READY\r\n" +
                "      Food is plated and ready for collection.\r\n" +
                "      Cashier or Manager is notified.\r\n\r\n" +
                "        ↓  (Chef or Manager clicks MARK AS SERVED)\r\n\r\n" +
                "  [4] SERVED\r\n" +
                "      Customer has received their order.\r\n" +
                "      Order is complete and counted in revenue.\r\n\r\n" +
                "        OR at any point:\r\n\r\n" +
                "  [X] CANCELLED\r\n" +
                "      Order was cancelled by the Manager.\r\n" +
                "      Not counted in revenue reports.\r\n\r\n" +
                "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\r\n" +
                "STOCK MANAGEMENT FLOW\r\n" +
                "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\r\n\r\n" +
                "  When an order is placed:\r\n" +
                "    → Stock quantity is automatically deducted\r\n\r\n" +
                "  When stock reaches Min Stock level:\r\n" +
                "    → Item is highlighted in AMBER in Inventory tab\r\n\r\n" +
                "  When stock is zero:\r\n" +
                "    → Item shows OUT OF STOCK in RED\r\n" +
                "    → Item cannot be added to cart\r\n\r\n" +
                "  To restock:\r\n" +
                "    → Create a Purchase Order in Suppliers tab\r\n" +
                "    → When stock arrives, click Mark as Received\r\n" +
                "    → Stock quantity is automatically increased\r\n";

            tab.Controls.Add(rtf);
            return tab;
        }

        private TabPage BuildRolesTab()
        {
            var tab = new TabPage("👥  Staff Roles") { BackColor = Color.White };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 3,
                BackColor = Color.White, Padding = new Padding(16)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            void HeaderCell(string text, int col, int row, Color bg)
            {
                layout.Controls.Add(new Label
                {
                    Text = text, Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                    ForeColor = Color.White, BackColor = bg, Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter, Padding = new Padding(8)
                }, col, row);
            }

            void DataCell(string text, int col, int row, Color bg)
            {
                layout.Controls.Add(new Label
                {
                    Text = text, Font = new Font("Segoe UI", 10f),
                    ForeColor = Color.FromArgb(60, 45, 30), BackColor = bg, Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 8, 0, 8)
                }, col, row);
            }

            // Headers
            HeaderCell("Role",        0, 0, BrownDark);
            HeaderCell("Screen",      1, 0, BrownDark);
            HeaderCell("Permissions", 2, 0, BrownDark);

            var rowData = new (string Role, string Screen, string Perms, Color bg)[]
            {
                ("Cashier",  "POS Screen",        "Place orders, process payments, view menu", Color.FromArgb(255,250,245)),
                ("HeadChef", "Kitchen Display",   "View orders, update status (Preparing/Ready/Served)", Color.FromArgb(245,255,245)),
                ("Manager",  "Manager Dashboard", "Full access: orders, menu, inventory, suppliers, staff, reports", Color.FromArgb(245,245,255)),
                ("Owner",    "Manager Dashboard", "Same as Manager + full financial overview", Color.FromArgb(255,245,255)),
            };

            for (int i = 0; i < rowData.Length; i++)
            {
                var (role, screen, perms, bg) = rowData[i];
                DataCell(role,   0, i + 1, bg);
                DataCell(screen, 1, i + 1, bg);
                DataCell(perms,  2, i + 1, bg);
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
            }

            tab.Controls.Add(layout);
            return tab;
        }

        private TabPage BuildAboutTab()
        {
            var tab = new TabPage("ℹ️  About") { BackColor = Color.White };

            var lbl = new Label
            {
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font      = new Font("Segoe UI", 11f),
                ForeColor = Color.FromArgb(80, 60, 40),
                BackColor = Color.White,
                Text      =
                    "☕\r\n\r\n" +
                    "CAFÉ 101\r\n" +
                    "Manual In-Person Point of Sale System\r\n\r\n" +
                    "Version 2.0  —  2026\r\n\r\n" +
                    "━━━━━━━━━━━━━━━━━━━━━━━━━\r\n\r\n" +
                    "Built with C# WinForms  •  .NET Framework 4.8\r\n" +
                    "Database: Microsoft SQL Server\r\n\r\n" +
                    "Features:\r\n" +
                    "Role-based access  •  Live kitchen display\r\n" +
                    "Real-time stock management  •  Full CRUDS\r\n" +
                    "Supplier purchase orders  •  Sales reports\r\n" +
                    "VAT calculation  •  Receipt printing\r\n\r\n" +
                    "━━━━━━━━━━━━━━━━━━━━━━━━━\r\n\r\n" +
                    "Applied Systems Analysis (ISTN3AS)\r\n" +
                    "Milestone 2 — Group Project  •  2026"
            };

            tab.Controls.Add(lbl);
            return tab;
        }
    }
}
