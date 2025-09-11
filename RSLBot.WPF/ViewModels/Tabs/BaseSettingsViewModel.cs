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
using AppContext = RSLBot.Core.Services.AppContext;

namespace RSLBot.WPF.ViewModels.Tabs;

public abstract class BaseSettingsViewModel<TScenario, TSettings> : ReactiveViewModelBase where TSettings : IScenarioSettings where TScenario : IScenario
{
    private readonly Tools tool;
    private readonly ScreenCaptureManager _manager;

    // Properties to hold the scenario and settings
    protected TScenario Scenario { get; }
    protected TSettings Settings { get; }
    
    public ReactiveCommand<Unit, Unit> RunScenarioCommand { get; }

    /// <summary>
    /// Initializes a new instance of the BaseSettingsViewModel class.
    /// </summary>
    /// <param name="scenario">The scenario instance.</param>
    /// <param name="settings">The settings for the scenario.</param>
    protected BaseSettingsViewModel(TScenario scenario, TSettings settings, Tools tool, ScreenCaptureManager manager)
    {
        this.tool = tool;
        _manager = manager;
        // Assign the passed objects to the public properties
        this.Scenario = scenario;
        this.Settings = settings;
        
        RunScenarioCommand = ReactiveCommand.CreateFromTask(RunScenarioAsync, outputScheduler: RxApp.MainThreadScheduler);
        
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
    }

    protected virtual void PreRunScenario()
    {
        
    }
    
    private async Task RunScenarioAsync()
    {
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
}