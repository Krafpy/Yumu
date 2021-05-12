using System.Windows.Forms;
using System.Drawing;

namespace Yumu
{
    /// <summary>Base class of all a windowed interface used by the software.</summary>
    class Window : Form
    {
        public const string FONT_NAME = "Microsoft PhagsPa";

        private static bool _opened = false;
        private static Window _current;

        public static bool Opened { get => _opened; }
        public static Window Current {get => _current;}

        public Window(string title, int width, int height)
        {
            _current = this;
            _opened = true;

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
            _opened = false;
        }
    }    
}