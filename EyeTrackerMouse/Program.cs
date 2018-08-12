namespace EyeTrackerMouse
{
    using System;
    using System.Drawing;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Timers;
    using System.Windows.Forms;

    using Tobii.Interaction;
    using Tobii.Interaction.Framework;

    using Timer = System.Timers.Timer;

    internal class Program
    {
        private const int MOUSEEVENTF_LEFTDOWN = 0x02;

        private const int MOUSEEVENTF_LEFTUP = 0x04;

        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;

        private const int MOUSEEVENTF_RIGHTUP = 0x10;

        private static Point currentGazePoint;

        private static Host host;

        private static KeyboardHook keyboardHook;

        private static DateTime lastHotkeyTime = DateTime.MinValue;

        private static NotifyIcon notifyIcon;

        private static FixationDataStream stream;

        private static Timer timer;

        [DllImport("User32.Dll")]
        public static extern bool ClientToScreen(IntPtr hWnd, ref POINT point);

        private static void CreateNotifyIcon()
        {
            var appIcon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
            notifyIcon = new NotifyIcon { ContextMenu = new ContextMenu(), Icon = appIcon };
            var aboutItem = new MenuItem("&About");
            aboutItem.Click += (sender, args) => new AboutBox().ShowDialog();
            notifyIcon.ContextMenu.MenuItems.Add(aboutItem);
            var exitItem = new MenuItem("&Exit");
            exitItem.Click += ExitItemOnClick;
            notifyIcon.ContextMenu.MenuItems.Add(exitItem);
            notifyIcon.Visible = true;
        }

        private static void DoMouseClick()
        {
            var X = (uint)Cursor.Position.X;
            var Y = (uint)Cursor.Position.Y;
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, X, Y, 0, 0);
        }

        private static void ExitItemOnClick(object sender, EventArgs eventArgs)
        {
            Application.Exit();
        }

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        private static void KeyboardHookOnKeyPressed(object sender, KeyPressedEventArgs keyPressedEventArgs)
        {
            if (timer.Enabled)
            {
                timer.Stop();
                DoMouseClick();
            }
            else
            {
                timer.Start();
            }

            keyPressedEventArgs.Handled = true;
        }

        [STAThread]
        public static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            SetupStream();
            SetupTimer();
            CreateNotifyIcon();

            SetupHotkeys();

            Application.Run();

            notifyIcon.Visible = false;

            keyboardHook.Dispose();
            notifyIcon.Dispose();
            host.Dispose();
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

        private static void MoveCursorToGazePosition()
        {
            MoveMouseCursor(currentGazePoint.X, currentGazePoint.Y);
        }

        private static void MoveMouseCursor(int x, int y)
        {
            SetCursorPos(x, y);
        }

        [DllImport("User32.Dll")]
        public static extern long SetCursorPos(int x, int y);

        private static void SetupHotkeys()
        {
            keyboardHook = new KeyboardHook();
            keyboardHook.KeyPressed += KeyboardHookOnKeyPressed;
            keyboardHook.RegisterHotKey(ModifierKeys.None, Keys.F14);
        }

        private static void SetupStream()
        {
            host = new Host();
            stream = host.Streams.CreateFixationDataStream(FixationDataMode.Slow);
            stream.Data(
                (x, y, ts) =>
                    {
                        currentGazePoint.X = (int)Math.Round(x);
                        currentGazePoint.Y = (int)Math.Round(y);
                    });
        }

        private static void SetupTimer()
        {
            timer = new Timer(250);
            timer.Elapsed += TimerOnElapsed;
        }

        private static void TimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            timer.Stop();
            MoveCursorToGazePosition();
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;

            public int y;
        }
    }
}