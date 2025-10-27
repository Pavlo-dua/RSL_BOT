using System;
using RSLBot.Core.CoreHelpers;
using RSLBot.Core.Scenarios.ArenaTag;
using RSLBot.Core.Services;
using RSLBot.Shared.Settings;
using ReactiveUI;

namespace RSLBot.WPF.ViewModels.Tabs
{
    /// <summary>
    /// ViewModel для вкладки налаштувань Тег Арени.
    /// </summary>
    public class TagArenaSettingsViewModel : BaseSettingsViewModel<ArenaTagFarmingScenario, ArenaFarmingSettings>
    {
        protected override string SettingsFileName => "tag_arena_settings.json";

        public TagArenaSettingsViewModel(
            ArenaTagFarmingScenario scenarioExecutor, 
            ArenaFarmingSettings settings, 
            Tools tool, 
            ScreenCaptureManager screenCaptureManager, 
            SharedSettings sharedSettings,
            SettingsService settingsService)
            : base(scenarioExecutor, settings, tool, screenCaptureManager, sharedSettings, settingsService)
        {
            this.WhenAnyValue(x => x.Settings.BuyTokensWithGems)
                .Subscribe(buyTokens =>
                {
                    if (buyTokens)
                    {
                        if (Settings.TokenPurchases == 0)
                        {
                            Settings.TokenPurchases = 1;
                        }
                    }
                    else
                    {
                        Settings.TokenPurchases = 0;
                    }
                });

            // Автоматично зберігати налаштування при зміні
            this.WhenAnyValue(
                x => x.Settings.BuyTokensWithGems,
                x => x.Settings.TokenPurchases,
                x => x.Settings.RefreshOpponentsOnStart)
                .Subscribe(_ => SaveSettings());
        }
    }
}
