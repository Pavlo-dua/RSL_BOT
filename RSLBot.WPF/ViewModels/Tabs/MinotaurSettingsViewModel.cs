using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using RSLBot.Core.CoreHelpers;
using RSLBot.Core.Services;
using RSLBot.Shared.Settings;
using ReactiveUI;
using RSLBot.Core.Scenarios.Dungeons.Minotaur;

namespace RSLBot.WPF.ViewModels.Tabs
{
    /// <summary>
    /// Модель для елемента сувоїв в ComboBox.
    /// </summary>
    public class ScrollComboBoxItem
    {
        public string? Route { get; set; }
        public RSLBot.Shared.Models.MinotaurScrollType Tag { get; set; }
        public string? ImagePath { get; set; }

        public override string ToString() => Route ?? "";
    }

    /// <summary>
    /// ViewModel для вкладки налаштувань Мінотавра.
    /// </summary>
    public class MinotaurSettingsViewModel : BaseSettingsViewModel<MinotaurScenario, MinotaurFarmingSettings>
    {
        protected override string SettingsFileName => "minotaur_farming_settings.json";

        private ObservableCollection<ScrollComboBoxItem>? _availableScrolls;
        public ObservableCollection<ScrollComboBoxItem>? AvailableScrolls
        {
            get => _availableScrolls;
            set => this.RaiseAndSetIfChanged(ref _availableScrolls, value);
        }

        public MinotaurSettingsViewModel(
            MinotaurScenario scenarioExecutor,
            MinotaurFarmingSettings settings,
            Tools tool,
            ScreenCaptureManager screenCaptureManager,
            SharedSettings sharedSettings,
            SettingsService settingsService)
            : base(scenarioExecutor, settings, tool, screenCaptureManager, sharedSettings, settingsService)
        {
            // Ініціалізувати список доступних сувоїв
            InitializeAvailableScrolls();

            // Автоматично зберігати налаштування при зміні
            this.WhenAnyValue(
                    x => x.Settings.UseFreeRefill,
                    x => x.Settings.UseGemRefill,
                    x => x.Settings.Scrolls,
                    x => x.Settings.MaxDefeat)
                .Subscribe(_ => SaveSettings());

            this.WhenAnyValue(
                    x => x.Settings.MaxBattles,
                    x => x.Settings.MaxFreeRefills,
                    x => x.Settings.MaxGemRefills,
                    x => x.Settings.OptimizeResources)
                .Subscribe(_ => SaveSettings());

            // Коли змінюється вибраний сувій, оновлювати налаштування
            this.WhenAnyValue(x => x.SelectedScroll)
                .Where(x => x != null)
                .Subscribe(scroll => Settings.Scrolls = scroll!.Tag);
        }

        private ScrollComboBoxItem? _selectedScroll;
        public ScrollComboBoxItem? SelectedScroll
        {
            get => _selectedScroll;
            set => this.RaiseAndSetIfChanged(ref _selectedScroll, value);
        }

        private void InitializeAvailableScrolls()
        {
            AvailableScrolls = new ObservableCollection<ScrollComboBoxItem>
            {
                new ScrollComboBoxItem
                {
                    Route = "Всі",
                    Tag = RSLBot.Shared.Models.MinotaurScrollType.Red,
                    ImagePath = "/RSLBot.Shared;component/Configuration/ScreenDefinition/Templates/red_scroll_res.png"
                },
                new ScrollComboBoxItem
                {
                    Route = "Незвичні",
                    Tag = RSLBot.Shared.Models.MinotaurScrollType.Green,
                    ImagePath = "/RSLBot.Shared;component/Configuration/ScreenDefinition/Templates/green_scroll_res.png"
                },
                new ScrollComboBoxItem
                {
                    Route = "Базові",
                    Tag = RSLBot.Shared.Models.MinotaurScrollType.Basic,
                    ImagePath = "/RSLBot.Shared;component/Configuration/ScreenDefinition/Templates/base_scroll_res.png"
                }
            };

            // Встановити вибраний елемент за замовчуванням
            var defaultScroll = AvailableScrolls.FirstOrDefault(s => s.Tag == Settings.Scrolls);
            if (defaultScroll != null)
            {
                SelectedScroll = defaultScroll;
            }
            else
            {
                SelectedScroll = AvailableScrolls.FirstOrDefault();
            }
        }
    }
}