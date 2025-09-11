namespace RSLBot.WPF.ViewModels.Tabs
{
    using System.Collections.ObjectModel;
    using System.Reactive;
    using ReactiveUI;

    /// <summary>
    /// ViewModel для головної вкладки "Dashboard", що керує послідовністю сценаріїв.
    /// </summary>
    public class DashboardViewModel : ReactiveViewModelBase
    {
        // TODO: Реалізувати логіку послідовного запуску
        public ObservableCollection<string> ScenarioQueue { get; } = new();
        public ReactiveCommand<Unit, Unit> RunSequenceCommand { get; }

        public DashboardViewModel()
        {
            // Поки що це заглушка
            ScenarioQueue.Add("Arena Farm (15 runs)");
            ScenarioQueue.Add("Minotaur Farm (10 runs)");

            RunSequenceCommand = ReactiveCommand.Create(() => { /* TODO */ });
        }
    }
}
