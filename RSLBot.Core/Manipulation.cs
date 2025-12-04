using RSLBot.Core.Services;
using RSLBot.Shared.Models;

namespace RSLBot.Core
{
    using System;
    using System.Drawing.Imaging;
    using System.Drawing;
    using RSLBot.Core.CoreHelpers;
    using RSLBot.Core.Extensions;
    using RSLBot.Shared.Settings;
    using RSLBot.Core.Interfaces;
    using System.Collections.Generic;

    public abstract class Manipulation(Tools tool, SharedSettings sharedSettings, ImageAnalyzer imageAnalyzer, ImageResourceManager imageResourceManager, ILoggingService logger)
    {
        public ImageAnalyzer ImageAnalyzer { get; } = imageAnalyzer;
        public ImageResourceManager ImageResourceManager { get; } = imageResourceManager;

        protected void Click(Point point, int msDelay = 300)
        {
            tool.SetForegroundRaid();
            Thread.Sleep(100);
            tool.ClickLeft(point.X, point.Y);
            Thread.Sleep(msDelay);
        }

        protected void MouseDrag(Point point, Point point2, int msDelay = 200, int msBeforeUp = 0)
        {
            tool.SetForegroundRaid();
            tool.MouseDragMove(point.X, point.Y, point2.X, point2.Y, msBeforeUp);
            Thread.Sleep(msDelay);
        }

        protected void MouseWheel(Point point, int wheelStep)
        {
            tool.SetForegroundRaid();
            tool.MouseWheel(point.X, point.Y, wheelStep);
        }

        protected void KeyDown(Messaging.VKeys key)
        {
            tool.VkDownKey(key);
        }

        private Messaging.VKeys? ParseKey(string? keyString)
        {
            if (string.IsNullOrEmpty(keyString))
                return null;

            // Convert single character to VKeys enum
            // For example: "R" -> VKeys.KEY_R
            if (keyString.Length == 1)
            {
                var keyName = $"KEY_{keyString.ToUpper()}";
                if (Enum.TryParse<Messaging.VKeys>(keyName, out var vKey))
                    return vKey;
            }

            return null;
        }

        /// <summary>
        /// Universal method that performs click or keyboard action and optionally waits for result.
        /// Handles both keyboard shortcuts (if element.Key is defined) and mouse clicks.
        /// </summary>
        private async Task<Rectangle> PerformActionAndWait(
            UIElement element,
            Point clickPoint,
            List<UIElement>? waitingElements = null,
            Func<Task>? misc = null)
        {
            var vKey = ParseKey(element.Key);

            if (vKey.HasValue)
            {
                // Use keyboard shortcut
                KeyDown(vKey.Value);
                Thread.Sleep(300);

                if (misc != null)
                    await misc.Invoke();

                if (waitingElements != null && waitingElements.Count > 0)
                    return await WaitAllImages(waitingElements);

                return new Rectangle(clickPoint, new Size(0, 0));
            }
            else
            {
                // Use mouse click
                if (waitingElements != null && waitingElements.Count > 0)
                {
                    return await ClickWithWait(clickPoint, async () =>
                    {
                        if (misc != null)
                            await misc.Invoke();

                        return await WaitAllImages(waitingElements);
                    });
                }
                else
                {
                    Click(clickPoint);
                    return new Rectangle(clickPoint, new Size(0, 0));
                }
            }
        }

        private Bitmap? window;

        protected Bitmap? Window
        {
            get => window;
            private set
            {
                window?.Dispose();
                window = value;
            }
        }

        public async Task<Bitmap?> SyncWindow()
        {
            Window = await tool.CaptureScreenShot();
            return Window;
        }

        protected Task<Bitmap?> CaptureScreenShot()
        {
            return tool.CaptureScreenShot();
        }

        protected Task<Bitmap> CaptureScreenShot(Rectangle rectangle)
        {
            return tool.TakeScreenRegion(rectangle);
        }

        protected async Task Wait(Func<bool, Task<bool>> funcCond, int maxIt = 10, int milliseconsUpdating = 1000)
        {
            while (true)
            {
                if (IsCancellationRequested())
                    return;

                if (maxIt == 0)
                {
                    logger.Error("Waiting object: Time Out");

                    await funcCond(true);

                    throw new TimeoutException("Waiting: Time Out");
                }

                await Task.Delay(milliseconsUpdating, sharedSettings.CancellationTokenSource.Token);

                if (await funcCond(false)) break;

                maxIt--;
            }
        }

        protected bool IsCancellationRequested()
        {
            return sharedSettings.CancellationTokenSource.Token.IsCancellationRequested;
        }

        public string GetWindowsCaption()
        {
            //var cs = CaptureScreenShot();

            //using var workingRect = cs?.Clone(new Rectangle(20, 8, 308, 38), PixelFormat.DontCare);

            return string.Empty; //ImageAnalyzer.FindText(workingRect).Trim();
        }

        protected void WaitText(Rectangle rec, string text, int maxIt = 10, int milliseconsUpdating = 300)
        {
            /*
            Wait((isTimeout) =>
            {
                if (!isTimeout)
                {
                    return string.Equals(ImageAnalyzer.FindText(CaptureScreenShot(), false, rec).ToLower(), text.ToLower(), StringComparison.CurrentCultureIgnoreCase);
                }
                else
                {
                    var problemString = $"Rect: rec.X {rec.X}, rec.Y {rec.Y}, rec.Width {rec.Width}, rec.Height {rec.Height}; \n Problem text: {text}";
                    logger.Warning(problemString);

                    return false;
                }

            }, maxIt, milliseconsUpdating);
            */
        }

        protected async Task<Rectangle> WaitImage(Bitmap img, Rectangle region = default, int maxIt = 10, int milliseconsUpdating = 1000)
        {
            Rectangle rec = default;

            await Wait(async (isTimeout) =>
            {
                if (IsCancellationRequested())
                    return true; // Exit early if cancellation is requested

                if (isTimeout)
                {
                    // logger.Warning(new[] { img }.Append(CaptureScreenShot()).ToArray(), $"reg: {rec.X}, {rec.Y}, {rec.Height}, {rec.Width}");
                    logger.Error("Waiting object: Time Out");
                    return false;
                }

                var currentStatusImg = await CaptureScreenShot();
                currentStatusImg?.Save("test.png");
                rec = ImageAnalyzer.FindImage(currentStatusImg, img, region, 0.95);
                return rec != default;
            }, maxIt, milliseconsUpdating);

            return rec;
        }

        protected async Task<(Rectangle, Bitmap? foundImage)> WaitImage(Bitmap[] imgs, Rectangle region = default, int maxIt = 10, int milliseconsUpdating = 1000, double accuracy = 0.988)
        {
            Rectangle rec = default;

            Bitmap? fImage = null;

            await Wait(async (isTimeout) =>
            {
                if (IsCancellationRequested())
                    return true; // Exit early if cancellation is requested

                if (isTimeout)
                {
                    //CurrentStatus.SaveDubug(imgs.Append(CaptureScreenShot()).ToArray(), $"reg: {rec.X}, {rec.Y}, {rec.Height}, {rec.Width}");
                    return false;
                }

                foreach (var img in imgs)
                {
                    rec = ImageAnalyzer.FindImage(await CaptureScreenShot(), img, region, accuracy);

                    if (rec != default)
                    {
                        fImage = img;
                        break;
                    }
                }

                return rec != default;
            }, maxIt, milliseconsUpdating);

            return (rec, fImage);
        }

        protected async Task<Rectangle> WaitImage(UIElement element, int maxIt = 10, int milliseconsUpdating = 1000)
        {
            return await WaitAllImages([element], maxIt, milliseconsUpdating);
        }

        protected async Task<Rectangle> WaitAllImages(List<UIElement> elements, int maxIt = 10, int milliseconsUpdating = 1000)
        {
            Rectangle firstRec = default;
            await Wait(async (isTimeout) =>
            {
                if (IsCancellationRequested())
                    return true; // Exit early if cancellation is requested

                if (isTimeout)
                {
                    logger.Error("Waiting for all images: Time Out");
                    return false; // timeout
                }

                //using var screenshot = await CaptureScreenShot();

                await SyncWindow();

                var allFound = true;
                var currentFirstRec = default(Rectangle);

                foreach (var element in elements)
                {
                    if (string.IsNullOrEmpty(element.ImageTemplatePath))
                    {
                        logger.Warning($"Verification image has an empty ImageTemplatePath.");
                        allFound = false;
                        break;
                    }
                    var template = ImageResourceManager[element.ImageTemplatePath];
                    var rec = ImageAnalyzer.FindImage(Window, template, element.Area, 0.98);
                    if (rec == default)
                    {
                        allFound = false;
                        break;
                    }
                    if (currentFirstRec == default)
                    {
                        currentFirstRec = rec;
                    }
                }

                if (allFound)
                {
                    firstRec = currentFirstRec;
                }

                return allFound;
            }, maxIt, milliseconsUpdating);

            return firstRec;
        }

        public async Task<Rectangle> ClickWithWait(Point clickPoint, Func<Task<Rectangle>> funcWaiter, int msDelay = 300)
        {
            if (IsCancellationRequested())
                return default;

            Click(clickPoint, msDelay);

            var itr = 0;

            while (true)
            {
                if (IsCancellationRequested())
                    return default;

                try
                {
                    return await funcWaiter();
                }
                catch (Exception)
                {
                    if (itr >= 3)
                    {
                        throw;
                    }

                    itr++;
                }
            }
        }

        public async Task<Rectangle> ClickWithWait(UIElement element, Func<Task<Rectangle>> funcWaiter, int msDelay = 300)
        {
            if (IsCancellationRequested())
                return default;

            await SyncWindow();

            var clickElement = ImageAnalyzer.FindImage(Window, ImageResourceManager[element.ImageTemplatePath], element.Area);

            return await ClickWithWait(clickElement.ToPoint(), funcWaiter, msDelay);
        }

        public async Task<Rectangle> Click(Bitmap part, Bitmap waitingBitmap, Rectangle imageRectangle = default)
        {
            if (IsCancellationRequested())
                return default;

            await SyncWindow();

            var btnRec = ImageAnalyzer.FindImage(Window, part);

            return await ClickWithWait(btnRec.ToPoint(), async () => await WaitImage(waitingBitmap));
        }

        public async Task<Rectangle> Click(UIElement element, UIElement waitingBitmap)
        {
            if (IsCancellationRequested())
                return default;

            await SyncWindow();

            var clickElement = ImageAnalyzer.FindImage(Window, ImageResourceManager[element.ImageTemplatePath], element.Area);

            if (clickElement == default)
            {
                logger.Error($"Could not find element to click: {element.Name}");
                return default;
            }

            // Wait for single element - convert to list
            var waitingList = new List<UIElement> { waitingBitmap };
            return await PerformActionAndWait(element, clickElement.ToPoint(), waitingList);
        }

        public async Task<Rectangle> Click(UIElement element, List<UIElement>? waitingBitmaps = null, Func<Task>? misc = null)
        {
            if (IsCancellationRequested())
                return default;

            await SyncWindow();

            var clickElementRect = ImageAnalyzer.FindImage(Window, ImageResourceManager[element.ImageTemplatePath], element.Area);

            if (clickElementRect == default)
            {
                logger.Error($"Could not find element to click: {element.Name}");
                return default;
            }

            return await PerformActionAndWait(element, clickElementRect.ToPoint(), waitingBitmaps, misc);
        }

        protected async Task<Rectangle> Click(Point point, List<UIElement> waitingBitmaps)
        {
            if (IsCancellationRequested())
                return default;

            return await ClickWithWait(point, async () => await WaitAllImages(waitingBitmaps));
        }

        public async Task<Rectangle> Click(UIElement element, ScreenDefinition waitingScreen, Func<Task>? misc = null)
        {
            return await Click(element, waitingScreen.VerificationImages, misc);
        }

        protected async Task<Rectangle> Click(UIElement element)
        {
            if (IsCancellationRequested())
                return default;

            await SyncWindow();

            var clickElementRect = ImageAnalyzer.FindImage(Window, ImageResourceManager[element.ImageTemplatePath], element.Area);

            if (clickElementRect == default)
            {
                logger.Error($"Could not find element to click: {element.Name}");
                return default;
            }

            return await PerformActionAndWait(element, clickElementRect.ToPoint());
        }

        public async Task<Rectangle> Click(Point element, ScreenDefinition waitingScreen, Func<Task>? misc = null)
        {
            if (IsCancellationRequested())
                return default;

            return await ClickWithWait(element, async () =>
            {
                if (misc != null)
                {
                    await misc.Invoke();
                }

                return await WaitAllImages(waitingScreen.VerificationImages);
            });
        }

        public async Task<List<Rectangle>> FindAllImages(Bitmap img, Rectangle region = default, double accuracy = 0.98)
        {
            if (IsCancellationRequested())
                return new List<Rectangle>();

            return ImageAnalyzer.FindAllImages(await CaptureScreenShot(), img, region, accuracy);
        }
    }
}
