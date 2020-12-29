using System;
using System.Windows.Forms;

namespace Yumu
{
    /// <summary>A hidden process to handle the global hotkey detection.</summary>
    class BackgroundHandler : NativeWindow
    {
        private static IntPtr currentHandle;
        public static IntPtr CurrentHandle {get => currentHandle;}

        public BackgroundHandler()
        {
            CreateHandle(new CreateParams());
            Application.ApplicationExit += new EventHandler(OnApplicationExit);

            currentHandle = Handle;
        }

        private void OnApplicationExit(object sender, EventArgs e)
        {
            DestroyHandle();

            currentHandle = IntPtr.Zero;
        }
    }
}