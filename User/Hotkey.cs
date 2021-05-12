using System;
using System.Windows.Forms;
using System.ComponentModel;
using System.Xml.Serialization;
using System.Runtime.InteropServices;
using System.IO;

namespace Yumu
{
	/*
	Adapted script from : https://bloggablea.wordpress.com/2007/05/01/global-hotkeys-with-net/
	*/
	/// <summary>Reprensents a hotkey.</summary>
    public class Hotkey : IMessageFilter
	{
		#region Interop

		[DllImport("user32.dll", SetLastError = true)]
		private static extern int RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, Keys vk);

		[DllImport("user32.dll", SetLastError=true)]
		private static extern int UnregisterHotKey(IntPtr hWnd, int id);

		private const uint WM_HOTKEY = 0x312;

		private const uint MOD_ALT = 0x1;
		private const uint MOD_CONTROL = 0x2;
		private const uint MOD_SHIFT = 0x4;
		private const uint MOD_WIN = 0x8;

		private const uint ERROR_HOTKEY_ALREADY_REGISTERED = 1409;

		#endregion

		private static int currentID;
		private const int maximumID = 0xBFFF;
		
		private Keys keyCode;
        private bool shift;
        private bool control;
        private bool alt;
		private bool windows;

		[XmlIgnore]
		private int id;
		[XmlIgnore]
		private bool registered;
		[XmlIgnore]
		private IntPtr windowHandle;

		public event HandledEventHandler Pressed;

		public Hotkey() : this(Keys.None, false, false, false, false)
		{
			// No work done here!
		}
		
		public Hotkey(Keys keyCode, bool shift, bool control, bool alt, bool windows)
		{
			// Assign properties
			this.KeyCode = keyCode;
			this.Shift = shift;
			this.Control = control;
			this.Alt = alt;
			this.Windows = windows;

			// Register us as a message filter
			Application.AddMessageFilter(this);
		}

		~Hotkey()
		{
			// Unregister the hotkey if necessary
			if (this.Registered)
			{ this.Unregister(); }
		}

		public Hotkey Clone()
		{
			// Clone the whole object
			return new Hotkey(this.keyCode, this.shift, this.control, this.alt, this.windows);
		}

		/// <summary>Loads the hotkey from the given file.</summary>
		public bool Load(string path)
		{
			if(!File.Exists(path)) { return false; }
			
			using(BinaryReader br = new BinaryReader(File.OpenRead(path)))
			{
				int modifiers = br.ReadByte();
				this.Shift 	 = (modifiers & 1) != 0;
				this.Control = (modifiers & 2) != 0;
				this.Alt 	 = (modifiers & 4) != 0;
				this.Windows = (modifiers & 8) != 0;

				this.KeyCode = (Keys)br.ReadInt32();
			}
			return true;
		}

		/// <summary>Saves the hotkey in the given file.</summary>
		public void Save(string path)
		{
			if(!File.Exists(path))
				File.Create(path).Dispose();

			using(BinaryWriter bw = new BinaryWriter(File.OpenWrite(path)))
			{
				int modifiers = 0;
				modifiers |= (this.Shift   ? 1 : 0);
				modifiers |= (this.Control ? 2 : 0);
				modifiers |= (this.Alt 	   ? 4 : 0);
				modifiers |= (this.Windows ? 8 : 0);

				bw.Write((byte)modifiers);
				bw.Write((int)this.KeyCode);
			}
		}

		/// <summary>Assigns properties from a keypress event.</summary>
		public void Read(KeyEventArgs e)
		{
			this.keyCode = e.KeyCode;
			this.shift = e.Shift;
			this.control = e.Control;
			this.alt = e.Alt;
		}

		/// <sumary>Assign properties from another hotkey instance.</summary>
		public void Assign(Hotkey hk)
		{
			if(hk.Empty) { return; }
			
			// Assign properties from another hotkey instance
			this.keyCode = hk.KeyCode;
			this.shift = hk.Shift;
			this.control = hk.Control;
			this.alt = hk.Alt;
			this.windows = hk.Windows;
		}

		public bool GetCanRegister(IntPtr windowHandle)
		{
			// Handle any exceptions: they mean "no, you can't register" :)
			try
			{
				// Attempt to register
				if (!this.Register(windowHandle))
				{ return false; }

				// Unregister and say we managed it
				this.Unregister();
				return true;
			}
			catch (Win32Exception)
			{ return false; }
			catch (NotSupportedException)
			{ return false; }
		}

		public bool Register(IntPtr windowHandle)
        {
            // Check that we have not registered
			if (this.registered)
			{ throw new NotSupportedException("You cannot register a hotkey that is already registered"); }
        
			// We can't register an empty hotkey
			if (this.Empty)
			{ throw new NotSupportedException("You cannot register an empty hotkey"); }

			// Get an ID for the hotkey and increase current ID
			this.id = Hotkey.currentID;
			Hotkey.currentID = Hotkey.currentID + 1 % Hotkey.maximumID;

			// Translate modifier keys into unmanaged version
			uint modifiers = (this.Alt ? Hotkey.MOD_ALT : 0) | (this.Control ? Hotkey.MOD_CONTROL : 0) |
							(this.Shift ? Hotkey.MOD_SHIFT : 0) | (this.Windows ? Hotkey.MOD_WIN : 0);

			// Register the hotkey
			if (Hotkey.RegisterHotKey(windowHandle, this.id, modifiers, keyCode) == 0)
			{ 
				// Is the error that the hotkey is registered?
				if (Marshal.GetLastWin32Error() == ERROR_HOTKEY_ALREADY_REGISTERED)
				{ return false; }
				else
				{ throw new Win32Exception(); } 
			}

			// Save the control reference and register state
			this.registered = true;
			this.windowHandle = windowHandle;

			// We successfully registered
			return true;
		}

		public void Unregister()
		{
			// Check that we have registered
			if (!this.registered)
			{ throw new NotSupportedException("You cannot unregister a hotkey that is not registered"); }
        
			// It's possible that the control itself has died: in that case, no need to unregister!
			try {
				// Clean up after ourselves
				if (Hotkey.UnregisterHotKey(windowHandle, this.id) == 0)
				{ throw new Win32Exception(); }
			} catch (Win32Exception)
			{ }
			catch (NotSupportedException)
			{ }

			/*if (this.windowHandle != IntPtr.Zero)
			{
				// Clean up after ourselves
				if (Hotkey.UnregisterHotKey(this.windowHandle, this.id) == 0)
				{ throw new Win32Exception(); }
			}*/

			// Clear the control reference and register state
			this.registered = false;
			this.windowHandle = IntPtr.Zero;
		}

		public void Reregister()
		{
			// Only do something if the key is already registered
			if (!this.registered)
			{ return; }

			// Unregister and then reregister again
			this.Unregister();
			this.Register(windowHandle);
		}

		public bool PreFilterMessage(ref Message message)
		{
			// Only process WM_HOTKEY messages
			if (message.Msg != Hotkey.WM_HOTKEY)
			{ return false; }

			// Check that the ID is our key and we are registerd
			if (this.registered && (message.WParam.ToInt32() == this.id))
			{
				// Fire the event and pass on the event if our handlers didn't handle it
				return this.OnPressed();
			}
			else
			{ return false; }
		}

		private bool OnPressed()
		{
			// Fire the event if we can
			HandledEventArgs handledEventArgs = new HandledEventArgs(false);
			if (this.Pressed != null)
			{ this.Pressed(this, handledEventArgs); }

			// Return whether we handled the event or not
			return handledEventArgs.Handled;
		}

        public override string ToString()
        {
			// We can be empty
			if (this.Empty)
			{ return "(none)"; }

			// Build key name
			string keyName = Enum.GetName(typeof(Keys), this.keyCode);;
			switch (this.keyCode)
			{
				case Keys.D0:
				case Keys.D1:
				case Keys.D2:
				case Keys.D3:
				case Keys.D4:
				case Keys.D5:
				case Keys.D6:
				case Keys.D7:
				case Keys.D8:
				case Keys.D9:
					// Strip the first character
					keyName = keyName.Substring(1);
					break;
				default:
					// Leave everything alone
					break;
			}

            // Build modifiers
            string modifiers = "";
            if (this.shift)
            { modifiers += "Shift+"; }
            if (this.control)
            { modifiers += "Control+"; }
            if (this.alt)
            { modifiers += "Alt+"; }
			if (this.windows)
			{ modifiers += "Windows+"; }

			// Return result
            return modifiers + keyName;
        }

		public bool Empty
		{
			get { return this.keyCode == Keys.None; }
		}

		public bool Registered
		{
			get { return this.registered; }
		}

        public Keys KeyCode
        {
            get { return this.keyCode; }
            set
			{
				// Save and reregister
				this.keyCode = value;
				this.Reregister();
			}
        }

        public bool Shift
        {
            get { return this.shift; }
            set 
			{
				// Save and reregister
				this.shift = value;
				this.Reregister();
			}
        }

        public bool Control
        {
            get { return this.control; }
            set
			{ 
				// Save and reregister
				this.control = value;
				this.Reregister();
			}
        }

        public bool Alt
        {
            get { return this.alt; }
            set
			{ 
				// Save and reregister
				this.alt = value;
				this.Reregister();
			}
        }

		public bool Windows
		{
			get { return this.windows; }
			set 
			{
				// Save and reregister
				this.windows = value;
				this.Reregister();
			}
		}
    }
}
