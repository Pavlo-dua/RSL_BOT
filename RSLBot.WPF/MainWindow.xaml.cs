using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Interop;
using RSLBot.Core.Services;
using RSLBot.WPF.ViewModels;
using AppContext = RSLBot.Core.Services.AppContext;

namespace RSLBot.WPF
{
    /// <summary>
    /// Головне вікно додатку.
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            // Крок 1: Отримуємо HWND НАШОГО вікна, а не вікна гри.
            IntPtr ownerHwnd = new WindowInteropHelper(this).Handle;
            
            // Крок 2: (Про всяк випадок) Активуємо наше вікно перед показом діалогу.
            //SetForegroundWindow(ownerHwnd);

            using var captureService = new ScreenCaptureService();

            // Крок 3: Передаємо правильний HWND власника.
            // Ми більше не у фоновому потоці, тому Dispatcher не потрібен.
            bool selected = await captureService.SelectWindowToCaptureAsync(ownerHwnd);

            if (!selected)
            {
                // Це повідомлення ви і бачили. Тепер воно з'явиться, лише якщо
                // ви скасуєте вибір, або якщо системні налаштування забороняють захоплення.
                MessageBox.Show("Вибір вікна було скасовано.");
                return;
            }

            // Крок 4: Захоплення кадру.
            using Bitmap? screenshot = await captureService.CaptureFrameAsync();

            if (screenshot != null)
            {
                screenshot.Save("final_test.png", ImageFormat.Png);
                MessageBox.Show("УСПІХ! Скріншот збережено.");
            }
            else
            {
                MessageBox.Show("ПОМИЛКА: Не вдалося захопити кадр.");
            }
        }

        private void MainWindow_OnSourceInitialized(object? sender, EventArgs e)
        {
            UiDispatcher.Initialize(this.Dispatcher);
            AppContext.Initialize(this);
        }
    }
}