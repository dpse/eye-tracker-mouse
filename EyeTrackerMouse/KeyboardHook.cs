namespace EyeTrackerMouse
{
    using System;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;

    internal sealed class KeyboardHook : IDisposable
    {
        private readonly Window window = new Window();

        private int currentId;

        public KeyboardHook()
        {
            // register the event of the inner native window.
            this.window.KeyPressed += delegate(object sender, KeyPressedEventArgs args)
                {
                    this.KeyPressed?.Invoke(this, args);
                };
        }

        #region IDisposable Members

        public void Dispose()
        {
            // unregister all the registered hot keys.
            for (var i = this.currentId; i > 0; i--) UnregisterHotKey(this.window.Handle, i);

            // dispose the inner native window.
            this.window.Dispose();
        }

        #endregion

        /// <summary>
        ///     A hot key has been pressed.
        /// </summary>
        public event EventHandler<KeyPressedEventArgs> KeyPressed;

        // Registers a hot key with Windows.
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        /// <summary>
        ///     Registers a hot key in the system.
        /// </summary>
        /// <param name="modifier">The modifiers that are associated with the hot key.</param>
        /// <param name="key">The key itself that is associated with the hot key.</param>
        public void RegisterHotKey(ModifierKeys modifier, Keys key)
        {
            // increment the counter.
            this.currentId = this.currentId + 1;

            // register the hot key.
            if (!RegisterHotKey(this.window.Handle, this.currentId, (uint)modifier, (uint)key))
                throw new InvalidOperationException("Couldn’t register the hot key.");
        }

        // Unregisters the hot key with Windows.
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        /// <summary>
        ///     Represents the window that is used internally to get the messages.
        /// </summary>
        private class Window : NativeWindow, IDisposable
        {
            private static readonly int WM_HOTKEY = 0x0312;

            public Window()
            {
                // create the handle for the window.
                this.CreateHandle(new CreateParams());
            }

            #region IDisposable Members

            public void Dispose()
            {
                this.DestroyHandle();
            }

            #endregion

            public event EventHandler<KeyPressedEventArgs> KeyPressed;

            /// <summary>
            ///     Overridden to get the notifications.
            /// </summary>
            /// <param name="m"></param>
            protected override void WndProc(ref Message m)
            {
                var handled = false;
                // check if we got a hot key pressed.
                if (m.Msg == WM_HOTKEY)
                {
                    // get the keys.
                    var key = (Keys)(((int)m.LParam >> 16) & 0xFFFF);
                    var modifier = (ModifierKeys)((int)m.LParam & 0xFFFF);

                    // invoke the event to notify the parent.
                    if (this.KeyPressed != null)
                    {
                        var args = new KeyPressedEventArgs(modifier, key);
                        this.KeyPressed(this, args);
                        if (args.Handled)
                            handled = true;
                    }
                }

                if (!handled)
                    base.WndProc(ref m);
            }
        }
    }

    /// <summary>
    ///     Event Args for the event that is fired after the hot key has been pressed.
    /// </summary>
    public class KeyPressedEventArgs : EventArgs
    {
        internal KeyPressedEventArgs(ModifierKeys modifier, Keys key)
        {
            this.Modifier = modifier;
            this.Key = key;
        }

        public bool Handled { get; set; }

        public Keys Key { get; }

        public ModifierKeys Modifier { get; }
    }

    /// <summary>
    ///     The enumeration of possible modifiers.
    /// </summary>
    [Flags]
    public enum ModifierKeys : uint
    {
        None = 0,

        Alt = 1,

        Control = 2,

        Shift = 4,

        Win = 8
    }
}