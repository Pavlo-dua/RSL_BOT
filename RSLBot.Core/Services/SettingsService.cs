using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RSLBot.Core.Services
{
    /// <summary>
    /// Сервіс для збереження та завантаження налаштувань у JSON файли.
    /// </summary>
    public class SettingsService
    {
        private readonly string _settingsDirectory;
        private readonly JsonSerializerOptions _jsonOptions;

        public SettingsService(string settingsDirectory = "Settings")
        {
            _settingsDirectory = settingsDirectory;
            
            // Створити директорію, якщо її немає
            if (!Directory.Exists(_settingsDirectory))
            {
                Directory.CreateDirectory(_settingsDirectory);
            }

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };
        }

        /// <summary>
        /// Зберігає налаштування у JSON файл.
        /// </summary>
        public void SaveSettings<T>(string fileName, T settings)
        {
            var filePath = Path.Combine(_settingsDirectory, fileName);
            var json = JsonSerializer.Serialize(settings, _jsonOptions);
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// Завантажує налаштування з JSON файлу.
        /// </summary>
        public T? LoadSettings<T>(string fileName) where T : class, new()
        {
            var filePath = Path.Combine(_settingsDirectory, fileName);
            
            if (!File.Exists(filePath))
            {
                // Повернути нові налаштування, якщо файл не існує
                return new T();
            }

            try
            {
                var json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<T>(json, _jsonOptions);
            }
            catch
            {
                // У випадку помилки повернути нові налаштування
                return new T();
            }
        }

        /// <summary>
        /// Перевіряє, чи існує файл налаштувань.
        /// </summary>
        public bool SettingsFileExists(string fileName)
        {
            var filePath = Path.Combine(_settingsDirectory, fileName);
            return File.Exists(filePath);
        }
    }
}

