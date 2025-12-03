using System;
using System.Reactive.Linq;
using RSLBot.Core.CoreHelpers;
using RSLBot.Core.Services;
using RSLBot.Shared.Settings;
using ReactiveUI;
using RSLBot.Core.Scenarios.Dungeons.SandDevil;
using DynamicData.Binding;

namespace RSLBot.WPF.ViewModels.Tabs
{
    /// <summary>
    /// ViewModel для вкладки налаштувань Дьявола пустелі.
    /// </summary>
    public class SandDevilSettingsViewModel : BaseSettingsViewModel<SandDevilScenario, SandDevilFarmingSettings>
    {
        protected override string SettingsFileName => "sand_devil_farming_settings.json";

        public SandDevilSettingsViewModel(
            SandDevilScenario scenarioExecutor,
            SandDevilFarmingSettings settings,
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
