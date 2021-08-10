using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;

namespace Yumu
{
    class Program
    {   
        [STAThread]
        static void Main(string[] args)
        {
            if(AppIsAlreadyRunning()) {
                string msg = "An instance of Yumu is already running in the background.";
                string caption = "Error Already runnin.";
                MessageBox.Show(msg, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.Run(new Context());
        }

        private static bool AppIsAlreadyRunning()
        {
            return Process.GetProcessesByName(Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetEntryAssembly().Location)).Length > 1;
        }
    }
}
