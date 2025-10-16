using RSLBot.Core.CoreHelpers;
using RSLBot.Core.Scenarios.ArenaTag;
using RSLBot.Core.Services;
using RSLBot.Shared.Settings;
using ReactiveUI;
using System;

namespace RSLBot.WPF.ViewModels.Tabs
{
    public class TagArenaSettingsViewModel : BaseSettingsViewModel<ArenaTagFarmingScenario, ArenaFarmingSettings>
    {
        public TagArenaSettingsViewModel(ArenaTagFarmingScenario scenarioExecutor, ArenaFarmingSettings settings, Tools tool, ScreenCaptureManager screenCaptureManager, SharedSettings sharedSettings)
            : base(scenarioExecutor, settings, tool, screenCaptureManager, sharedSettings)
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
        }
    }
}
