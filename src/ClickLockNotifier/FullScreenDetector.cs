using System.Runtime.InteropServices;

namespace ClickLockNotifier;

internal static class FullScreenDetector
{
    private const uint MonitorDefaultToNearest = 2;
    private const int BoundaryTolerancePixels = 2;

    public static bool IsForegroundWindowFullScreen()
    {
        var foregroundWindow = GetForegroundWindow();
        if (foregroundWindow == IntPtr.Zero ||
            foregroundWindow == GetDesktopWindow() ||
            foregroundWindow == GetShellWindow())
        {
            return false;
        }

        if (!IsWindowVisible(foregroundWindow) ||
            !GetWindowRect(foregroundWindow, out var windowRect))
        {
            return false;
        }

        var monitor = MonitorFromWindow(foregroundWindow, MonitorDefaultToNearest);
        if (monitor == IntPtr.Zero)
        {
            return false;
        }

        var monitorInfo = new MonitorInfo
        {
            Size = Marshal.SizeOf<MonitorInfo>()
        };

        if (!GetMonitorInfo(monitor, ref monitorInfo))
        {
            return false;
        }

        return windowRect.Left <= monitorInfo.Monitor.Left + BoundaryTolerancePixels &&
               windowRect.Top <= monitorInfo.Monitor.Top + BoundaryTolerancePixels &&
               windowRect.Right >= monitorInfo.Monitor.Right - BoundaryTolerancePixels &&
               windowRect.Bottom >= monitorInfo.Monitor.Bottom - BoundaryTolerancePixels;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetDesktopWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetShellWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo lpmi);

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int Size;
        public Rect Monitor;
        public Rect WorkArea;
        public uint Flags;
    }
}
