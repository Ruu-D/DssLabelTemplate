using System.Runtime.InteropServices;

namespace DssLabelTemplate;

/// <summary>
/// The few Windows functions and numbers a window WITHOUT a Windows frame needs (2.0.0, Daan 2026-11-21: the top bar of
/// the program replaces the Windows title bar): move the window when the top bar is dragged, tell Windows where the
/// window can be resized, and keep a maximised window off the taskbar. Only used by <see cref="MainForm"/>.
/// </summary>
internal static class NativeWindowMethods
{
    // Messages
    public const int WM_NCHITTEST = 0x0084;      // "what is at this point of the window?" (title bar, edge, ...)
    public const int WM_NCLBUTTONDOWN = 0x00A1;  // the mouse went down on the frame / title bar
    public const int WM_GETMINMAXINFO = 0x0024;  // "how big may the window be, also maximised?"

    // Answers to WM_NCHITTEST
    public const int HTCLIENT = 1;
    public const int HTCAPTION = 2;
    public const int HTLEFT = 10;
    public const int HTRIGHT = 11;
    public const int HTTOP = 12;
    public const int HTTOPLEFT = 13;
    public const int HTTOPRIGHT = 14;
    public const int HTBOTTOM = 15;
    public const int HTBOTTOMLEFT = 16;
    public const int HTBOTTOMRIGHT = 17;

    // Window styles that draw nothing but let the taskbar minimise / restore the window, with the usual animation.
    public const int WS_SYSMENU = 0x00080000;
    public const int WS_MINIMIZEBOX = 0x00020000;
    public const int WS_MAXIMIZEBOX = 0x00010000;

    [DllImport("user32.dll")]
    public static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    public static extern IntPtr SendMessage(IntPtr window, int message, int wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MinMaxInfo
    {
        public NativePoint Reserved;
        public NativePoint MaxSize;
        public NativePoint MaxPosition;
        public NativePoint MinTrackSize;
        public NativePoint MaxTrackSize;
    }

    /// <summary>
    /// Answers WM_GETMINMAXINFO: maximised = the working area of the monitor the window is on (without the taskbar). A
    /// window without a frame would otherwise cover the whole monitor, taskbar included. <paramref name="wholeMonitor"/>:
    /// the whole monitor after all, on purpose (F11, full screen). The position is relative to that monitor, as Windows
    /// wants it.
    /// </summary>
    public static void LimitMaximizedToWorkingArea(IntPtr window, IntPtr minMaxInfo, bool wholeMonitor)
    {
        Screen screen = Screen.FromHandle(window);
        Rectangle whole = screen.Bounds, work = wholeMonitor ? whole : screen.WorkingArea;
        MinMaxInfo info = Marshal.PtrToStructure<MinMaxInfo>(minMaxInfo);
        info.MaxPosition = new NativePoint { X = work.Left - whole.Left, Y = work.Top - whole.Top };
        info.MaxSize = new NativePoint { X = work.Width, Y = work.Height };
        Marshal.StructureToPtr(info, minMaxInfo, false);
    }
}
