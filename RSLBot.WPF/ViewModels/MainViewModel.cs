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

        public MainViewModel(LogViewModel logViewModel, DashboardViewModel dashboardVm, ArenaSettingsViewModel arenaVm, TagArenaSettingsViewModel tagArenaVm, ILoggingService loggingService)
        {
            LogViewModel = logViewModel;
            NavigationItems = new List<INavigationItem>
            {
                new FirstLevelNavigationItem { Label = "Dashboard", Icon = PackIconKind.ViewDashboard, IsSelected = true },
                new FirstLevelNavigationItem { Label = "Classic Arena", Icon = PackIconKind.ShieldSword},
                new FirstLevelNavigationItem { Label = "Tag Arena", Icon = PackIconKind.ShieldStar}
            };

            // Підписуємося на зміни SelectedItem і оновлюємо контент
            this.WhenAnyValue(x => x.SelectedItem)
                .Subscribe(selected =>
                {
                    if (selected is FirstLevelNavigationItem item)
                    {
                        switch (item.Label)
                        {
                            case "Dashboard":
                                CurrentContentViewModel = dashboardVm;
                                break;
                            case "Classic Arena":
                                CurrentContentViewModel = arenaVm;
                                break;
                            case "Tag Arena":
                                CurrentContentViewModel = tagArenaVm;
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