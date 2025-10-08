using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using RSLBot.Core.Services.ScreenCapture.Interop;
using Vortice.Direct3D11;
using Vortice.DXGI;
using WinRT;
using Marshal = System.Runtime.InteropServices.Marshal;
using IDirect3DDxgiInterfaceAccess = RSLBot.Core.Services.ScreenCapture.Interop.IDirect3DDxgiInterfaceAccess;

namespace RSLBot.Core.Services
{
    public sealed class ScreenCaptureService : IDisposable
    {
        /// <summary>
        /// Gets the handle (HWND) of the currently captured window.
        /// Returns IntPtr.Zero if no capture is active.
        /// </summary>
        public IntPtr CapturedWindowHandle { get; private set; } = IntPtr.Zero;
        
        public event Action? CaptureSessionEnded;
        
        private GraphicsCaptureItem? _captureItem;
        private Direct3D11CaptureFramePool? _framePool;
        private GraphicsCaptureSession? _session;
        private ID3D11Device? _device;
        private ID3D11DeviceContext? _context;
        
        // Нові поля для постійного зчитування фреймів
        private CancellationTokenSource? _frameProcessingCts;
        private Task? _frameProcessingTask;
        private Direct3D11CaptureFrame? _latestFrame;
        private readonly object _frameLock = new object();
        private DateTime _lastFrameTime = DateTime.MinValue;
        private const int FrameTimeoutSeconds = 5; // Таймаут для визначення застарілих фреймів

        /// <summary>
        /// Prompts the user to select a window and starts the capture session.
        /// The method completes only when the capture session is fully initialized and ready to provide frames.
        /// </summary>
        /// <param name="ownerHwnd">The handle of the owner window for the picker dialog.</param>
        /// <returns>True if a window was selected and capture started successfully, otherwise false.</returns>
        public async Task<bool> SelectWindowToCaptureAsync(IntPtr ownerHwnd)
        {
            var item = await WindowCaptureHelper.PickWindowToCaptureAsync(ownerHwnd);
            if (item == null)
            {
                return false; // User cancelled the picker.
            }
            
            return await StartCaptureAsync(item);
        }
        
        /// <summary>
        /// Captures the current frame from the session.
        /// This now returns the most recent frame that was continuously captured in the background.
        /// </summary>
        /// <returns>A Bitmap of the captured frame, or null if a frame was not available.</returns>
        public unsafe Task<Bitmap?> CaptureFrameAsync()
        {
            if (_framePool == null || _device == null || _context == null)
            {
                return Task.FromResult<Bitmap?>(null);
            }

            Direct3D11CaptureFrame? frameToProcess = null;
            
            lock (_frameLock)
            {
                // Перевіряємо, чи фрейм не застарілий
                if (_latestFrame != null && 
                    (DateTime.Now - _lastFrameTime).TotalSeconds < FrameTimeoutSeconds)
                {
                    frameToProcess = _latestFrame;
                    _latestFrame = null; // Забираємо фрейм для обробки
                }
            }
            
            if (frameToProcess == null)
            {
                // Якщо немає свіжого фрейму, спробуємо взяти прямо зараз
                frameToProcess = _framePool.TryGetNextFrame();
                if (frameToProcess == null)
                {
                    return Task.FromResult<Bitmap?>(null);
                }
            }

            using (frameToProcess)
            {
                using var sourceTexture = GetTextureFromFrame(frameToProcess);
                if (sourceTexture == null)
                {
                    return Task.FromResult<Bitmap?>(null);
                }

                var description = sourceTexture.Description;
                description.Usage = Vortice.Direct3D11.ResourceUsage.Staging;
                description.BindFlags = Vortice.Direct3D11.BindFlags.None;
                description.CPUAccessFlags = Vortice.Direct3D11.CpuAccessFlags.Read;
                description.MiscFlags = Vortice.Direct3D11.ResourceOptionFlags.None;

                using var stagingTexture = _device.CreateTexture2D(description);
                _context.CopyResource(stagingTexture, sourceTexture);

                var mappedSubresource = _context.Map(stagingTexture, 0, MapMode.Read, Vortice.Direct3D11.MapFlags.None);
                try
                {
                    var bitmap = new Bitmap((int)description.Width, (int)description.Height, PixelFormat.Format32bppArgb);
                    var boundsRect = new Rectangle(0, 0, (int)description.Width, (int)description.Height);

                    var mapDest = bitmap.LockBits(boundsRect, ImageLockMode.WriteOnly, bitmap.PixelFormat);
                    var sourcePtr = mappedSubresource.DataPointer;
                    var destPtr = mapDest.Scan0;

                    for (int y = 0; y < description.Height; y++)
                    {
                        System.Runtime.CompilerServices.Unsafe.CopyBlockUnaligned(
                            ref ((byte*)destPtr)[0], 
                            ref ((byte*)sourcePtr)[0], 
                            (uint)(description.Width * 4));

                        sourcePtr = IntPtr.Add(sourcePtr, (int)mappedSubresource.RowPitch);
                        destPtr = IntPtr.Add(destPtr, mapDest.Stride);
                    }

                    bitmap.UnlockBits(mapDest);
                    return Task.FromResult<Bitmap?>(bitmap);
                }
                finally
                {
                    _context.Unmap(stagingTexture, 0);
                }
            }
        }
        
        /// <summary>
        /// Finds the handle of the first visible window that has an exact title match.
        /// </summary>
        private static IntPtr FindWindowByTitle(string windowTitle)
        {
            IntPtr foundHandle = IntPtr.Zero;
            User32.EnumWindows((hWnd, lParam) =>
            {
                if (!User32.IsWindowVisible(hWnd))
                {
                    return true; // Continue enumeration
                }
                
                var titleBuilder = new StringBuilder(1024);
                User32.GetWindowText(hWnd, titleBuilder, titleBuilder.Capacity);

                if (titleBuilder.ToString() == windowTitle)
                {
                    foundHandle = hWnd;
                    return false; // Stop enumeration
                }

                return true; // Continue enumeration
            }, IntPtr.Zero);

            return foundHandle;
        }
        
        /// <summary>
        /// Continuously processes frames in the background to keep the capture stream active.
        /// </summary>
        private async Task ProcessFramesAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (_framePool != null)
                    {
                        var frame = _framePool.TryGetNextFrame();
                        if (frame != null)
                        {
                            lock (_frameLock)
                            {
                                // Dispose старого фрейму, якщо він є
                                _latestFrame?.Dispose();
                                _latestFrame = frame;
                                _lastFrameTime = DateTime.Now;
                            }
                        }
                    }
                    
                    // Невелика затримка, щоб не перевантажувати CPU
                    // Але достатньо часта, щоб підтримувати потік активним
                    await Task.Delay(16, cancellationToken); // ~60 FPS
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    // Ігноруємо помилки і продовжуємо
                    await Task.Delay(100, cancellationToken);
                }
            }
        }
        
        /// <summary>
        /// Initializes all resources and starts the capture session.
        /// It waits for the first frame to arrive to ensure the session is ready.
        /// </summary>
        private async Task<bool> StartCaptureAsync(GraphicsCaptureItem item)
        {
            StopCapture();
            try
            {
                _captureItem = item;
                
                this.CapturedWindowHandle = FindWindowByTitle(_captureItem.DisplayName);
                
                _captureItem.Closed += OnCaptureItemClosed;

                var result = D3D11.D3D11CreateDevice(
                    null,
                    Vortice.Direct3D.DriverType.Hardware,
                    DeviceCreationFlags.BgraSupport,
                    null,
                    out _device);

                if (result.Failure) return false;
                _context = _device.ImmediateContext;

                var dxgiDevice = _device.QueryInterface<IDXGIDevice>();
                var hr = NativeMethods.CreateDirect3D11DeviceFromDXGIDevice(dxgiDevice.NativePointer, out var pUnknown);
                if (hr != 0) return false;

                var winrtDevice = MarshalInterface<IDirect3DDevice>.FromAbi(pUnknown);
                dxgiDevice.Dispose();

                _framePool = Direct3D11CaptureFramePool.Create(winrtDevice, DirectXPixelFormat.B8G8R8A8UIntNormalized, 2, _captureItem.Size);
                
                // Wait for the first frame
                var frameArrivedCompletionSource = new TaskCompletionSource<bool>();
                void OnFirstFrameArrived(Direct3D11CaptureFramePool sender, object args)
                {
                    frameArrivedCompletionSource.TrySetResult(true);
                    _framePool.FrameArrived -= OnFirstFrameArrived;
                }
                
                _framePool.FrameArrived += OnFirstFrameArrived;

                _session = _framePool.CreateCaptureSession(_captureItem);
                _session.IsCursorCaptureEnabled = false;
                await Task.Delay(50);
                _session.StartCapture();

                // Wait for the FrameArrived event to fire, with a timeout.
                var completedTask = await Task.WhenAny(frameArrivedCompletionSource.Task, Task.Delay(5000));
                if (completedTask != frameArrivedCompletionSource.Task)
                {
                    // Timeout occurred
                    StopCapture();
                    return false;
                }
                
                // Запускаємо фонову обробку фреймів
                _frameProcessingCts = new CancellationTokenSource();
                _frameProcessingTask = ProcessFramesAsync(_frameProcessingCts.Token);

                return true;
            }
            catch
            {
                StopCapture();
                return false;
            }
        }
        
        private static ID3D11Texture2D? GetTextureFromFrame(Direct3D11CaptureFrame frame)
        {
            var surface = frame.Surface.As<IDirect3DDxgiInterfaceAccess>();
            var iid = new Guid("dc8e63f3-d12b-4952-b47b-5e45026a862d"); // ID3D11Texture2D
            var pResource = surface.GetInterface(ref iid);
            if (pResource == IntPtr.Zero)
            {
                return null;
            }
            var texture = new ID3D11Texture2D(pResource);
            return texture;
        }

        private void OnCaptureItemClosed(GraphicsCaptureItem sender, object args)
        {
            StopCapture();
        }

        public void Dispose()
        {
            StopCapture();
        }

        private void StopCapture()
        {
            // Зупиняємо фонову обробку
            _frameProcessingCts?.Cancel();
            try
            {
                _frameProcessingTask?.Wait(TimeSpan.FromSeconds(2));
            }
            catch { }
            _frameProcessingCts?.Dispose();
            _frameProcessingCts = null;
            _frameProcessingTask = null;
            
            // Очищаємо останній фрейм
            lock (_frameLock)
            {
                _latestFrame?.Dispose();
                _latestFrame = null;
            }
            
            _session?.Dispose();
            _framePool?.Dispose();
            _context?.Dispose();
            _device?.Dispose();
            
            if (_captureItem != null)
            {
                _captureItem.Closed -= OnCaptureItemClosed;
                _captureItem = null;
            }
            
            _session = null;
            _framePool = null;
            _context = null;
            _device = null;
            
            CapturedWindowHandle = IntPtr.Zero;
            
            CaptureSessionEnded?.Invoke();
        }
    }
}