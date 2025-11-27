using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RSLBot.Core.Services;

namespace RSLBot.Core.CoreHelpers
{
    public class Tools
    {
        private readonly ScreenCaptureManager _screenCaptureManager;

        public bool IsInjectionMode
        {
            get => ScreenCapture.Instance.IsInjection;
            set => ScreenCapture.Instance.IsInjection = value;
        }

        private enum GetWindowType : uint
        {
            /// <summary>
            /// The retrieved handle identifies the window of the same type that is highest in the Z order.
            /// <para/>
            /// If the specified window is a topmost window, the handle identifies a topmost window.
            /// If the specified window is a top-level window, the handle identifies a top-level window.
            /// If the specified window is a child window, the handle identifies a sibling window.
            /// </summary>
            GW_HWNDFIRST = 0,
            /// <summary>
            /// The retrieved handle identifies the window of the same type that is lowest in the Z order.
            /// <para />
            /// If the specified window is a topmost window, the handle identifies a topmost window.
            /// If the specified window is a top-level window, the handle identifies a top-level window.
            /// If the specified window is a child window, the handle identifies a sibling window.
            /// </summary>
            GW_HWNDLAST = 1,
            /// <summary>
            /// The retrieved handle identifies the window below the specified window in the Z order.
            /// <para />
            /// If the specified window is a topmost window, the handle identifies a topmost window.
            /// If the specified window is a top-level window, the handle identifies a top-level window.
            /// If the specified window is a child window, the handle identifies a sibling window.
            /// </summary>
            GW_HWNDNEXT = 2,
            /// <summary>
            /// The retrieved handle identifies the window above the specified window in the Z order.
            /// <para />
            /// If the specified window is a topmost window, the handle identifies a topmost window.
            /// If the specified window is a top-level window, the handle identifies a top-level window.
            /// If the specified window is a child window, the handle identifies a sibling window.
            /// </summary>
            GW_HWNDPREV = 3,
            /// <summary>
            /// The retrieved handle identifies the specified window's owner window, if any.
            /// </summary>
            GW_OWNER = 4,
            /// <summary>
            /// The retrieved handle identifies the child window at the top of the Z order,
            /// if the specified window is a parent window; otherwise, the retrieved handle is NULL.
            /// The function examines only child windows of the specified window. It does not examine descendant windows.
            /// </summary>
            GW_CHILD = 5,
            /// <summary>
            /// The retrieved handle identifies the enabled popup window owned by the specified window (the
            /// search uses the first such window found using GW_HWNDNEXT); otherwise, if there are no enabled
            /// popup windows, the retrieved handle is that of the specified window.
            /// </summary>
            GW_ENABLEDPOPUP = 6
        }

        private const int MOUSEEVENTF_MOVE = 0x0001;
        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_WHEEL = 0x0800;

        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;


        private const int WM_ACTIVATE = 0x0006;
        private const int WA_ACTIVE = 1;

        const int WM_LBUTTONDOWN = 0x0201;
        const int WM_LBUTTONUP = 0x0202;
        const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_CHAR = 0x0102;

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        public int MakeLParam(int LoWord, int HiWord)
        {
            return HiWord << 16 | LoWord & 0xFFFF;
        }

        [DllImport("user32.dll", EntryPoint = "SetForegroundWindow")]
        private static extern int SetForegroundWindow(nint hwnd);

        [DllImport("user32.dll", EntryPoint = "SwitchToThisWindow")]
        private static extern void SwitchToThisWindow(nint hwnd, int fUnknown);

        [DllImport("user32.dll", EntryPoint = "BringWindowToTop")]
        private static extern nint BringWindowToTop(nint hwnd);

        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        public static extern nint SetWindowPos(nint hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);


        [DllImport("user32.dll")]
        public static extern nint PostMessage(nint hWnd, uint Msg, nint wParam, nint lParam);

        [DllImport("user32.dll")]
        public static extern bool SendMessage(nint hWnd, int wMsg, nint wParam, nint lParam);

        [DllImport("user32.dll", SetLastError = true)]
        static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

        public string initVector = "2pIjGh";

        [DllImport("user32.dll", SetLastError = true)]
        private static extern nint GetWindow(nint hWnd, GetWindowType uCmd);

        [DllImport("user32.dll")]
        public static extern nint FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        public static extern nint SetParent(nint hWndChild, nint hWndNewParent);

        [DllImport("user32.dll")]
        public static extern nint GetClientRect(nint hWnd, ref RECT rect);

        [DllImport("user32.dll")]
        public static extern nint GetWindowRect(nint hWnd, ref RECT rect);

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

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(long dwFlags, long dx, long dy, long cButtons, long dwExtraInfo);

        [DllImport("user32.dll")]
        public static extern bool ClientToScreen(nint hWnd, ref POINT lpPoint);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern int ShowWindow(
            nint hWnd,
            int nCmdShow
        );

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern bool UpdateWindow(nint hWnd);

        [DllImport("user32.dll")]
        public static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        static extern nint WindowFromPoint(POINT Point);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool MoveWindow(nint hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll")]
        static extern void keybd_event(byte bVk, byte bScan, uint dwFlags,
            nuint dwExtraInfo);

        public const uint KEYEVENTF_EXTENDEDKEY = 1;
        public const uint KEYEVENTF_KEYUP = 2;

        private readonly string RaidNameWindow = "Raid: Shadow Legends";
        public nint hWnd;
        public Process raidProcess;

        public Tools(ScreenCaptureManager screenCaptureManager)
        {
            _screenCaptureManager = screenCaptureManager;
            Init();
        }

        RECT windowRect = new RECT();
        RECT windowPos = new RECT();

        public void Init(int nWidth = 1173, int nHeight = 652)
        {
            hWnd = nint.Zero;

            foreach (var pList in Process.GetProcesses())
            {
                if (pList.MainWindowTitle.Contains(RaidNameWindow))
                {
                    hWnd = pList.MainWindowHandle;
                    raidProcess = pList;
                    break;
                }
            }

            SetRaidDefaultSize(nWidth, nHeight);
        }

        public void SetRaidDefaultSize(int nWidth, int nHeight)
        {
            ClientResize(hWnd, nWidth, nHeight);

            //MoveWindow(hWnd, 0, 0, 1189, 691, true);

            GetClientRect(hWnd, ref windowRect);
            GetWindowRect(hWnd, ref windowPos);
        }

        private int startCLientX = 0;
        private int startCLientY = 0;

        void ClientResize(nint hWnd, int nWidth, int nHeight)
        {
            RECT rcClient = new RECT(), rcWind = new RECT();

            GetClientRect(hWnd, ref rcClient);
            GetWindowRect(hWnd, ref rcWind);

            POINT ptDiff = new POINT(rcWind.right - rcWind.left - rcClient.right, rcWind.bottom - rcWind.top - rcClient.bottom);

            MoveWindow(hWnd, 0, 0, nWidth + ptDiff.X, nHeight + ptDiff.Y, true);
            Task.Delay(1000).Wait();
            // var capCoord = ClientToScreen(hWnd, ref lefttop); //GetCaptionCoordinates();

            POINT lefttop = new(rcClient.left, rcClient.top); // Practicaly both are 0
            ClientToScreen(hWnd, ref lefttop);

            startCLientX = lefttop.X;
            startCLientY = lefttop.Y;
        }

        private (int left, int rigth, int top, int bottom) GetCaptionCoordinates()
        {
            RECT rcClient = new RECT(), rcWind = new RECT();
            GetWindowRect(hWnd, ref rcWind);

            GetClientRect(hWnd, ref rcClient);

            POINT lefttop = new(rcClient.left, rcClient.top); // Practicaly both are 0
            ClientToScreen(hWnd, ref lefttop);
            POINT rightbottom = new(rcClient.right, rcClient.bottom);
            ClientToScreen(hWnd, ref rightbottom);

            int left_border = lefttop.X - rcWind.left; // Windows 10: includes transparent part
            int right_border = rcWind.right - rightbottom.X; // As above
            int bottom_border = rcWind.bottom - rightbottom.Y; // As above
            int top_border_with_title_bar = lefttop.Y - rcWind.top; // There is no transparent part

            return (left_border, right_border, top_border_with_title_bar, bottom_border);
        }

        private int NormalizeY(int y)
        {
            return startCLientY + y;
        }

        private int NormalizeX(int x)
        {
            return startCLientX + x;
        }

        public void ClickLeft(int x, int y, bool absolutlyCoord = false)
        {
            if (!absolutlyCoord)
            {
                x = NormalizeX(x);
                y = NormalizeY(y);
            }

            SetCursorPos(x, y);
            Thread.Sleep(500);
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
            Thread.Sleep(30);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        }

        public string key = "2pIj!6";


        [Flags]
        public enum EXECUTION_STATE : uint
        {
            ES_AWAYMODE_REQUIRED = 0x00000040,
            ES_CONTINUOUS = 0x80000000,
            ES_DISPLAY_REQUIRED = 0x00000002,
            ES_SYSTEM_REQUIRED = 0x00000001
            // Legacy flag, should not be used.
            // ES_USER_PRESENT = 0x00000004
        }


        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

        public static void ResetScreensaver()
        {
            SetThreadExecutionState(EXECUTION_STATE.ES_DISPLAY_REQUIRED);
        }

        public void MouseDragMove(int x, int y, int x2, int y2, int waitBeforeUp)
        {
            // 1. Гарантовано робимо вікно гри активним перед початком дій
            SetForegroundRaid();
            Thread.Sleep(100); // Даємо час системі на переключення вікна

            // 2. Нормалізуємо координати для роботи на всьому екрані
            x = NormalizeX(x);
            y = NormalizeY(y);
            x2 = NormalizeX(x2);
            y2 = NormalizeY(y2);

            // 3. Переміщуємо курсор в початкову точку і натискаємо
            SetCursorPos(x, y);
            Thread.Sleep(200);
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
            Thread.Sleep(30);

            // 4. Імітуємо плавний рух
            int steps = 80; // Можна погратися з цим значенням
            for (int i = 1; i <= steps; i++)
            {
                int currentX = x + (x2 - x) * i / steps;
                int currentY = y + (y2 - y) * i / steps;

                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                SetCursorPos(currentX, currentY);
                Thread.Sleep(5); // Пауза між кроками для плавності
            }

            // 5. КЛЮЧОВИЙ МОМЕНТ: Завершальний етап для боротьби з інерцією
            // Переконуємось, що курсор точно в кінцевій позиції
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
            SetCursorPos(x2, y2);
            // Робимо значну паузу, щоб гра 100% зрозуміла, що рух зупинився
            Thread.Sleep(waitBeforeUp);

            // 6. Тільки після повної зупинки відпускаємо кнопку миші
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        }

        public void MouseWheel(int x, int y, int wheel)
        {
            x = NormalizeX(x);
            y = NormalizeY(y);

            SetCursorPos(x, y);

            mouse_event(MOUSEEVENTF_WHEEL, x, y, wheel, 0);
        }

        public Task<Bitmap?> CaptureScreenShot()
        {
            return _screenCaptureManager.CaptureFrameAsync();
        }

        public void VkDownKey(Messaging.VKeys key)
        {
            SendMessage(hWnd, WM_ACTIVATE, WA_ACTIVE, 0);

            new Key(key).Press(hWnd, false);
        }

        public async Task<Bitmap> TakeScreenRegion(Rectangle region)
        {
            return CropImage(await _screenCaptureManager.CaptureFrameAsync(), region);
        }

        /// <summary>
        /// Crops a bitmap to a specified rectangular area.
        /// </summary>
        /// <param name="sourceBitmap">The source bitmap to crop from.</param>
        /// <param name="cropArea">The rectangle defining the area to be cropped.</param>
        /// <returns>A new Bitmap object containing the cropped image.</returns>
        public static Bitmap CropImage(Bitmap? sourceBitmap, Rectangle cropArea)
        {
            if (sourceBitmap == null)
            {
                throw new ArgumentNullException(nameof(sourceBitmap));
            }

            // The Clone method is the most efficient way to perform a crop operation.
            return sourceBitmap.Clone(cropArea, sourceBitmap.PixelFormat);
        }

        public void SetForegroundRaid()
        {
            // BringWindowToTop(this.hWnd);

            //SetWindowPos(this.hWnd, 0, )
            ShowWindow(hWnd, 5);
            SetForegroundWindow(hWnd);
        }

        public nint SetParent(nint hWndNewParent)
        {
            return SetParent(hWnd, hWndNewParent);
        }

        public static List<string> GetListOfScripts()
        {
            return Directory.GetFiles("Scenarios").ToList();
        }

        //private async void ProccesMessages(object sender, MessageEventArgs e)
        //{
        //    switch (e.Message.Text?.ToLower())
        //    {
        //        case "фото":
        //        case "photo":
        //            await SendReport("Current state", true);
        //            break;
        //        case "stop":
        //        case "стоп":
        //            RootInit.Instance.CancellationTokenSource.Cancel();
        //            break;
        //        default:
        //            await SendReport("Available commands: Photo, Stop", priorsendScreenShot: false);
        //            break;
        //    }
        //}

        public void Restore()
        {
            ShowWindow(hWnd, 9); // SW_RESTORE = 9
        }

        public void Free()
        {
            ScreenCapture.Release();
        }

        private CancellationTokenSource cancellationTokenSource;

        //public static void ShutDown()
        //{
        //    var bs = RootInit.ServiceProvider.GetService<BotSettings>();

        //    var tl = RootInit.ServiceProvider.GetService<Tools>();

        //    tl.SendReport("The computer will be turned off").Wait(3000);

        //    switch (bs.TypeOfPowerOff)
        //    {
        //        case BotSettings.TypePowerOff.Sleep:
        //            Process.Start("shutdown", "/h");
        //            break;
        //        //case BotSettings.TypePowerOff.Hibernate:
        //        //    Process.Start("shutdown", "/h");
        //        //    break;
        //        case BotSettings.TypePowerOff.ShutDown:
        //            Process.Start("shutdown", "/s");
        //            break;
        //    }            
        //}
    }
}
