using ReactiveUI;
using RSLBot.Shared.Interfaces;

namespace RSLBot.Shared.Settings
{
    /// <summary>
    /// Базові налаштування для стандартних підземель (Мінотавр, Сьогун, Дьявол пустелі).
    /// </summary>
    public abstract class StandardDungeonSettings : ReactiveObject, IScenarioSettings
    {
        private bool _useFreeRefill = false;
        private bool _useGemRefill = false;
        private int _maxDefeat = -1;
        private int _maxGemRefills = 0;
        private int _maxFreeRefills = 1;
        private int _maxBattles = 0;
        private bool _optimizeResources = false;

        public abstract string ScenarioName { get; }

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
        /// Максимальна кількість поразок перед зупинкою (-1 = необмежено).
        /// </summary>
        public int MaxDefeat
        {
            get => _maxDefeat;
            set => this.RaiseAndSetIfChanged(ref _maxDefeat, value);
        }

        /// <summary>
        /// Максимальна кількість покупок енергії за рубіни (0 = необмежено).
        /// </summary>
        public int MaxGemRefills
        {
            get => _maxGemRefills;
            set => this.RaiseAndSetIfChanged(ref _maxGemRefills, value);
        }

        /// <summary>
        /// Максимальна кількість використань безкоштовної енергії (0 = необмежено).
        /// </summary>
        public int MaxFreeRefills
        {
            get => _maxFreeRefills;
            set => this.RaiseAndSetIfChanged(ref _maxFreeRefills, value);
        }

        /// <summary>
        /// Максимальна кількість боїв (0 = необмежено).
        /// </summary>
        public int MaxBattles
        {
            get => _maxBattles;
            set => this.RaiseAndSetIfChanged(ref _maxBattles, value);
        }

        /// <summary>
        /// Оптимізація ресурсів: продовжувати бій, якщо ліміт боїв вичерпано, але є енергія.
        /// </summary>
        public bool OptimizeResources
        {
            get => _optimizeResources;
            set => this.RaiseAndSetIfChanged(ref _optimizeResources, value);
        }

        private int _tournamentPoints = 0;
        /// <summary>
        /// Цільова кількість турнірних балів (0 = вимкнено).
        /// </summary>
        public int TournamentPoints
        {
            get => _tournamentPoints;
            set => this.RaiseAndSetIfChanged(ref _tournamentPoints, value);
        }
    }
}
