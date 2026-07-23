using System;
using System.Drawing;
using System.Windows.Forms;

namespace Cafe101
{
    public static class InputDialog
    {
        public static string Show(string prompt, string title = "Input", string defaultValue = "")
        {
            string result = null;

            var form = new Form
            {
                Text            = title,
                Size            = new Size(420, 180),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition   = FormStartPosition.CenterParent,
                MaximizeBox     = false,
                MinimizeBox     = false,
                BackColor       = Color.White,
                Font            = new Font("Segoe UI", 10f)
            };

            var lbl = new Label
            {
                Text      = prompt,
                Location  = new Point(16, 16),
                Size      = new Size(376, 24),
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(70, 50, 35)
            };

            var txt = new TextBox
            {
                Text        = defaultValue,
                Location    = new Point(16, 46),
                Size        = new Size(376, 34),
                Font        = new Font("Segoe UI", 11f),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor   = Color.FromArgb(252, 250, 248)
            };

            var btnOk = new Button
            {
                Text         = "OK",
                DialogResult = DialogResult.OK,
                Location     = new Point(196, 94),
                Size         = new Size(100, 36),
                BackColor    = Color.FromArgb(111, 78, 55),
                ForeColor    = Color.White,
                FlatStyle    = FlatStyle.Flat,
                Font         = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor       = Cursors.Hand
            };
            btnOk.FlatAppearance.BorderSize = 0;

            var btnCancel = new Button
            {
                Text         = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location     = new Point(304, 94),
                Size         = new Size(88, 36),
                BackColor    = Color.FromArgb(200, 195, 188),
                ForeColor    = Color.FromArgb(60, 45, 30),
                FlatStyle    = FlatStyle.Flat,
                Font         = new Font("Segoe UI", 10f),
                Cursor       = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            form.AcceptButton = btnOk;
            form.CancelButton = btnCancel;
            form.Controls.AddRange(new Control[] { lbl, txt, btnOk, btnCancel });
            form.Shown += (s, e) => { txt.Focus(); txt.SelectAll(); };

            if (form.ShowDialog() == DialogResult.OK)
                result = txt.Text;

            form.Dispose();
            return result;
        }
    }
}
