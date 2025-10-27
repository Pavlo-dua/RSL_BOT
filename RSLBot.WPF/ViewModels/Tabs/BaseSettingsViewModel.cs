using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reactive;
using System.Threading.Tasks;
using ReactiveUI;
using RSLBot.Core.CoreHelpers;
using RSLBot.Core.Interfaces;
using RSLBot.Core.Services;
using RSLBot.Shared.Interfaces;
using RSLBot.Shared.Settings;
using AppContext = RSLBot.Core.Services.AppContext;

namespace RSLBot.WPF.ViewModels.Tabs;

public abstract class BaseSettingsViewModel<TScenario, TSettings> : ReactiveViewModelBase where TSettings : class, IScenarioSettings, new() where TScenario : IScenario 
{
    private readonly Tools tool;
    private readonly ScreenCaptureManager _manager;
    private readonly SettingsService _settingsService;

    // Properties to hold the scenario and settings
    protected TScenario Scenario { get; }
    public TSettings Settings { get; }
    
    public ReactiveCommand<Unit, Unit> RunScenarioCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelScenarioCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveSettingsCommand { get; }

    /// <summary>
    /// Ім'я файлу для збереження налаштувань. Має бути визначене в дочірньому класі.
    /// </summary>
    protected abstract string SettingsFileName { get; }

    /// <summary>
    /// Initializes a new instance of the BaseSettingsViewModel class.
    /// </summary>
    /// <param name="scenario">The scenario instance.</param>
    /// <param name="settings">The settings for the scenario.</param>
    protected BaseSettingsViewModel(TScenario scenario, TSettings settings, Tools tool, ScreenCaptureManager manager, SharedSettings sharedSettings, SettingsService settingsService)
    {
        this.tool = tool;
        _manager = manager;
        _settingsService = settingsService;
        
        // Assign the passed objects to the public properties
        this.Scenario = scenario;
        this.Settings = settings;
        
        RunScenarioCommand = ReactiveCommand.CreateFromTask(RunScenarioAsync, outputScheduler: RxApp.MainThreadScheduler);
        CancelScenarioCommand = ReactiveCommand.Create(() => { sharedSettings.CancellationTokenSource.Cancel(); });
        SaveSettingsCommand = ReactiveCommand.Create(SaveSettings);
        
        RunScenarioCommand.ThrownExceptions
            .Subscribe(ex =>
            {
                System.Windows.MessageBox.Show($"Виникла помилка під час виконання команди:\n\n{ex.Message}", 
                    "Помилка захоплення", 
                    System.Windows.MessageBoxButton.OK, 
                    System.Windows.MessageBoxImage.Error);
            
                // Для детального аналізу можна вивести повний стек трейс в консоль або логер.
                System.Diagnostics.Debug.WriteLine(ex);
            });

        // Завантажити налаштування при створенні
        LoadSettings();
    }

    protected virtual void PreRunScenario()
    {
        
    }
    
    private async Task RunScenarioAsync()
    {
        // Зберегти налаштування перед запуском
        SaveSettings();
        
        bool selected = await _manager.EnsureCaptureIsActiveAsync(AppContext.MainWindowHandle);
        
        if (!selected)
        {
            System.Windows.MessageBox.Show("Вибір вікна було скасовано.");
            return;
        }
        
        if (tool.raidProcess.MainWindowHandle != _manager.CapturedWindowHandle)
        {
            System.Windows.MessageBox.Show("Процес гри не знайдено або його вікно недоступне, або ви обрали не вікно з грою.");
            return;
        }

        Bitmap b = await _manager.CaptureFrameAsync();
        
        b.Save("test.png", ImageFormat.Png);
        
        await Scenario.ExecuteAsync();
    }

    /// <summary>
    /// Зберігає поточні налаштування у файл.
    /// </summary>
    protected virtual void SaveSettings()
    {
        _settingsService.SaveSettings(SettingsFileName, Settings);
    }

    /// <summary>
    /// Завантажує налаштування з файлу.
    /// </summary>
    protected virtual void LoadSettings()
    {
        var loadedSettings = _settingsService.LoadSettings<TSettings>(SettingsFileName);
        if (loadedSettings != null)
        {
            // Копіюємо властивості з завантажених налаштувань
            CopySettingsProperties(loadedSettings, Settings);
        }
    }

    /// <summary>
    /// Копіює властивості з одного об'єкта налаштувань до іншого.
    /// </summary>
    private void CopySettingsProperties(TSettings source, TSettings destination)
    {
        var properties = typeof(TSettings).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        foreach (var property in properties)
        {
            if (property.CanWrite && property.CanRead)
            {
                try
                {
                    var value = property.GetValue(source);
                    property.SetValue(destination, value);
                }
                catch
                {
                    // Ігноруємо помилки копіювання окремих властивостей
                }
            }
        }
    }
}