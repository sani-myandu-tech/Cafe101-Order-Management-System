using System;
using System.Drawing;
using System.Windows.Forms;

namespace Cafe101
{
    public partial class Form2 : Form
    {
        private static readonly Color Brown     = Color.FromArgb(111, 78, 55);
        private static readonly Color BrownDark = Color.FromArgb( 74, 50, 37);

        private TextBox  txtFirstName, txtLastName, txtEmail, txtPhone;
        private TextBox  txtPassword, txtConfirm, txtAddress;
        private ComboBox cmbRole, cmbGender;
        private DateTimePicker dtpDob;
        private Button   btnCreate;
        private Label    lblMsg;

        public Form2()
        {
            InitializeComponent();
            BuildUI();
        }

        private void BuildUI()
        {
            this.Text            = "Café 101 — Add Staff Account";
            this.Size            = new Size(540, 660);
            this.StartPosition   = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox     = false;
            this.BackColor       = Color.White;
            this.Font            = new Font("Segoe UI", 10f);

            // Header
            var hdr = new Panel { Location = new Point(0,0), Size = new Size(540,60), BackColor = BrownDark };
            hdr.Controls.Add(new Label
            {
                Text = "☕  Add Staff Account", Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Color.White, AutoSize = true, Location = new Point(18, 14)
            });
            this.Controls.Add(hdr);

            int y = 76, lx = 20, w = 238;

            // ── Row helper ──
            void Field(string label, int x, ref int yy, int width, out TextBox tb, bool pw = false)
            {
                this.Controls.Add(new Label { Text = label, Location = new Point(x, yy), AutoSize = true,
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.FromArgb(70,50,35) });
                yy += 22;
                tb = new TextBox { Location = new Point(x, yy), Size = new Size(width, 32),
                    Font = new Font("Segoe UI", 10.5f), BorderStyle = BorderStyle.FixedSingle,
                    BackColor = Color.FromArgb(252,250,248), PasswordChar = pw ? '●' : '\0' };
                this.Controls.Add(tb);
            }

            // Row 1: First + Last name
            int yr = y;
            Field("First Name:", lx,        ref yr, w, out txtFirstName);
            yr = y;
            Field("Last Name:",  lx+w+20,   ref yr, w, out txtLastName);
            y = yr + 38;

            // Row 2: Email full width
            Field("Email Address:", lx, ref y, 496, out txtEmail); y += 38;

            // Row 3: Phone + Gender
            yr = y;
            Field("Phone Number:", lx, ref yr, w, out txtPhone);
            yr = y;
            this.Controls.Add(new Label { Text="Gender:", Location=new Point(lx+w+20, y), AutoSize=true,
                Font=new Font("Segoe UI",9f,FontStyle.Bold), ForeColor=Color.FromArgb(70,50,35) });
            y += 22;
            cmbGender = new ComboBox { Location=new Point(lx+w+20, yr+22), Size=new Size(w,32),
                DropDownStyle=ComboBoxStyle.DropDownList, Font=new Font("Segoe UI",10.5f) };
            cmbGender.Items.AddRange(new object[]{"Male","Female","Other","Prefer not to say"});
            cmbGender.SelectedIndex = 0;
            this.Controls.Add(cmbGender);
            y = yr + 38 + 22;

            // Row 4: Role + DOB
            this.Controls.Add(new Label { Text="Role:", Location=new Point(lx, y), AutoSize=true,
                Font=new Font("Segoe UI",9f,FontStyle.Bold), ForeColor=Color.FromArgb(70,50,35) });
            this.Controls.Add(new Label { Text="Date of Birth:", Location=new Point(lx+w+20, y), AutoSize=true,
                Font=new Font("Segoe UI",9f,FontStyle.Bold), ForeColor=Color.FromArgb(70,50,35) });
            y += 22;
            cmbRole = new ComboBox { Location=new Point(lx, y), Size=new Size(w,32),
                DropDownStyle=ComboBoxStyle.DropDownList, Font=new Font("Segoe UI",10.5f) };
            cmbRole.Items.AddRange(new object[]{"Cashier","HeadChef","Manager","Owner"});
            cmbRole.SelectedIndex = 0;
            this.Controls.Add(cmbRole);
            dtpDob = new DateTimePicker { Location=new Point(lx+w+20, y), Size=new Size(w,32),
                Font=new Font("Segoe UI",10.5f), Format=DateTimePickerFormat.Short,
                Value=new DateTime(1995,1,1) };
            this.Controls.Add(dtpDob);
            y += 42;

            // Row 5: Password + Confirm
            yr = y;
            Field("Password:", lx, ref yr, w, out txtPassword, pw:true);
            yr = y;
            Field("Confirm Password:", lx+w+20, ref yr, w, out txtConfirm, pw:true);
            y = yr + 38;

            // Row 6: Address
            Field("Address (optional):", lx, ref y, 496, out txtAddress); y += 38;

            // Msg label
            lblMsg = new Label { Location=new Point(lx,y), Size=new Size(496,26),
                Font=new Font("Segoe UI",9.5f,FontStyle.Bold), Visible=false,
                TextAlign=ContentAlignment.MiddleLeft };
            this.Controls.Add(lblMsg); y += 30;

            // Create button
            btnCreate = new Button { Text="✔  CREATE STAFF ACCOUNT",
                Location=new Point(lx,y), Size=new Size(496,46),
                BackColor=Brown, ForeColor=Color.White, FlatStyle=FlatStyle.Flat,
                Font=new Font("Segoe UI",12f,FontStyle.Bold), Cursor=Cursors.Hand };
            btnCreate.FlatAppearance.BorderSize = 0;
            btnCreate.Click += BtnCreate_Click;
            this.Controls.Add(btnCreate);

            this.ClientSize = new Size(540, y + 62);
        }

        private void BtnCreate_Click(object sender, EventArgs e)
        {
            lblMsg.Visible = false;
            string fn=txtFirstName.Text.Trim(), ln=txtLastName.Text.Trim();
            string em=txtEmail.Text.Trim(), ph=txtPhone.Text.Trim();
            string pw=txtPassword.Text, cf=txtConfirm.Text;
            string ad=txtAddress.Text.Trim();

            if (string.IsNullOrEmpty(fn)) { Msg("First name is required.",  true); return; }
            if (string.IsNullOrEmpty(ln)) { Msg("Last name is required.",   true); return; }
            if (string.IsNullOrEmpty(em)) { Msg("Email is required.",       true); return; }
            if (!em.Contains("@"))         { Msg("Enter a valid email.",    true); return; }
            if (string.IsNullOrEmpty(pw)) { Msg("Password is required.",   true); return; }
            if (pw.Length < 6)            { Msg("Minimum 6 characters.",   true); return; }
            if (pw != cf)                 { Msg("Passwords do not match.", true); return; }

            btnCreate.Enabled = false;
            try
            {
                bool ok = DatabaseHelper.RegisterUser(fn, ln, em, ph, pw,
                    cmbRole.Text, cmbGender.Text, dtpDob.Value, ad);
                if (ok)
                {
                    Msg($"✔  {fn} {ln} ({cmbRole.Text}) created successfully!", false);
                    var t = new Timer { Interval = 2000 };
                    t.Tick += (s, ev) => { t.Stop(); this.Close(); };
                    t.Start();
                }
                else Msg("⚠  An account with this email already exists.", true);
            }
            catch (Exception ex) { Msg("Error: " + ex.Message, true); }
            finally { btnCreate.Enabled = true; }
        }

        private void Msg(string text, bool isError)
        {
            lblMsg.Text      = text;
            lblMsg.ForeColor = isError ? Color.FromArgb(180,50,50) : Color.FromArgb(50,130,50);
            lblMsg.BackColor = isError ? Color.FromArgb(255,240,240) : Color.FromArgb(240,255,240);
            lblMsg.Visible   = true;
        }

        private void label5_Click(object s, EventArgs e) { }
        private void label6_Click(object s, EventArgs e) { }
        private void Form2_Load(object s,   EventArgs e) { }
    }
}
