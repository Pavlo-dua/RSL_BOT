using System.Collections.Generic;
using ReactiveUI;
using RSLBot.Core.Interfaces;

namespace RSLBot.Shared.Settings
{
    /// <summary>
    /// Модель для елемента черги сценаріїв.
    /// </summary>
    public class ScenarioQueueItem : ReactiveObject
    {
        private bool _isEnabled = true;
        
        public IScenario.ScenarioId ScenarioId { get; set; }
        
        public string DisplayName { get; set; } = string.Empty;
        
        public bool IsEnabled
        {
            get => _isEnabled;
            set => this.RaiseAndSetIfChanged(ref _isEnabled, value);
        }
    }

    /// <summary>
    /// Налаштування для Dashboard (черга сценаріїв).
    /// </summary>
    public class DashboardSettings
    {
        public List<ScenarioQueueItem> ScenarioQueue { get; set; } = new();
    }
}
