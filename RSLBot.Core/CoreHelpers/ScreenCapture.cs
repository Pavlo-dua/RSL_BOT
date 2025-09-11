using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;

using System.Diagnostics;
using System.Runtime.CompilerServices;
using RSLBot.Core.Services;
using Telegram.Bot.Requests.Abstractions;


namespace RSLBot.Core.CoreHelpers
{

    /// <summary>
    /// Provides functions to capture the entire screen, or a particular window, and save it to a file.
    /// </summary>
    public class ScreenCapture
    {
        // private static ScreenCapture instance = ;

        private ScreenCapture()
        { }

        public static ScreenCapture Instance
        {
            get;
        } = new();

        public static void Release()
        {
            
        }

        public bool IsInjection { get; set; } = true;

        [DllImport("user32.dll")]
        static extern bool PrintWindow(
            nint hwnd,
            nint hdcBlt,
            int nFlags
        );

        public static class ScreenExtensions
        {
            public static void GetDpi(DpiType dpiType, out uint dpiX, out uint dpiY)
            {
                var pnt = new Point(1, 1);
                var mon = MonitorFromPoint(pnt, 2 /*MONITOR_DEFAULTTONEAREST*/);
                GetDpiForMonitor(mon, dpiType, out dpiX, out dpiY);
            }

            //https://msdn.microsoft.com/en-us/library/windows/desktop/dd145062(v=vs.85).aspx
            [DllImport("User32.dll")]
            public static extern nint MonitorFromPoint([In] Point pt, [In] uint dwFlags);

            //https://msdn.microsoft.com/en-us/library/windows/desktop/dn280510(v=vs.85).aspx
            [DllImport("Shcore.dll")]
            public static extern nint GetDpiForMonitor([In] nint hmonitor, [In] DpiType dpiType,
                [Out] out uint dpiX, [Out] out uint dpiY);
        }

        //https://msdn.microsoft.com/en-us/library/windows/desktop/dn280511(v=vs.85).aspx
        public enum DpiType
        {
            Effective = 0,
            Angular = 1,
            Raw = 2,
        }

        [DllImport("user32.dll")]
        public static extern bool SetProcessDPIAware();

        [DllImport("user32.dll")]
        static extern bool ClientToScreen(nint hWnd, ref POINT lpPoint);

        [DllImport("user32.dll")]
        static extern bool ScreenToClient(nint hWnd, ref POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                X = x;
                Y = y;
            }

            public static implicit operator Point(POINT p)
            {
                return new Point(p.X, p.Y);
            }

            public static implicit operator POINT(Point p)
            {
                return new POINT(p.X, p.Y);
            }
        }

        [DllImport("user32.dll")]
        static extern nint MonitorFromWindow(nint hwnd, uint dwFlags);
        
        //// get te hDC of the target window
        //IntPtr hdcSrc = User32.GetDC(handle);
        //// get the size

        //int width = 0;
        //int height = 0;

        //User32.RECT windowRect = new User32.RECT();
        //User32.GetClientRect(handle, ref windowRect);
        //width = windowRect.right - windowRect.left;
        //height = windowRect.bottom - windowRect.top;

        //// create a device context we can copy to
        //IntPtr hdcDest = GDI32.CreateCompatibleDC(hdcSrc);
        //// create a bitmap we can copy it to,
        //// using GetDeviceCaps to get the width/height
        //IntPtr hBitmap = GDI32.CreateCompatibleBitmap(hdcSrc, width, height);
        //// select the bitmap object
        //IntPtr hOld = GDI32.SelectObject(hdcDest, hBitmap);
        //// bitblt over
        //GDI32.BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, GDI32.SRCCOPY);
        //// restore selection
        //GDI32.SelectObject(hdcDest, hOld);
        //// clean up
        //GDI32.DeleteDC(hdcDest);
        //User32.ReleaseDC(handle, hdcSrc);
        //// get a .NET image object for it
        //Bitmap img = Image.FromHbitmap(hBitmap);
        //// free up the Bitmap object
        //GDI32.DeleteObject(hBitmap);
        //GDI32.DeleteDC(hOld);
        //if (!(onlyRect.top == 0 && onlyRect.bottom == 0 && onlyRect.left == 0 && onlyRect.right == 0))
        //{
        //    img = img.Clone(new Rectangle(onlyRect.left, onlyRect.top, onlyRect.right - onlyRect.left, onlyRect.bottom - onlyRect.top), PixelFormat.Format32bppArgb);
        //}

        //return img;
    }
    //public Bitmap CaptureWindow(IntPtr handle, User32.RECT onlyRect = default)
    //{
    //    //Direct3D9TakeScreenshots(0, 1, handle);


    //    //var bitmap = new ComposedScreenshot(handle, ScreenshotMethod.DWM).ComposedScreenshotImage;

    //    //if (!(onlyRect.top == 0 && onlyRect.bottom == 0 && onlyRect.left == 0 && onlyRect.right == 0))
    //    //{
    //    //    return bitmap.Clone(new Rectangle(onlyRect.left, onlyRect.top, onlyRect.right - onlyRect.left, onlyRect.bottom - onlyRect.top), PixelFormat.Format32bppArgb);
    //    //}

    //    //return bitmap;

    //    //return GetScreenshot(handle, onlyRect);

    //    // get te hDC of the target window
    //    IntPtr hdcSrc = User32.GetDC(IntPtr.Zero);
    //    // get the size

    //    int width = 0;
    //    int height = 0;

    //    User32.RECT windowRect = new User32.RECT();
    //    User32.GetClientRect(handle, ref windowRect);
    //    width = windowRect.right - windowRect.left;
    //    height = windowRect.bottom - windowRect.top;

    //    Bitmap bmp = new Bitmap(width, height);
    //    Graphics g = Graphics.FromImage(bmp);

    //    g.CopyFromScreen(0,
    //                    0,
    //                    0,
    //                    0,
    //                    new Size(width, height),
    //                    CopyPixelOperation.SourceCopy);

    //    IntPtr dc = g.GetHdc();

    //    PrintWindow(handle, dc, 0);
    //    bmp.Save("test.bmp");





    //    // create a device context we can copy to
    //    IntPtr hdcDest = GDI32.CreateCompatibleDC(hdcSrc);
    //    // create a bitmap we can copy it to,
    //    // using GetDeviceCaps to get the width/height
    //    IntPtr hBitmap = GDI32.CreateCompatibleBitmap(hdcSrc, width, height);

    //    // select the bitmap object
    //    IntPtr hOld = GDI32.SelectObject(hdcDest, hBitmap);
    //    // bitblt over
    //    GDI32.BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, GDI32.SRCCOPY);
    //    // restore selection
    //    GDI32.SelectObject(hdcDest, hOld);
    //    // clean up
    //    GDI32.DeleteDC(hdcDest);
    //    User32.ReleaseDC(handle, hdcSrc);
    //    // get a .NET image object for it
    //    Bitmap img = Image.FromHbitmap(hBitmap);
    //    // free up the Bitmap object
    //    GDI32.DeleteObject(hBitmap);
    //    GDI32.DeleteDC(hOld);

    //    if (!(onlyRect.top == 0 && onlyRect.bottom == 0 && onlyRect.left == 0 && onlyRect.right == 0))
    //    {
    //        img = img.Clone(new Rectangle (onlyRect.left, onlyRect.top, onlyRect.right - onlyRect.left, onlyRect.bottom - onlyRect.top), PixelFormat.Format32bppArgb);
    //    }






    //    return img;
    //}



    /// <summary>
    /// Captures a screen shot of a specific window, and saves it to a file
    /// </summary>
    /// <param name="handle"></param>
    /// <param name="filename"></param>
    /// <param name="format"></param>
    //public void CaptureWindowToFile(IntPtr handle, string filename, ImageFormat format)
    //{
    //    Image img = CaptureWindow(handle);
    //    img.Save(filename, format);
    //}

    /// <summary>
    /// Captures a screen shot of the entire desktop, and saves it to a file
    /// </summary>
    /// <param name="filename"></param>
    /// <param name="format"></param>
    //public void CaptureScreenToFile(string filename, ImageFormat format)
    //{
    //    Image img = CaptureScreen();
    //    img.Save(filename, format);
    //}

    /// <summary>
    /// Helper class containing Gdi32 API functions
    ///// </summary>
    //private class GDI32
    //{

    //    public const int SRCCOPY = 0x00CC0020; // BitBlt dwRop parameter
    //    [DllImport("gdi32.dll")]
    //    public static extern bool BitBlt(IntPtr hObject, int nXDest, int nYDest,
    //        int nWidth, int nHeight, IntPtr hObjectSource,
    //        int nXSrc, int nYSrc, int dwRop);
    //    [DllImport("gdi32.dll")]
    //    public static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth,
    //        int nHeight);
    //    [DllImport("gdi32.dll")]
    //    public static extern IntPtr CreateCompatibleDC(IntPtr hDC);
    //    [DllImport("gdi32.dll")]
    //    public static extern bool DeleteDC(IntPtr hDC);
    //    [DllImport("gdi32.dll")]
    //    public static extern bool DeleteObject(IntPtr hObject);
    //    [DllImport("gdi32.dll")]
    //    public static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);
    //}

    /// <summary>
    /// Helper class containing User32 API functions
    /// </summary>
    public class User32
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [DllImport("user32.dll")]
        public static extern nint GetDesktopWindow();

        [DllImport("user32.dll")]
        public static extern nint GetDC(nint hWnd);

        [DllImport("user32.dll")]
        public static extern nint ReleaseDC(nint hWnd, nint hDC);

        [DllImport("user32.dll")]
        public static extern nint GetWindowRect(nint hWnd, ref RECT rect);

        [DllImport("user32.dll")]
        public static extern nint GetClientRect(nint hWnd, ref RECT rect);

        [DllImport("gdi32.dll")]
        public static extern nint CreateRectRgn(
            int x1,
            int y1,
            int x2,
            int y2
        );

        [DllImport("user32.dll")]
        public static extern int GetWindowRgn(
            nint hWnd,
            nint hRgn);
    }
}


