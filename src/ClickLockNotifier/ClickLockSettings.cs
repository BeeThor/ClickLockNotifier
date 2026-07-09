using System.ComponentModel;
using System.Runtime.InteropServices;

namespace ClickLockNotifier;

internal sealed record ClickLockState(bool IsEnabled, int LockTimeMilliseconds);

internal static class ClickLockSettings
{
    private const uint SPI_GETMOUSECLICKLOCK = 0x101E;
    private const uint SPI_SETMOUSECLICKLOCK = 0x101F;
    private const uint SPI_GETMOUSECLICKLOCKTIME = 0x2008;
    private const uint SPI_SETMOUSECLICKLOCKTIME = 0x2009;
    private const uint SPIF_UPDATEINIFILE = 0x01;
    private const uint SPIF_SENDCHANGE = 0x02;
    private const int DefaultClickLockTimeMilliseconds = 1200;

    public static ClickLockState Read()
    {
        var enabled = false;
        var lockTime = (uint)DefaultClickLockTimeMilliseconds;

        if (!SystemParametersInfoBool(SPI_GETMOUSECLICKLOCK, 0, ref enabled, 0))
        {
            enabled = false;
        }

        if (!SystemParametersInfoUInt(SPI_GETMOUSECLICKLOCKTIME, 0, ref lockTime, 0))
        {
            lockTime = (uint)DefaultClickLockTimeMilliseconds;
        }

        return new ClickLockState(enabled, ClampLockTime((int)lockTime));
    }

    public static void SetEnabled(bool isEnabled)
    {
        var value = isEnabled ? new IntPtr(1) : IntPtr.Zero;
        if (!SystemParametersInfoPointer(SPI_SETMOUSECLICKLOCK, 0, value, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to set Mouse ClickLock.");
        }

        var current = Read();
        if (current.IsEnabled != isEnabled)
        {
            throw new InvalidOperationException("Windows did not apply the Mouse ClickLock setting.");
        }
    }

    public static void SetLockTime(int milliseconds)
    {
        var lockTime = ClampLockTime(milliseconds);
        if (!SystemParametersInfoPointer(SPI_SETMOUSECLICKLOCKTIME, 0, new IntPtr(lockTime), SPIF_UPDATEINIFILE | SPIF_SENDCHANGE))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to set Mouse ClickLock time.");
        }

        var current = Read();
        if (current.LockTimeMilliseconds != lockTime)
        {
            throw new InvalidOperationException("Windows did not apply the Mouse ClickLock time setting.");
        }
    }

    public static int ClampLockTime(int milliseconds)
    {
        return Math.Clamp(milliseconds, 200, 5000);
    }

    [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SystemParametersInfoBool(
        uint uiAction,
        uint uiParam,
        [MarshalAs(UnmanagedType.Bool)] ref bool pvParam,
        uint fWinIni);

    [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SystemParametersInfoUInt(
        uint uiAction,
        uint uiParam,
        ref uint pvParam,
        uint fWinIni);

    [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SystemParametersInfoPointer(
        uint uiAction,
        uint uiParam,
        IntPtr pvParam,
        uint fWinIni);
}
