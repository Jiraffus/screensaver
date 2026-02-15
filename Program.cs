using System.Runtime.InteropServices;
using Timer = System.Windows.Forms.Timer;

namespace ScreenSaverTrayApp;

internal static class Program
{
    // Windows API: idle time detection
    [DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(ref LastInputInfo plii);

    [StructLayout(LayoutKind.Sequential)]
    private struct LastInputInfo
    {
        public uint cbSize;
        public uint dwTime;
    }

    // Windows API: send message to window
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    // Windows API: get active window
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr GetForegroundWindow();

    private const int ScScreenSave = 0xF140;
    private const int WmSysCommand = 0x0112;

    private static NotifyIcon? _notifyIcon;
    private static Timer? _idleTimer;
    private static uint _idleLimitMs = 60 * 1000; // default: 1 minute

    private static ToolStripMenuItem? _menuItem30Sec;
    private static ToolStripMenuItem? _menuItem1Min;
    private static ToolStripMenuItem? _menuItem5Min;

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Information,
            Text = "ScreenSaver Tray",
            Visible = true,
            ContextMenuStrip = CreateContextMenu()
        };

        _idleTimer = new Timer { Interval = 1000 };
        _idleTimer.Tick += OnIdleTimerTick;
        _idleTimer.Start();

        Application.ApplicationExit += OnApplicationExit;
        Application.Run();
    }

    private static ContextMenuStrip CreateContextMenu()
    {
        var menu = new ContextMenuStrip();

        _menuItem30Sec = new ToolStripMenuItem("30 сек",   null, (_, _) => SetIdleLimit(30 * 1000,      _menuItem30Sec));
        _menuItem1Min  = new ToolStripMenuItem("1 минута", null, (_, _) => SetIdleLimit(60 * 1000,      _menuItem1Min));
        _menuItem5Min  = new ToolStripMenuItem("5 минут",  null, (_, _) => SetIdleLimit(5 * 60 * 1000,  _menuItem5Min));

        _menuItem1Min.Checked = true; // default selection

        menu.Items.Add(_menuItem30Sec);
        menu.Items.Add(_menuItem1Min);
        menu.Items.Add(_menuItem5Min);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Выход", null, (_, _) => Application.Exit()));

        return menu;
    }

    private static void SetIdleLimit(uint ms, ToolStripMenuItem? selected)
    {
        _idleLimitMs = ms;

        if (_menuItem30Sec != null) _menuItem30Sec.Checked = false;
        if (_menuItem1Min  != null) _menuItem1Min.Checked  = false;
        if (_menuItem5Min  != null) _menuItem5Min.Checked  = false;
        if (selected      != null) selected.Checked      = true;
    }

    private static void OnIdleTimerTick(object? sender, EventArgs e)
    {
        if (GetIdleTimeMs() >= _idleLimitMs)
            LaunchScreenSaver();
    }

    /// <summary>Returns the number of milliseconds since the last user input.</summary>
    private static uint GetIdleTimeMs()
    {
        var info = new LastInputInfo { cbSize = (uint)Marshal.SizeOf<LastInputInfo>() };
        if (!GetLastInputInfo(ref info))
            return 0;

        // Use TickCount64 to avoid the ~24.8-day uint overflow of Environment.TickCount
        return (uint)Math.Min(Environment.TickCount64 - info.dwTime, uint.MaxValue);
    }

    private static void LaunchScreenSaver()
    {
        IntPtr hWnd = GetForegroundWindow();
        SendMessage(hWnd, WmSysCommand, new IntPtr(ScScreenSave), IntPtr.Zero);
    }

    private static void OnApplicationExit(object? sender, EventArgs e)
    {
        _idleTimer?.Stop();
        _idleTimer?.Dispose();
        _notifyIcon?.Dispose();
    }
}