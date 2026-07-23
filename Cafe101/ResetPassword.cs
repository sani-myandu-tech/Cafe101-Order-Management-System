using System;
using System.Drawing;
using System.Windows.Forms;

namespace Cafe101
{
    public partial class ResetPassword : Form
    {
        private static readonly Color Brown   = Color.FromArgb(111, 78, 55);
        private static readonly Color BrownDk = Color.FromArgb( 74, 50, 37);

        private TextBox txtEmail, txtNew, txtConfirm;
        private Button  btnReset;
        private Label   lblMsg;
        private CheckBox chkShow;

        public ResetPassword()
        {
            InitializeComponent();
            BuildUI();
        }

        private void BuildUI()
        {
            this.Text            = "Café 101 — Reset Password";
            this.Size            = new Size(480, 480);
            this.StartPosition   = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox     = false;
            this.BackColor       = Color.White;
            this.Font            = new Font("Segoe UI", 10f);

            // Header
            var hdr = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = BrownDk };
            hdr.Controls.Add(new Label
            {
                Text = "🔐  Reset Password",
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Color.White, AutoSize = true, Location = new Point(18, 16)
            });
            this.Controls.Add(hdr);

            int y = 80, lx = 28, w = 406;

            TextBox Field(string label, bool isPassword = false)
            {
                this.Controls.Add(new Label
                {
                    Text = label, Location = new Point(lx, y), AutoSize = true,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(70, 50, 35)
                }); y += 24;
                var tb = new TextBox
                {
                    Location = new Point(lx, y), Size = new Size(w, 34),
                    Font = new Font("Segoe UI", 11f), BorderStyle = BorderStyle.FixedSingle,
                    BackColor = Color.FromArgb(252, 250, 248)
                };
                if (isPassword) tb.PasswordChar = '●';
                this.Controls.Add(tb); y += 44;
                return tb;
            }

            txtEmail   = Field("Email Address:");
            txtNew     = Field("New Password:", true);
            txtConfirm = Field("Confirm New Password:", true);

            // Show/hide password toggle
            chkShow = new CheckBox
            {
                Text = "Show passwords",
                Location = new Point(lx, y), AutoSize = true,
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(100, 80, 60),
                Cursor = Cursors.Hand
            };
            chkShow.CheckedChanged += (s, e) =>
            {
                char c = chkShow.Checked ? '\0' : '●';
                txtNew.PasswordChar     = c;
                txtConfirm.PasswordChar = c;
            };
            this.Controls.Add(chkShow); y += 34;

            // Password rules hint
            this.Controls.Add(new Label
            {
                Text = "Password must be at least 6 characters.",
                Location = new Point(lx, y), AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Italic),
                ForeColor = Color.FromArgb(140, 120, 100)
            }); y += 26;

            // Message label
            lblMsg = new Label
            {
                Location = new Point(lx, y), Size = new Size(w, 28),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(6, 0, 0, 0),
                Visible = false
            };
            this.Controls.Add(lblMsg); y += 34;

            // Reset button
            btnReset = new Button
            {
                Text = "RESET PASSWORD",
                Location = new Point(lx, y), Size = new Size(w, 44),
                BackColor = Brown, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold), Cursor = Cursors.Hand
            };
            btnReset.FlatAppearance.BorderSize = 0;
            btnReset.Click += BtnReset_Click;
            this.Controls.Add(btnReset); y += 52;

            // Cancel link
            var lnkCancel = new LinkLabel
            {
                Text = "Back to Login", Location = new Point(lx + 152, y),
                AutoSize = true, Font = new Font("Segoe UI", 9.5f),
                LinkColor = Brown
            };
            lnkCancel.LinkClicked += (s, e) => this.Close();
            this.Controls.Add(lnkCancel);

            this.ClientSize = new Size(460, y + 44);
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            lblMsg.Visible = false;
            string em = txtEmail.Text.Trim();
            string np = txtNew.Text;
            string cf = txtConfirm.Text;

            if (string.IsNullOrEmpty(em))  { Msg("Email is required.",           true); return; }
            if (!em.Contains("@"))          { Msg("Enter a valid email address.", true); return; }
            if (string.IsNullOrEmpty(np))  { Msg("Enter a new password.",        true); return; }
            if (np.Length < 6)             { Msg("Minimum 6 characters.",        true); return; }
            if (np != cf)                  { Msg("Passwords do not match.",      true); return; }

            btnReset.Enabled = false;
            btnReset.Text    = "Resetting...";
            try
            {
                bool ok = DatabaseHelper.UpdatePassword(em, np);
                if (ok)
                {
                    Msg("✔  Password updated! You may now sign in.", false);
                    var t = new Timer { Interval = 2800 };
                    t.Tick += (s, ev) => { t.Stop(); this.Close(); };
                    t.Start();
                }
                else
                {
                    Msg("⚠  Email not found. Please check and try again.", true);
                }
            }
            catch (Exception ex)
            {
                Msg("Error: " + ex.Message, true);
            }
            finally
            {
                btnReset.Enabled = true;
                btnReset.Text    = "RESET PASSWORD";
            }
        }

        private void Msg(string text, bool isError)
        {
            lblMsg.Text      = text;
            lblMsg.ForeColor = isError ? Color.FromArgb(180, 50,  50) : Color.FromArgb(50, 130, 50);
            lblMsg.BackColor = isError ? Color.FromArgb(255, 240, 240) : Color.FromArgb(240, 255, 240);
            lblMsg.Visible   = true;
        }

        private void label3_Click(object s, EventArgs e) { }
    }
}
