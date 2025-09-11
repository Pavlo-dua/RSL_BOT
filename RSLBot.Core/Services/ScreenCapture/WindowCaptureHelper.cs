using System;
using System.Threading.Tasks;
using Windows.Graphics.Capture;
using RSLBot.Core.Services.ScreenCapture.Interop;
using WinRT.Interop; // This 'using' is crucial for the solution!

namespace RSLBot.Core.Services
{
    public static class WindowCaptureHelper
    {
        /// <summary>
        /// Shows the modern, system-provided window picker and returns the selected capture item.
        /// This is the correct and most stable way to let the user select a window.
        /// It uses the officially supported InitializeWithWindow helper to work even when the app is elevated.
        /// </summary>
        /// <param name="ownerHwnd">The handle of the owner window for the picker dialog.</param>
        /// <returns>A GraphicsCaptureItem if the user selected a window; otherwise, null.</returns>
        public static async Task<GraphicsCaptureItem?> PickWindowToCaptureAsync(IntPtr ownerHwnd)
        {
            var picker = new GraphicsCapturePicker();
            
            // --- THE CORRECT WAY ---
            // This static helper method correctly handles the cast and initialization.
            // It associates the picker with our window handle, allowing it to be launched
            // from an elevated process without a casting error.
            InitializeWithWindow.Initialize(picker, ownerHwnd);
            
            // This will now show the standard Windows UI for window selection.
            GraphicsCaptureItem? item = await picker.PickSingleItemAsync();
            return item;
        }
    }
}