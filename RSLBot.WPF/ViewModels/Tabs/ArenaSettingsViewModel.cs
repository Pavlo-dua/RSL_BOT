using System.Drawing.Imaging;
using System.Threading;
using System.Windows.Interop;
using RSLBot.Core.CoreHelpers;
using RSLBot.Core.Services;
using AppContext = RSLBot.Core.Services.AppContext;

namespace RSLBot.WPF.ViewModels.Tabs
{
    using System.Reactive;
    using ReactiveUI;
    using RSLBot.Core.Scenarios.ArenaClassic;
    using RSLBot.Shared.Settings;

    /// <summary>
    /// ViewModel для вкладки налаштувань Арени.
    /// </summary>
    public class ArenaSettingsViewModel : BaseSettingsViewModel<ArenaFarmingScenario, ArenaFarmingSettings>
    {
        public ArenaSettingsViewModel(ArenaFarmingScenario scenarioExecutor, ArenaFarmingSettings settings, Tools tool, ScreenCaptureManager screenCaptureManager):base(scenarioExecutor, settings, tool, screenCaptureManager)
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

