using System.ComponentModel;
using System.Runtime.InteropServices;

namespace HuksHomework;

internal sealed class GlobalMouseHook : IDisposable
{
    private const int WhMouseLl = 14;
    private const int WmLButtonDown = 0x0201;
    private readonly HookProcedure _hookProcedure;
    private nint _hook;

    public GlobalMouseHook()
    {
        _hookProcedure = HookCallback;
        _hook = SetWindowsHookEx(WhMouseLl, _hookProcedure, GetModuleHandle(null), 0);
        if (_hook == nint.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error(),
                "Не вдалося встановити глобальний хук миші.");
    }


    public event Func<Point, bool>? LeftButtonDown;

    private nint HookCallback(int code, nint message, nint data)
    {
        try
        {
            if (code >= 0 && message == WmLButtonDown)
            {
                MouseLowLevelHookData mouseData = Marshal.PtrToStructure<MouseLowLevelHookData>(data);
                bool blockClick = LeftButtonDown?.Invoke(
                    new Point(mouseData.Point.X, mouseData.Point.Y)) ?? false;

                if (blockClick) return (nint)1;
            }
        }
        catch
        {
        }

        return CallNextHookEx(_hook, code, message, data);
    }

    public void Dispose()
    {
        if (_hook != nint.Zero)
        {
            UnhookWindowsHookEx(_hook);
            _hook = nint.Zero;
        }

        GC.SuppressFinalize(this);
    }

    private delegate nint HookProcedure(int code, nint message, nint data);

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct NativePoint
    {
        public readonly int X;
        public readonly int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct MouseLowLevelHookData
    {
        public readonly NativePoint Point;
        public readonly uint MouseData;
        public readonly uint Flags;
        public readonly uint Time;
        public readonly nuint ExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetWindowsHookEx(
        int hookId,
        HookProcedure callback,
        nint moduleHandle,
        uint threadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(nint hook);

    [DllImport("user32.dll")]
    private static extern nint CallNextHookEx(nint hook, int code, nint message, nint data);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint GetModuleHandle(string? moduleName);
}
