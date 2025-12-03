using MaterialDesignExtensions.Model;
using MaterialDesignThemes.Wpf;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using RSLBot.Core.Interfaces;

namespace RSLBot.WPF.ViewModels
{
    using RSLBot.WPF.ViewModels.Tabs;

    /// <summary>
    /// Головна ViewModel додатку. Керує навігацією та контентом.
    /// </summary>
    public class MainViewModel : ReactiveViewModelBase
    {
        private object _currentContentViewModel;
        private INavigationItem _selectedItem;

        public List<INavigationItem> NavigationItems { get; }
        public LogViewModel LogViewModel { get; }

        public object CurrentContentViewModel
        {
            get => _currentContentViewModel;
            set => this.RaiseAndSetIfChanged(ref _currentContentViewModel, value);
        }

        public INavigationItem SelectedItem
        {
            get => _selectedItem;
            set => this.RaiseAndSetIfChanged(ref _selectedItem, value);
        }

        public MainViewModel(
            LogViewModel logViewModel,
            DashboardViewModel dashboardVm,
            ArenaSettingsViewModel arenaVm,
            TagArenaSettingsViewModel tagArenaVm,
            TwinsSettingsViewModel twinsVm,
            MinotaurSettingsViewModel MinVm,
            ShogunSettingsViewModel shogunVm,
            SandDevilSettingsViewModel sandDevilVm,
            SettingsViewModel settingsVm,
            ILoggingService loggingService)
        {
            LogViewModel = logViewModel;
            NavigationItems = new List<INavigationItem>
            {
                new FirstLevelNavigationItem { Label = "Послідовність", Icon = PackIconKind.PlaylistPlay, IsSelected = true },
                new FirstLevelNavigationItem { Label = "Класична Арена", Icon = PackIconKind.ShieldSword},
                new FirstLevelNavigationItem { Label = "Тег Арена", Icon = PackIconKind.ShieldStar},
                new FirstLevelNavigationItem { Label = "Твінс", Icon = PackIconKind.AccountMultiple},
                new FirstLevelNavigationItem { Label = "Лабіринт Мінотавра", Icon = PackIconKind.Cow},
                new FirstLevelNavigationItem { Label = "Сьогун", Icon = PackIconKind.Sword},
                new FirstLevelNavigationItem { Label = "Дьявол пустелі", Icon = PackIconKind.Skull},
                new FirstLevelNavigationItem { Label = "Налаштування", Icon = PackIconKind.Cog}
            };

            // Підписуємося на зміни SelectedItem і оновлюємо контент
            this.WhenAnyValue(x => x.SelectedItem)
                .Subscribe(selected =>
                {
                    if (selected is FirstLevelNavigationItem item)
                    {
                        switch (item.Label)
                        {
                            case "Послідовність":
                                CurrentContentViewModel = dashboardVm;
                                break;
                            case "Класична Арена":
                                CurrentContentViewModel = arenaVm;
                                break;
                            case "Тег Арена":
                                CurrentContentViewModel = tagArenaVm;
                                break;
                            case "Твінс":
                                CurrentContentViewModel = twinsVm;
                                break;
                            case "Лабіринт Мінотавра":
                                CurrentContentViewModel = MinVm;
                                break;
                            case "Сьогун":
                                CurrentContentViewModel = shogunVm;
                                break;
                            case "Дьявол пустелі":
                                CurrentContentViewModel = sandDevilVm;
                                break;
                            case "Налаштування":
                                CurrentContentViewModel = settingsVm;
                                break;
                        }
                    }
                });

            // Встановлюємо початковий вибраний елемент.
            // Це автоматично викличе підписку вище і встановить початковий контент.
            SelectedItem = NavigationItems.OfType<FirstLevelNavigationItem>().FirstOrDefault(item => item.IsSelected);

            loggingService.InfoUi("Ready to use!");
        }
    }
}