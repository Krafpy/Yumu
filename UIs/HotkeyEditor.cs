using System;
using System.Windows.Forms;
using System.Drawing;

namespace Yumu
{
    class HotkeyEditor : Window
    {
        private Hotkey hk;
        private string hkPath;

        private Hotkey newHk;

        private Label newHkLab;

        const int X_MARGIN = 10;
        const int Y_MARGIN = 10;

        public HotkeyEditor(Hotkey hk, string hkPath) : base("Yumu hotkey", 350, 100)
        {
            this.hk = hk;
            this.hkPath = hkPath;

            newHk = new Hotkey();

            InitializeComponents();
        }

        private void InitializeComponents()
        {
            Label curHkInfo = new Label(){
                Font = new Font(FONT_NAME, 10),
                Text = "Current hotkey :",
                Location = new Point(X_MARGIN, Y_MARGIN),
                AutoSize = true
            };
            Controls.Add(curHkInfo);

            Label curHk = new Label(){
                Font = new Font(FONT_NAME, 10, FontStyle.Bold),
                Text = hk.ToString(),
                ForeColor = Color.FromArgb(100, 100, 100),
                Location = new Point(curHkInfo.Location.X + curHkInfo.PreferredWidth, Y_MARGIN),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft
            };
            Controls.Add(curHk);

            Label newHkInfo = new Label(){
                Font = new Font(FONT_NAME, 12),
                Text = "Type a new key combination :",
                Location = new Point(X_MARGIN, curHkInfo.Height + Y_MARGIN),
                AutoSize = true
            };
            Controls.Add(newHkInfo);

            newHkLab = new Label(){
                Font = new Font(FONT_NAME, 14, FontStyle.Bold),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, ClientSize.Height / 2 + newHkInfo.Height / 2)
            };
            Controls.Add(newHkLab);
        }

        protected override void OnKeyDown(object sender, KeyEventArgs e)
        {
            base.OnKeyDown(sender, e);

            if(e.KeyCode == Keys.Enter)
                SaveNewHotkey();
            else
                RecordHotkey(e);
        }

        private void SaveNewHotkey()
        {
            hk.Assign(newHk);
            if(!hk.Registered){
                IntPtr handle = BackgroundHandler.CurrentHandle;
                if(hk.GetCanRegister(handle)){
                    hk.Register(handle);
                } else {
                    string message = "Cannot register this hotkey.";
                    string caption = "Yumu Hotkey Error";
                    MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            } else {
                hk.Reregister();
            }
            hk.Save(hkPath);
            
            Close();
        }

        private void RecordHotkey(KeyEventArgs e)
        {
            newHk.Read(e);
            newHkLab.Text = newHk.ToString();

            // Recenter the hotkey label
            int posX = ClientSize.Width / 2 - newHkLab.Width / 2;
            int posY = newHkLab.Location.Y;

            newHkLab.Location = new Point(posX, posY);
        }
    }
}