using ReactiveUI;
using RSLBot.Shared.Interfaces;
using RSLBot.Shared.Models;
using ReactiveUI;
using RSLBot.Shared.Interfaces;
using RSLBot.Shared.Models;

namespace RSLBot.Shared.Settings
{
    /// <summary>
    /// Налаштування для сценарію фарму Мінотавра.
    /// </summary>
    public class MinotaurFarmingSettings : StandardDungeonSettings
    {
        private MinotaurScrollType _scrolls = MinotaurScrollType.Red;

        public override string ScenarioName => "MinotaurFarm";

        /// <summary>
        /// Тип сувоїв, які потрібно фармити.
        /// </summary>
        public MinotaurScrollType Scrolls
        {
            get => _scrolls;
            set => this.RaiseAndSetIfChanged(ref _scrolls, value);
        }
    }
}
