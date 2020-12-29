using System;
using System.Windows.Forms;
using System.Drawing;

namespace Yumu
{
    class Context : ApplicationContext
    {
        private const string HK_FILE = "./hotkey.hk";

        private Hotkey hk;
        private BackgroundHandler hkHandler;

        private NotifyIcon trayIcon;

        public Context()
        {
            // Create data files (for directory and image referencing)
            // Files are only created if they don't exist
            DB.CreateDataFiles();

            // Initialize Tray Icon
            trayIcon = new NotifyIcon()
            {
                Icon = SystemIcons.Application,
                ContextMenu = new ContextMenu(new MenuItem[] {
                    new MenuItem("Search...", OpenSearch),
                    new MenuItem("Manage directories", OpenDirectoryManager),
                    new MenuItem("Edit hotkey", OpenHotkeyEditor),
                    new MenuItem("Exit", Exit)
                }),
                Visible = true,
                Text = "Yumu image searcher"
            };
            trayIcon.DoubleClick += OpenDirectoryManager;

            // Initialize hotkey detection
            hk = new Hotkey(Keys.F9, false, false, false, false); // Default hotkey
            hk.Pressed += OpenSearch;
            if(!hk.Load(HK_FILE))
                hk.Save(HK_FILE);
            
            hkHandler = new BackgroundHandler();
            IntPtr handle = hkHandler.Handle;
            if(hk.GetCanRegister(handle)){
                hk.Register(handle);
            } else {
                string message = "Cannot register the hotkey, change it in the configuration.";
                string caption = "Yumu Hotkey Error";
                MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Exit(object sender, EventArgs e)
        {
            // Hide tray icon, otherwise it will remain shown until user mouses over it
            trayIcon.Visible = false;
            
            hk.Unregister();

            Application.Exit();
        }

        private void OpenHotkeyEditor(object sender, EventArgs e)
        {
            if(!Window.Opened){
                HotkeyEditor hkEditor = new HotkeyEditor(hk, HK_FILE);
                hkEditor.Show();
            }
            Window.Current.Activate();
        }

        private void OpenSearch(object sender, EventArgs e)
        {
            if(!Window.Opened){
                SearchWindow imageSearch = new SearchWindow();
                imageSearch.Show();
            }
            Window.Current.Activate();
        }

        private void OpenDirectoryManager(object sender, EventArgs e)
        {
            if(!Window.Opened){
                DirectoryManager dirManager = new DirectoryManager();
                dirManager.Show();
            }
            Window.Current.Activate();
        }
    }
}
