using System;
using RSLBot.Core.CoreHelpers;
using RSLBot.Core.Services;
using ReactiveUI;
using RSLBot.Core.Scenarios.Dungeons.Twins;
using RSLBot.Shared.Settings;

namespace RSLBot.WPF.ViewModels.Tabs
{
    /// <summary>
    /// ViewModel для вкладки налаштувань Twins.
    /// </summary>
    public class TwinsSettingsViewModel : BaseSettingsViewModel<TwinsScenario, TwinsFarmingSettings>
    {
        protected override string SettingsFileName => "twins_settings.json";

        public TwinsSettingsViewModel(
            TwinsScenario scenarioExecutor, 
            TwinsFarmingSettings settings, 
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
                x => x.Settings.MaxDefeat)
                .Subscribe(_ => SaveSettings());
        }
    }
}

