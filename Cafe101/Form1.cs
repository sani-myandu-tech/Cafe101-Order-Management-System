using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace Cafe101
{
    public partial class Form1 : Form
    {
        // ── Brand palette ────────────────────────────────────────
        static readonly Color C_DarkBg   = Color.FromArgb(18, 11, 5);
        static readonly Color C_DarkMid  = Color.FromArgb(38, 22, 10);
        static readonly Color C_Brown    = Color.FromArgb(88, 48, 26);
        static readonly Color C_BrownHov = Color.FromArgb(112, 64, 36);
        static readonly Color C_Gold     = Color.FromArgb(196, 154, 82);
        static readonly Color C_GoldPale = Color.FromArgb(220, 185, 120);
        static readonly Color C_Cream    = Color.FromArgb(244, 237, 226);
        static readonly Color C_CreamDk  = Color.FromArgb(232, 220, 202);
        static readonly Color C_Text     = Color.FromArgb(62, 40, 22);
        static readonly Color C_Sub      = Color.FromArgb(148, 118, 88);
        static readonly Color C_Border   = Color.FromArgb(212, 198, 178);
        static readonly Color C_FieldBg  = Color.FromArgb(251, 248, 244);

        TextBox  _txtEmail, _txtPass;
        Button   _btnLogin;
        Label    _lblError;
        CheckBox _chkRemember;

        public Form1()
        {
            InitializeComponent();
            BuildUI();
        }

        // ═══════════════════════════════════════════════════════════
        void BuildUI()
        {
            Text            = "Café 101 — Login";
            Size            = new Size(1280, 800);
            MinimumSize     = new Size(960, 640);
            StartPosition   = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
            BackColor       = C_DarkBg;
            DoubleBuffered  = true;

            var split = new TableLayoutPanel {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent
            };
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38f));
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62f));
            Controls.Add(split);

            split.Controls.Add(BuildLeftPanel(), 0, 0);
            split.Controls.Add(BuildRightPanel(), 1, 0);
        }

        // ═══════════════════════════════════════════════════════════
        //  LEFT  – Branding
        // ═══════════════════════════════════════════════════════════
        Panel BuildLeftPanel()
        {
            var pnl = new DoubleBufferedPanel { Dock = DockStyle.Fill };
            pnl.Paint += PaintLeft;

            // ── Centralised content host ─────────────────────────
            var host = new Panel {
                BackColor = Color.Transparent,
                Anchor    = AnchorStyles.None   // we'll position it manually
            };

            // Cup icon  (drawn, not emoji, to avoid font-render artefacts)
            var picCup = new PictureBox {
                Size = new Size(100, 100),
                BackColor = Color.Transparent
            };
            picCup.Paint += (s, e) => DrawCupIcon(e.Graphics, picCup.ClientRectangle);
            host.Controls.Add(picCup);

            // Café 101
            var lblBrand = MakeLabel("CAFÉ 101", new Font("Segoe UI", 36f, FontStyle.Bold),
                                     Color.White, ContentAlignment.MiddleCenter);
            host.Controls.Add(lblBrand);

            // Sub-heading
            var lblSub = MakeLabel("ORDER & DELIVERY MANAGEMENT SYSTEM",
                                   new Font("Segoe UI", 8.5f, FontStyle.Bold | FontStyle.Regular),
                                   C_Gold, ContentAlignment.MiddleCenter);
            host.Controls.Add(lblSub);

            // Gold rule
            var rule = new Panel { Height = 1, BackColor = Color.FromArgb(90, C_Gold) };
            host.Controls.Add(rule);

            // Quote
            var lblQuote = MakeLabel(
                "\u201cWhere Technology Meets Taste \u2014\nSmarter Ordering, Faster Service.\u201d",
                new Font("Segoe UI", 10.5f, FontStyle.Italic),
                Color.FromArgb(195, 168, 128), ContentAlignment.MiddleCenter);
            host.Controls.Add(lblQuote);

            // Feature rows
            var features = new (string glyph, string title, string desc)[] {
                ("\uD83D\uDECE", "Smart Ordering",    "Fast, simple and convenient ordering experience."),
                ("\uD83D\uDE9A", "Reliable Delivery",  "Real-time tracking from kitchen to your door."),
                ("\uD83D\uDEE1", "Secure Payments",    "Multiple secure payment options for peace of mind."),
                ("\uD83D\uDCCA", "Powerful Insights",  "Reports and analytics to grow your business."),
            };

            var featRows = new Panel[features.Length];
            for (int i = 0; i < features.Length; i++)
                featRows[i] = BuildFeatureRow(features[i].glyph, features[i].title, features[i].desc);

            foreach (var r in featRows) host.Controls.Add(r);
            pnl.Controls.Add(host);

            // ── UKZN footer ──────────────────────────────────────
            var footer = new DoubleBufferedPanel {
                Dock      = DockStyle.Bottom,
                Height    = 56,
                BackColor = Color.FromArgb(24, 14, 6)
            };
            footer.Paint += (s, e) => {
                using (var p = new Pen(Color.FromArgb(50, C_Gold)))
                    e.Graphics.DrawLine(p, 0, 0, footer.Width, 0);
            };

            var lGrad = new Label {
                Text      = "🎓  Proudly on UKZN Westville Campus",
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = C_Gold,
                AutoSize  = true,
                Location  = new Point(16, 8)
            };
            var lSub = new Label {
                Text      = "Serving students & staff with excellence.",
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = C_Sub,
                AutoSize  = true,
                Location  = new Point(16, 30)
            };
            var lDate = new Label {
                Text      = $"📅  {DateTime.Now:dddd, dd MMMM yyyy}",
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = C_Sub,
                AutoSize  = true,
                Anchor    = AnchorStyles.Right | AnchorStyles.Top
            };
            footer.Controls.AddRange(new Control[] { lGrad, lSub, lDate });
            footer.Resize += (s, e) => lDate.Location = new Point(footer.Width - lDate.Width - 16, 22);
            pnl.Controls.Add(footer);

            // ── Responsive layout ────────────────────────────────
            Action layout = () => {
                int pw  = pnl.Width;
                int ph  = pnl.Height - footer.Height;
                int pad = 32;
                int cw  = Math.Min(pw - pad * 2, 460);
                int x0  = (pw - cw) / 2;

                // size host
                host.Width = cw;

                // stack controls inside host
                int hy = 0;

                picCup.Width  = 88; picCup.Height = 88;
                picCup.Location = new Point((cw - picCup.Width) / 2, hy); hy += picCup.Height + 6;

                lblBrand.Width = cw; lblBrand.Height = 52;
                lblBrand.Location = new Point(0, hy); hy += 48;

                lblSub.Width = cw; lblSub.Height = 24;
                lblSub.Location = new Point(0, hy); hy += 28;

                rule.Width = Math.Min(240, cw);
                rule.Location = new Point((cw - rule.Width) / 2, hy); hy += 14;

                lblQuote.Width = cw; lblQuote.Height = 46;
                lblQuote.Location = new Point(0, hy); hy += 52;

                foreach (var r in featRows) {
                    r.Width    = cw;
                    r.Location = new Point(0, hy);
                    // Fix: ensure inner labels fill the row correctly
                    foreach (Control c in r.Controls) {
                        if (c is Label lbl && !lbl.AutoSize)
                            lbl.Width = cw - 64;
                    }
                    hy += r.Height + 6;
                }

                host.Height   = hy;
                host.Location = new Point(x0, Math.Max(pad, (ph - hy) / 2));
            };

            pnl.Resize += (s, e) => layout();
            pnl.VisibleChanged += (s, e) => { if (pnl.Visible) layout(); };
            Shown += (s, e) => layout();
            return pnl;
        }

        Panel BuildFeatureRow(string emoji, string title, string desc)
        {
            var row = new DoubleBufferedPanel {
                Height    = 56,
                BackColor = Color.FromArgb(32, 20, 8)
            };
            row.Paint += (s, e) => {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var p = new Pen(Color.FromArgb(45, C_Gold), 1f))
                    g.DrawRectangle(p, 0, 0, row.Width - 1, row.Height - 1);
                // Left gold accent bar
                using (var br = new SolidBrush(Color.FromArgb(160, C_Gold)))
                    g.FillRectangle(br, 0, 0, 3, row.Height);
            };

            var lIcon = new Label {
                Text      = emoji,
                Font      = new Font("Segoe UI Emoji", 20f),
                ForeColor = C_Gold,
                Location  = new Point(14, 14),
                AutoSize  = true,
                BackColor = Color.Transparent
            };
            var lTitle = new Label {
                Text      = title,
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.White,
                Location  = new Point(58, 10),
                AutoSize  = true,  
                BackColor = Color.Transparent
            };
            var lDesc = new Label {
                Text      = desc,
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = C_Sub,
                Location  = new Point(58, 30),
                Size      = new Size(360, 20),
                BackColor = Color.Transparent
            };
            row.Controls.AddRange(new Control[] { lIcon, lTitle, lDesc });
            return row;
        }

        void PaintLeft(object sender, PaintEventArgs e)
        {
            var pnl = (Panel)sender;
            var g   = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Rich dark gradient
            using (var br = new LinearGradientBrush(
                new Point(0, 0), new Point(pnl.Width, pnl.Height),
                Color.FromArgb(12, 7, 3), Color.FromArgb(52, 32, 12)))
                g.FillRectangle(br, pnl.ClientRectangle);

            // Subtle radial glow using a safe GraphicsPath-based PathGradientBrush
            try {
                var glowRect = new RectangleF(
                    pnl.Width * 0.05f, pnl.Height * 0.1f,
                    pnl.Width * 0.9f,  pnl.Height * 0.7f);
                using (var gp = new System.Drawing.Drawing2D.GraphicsPath()) {
                    gp.AddEllipse(glowRect);
                    using (var pg = new PathGradientBrush(gp)) {
                        pg.CenterColor    = Color.FromArgb(30, C_Gold);
                        pg.SurroundColors = new[] { Color.FromArgb(0, C_Gold) };
                        g.FillEllipse(pg, glowRect);
                    }
                }
            } catch { /* skip glow if GDI+ unavailable */ }

            // Gold wavy accent on right edge
            using (var pen = new Pen(Color.FromArgb(70, C_Gold), 2.5f)) {
                var pts = new PointF[] {
                    new PointF(pnl.Width - 3,  0),
                    new PointF(pnl.Width - 18, pnl.Height * 0.25f),
                    new PointF(pnl.Width - 6,  pnl.Height * 0.55f),
                    new PointF(pnl.Width - 20, pnl.Height * 0.8f),
                    new PointF(pnl.Width - 3,  pnl.Height)
                };
                g.DrawCurve(pen, pts, 0.5f);
            }
        }

        void DrawCupIcon(Graphics g, Rectangle r)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            // Saucer
            using (var br = new SolidBrush(Color.FromArgb(60, C_Gold)))
                g.FillEllipse(br, r.X + 8, r.Bottom - 22, r.Width - 16, 14);
            using (var p = new Pen(C_Gold, 2f))
                g.DrawEllipse(p, r.X + 8, r.Bottom - 22, r.Width - 16, 14);

            // Cup body
            var cupRect = new Rectangle(r.X + 14, r.Y + 12, r.Width - 28, r.Height - 30);
            using (var br = new LinearGradientBrush(cupRect,
                Color.FromArgb(80, C_Gold), Color.FromArgb(40, C_Brown), 135f))
                g.FillEllipse(br, cupRect);
            using (var p = new Pen(C_Gold, 2f))
                g.DrawEllipse(p, cupRect);

            // Handle
            var handleR = new Rectangle(r.Right - 26, r.Y + 24, 18, 26);
            using (var p = new Pen(C_Gold, 2.5f))
                g.DrawArc(p, handleR, -60, 180);

            // Steam lines
            using (var p = new Pen(Color.FromArgb(130, C_Gold), 1.8f)) {
                p.DashStyle = DashStyle.Solid;
                g.DrawCurve(p, new[] {
                    new PointF(r.X + r.Width * 0.38f, r.Y + 8),
                    new PointF(r.X + r.Width * 0.33f, r.Y + 3),
                    new PointF(r.X + r.Width * 0.38f, r.Y - 2)
                });
                g.DrawCurve(p, new[] {
                    new PointF(r.X + r.Width * 0.55f, r.Y + 6),
                    new PointF(r.X + r.Width * 0.50f, r.Y + 1),
                    new PointF(r.X + r.Width * 0.55f, r.Y - 4)
                });
            }

            // Coffee bean dot inside cup
            using (var br = new SolidBrush(C_Gold))
                g.FillEllipse(br, r.X + r.Width / 2 - 5, r.Y + r.Height / 2 - 2, 10, 8);
        }

        // ═══════════════════════════════════════════════════════════
        //  RIGHT  – Login
        // ═══════════════════════════════════════════════════════════
        Panel BuildRightPanel()
        {
            var pnl = new DoubleBufferedPanel { Dock = DockStyle.Fill };
            pnl.Paint += (s, e) => {
                using (var br = new LinearGradientBrush(
                    new Point(0, 0), new Point(0, pnl.Height),
                    C_Cream, C_CreamDk))
                    e.Graphics.FillRectangle(br, pnl.ClientRectangle);
            };

            // ── Card ─────────────────────────────────────────────
            var card = new DoubleBufferedPanel {
                BackColor = Color.White,
                Size      = new Size(400, 10)   // height set later
            };
            card.Paint += (s, e) => {
                var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
                // Card shadow (fake via border)
                using (var p = new Pen(C_Border, 1f))
                    g.DrawRectangle(p, 0, 0, card.Width - 1, card.Height - 1);
            };
            pnl.Controls.Add(card);

            // Bean circle on top of card (sits on pnlRight so it can overflow)
            var bean = new DoubleBufferedPanel {
                Size      = new Size(64, 64),
                BackColor = Color.Transparent
            };
            bean.Paint += (s, e) => {
                var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var br = new SolidBrush(C_Brown))
                    g.FillEllipse(br, 0, 0, 63, 63);
                using (var shadow = new Pen(Color.FromArgb(40, 0, 0, 0), 2f))
                    g.DrawEllipse(shadow, 1, 1, 62, 62);
                DrawCupIcon(g, new Rectangle(10, 10, 44, 44));
            };
            pnl.Controls.Add(bean);
            bean.BringToFront();

            // ── Card content ─────────────────────────────────────
            int pad = 36; int fw = 400 - pad * 2;
            int cy  = 50;

            // Welcome
            AddToCard(card, new Label {
                Text      = "Welcome Back!  \uD83D\uDC4B",
                Font      = new Font("Segoe UI", 22f, FontStyle.Bold),
                ForeColor = C_Text,
                AutoSize  = true,
                Location  = new Point(pad, cy)
            }); cy += 40;

            AddToCard(card, new Label {
                Text      = "Sign in to continue to Café 101 POS",
                Font      = new Font("Segoe UI", 10f),
                ForeColor = C_Sub,
                AutoSize  = true,
                Location  = new Point(pad, cy)
            }); cy += 28;

            // Divider line
            AddToCard(card, new Panel {
                Location  = new Point(pad, cy),
                Size      = new Size(fw, 1),
                BackColor = C_Border
            }); cy += 20;

            // Email field
            _txtEmail = new TextBox {
                Font        = new Font("Segoe UI", 11f),
                BorderStyle = BorderStyle.None,
                BackColor   = C_FieldBg
            };
            AddToCard(card, MakeInputPanel("✉", _txtEmail, "Email address", false, pad, cy, fw)); cy += 56;

            // Password field
            _txtPass = new TextBox {
                Font         = new Font("Segoe UI", 11f),
                BorderStyle  = BorderStyle.None,
                BackColor    = C_FieldBg,
                PasswordChar = '●'
            };
            var passPanel = MakeInputPanel("🔒", _txtPass, "Password", true, pad, cy, fw);
            // Eye button inside the pass panel
            var btnEye = new Button {
                Text      = "👁",
                Size      = new Size(38, 38),
                Location  = new Point(fw - 40, 5),
                BackColor = C_FieldBg,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 12f),
                Cursor    = Cursors.Hand,
                ForeColor = C_Sub,
                TabStop   = false
            };
            btnEye.FlatAppearance.BorderSize = 0;
            btnEye.Click += (s, e) => _txtPass.PasswordChar = _txtPass.PasswordChar == '●' ? '\0' : '●';
            passPanel.Controls.Add(btnEye);
            AddToCard(card, passPanel); cy += 56;

            // Remember + Forgot
            _chkRemember = new CheckBox {
                Text      = "Remember me",
                Font      = new Font("Segoe UI", 9.5f),
                ForeColor = C_Sub,
                Location  = new Point(pad, cy),
                AutoSize  = true,
                Cursor    = Cursors.Hand
            };
            AddToCard(card, _chkRemember);

            var lnkForgot = new LinkLabel {
                Text      = "Forgot password?",
                Font      = new Font("Segoe UI", 9.5f),
                LinkColor = C_Brown,
                AutoSize  = true,
                Cursor    = Cursors.Hand
            };
            lnkForgot.Location = new Point(pad + fw - lnkForgot.PreferredWidth, cy + 2);
            lnkForgot.LinkClicked += (s, e) => new ResetPassword().ShowDialog();
            AddToCard(card, lnkForgot);
            cy += 34;

            // Error label (hidden)
            _lblError = new Label {
                Location  = new Point(pad, cy),
                Size      = new Size(fw, 28),
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(185, 40, 40),
                BackColor = Color.FromArgb(255, 235, 235),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(10, 0, 0, 0),
                Visible   = false
            };
            AddToCard(card, _lblError); cy += 34;

            // Sign In button
            _btnLogin = new Button {
                Text      = "→   Sign In",
                Location  = new Point(pad, cy),
                Size      = new Size(fw, 50),
                BackColor = C_Brown,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            _btnLogin.FlatAppearance.BorderSize = 0;
            _btnLogin.Click      += BtnLogin_Click;
            _btnLogin.MouseEnter += (s, e) => _btnLogin.BackColor = C_BrownHov;
            _btnLogin.MouseLeave += (s, e) => _btnLogin.BackColor = C_Brown;
            AddToCard(card, _btnLogin); cy += 58;

            // "or" divider
            var orPanel = new Panel { Location = new Point(pad, cy), Size = new Size(fw, 22), BackColor = Color.White };
            orPanel.Paint += (s, e) => {
                var g = e.Graphics;
                using (var p = new Pen(C_Border)) {
                    g.DrawLine(p, 0, 11, fw / 2 - 20, 11);
                    g.DrawLine(p, fw / 2 + 20, 11, fw, 11);
                }
                using (var f2 = new Font("Segoe UI", 9f))
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    g.DrawString("or", f2, new SolidBrush(C_Sub), new RectangleF(0, 0, fw, 22), sf);
            };
            AddToCard(card, orPanel); cy += 28;

            // Create Account button
            var btnReg = new Button {
                Text      = "👤  Create New Account",
                Location  = new Point(pad, cy),
                Size      = new Size(fw, 46),
                BackColor = Color.White,
                ForeColor = C_Brown,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 11f),
                Cursor    = Cursors.Hand
            };
            btnReg.FlatAppearance.BorderSize  = 1;
            btnReg.FlatAppearance.BorderColor = C_Border;
            btnReg.MouseEnter += (s, e) => btnReg.BackColor = Color.FromArgb(250, 245, 238);
            btnReg.MouseLeave += (s, e) => btnReg.BackColor = Color.White;
            btnReg.Click += (s, e) => new Form2().ShowDialog();
            AddToCard(card, btnReg); cy += 54;

            card.ClientSize = new Size(400, cy);

            // ── Centre card on resize ─────────────────────────────
            Action centre = () => {
                int cx2 = Math.Max(0, (pnl.Width - card.Width) / 2);
                int cy2 = Math.Max(8, (pnl.Height - card.Height) / 2);
                card.Location = new Point(cx2, cy2);
                bean.Location = new Point(cx2 + (card.Width - bean.Width) / 2, cy2 - 32);
            };
            pnl.Resize        += (s, e) => centre();
            Shown             += (s, e) => centre();

            // Enter key
            _txtEmail.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) BtnLogin_Click(s, e); };
            _txtPass.KeyDown  += (s, e) => { if (e.KeyCode == Keys.Enter) BtnLogin_Click(s, e); };

            return pnl;
        }

        // ── Input composite panel ────────────────────────────────
        Panel MakeInputPanel(string icon, TextBox txt, string ph, bool isPass, int x, int y, int w)
        {
            var pnl = new DoubleBufferedPanel {
                Location  = new Point(x, y),
                Size      = new Size(w, 48),
                BackColor = C_FieldBg
            };
            pnl.Paint += (s, e) => {
                var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
                bool focused = txt.Focused;
                var borderCol = focused ? C_Brown : C_Border;
                using (var p = new Pen(borderCol, focused ? 1.8f : 1f))
                    g.DrawRectangle(p, 0, 0, pnl.Width - 1, pnl.Height - 1);
                // Icon
                using (var f2 = new Font("Segoe UI Emoji", 14f))
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    g.DrawString(icon, f2, new SolidBrush(C_Sub), new RectangleF(0, 0, 42, pnl.Height));
            };

            txt.Location = new Point(42, (pnl.Height - txt.Font.Height - 4) / 2);
            txt.Size     = new Size(w - 88, txt.Font.Height + 6);
            txt.BackColor = C_FieldBg;

            SetPH(txt, ph, isPass, pnl);

            txt.GotFocus  += (s, e) => pnl.Invalidate();
            txt.LostFocus += (s, e) => pnl.Invalidate();

            pnl.Controls.Add(txt);
            pnl.Click += (s, e) => txt.Focus();
            return pnl;
        }

        void SetPH(TextBox txt, string ph, bool isPass, Panel parent)
        {
            txt.Text      = ph;
            txt.ForeColor = C_Sub;
            if (isPass) txt.PasswordChar = '\0';

            txt.GotFocus += (s, e) => {
                if (txt.ForeColor == C_Sub) {
                    txt.Text = ""; txt.ForeColor = C_Text;
                    if (isPass) txt.PasswordChar = '●';
                }
            };
            txt.LostFocus += (s, e) => {
                if (string.IsNullOrEmpty(txt.Text)) {
                    txt.Text = ph; txt.ForeColor = C_Sub;
                    if (isPass) txt.PasswordChar = '\0';
                }
            };
        }

        // ── Utilities ────────────────────────────────────────────
        Label MakeLabel(string text, Font font, Color fore, ContentAlignment align)
        {
            return new Label {
                Text      = text,
                Font      = font,
                ForeColor = fore,
                BackColor = Color.Transparent,
                TextAlign = align,
                AutoSize  = false
            };
        }

        void AddToCard(Panel card, Control c) => card.Controls.Add(c);

        // ═══════════════════════════════════════════════════════════
        //  LOGIN
        // ═══════════════════════════════════════════════════════════
        void BtnLogin_Click(object sender, EventArgs e)
        {
            _lblError.Visible = false;
            string email = _txtEmail.Text.Trim();
            string pw    = _txtPass.Text;

            if (email == "Email address" || string.IsNullOrEmpty(email)) { ShowErr("⚠  Please enter your email address."); return; }
            if (!email.Contains("@"))                                     { ShowErr("⚠  Enter a valid email address.");     return; }
            if (pw == "Password"        || string.IsNullOrEmpty(pw))     { ShowErr("⚠  Please enter your password.");      return; }

            _btnLogin.Enabled = false; _btnLogin.Text = "Signing in..."; Cursor = Cursors.WaitCursor;
            try {
                string role = DatabaseHelper.Login(email, pw);
                if (role == null) { ShowErr("⚠  Incorrect email or password."); return; }
                Form next;
                switch (role) {
                    case "Manager":  next = new Manager();        break;
                    case "Owner":    next = new OwnerDashboard(); break;
                    case "HeadChef": next = new HeadChef();       break;
                    default:         next = new Cashier();        break;
                }
                next.FormClosed += (s2, e2) => Close();
                next.Show(); Hide();
            }
            catch { ShowErr("⚠  Connection error. Check your network."); }
            finally { _btnLogin.Enabled = true; _btnLogin.Text = "→   Sign In"; Cursor = Cursors.Default; }
        }

        void ShowErr(string msg) { _lblError.Text = msg; _lblError.Visible = true; }
    }

    // ── Double-buffered panel to eliminate flicker ───────────────
    class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel() { DoubleBuffered = true; ResizeRedraw = true; }
    }
}
