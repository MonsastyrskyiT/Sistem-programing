using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace HuksHomework;

internal sealed class KeyboardLogger : IDisposable
{
    private const int WhKeyboardLl = 13;
    private const int WmKeyDown = 0x0100;
    private const int WmKeyUp = 0x0101;
    private const int WmSysKeyDown = 0x0104;
    private const int WmSysKeyUp = 0x0105;

    private readonly HookProcedure _hookProcedure;
    private readonly HashSet<uint> _pressedKeys = new();
    private nint _hook;
    private StreamWriter? _writer;

    public KeyboardLogger()
    {
        _hookProcedure = HookCallback;
    }

    public bool IsRunning => _hook != nint.Zero;
    public string? LogPath { get; private set; }

    public event EventHandler<string>? KeyLogged;

    public void Start(string logPath)
    {
        if (IsRunning) return;

        _writer = new StreamWriter(logPath, append: true, new UTF8Encoding(false))
        {
            AutoFlush = true
        };
        LogPath = logPath;
        _pressedKeys.Clear();

        nint moduleHandle = GetModuleHandle(null);
        _hook = SetWindowsHookEx(WhKeyboardLl, _hookProcedure, moduleHandle, 0);
        if (_hook == nint.Zero)
        {
            int error = Marshal.GetLastWin32Error();
            _writer.Dispose();
            _writer = null;
            LogPath = null;
            throw new Win32Exception(error, "Не вдалося встановити глобальний хук клавіатури.");
        }

        _writer.WriteLine($"--- Логування розпочато {DateTime.Now:yyyy-MM-dd HH:mm:ss} ---");
    }

    public void Stop()
    {
        if (_hook != nint.Zero)
        {
            UnhookWindowsHookEx(_hook);
            _hook = nint.Zero;
        }

        if (_writer is not null)
        {
            _writer.WriteLine($"--- Логування зупинено {DateTime.Now:yyyy-MM-dd HH:mm:ss} ---");
            _writer.Dispose();
            _writer = null;
        }

        _pressedKeys.Clear();
    }

    private nint HookCallback(int code, nint message, nint data)
    {
        try
        {
            if (code >= 0 && _writer is not null)
            {
                uint virtualKey = (uint)Marshal.ReadInt32(data);
                bool keyDown = message == WmKeyDown || message == WmSysKeyDown;
                bool keyUp = message == WmKeyUp || message == WmSysKeyUp;

                if (keyDown && _pressedKeys.Add(virtualKey))
                {
                    string keyName = ((Keys)virtualKey).ToString();
                    string line = $"{DateTime.Now:HH:mm:ss.fff}  {keyName}";
                    _writer.WriteLine(line);
                    KeyLogged?.Invoke(this, line);
                }
                else if (keyUp)
                {
                    _pressedKeys.Remove(virtualKey);
                }
            }
        }
        catch
        {
        }

        return CallNextHookEx(_hook, code, message, data);
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }

    private delegate nint HookProcedure(int code, nint message, nint data);

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
