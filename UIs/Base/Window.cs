using System.Windows.Forms;
using System.Drawing;

namespace Yumu
{
    /// <summary>Base class of all a windowed interface used by the software.</summary>
    class Window : Form
    {
        public const string FONT_NAME = "Microsoft PhagsPa";

        private static bool s_opened = false;
        private static Window s_current;

        public static bool Opened { get => s_opened; }
        public static Window Current {get => s_current;}

        public Window(string title, int width, int height)
        {
            s_current = this;
            s_opened = true;

            Text = title;
            ClientSize = new Size(width, height);
            CenterToScreen();

            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;

            KeyPreview = true;

            KeyDown += OnKeyDown;
            FormClosed += OnFormClose;
        }
        
        protected virtual void OnKeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Escape)
                Close();
        }

        protected virtual void OnFormClose(object sender, FormClosedEventArgs e)
        {
            s_opened = false;
            s_current = null;
        }
    }    
}