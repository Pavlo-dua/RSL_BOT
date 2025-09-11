using System.Runtime.InteropServices;
using System.Text;

namespace RSLBot.Core.Services.ScreenCapture.Interop
{
    internal static class NativeMethods
    {
        [DllImport("d3d11.dll", EntryPoint = "CreateDirect3D11DeviceFromDXGIDevice", SetLastError = true)]
        public static extern uint CreateDirect3D11DeviceFromDXGIDevice(IntPtr dxgiDevice, out IntPtr graphicsDevice);
        
        [DllImport("combase.dll", PreserveSig = true)]
        internal static extern int RoGetActivationFactory(
            [MarshalAs(UnmanagedType.HString)] string activatableClassId,
            ref Guid iid,
            out IntPtr factory);
    }

    internal static class User32
    {
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder strText, int maxCount);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    }
}

internal static class Dwmapi
{
    /// <summary>
    /// Retrieves the value of a specified Desktop Window Manager (DWM) attribute for a window.
    /// </summary>
    [DllImport("dwmapi.dll")]
    public static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out bool pvAttribute, int cbAttribute);
        
    /// <summary>
    /// DWM Window Attribute: Is Cloaked.
    /// If this returns true, the window is not actually visible to the user.
    /// </summary>
    public const int DWMWA_CLOAKED = 14;
}