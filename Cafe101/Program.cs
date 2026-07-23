using System;
using System.Windows.Forms;

namespace Cafe101
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            bool dbOk = false;
            Exception dbEx = null;

            var bgThread = new System.Threading.Thread(() => {
                try {
                    dbOk = DatabaseHelper.TestConnection();
                    if (dbOk) DatabaseHelper.CreateDatabaseIfNeeded();
                } catch (Exception ex) { dbEx = ex; }
            });
            bgThread.IsBackground = true;
            bgThread.Start();

            // Show splash — it will hold at 85% until DB thread signals ready
            // Run splash on this thread (DoEvents loop inside ShowSplash)
            // Signal from bg thread via SplashScreen.DbReady
            var watchThread = new System.Threading.Thread(() => {
                bgThread.Join();
                SplashScreen.DbReady = true;
            });
            watchThread.IsBackground = true;
            watchThread.Start();

            SplashScreen.ShowSplash(3200);
            bgThread.Join(500); // ensure fully done

            if (!dbOk) {
                var result = MessageBox.Show(
                    "Cannot connect to the database.\n\n" +
                    "Server:   146.230.177.46\n" +
                    "Database: ist3dy\n" +
                    "User:     ist3dy\n\n" +
                    "Possible causes:\n" +
                    "• Wrong password in DatabaseHelper.cs\n" +
                    "• SQL Server not reachable (check VPN/network)\n" +
                    "• Firewall blocking port 1433\n\n" +
                    "Click OK to open the app anyway (data won't load)\n" +
                    "Click Cancel to exit.",
                    "Database Connection Failed",
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                if (result == DialogResult.Cancel) return;
            } else if (dbEx != null) {
                MessageBox.Show(
                    "Database setup warning:\n\n" + dbEx.Message,
                    "Setup Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            Application.Run(new Form1());
        }

    }
}
