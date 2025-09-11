using System.Windows;
using System.Windows.Interop;

namespace RSLBot.Core.Services;

public class AppContext
{
    /// <summary>
    /// Дескриптор (HWND) головного вікна додатку.
    /// </summary>
    public static IntPtr MainWindowHandle { get; private set; } = IntPtr.Zero;

    /// <summary>
    /// Ініціалізує контекст, зберігаючи HWND головного вікна.
    /// </summary>
    public static void Initialize(Window mainWindow)
    {
        MainWindowHandle = new WindowInteropHelper(mainWindow).Handle;
    }
}