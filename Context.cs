using System;
using System.Windows.Forms;
using System.Drawing;

namespace Yumu
{
    class Context : ApplicationContext
    {
        private const string HK_FILE = "./hotkey.hk";

        private Hotkey _hk;
        private BackgroundHandler _hkHandler;

        private NotifyIcon _trayIcon;

        public Context()
        {
            // Create data files (for directory and image referencing)
            // Files are only created if they don't exist
            DB.CreateDataFiles(new string[] {
                DBAccessor.IMGS_DB_FILE, DBAccessor.DIRS_DB_FILE
            });

            // Initialize Tray Icon
            _trayIcon = new NotifyIcon()
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
            _trayIcon.DoubleClick += OpenDirectoryManager;

            // Initialize hotkey detection
            _hk = new Hotkey(Keys.F9, false, false, false, false); // Default hotkey
            _hk.Pressed += OpenSearch;
            if(!_hk.Load(HK_FILE))
                _hk.Save(HK_FILE);
            
            _hkHandler = new BackgroundHandler();
            IntPtr handle = _hkHandler.Handle;
            if(_hk.GetCanRegister(handle)){
                _hk.Register(handle);
            } else {
                string message = "Cannot register the hotkey, change it in the configuration.";
                string caption = "Yumu Hotkey Error";
                MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Exit(object sender, EventArgs e)
        {
            // Hide tray icon, otherwise it will remain shown until user mouses over it
            _trayIcon.Visible = false;
            _hk.Unregister();
            Application.Exit();
        }

        private void OpenHotkeyEditor(object sender, EventArgs e)
        {
            if(!Window.Opened){
                HotkeyEditor hkEditor = new HotkeyEditor(_hk, HK_FILE);
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
