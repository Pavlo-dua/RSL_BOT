using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using RSLBot.Core.CoreHelpers;

namespace RSLBot.Core.Services
{
    /// <summary>
    /// A singleton manager for the screen capture service.
    /// This ensures that the capture is initialized only once and can be shared
    /// across different parts of the application (scenarios).
    /// It now handles capture session termination automatically.
    /// </summary>
    public sealed class ScreenCaptureManager : IDisposable
    {
        private ScreenCaptureService? _captureService;
        
        public IntPtr CapturedWindowHandle => _captureService?.CapturedWindowHandle ?? IntPtr.Zero;

        private int _cropPixelsTop = 0;
        // Using a simple lock which is lighter than a semaphore but still ensures thread-safety during initialization.
        private readonly object _lock = new ();
        
        /// <summary>
        /// Checks if the screen capture session is currently active.
        /// </summary>
        public bool IsCaptureActive => _captureService != null;

        /// <summary>
        /// Ensures that the screen capture is active. If not, it prompts the user to select a window.
        /// This method is thread-safe and handles re-initialization if the previous session was closed.
        /// </summary>
        /// <param name="ownerHwnd">The handle of the owner window for the picker dialog.</param>
        /// <returns>True if capture is active or was successfully started; false if the user cancelled.</returns>
        public async Task<bool> EnsureCaptureIsActiveAsync(IntPtr ownerHwnd)
        {
            if (IsCaptureActive)
            {
                return true;
            }

            // Lock to prevent multiple initializations at the same time.
            lock (_lock)
            {
                // Double-check inside the lock to handle race conditions.
                if (IsCaptureActive)
                {
                    return true;
                }
            }

            // Awaiting outside the lock to avoid holding it during an async operation.
            var newService = new ScreenCaptureService();
            
            // Subscribe to the event that signals the capture has stopped (e.g., window closed).
            newService.CaptureSessionEnded += OnCaptureSessionEnded;
            
            bool success = await newService.SelectWindowToCaptureAsync(ownerHwnd);

            lock (_lock)
            {
                if (success)
                {
                    _captureService = newService;
                    _cropPixelsTop = CalculateTopCropPixels(_captureService.CapturedWindowHandle);
                    return true;
                }
                else
                {
                    // If the user cancelled, clean up immediately.
                    newService.CaptureSessionEnded -= OnCaptureSessionEnded;
                    newService.Dispose();
                    return false;
                }
            }
        }

        /// <summary>
        /// A helper method to calculate the non-client area (borders, title bar).
        /// This logic is adapted from your Tools.cs file.
        /// </summary>
        private static int CalculateTopCropPixels(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return 0;

            var windowRect = new User32.RECT();
            var clientRect = new User32.RECT();
            User32.GetWindowRect(hWnd, ref windowRect);
            User32.GetClientRect(hWnd, ref clientRect);

            var clientTopLeft = new Tools.POINT(clientRect.left, clientRect.top);
            Tools.ClientToScreen(hWnd, ref clientTopLeft);

            // The height of the title bar is the difference between the top of the whole window
            // and the top of the client area, in screen coordinates.
            int topBorderHeight = clientTopLeft.Y - windowRect.top;

            return topBorderHeight > 0 ? topBorderHeight : 0;
        }
        
        /// <summary>
        /// Handles the event fired when the capture session ends (e.g., the captured window is closed).
        /// </summary>
        private void OnCaptureSessionEnded()
        {
            lock (_lock)
            {
                if (_captureService != null)
                {
                    _captureService.CaptureSessionEnded -= OnCaptureSessionEnded;
                    _captureService = null; // Set to null to allow re-initialization on next request.
                }
            }
        }

        /// <summary>
        /// Captures a single frame. Throws an exception if the capture is not active.
        /// </summary>
        /// <returns>A Bitmap of the captured frame.</returns>
        /// <exception cref="InvalidOperationException">Thrown if capture has not been initialized by calling EnsureCaptureIsActiveAsync first.</exception>
        public async Task<Bitmap?> CaptureFrameAsync()
        {
            ScreenCaptureService? service;
            lock (_lock)
            {
                service = _captureService;
            }

            if (service is null)
            {
                throw new InvalidOperationException("Screen capture is not active. Call EnsureCaptureIsActiveAsync before capturing a frame.");
            }

            var capturedBitmap = await GetCaptureFrameAsync();

            if (capturedBitmap == null)
            {
                return null;
            }
            
            if (_cropPixelsTop > 0 && capturedBitmap.Height > _cropPixelsTop)
            {
                var cropRect = new Rectangle(0, _cropPixelsTop, capturedBitmap.Width, capturedBitmap.Height - _cropPixelsTop);
                // The Clone method is very fast for this operation.
                return capturedBitmap.Clone(cropRect, capturedBitmap.PixelFormat);
            }

            return (Bitmap)capturedBitmap.Clone();

            async Task<Bitmap?> GetCaptureFrameAsync()
            {
                for (var i = 0; i < 60; i++)
                {
                    var capturedBmp = await service.CaptureFrameAsync();

                    if (capturedBmp != null)
                        return capturedBmp;
                        
                    await Task.Delay(30);
                }
                    
                return null;
            }
        }

        /// <summary>
        /// Disposes the underlying screen capture service.
        /// Should be called on application exit.
        /// </summary>
        public void Dispose()
        {
            lock (_lock)
            {
                if (_captureService != null)
                {
                    _captureService.CaptureSessionEnded -= OnCaptureSessionEnded;
                    _captureService.Dispose();
                    _captureService = null;
                }
            }
        }
    }
}