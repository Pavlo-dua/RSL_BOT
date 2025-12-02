using ReactiveUI;
using RSLBot.Shared.Interfaces;
using RSLBot.Shared.Models;

namespace RSLBot.Shared.Settings
{
    /// <summary>
    /// Налаштування для сценарію фарму Мінотавра.
    /// </summary>
    public class MinotaurFarmingSettings : ReactiveObject, IScenarioSettings
    {
        private bool _useFreeRefill = false;
        private bool _useGemRefill = false;
        private MinotaurScrollType _scrolls = MinotaurScrollType.Red;
        private int _maxDefeat = -1;

        public string ScenarioName => "MinotaurFarm";

        /// <summary>
        /// Чи можна використати безкоштовну додаткову енергію.
        /// </summary>
        public bool UseFreeRefill
        {
            get => _useFreeRefill;
            set => this.RaiseAndSetIfChanged(ref _useFreeRefill, value);
        }

        /// <summary>
        /// Чи можна використовувати рубіни для покупки енергії.
        /// </summary>
        public bool UseGemRefill
        {
            get => _useGemRefill;
            set => this.RaiseAndSetIfChanged(ref _useGemRefill, value);
        }

        /// <summary>
        /// Які сувої фармити.
        /// </summary>
        public MinotaurScrollType Scrolls
        {
            get => _scrolls;
            set => this.RaiseAndSetIfChanged(ref _scrolls, value);
        }

        /// <summary>
        /// Максимальна кількість поразок перед зупинкою (-1 = необмежено).
        /// </summary>
        public int MaxDefeat
        {
            get => _maxDefeat;
            set => this.RaiseAndSetIfChanged(ref _maxDefeat, value);
        }
    }
}
