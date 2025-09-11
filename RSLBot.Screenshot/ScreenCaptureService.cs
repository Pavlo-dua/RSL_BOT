namespace RSLBot.Screenshot;

using System.Drawing;
using Capture;
using Capture.Hook;
using Capture.Interface;
using RSLBot.Screenshot.Interfaces;
using RSLBot.Shared.Settings;

public class ScreenCaptureService : IScreenCaptureService
{
    private readonly SharedSettings sharedSettings;
    private CaptureProcess captureProcess;

    public ScreenCaptureService(SharedSettings sharedSettings)
    {
        this.sharedSettings = sharedSettings;
    }

    public Bitmap CaptureGameWindow(Rectangle rectangle = default, int iter = 1)
    {
            if (!HookManager.IsHooked(sharedSettings.RaidProcess.Id))
            {
                var direct3DVersion = Direct3DVersion.AutoDetect;

                var cc = new CaptureConfig
                {
                    Direct3DVersion = direct3DVersion,
                    ShowOverlay = true
                };

                var captureInterface = new CaptureInterface();
                captureProcess = new CaptureProcess(sharedSettings.RaidProcess, cc, captureInterface);

                Thread.Sleep(10);
            }

            Bitmap screenshotBmp = default;
            var isDone = false;

            captureProcess.CaptureInterface.BeginGetScreenshot(rectangle, TimeSpan.FromSeconds(2), result =>
            {
                using Screenshot screenshot = captureProcess.CaptureInterface.EndGetScreenshot(result);
                try
                {
                    captureProcess.CaptureInterface.DisplayInGameText("Screenshot captured...");
                    if (screenshot is { Data: { } })
                    {
                        screenshotBmp = screenshot.ToBitmap();
                    }
                }
                catch
                {
                }
                finally
                {
                    isDone = true;
                }
            });

            var task = Task.Run(() =>
            {
                while (!isDone)
                {
                    Task.Delay(100);
                }
            });

            task.Wait();

            if (screenshotBmp == null && iter > 0)
            {
                screenshotBmp = this.CaptureGameWindow(rectangle, iter - 1);
            }

            return screenshotBmp;
    }
}