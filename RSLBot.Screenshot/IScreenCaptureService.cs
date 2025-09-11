using System.Drawing;

namespace RSLBot.Screenshot.Interfaces
{
    /// <summary>
    /// Відповідає за захоплення зображення з вікна гри.
    /// </summary>
    public interface IScreenCaptureService
    {
        /// <summary>
        /// Робить знімок вікна гри.
        /// </summary>
        /// <returns>Зображення у форматі Bitmap.</returns>
        Bitmap CaptureGameWindow(Rectangle rectangle = default, int iter = 1);
    }
}
