using ReactiveUI;
using RSLBot.Shared.Interfaces;

namespace RSLBot.Shared.Settings
{
    /// <summary>
    /// Налаштування для сценарію фарму Twins.
    /// </summary>
    public class TwinsFarmingSettings : ReactiveObject, IScenarioSettings
    {
        private bool _buyTokensWithGems = false;
        private int _maxDefeat = -1;
        private int _tokenPurchases = 0;
        
        public string ScenarioName => "TwinsFarm";

        /// <summary>
        /// Чи треба купувати мішечки за рубіни.
        /// </summary>
        public bool BuyTokensWithGems
        {
            get => _buyTokensWithGems;
            set => this.RaiseAndSetIfChanged(ref _buyTokensWithGems, value);
        }

        /// <summary>
        /// Максимальна кількість поразок перед зупинкою (-1 = необмежено).
        /// </summary>
        public int MaxDefeat
        {
            get => _maxDefeat;
            set => this.RaiseAndSetIfChanged(ref _maxDefeat, value);         
        }

        /// <summary>
        /// Кількість покупок мішечків (0 - необмежена кількість).
        /// </summary>
        public int TokenPurchases
        {
            get => _tokenPurchases;
            set => this.RaiseAndSetIfChanged(ref _tokenPurchases, value);
        }
    }
}
