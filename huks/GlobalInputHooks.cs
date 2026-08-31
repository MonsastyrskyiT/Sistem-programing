using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Huks;

internal sealed class GlobalInputHooks : IDisposable
{
    private const int WhKeyboardLl = 13;
    private const int WhMouseLl = 14;
    private const int WmKeyDown = 0x0100;
    private const int WmKeyUp = 0x0101;
    private const int WmSysKeyDown = 0x0104;
    private const int WmSysKeyUp = 0x0105;
    private const int WmMouseMove = 0x0200;
    private const uint VkControl = 0x11;
    private const uint VkShift = 0x10;
    private const uint VkMenu = 0x12;
    private const uint VkLControl = 0xA2;
    private const uint VkRControl = 0xA3;
    private const uint VkLShift = 0xA0;
    private const uint VkRShift = 0xA1;
    private const uint VkLMenu = 0xA4;
    private const uint VkRMenu = 0xA5;
    private const uint VkQ = 0x51;

    private readonly HookProcedure _keyboardProcedure;
    private readonly HookProcedure _mouseProcedure;
    private readonly Rectangle _cursorBounds;
    private nint _keyboardHook;
    private nint _mouseHook;
    private bool _controlPressed;
    private bool _shiftPressed;
    private bool _altPressed;
    private bool _hotkeyPressed;
    private bool _disposed;

    public GlobalInputHooks(Rectangle cursorBounds)
    {
        _cursorBounds = cursorBounds;
        _keyboardProcedure = KeyboardHookCallback;
        _mouseProcedure = MouseHookCallback;

        nint moduleHandle = GetModuleHandle(null);
        _keyboardHook = SetWindowsHookEx(WhKeyboardLl, _keyboardProcedure, moduleHandle, 0);
        if (_keyboardHook == nint.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Не вдалося встановити хук клавіатури.");

        _mouseHook = SetWindowsHookEx(WhMouseLl, _mouseProcedure, moduleHandle, 0);
        if (_mouseHook == nint.Zero)
        {
            UnhookWindowsHookEx(_keyboardHook);
            _keyboardHook = nint.Zero;
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Не вдалося встановити хук миші.");
        }
    }

    public event EventHandler? VisibilityToggleRequested;

    public Rectangle CursorBounds => _cursorBounds;

    private nint KeyboardHookCallback(int code, nint message, nint data)
    {
        try
        {
            if (code >= 0)
            {
                uint virtualKey = (uint)Marshal.ReadInt32(data);
                bool isKeyDown = message == WmKeyDown || message == WmSysKeyDown;
                bool isKeyUp = message == WmKeyUp || message == WmSysKeyUp;

                if (isKeyDown || isKeyUp)
                    UpdateModifierState(virtualKey, isKeyDown);

                if (virtualKey == VkQ && isKeyDown && !_hotkeyPressed)
                {
                    _hotkeyPressed = true;
                    if (_controlPressed && _shiftPressed)
                        VisibilityToggleRequested?.Invoke(this, EventArgs.Empty);
                }
                else if (virtualKey == VkQ && isKeyUp)
                {
                    _hotkeyPressed = false;
                }
            }
        }
        catch
        {
            // Виняток не повинен виходити з unmanaged callback.
        }

        return CallNextHookEx(_keyboardHook, code, message, data);
    }

    private nint MouseHookCallback(int code, nint message, nint data)
    {
        try
        {
            if (code >= 0 && message == WmMouseMove && _altPressed)
            {
                MouseLowLevelHookData mouseData = Marshal.PtrToStructure<MouseLowLevelHookData>(data);
                int x = Math.Clamp(mouseData.Point.X, _cursorBounds.Left, _cursorBounds.Right - 1);
                int y = Math.Clamp(mouseData.Point.Y, _cursorBounds.Top, _cursorBounds.Bottom - 1);

                if (x != mouseData.Point.X || y != mouseData.Point.Y)
                {
                    SetCursorPos(x, y);
                    return (nint)1; 
                }
            }
        }
        catch
        {
        }

        return CallNextHookEx(_mouseHook, code, message, data);
    }

    private void UpdateModifierState(uint virtualKey, bool pressed)
    {
        switch (virtualKey)
        {
            case VkControl:
            case VkLControl:
            case VkRControl:
                _controlPressed = pressed;
                break;
            case VkShift:
            case VkLShift:
            case VkRShift:
                _shiftPressed = pressed;
                break;
            case VkMenu:
            case VkLMenu:
            case VkRMenu:
                _altPressed = pressed;
                break;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_mouseHook != nint.Zero)
        {
            UnhookWindowsHookEx(_mouseHook);
            _mouseHook = nint.Zero;
        }

        if (_keyboardHook != nint.Zero)
        {
            UnhookWindowsHookEx(_keyboardHook);
            _keyboardHook = nint.Zero;
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

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint GetModuleHandle(string? moduleName);
}
