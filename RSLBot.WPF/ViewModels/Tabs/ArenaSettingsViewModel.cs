using System;
using RSLBot.Core.CoreHelpers;
using RSLBot.Core.Services;
using ReactiveUI;
using RSLBot.Core.Scenarios.ArenaClassic;
using RSLBot.Shared.Settings;

namespace RSLBot.WPF.ViewModels.Tabs
{
    /// <summary>
    /// ViewModel для вкладки налаштувань Класичної Арени.
    /// </summary>
    public class ArenaSettingsViewModel : BaseSettingsViewModel<ArenaFarmingScenario, ArenaFarmingSettings>
    {
        protected override string SettingsFileName => "classic_arena_settings.json";

        public ArenaSettingsViewModel(
            ArenaFarmingScenario scenarioExecutor, 
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

