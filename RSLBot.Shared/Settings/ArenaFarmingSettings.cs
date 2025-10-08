using ReactiveUI;
using RSLBot.Shared.Interfaces;

namespace RSLBot.Shared.Settings
{
    /// <summary>
    /// Налаштування для сценарію фарму Арени.
    /// </summary>
    public class ArenaFarmingSettings : ReactiveObject, IScenarioSettings
    {
        private int _tokenPurchases;
        private bool _buyTokensWithGems;
        private bool _refreshOpponentsOnStart = false;

        public string ScenarioName => "ArenaFarm";

        /// <summary>
        /// Кількість покупок мішечків (0 - необмежена кількість).
        /// </summary>
        public int TokenPurchases
        {
            get => _tokenPurchases;
            set => this.RaiseAndSetIfChanged(ref _tokenPurchases, value);
        }

        /// <summary>
        /// Чи треба купувати мішечки за рубіни.
        /// </summary>
        public bool BuyTokensWithGems
        {
            get => _buyTokensWithGems;
            set => this.RaiseAndSetIfChanged(ref _buyTokensWithGems, value);
        }

        /// <summary>
        /// Починати Арену з рефрешу противників.
        /// </summary>
        public bool RefreshOpponentsOnStart
        {
            get => _refreshOpponentsOnStart;
            set => this.RaiseAndSetIfChanged(ref _refreshOpponentsOnStart, value);
        }

    }
}
