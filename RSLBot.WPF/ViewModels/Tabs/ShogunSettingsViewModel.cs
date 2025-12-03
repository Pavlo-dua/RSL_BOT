using System;
using System.Reactive.Linq;
using RSLBot.Core.CoreHelpers;
using RSLBot.Core.Services;
using RSLBot.Shared.Settings;
using ReactiveUI;
using RSLBot.Core.Scenarios.Dungeons.Shogun;
using DynamicData.Binding;

namespace RSLBot.WPF.ViewModels.Tabs
{
    /// <summary>
    /// ViewModel для вкладки налаштувань Сьогуна.
    /// </summary>
    public class ShogunSettingsViewModel : BaseSettingsViewModel<ShogunScenario, ShogunFarmingSettings>
    {
        protected override string SettingsFileName => "shogun_farming_settings.json";

        public ShogunSettingsViewModel(
            ShogunScenario scenarioExecutor,
            ShogunFarmingSettings settings,
            Tools tool,
            ScreenCaptureManager screenCaptureManager,
            SharedSettings sharedSettings,
            SettingsService settingsService)
            : base(scenarioExecutor, settings, tool, screenCaptureManager, sharedSettings, settingsService)
        {
            settings.WhenAnyPropertyChanged().Subscribe(_ => SaveSettings());
        }
    }
}
