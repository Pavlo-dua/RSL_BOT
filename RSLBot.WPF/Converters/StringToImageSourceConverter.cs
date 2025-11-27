using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace RSLBot.WPF.Converters
{
    public class StringToImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string imagePath && !string.IsNullOrEmpty(imagePath))
            {
                try
                {
                    // Створюємо BitmapImage для правильного кешування
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    
                    // Якщо це pack URI або абсолютний URI
                    if (imagePath.StartsWith("pack://") || imagePath.StartsWith("http://") || imagePath.StartsWith("https://") || imagePath.Contains(":/"))
                    {
                        bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
                    }
                    else
                    {
                        // Відносний шлях
                        bitmap.UriSource = new Uri(imagePath, UriKind.RelativeOrAbsolute);
                    }
                    
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze(); // Потрібно для використання в кількох місцях
                    
                    return bitmap;
                }
                catch (Exception ex)
                {
                    // Логуємо помилку для налагодження
                    System.Diagnostics.Debug.WriteLine($"Failed to load image from: {imagePath}, Error: {ex.Message}");
                    return null;
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
