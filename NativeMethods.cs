using System.Runtime.InteropServices;

namespace GridOverlay;

/// <summary>
/// Win32 P/Invoke declarations and constants used to build the layered,
/// click-through, always-on-top overlay window.
/// </summary>
internal static class NativeMethods
{
    // Extended window style index for GetWindowLong / SetWindowLong.
    public const int GWL_EXSTYLE = -20;

    // Extended window styles.
    public const int WS_EX_TOPMOST    = 0x00000008;
    public const int WS_EX_TRANSPARENT = 0x00000020; // click-through (mouse passes through)
    public const int WS_EX_TOOLWINDOW = 0x00000080;  // keep out of the alt-tab list
    public const int WS_EX_LAYERED    = 0x00080000;  // required for transparency / per-pixel alpha
    public const int WS_EX_NOACTIVATE = 0x08000000;  // never steal focus from the game

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    // 64-bit safe variants. We marshal through these so the overlay behaves on
    // both x86 and x64 hosts.
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    public static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
        => IntPtr.Size == 8 ? GetWindowLongPtr64(hWnd, nIndex) : new IntPtr(GetWindowLong(hWnd, nIndex));

    public static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        => IntPtr.Size == 8 ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong) : new IntPtr(SetWindowLong(hWnd, nIndex, dwNewLong.ToInt32()));

    // ---- Global hotkeys (RegisterHotKey / WM_HOTKEY) ----

    public const int WM_HOTKEY = 0x0312;

    // Modifier flags for RegisterHotKey.
    public const uint MOD_ALT      = 0x0001;
    public const uint MOD_CONTROL  = 0x0002;
    public const uint MOD_SHIFT    = 0x0004;
    public const uint MOD_WIN      = 0x0008;
    public const uint MOD_NOREPEAT = 0x4000; // don't fire repeatedly while held

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
