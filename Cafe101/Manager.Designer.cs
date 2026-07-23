namespace Cafe101
{
    partial class Manager
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            // All UI is built in Manager.cs BuildUI() — nothing needed here.
            this.SuspendLayout();
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize    = new System.Drawing.Size(1280, 768);
            this.Name = "Manager";
            this.Text = "Café 101 — Manager Dashboard";
            this.ResumeLayout(false);
        }
    }
}
