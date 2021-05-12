using System;
using System.Windows.Forms;

namespace Yumu
{
    /// <summary>A hidden process to handle the global hotkey detection.</summary>
    class BackgroundHandler : NativeWindow
    {
        private static IntPtr s_currentHandle;
        public static IntPtr CurrentHandle {get => s_currentHandle;}

        public BackgroundHandler()
        {
            CreateHandle(new CreateParams());
            Application.ApplicationExit += new EventHandler(OnApplicationExit);

            s_currentHandle = Handle;
        }

        private void OnApplicationExit(object sender, EventArgs e)
        {
            DestroyHandle();

            s_currentHandle = IntPtr.Zero;
        }
    }
}