using System;
using System.IO.Compression;
using System.Reactive;
using ReactiveUI;
using RSLBot.Core.Services;
using RSLBot.Shared.Settings;

namespace RSLBot.WPF.ViewModels.Tabs
{
    /// <summary>
    /// ViewModel для вкладки глобальних налаштувань додатку.
    /// </summary>
    public class SettingsViewModel : ReactiveViewModelBase
    {
        private readonly SharedSettings _sharedSettings;
        private readonly SettingsService _settingsService;
        private const string SettingsFileName = "app_settings.json";

        private SharedSettings.LanguageSettings.LanguageId _selectedLanguage;
        private string _settingsPath;

        public SharedSettings.LanguageSettings.LanguageId SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedLanguage, value);
                // Тут можна додати логіку зміни мови
            }
        }

        public string SettingsPath
        {
            get => _settingsPath;
            set => this.RaiseAndSetIfChanged(ref _settingsPath, value);
        }

        public ReactiveCommand<Unit, Unit> OpenSettingsFolderCommand { get; }
        public ReactiveCommand<Unit, Unit> ResetAllSettingsCommand { get; }
        public ReactiveCommand<Unit, Unit> ExportSettingsCommand { get; }
        public ReactiveCommand<Unit, Unit> ImportSettingsCommand { get; }

        public SettingsViewModel(SharedSettings sharedSettings, SettingsService settingsService)
        {
            _sharedSettings = sharedSettings;
            _settingsService = settingsService;

            // Встановити поточні значення
            _selectedLanguage = SharedSettings.LanguageSettings.LanguageId.Ukr;
            _settingsPath = System.IO.Path.GetFullPath("Settings");

            // Команди
            OpenSettingsFolderCommand = ReactiveCommand.Create(OpenSettingsFolder);
            ResetAllSettingsCommand = ReactiveCommand.Create(ResetAllSettings);
            ExportSettingsCommand = ReactiveCommand.Create(ExportSettings);
            ImportSettingsCommand = ReactiveCommand.Create(ImportSettings);
        }

        private void OpenSettingsFolder()
        {
            try
            {
                if (System.IO.Directory.Exists("Settings"))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = System.IO.Path.GetFullPath("Settings"),
                        UseShellExecute = true
                    });
                }
                else
                {
                    System.Windows.MessageBox.Show(
                        "Папка з налаштуваннями ще не створена. Вона буде створена після першого збереження.",
                        "Інформація",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Не вдалося відкрити папку: {ex.Message}",
                    "Помилка",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private void ResetAllSettings()
        {
            var result = System.Windows.MessageBox.Show(
                "Ви впевнені, що хочете скинути всі налаштування? Це видалить всі збережені конфігурації.",
                "Підтвердження",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                try
                {
                    if (System.IO.Directory.Exists("Settings"))
                    {
                        foreach (var file in System.IO.Directory.GetFiles("Settings", "*.json"))
                        {
                            System.IO.File.Delete(file);
                        }
                        System.Windows.MessageBox.Show(
                            "Всі налаштування успішно скинуті. Перезапустіть додаток.",
                            "Успіх",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(
                        $"Помилка при скиданні налаштувань: {ex.Message}",
                        "Помилка",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                }
            }
        }

        private void ExportSettings()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "ZIP архів (*.zip)|*.zip",
                FileName = $"RSLBot_Settings_{DateTime.Now:yyyyMMdd_HHmmss}.zip"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    if (System.IO.Directory.Exists("Settings"))
                    {
                        System.IO.Compression.ZipFile.CreateFromDirectory("Settings", dialog.FileName);
                        System.Windows.MessageBox.Show(
                            "Налаштування успішно експортовані!",
                            "Успіх",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Information);
                    }
                    else
                    {
                        System.Windows.MessageBox.Show(
                            "Папка з налаштуваннями не знайдена.",
                            "Помилка",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(
                        $"Помилка при експорті: {ex.Message}",
                        "Помилка",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                }
            }
        }

        private void ImportSettings()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "ZIP архів (*.zip)|*.zip"
            };

            if (dialog.ShowDialog() == true)
            {
                var result = System.Windows.MessageBox.Show(
                    "Імпорт налаштувань замінить поточні конфігурації. Продовжити?",
                    "Підтвердження",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    try
                    {
                        if (!System.IO.Directory.Exists("Settings"))
                        {
                            System.IO.Directory.CreateDirectory("Settings");
                        }

                        System.IO.Compression.ZipFile.ExtractToDirectory(dialog.FileName, "Settings", overwriteFiles: true);
                        
                        System.Windows.MessageBox.Show(
                            "Налаштування успішно імпортовані! Перезапустіть додаток.",
                            "Успіх",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show(
                            $"Помилка при імпорті: {ex.Message}",
                            "Помилка",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}

