using MaterialDesignExtensions.Model;
using MaterialDesignThemes.Wpf;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;

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

        public MainViewModel(LogViewModel logViewModel, DashboardViewModel dashboardVm, ArenaSettingsViewModel arenaVm)
        {
            LogViewModel = logViewModel;
            NavigationItems = new List<INavigationItem>
            {
                new FirstLevelNavigationItem { Label = "Dashboard", Icon = PackIconKind.ViewDashboard, IsSelected = true },
                new FirstLevelNavigationItem { Label = "Arena", Icon = PackIconKind.ShieldHalfFull}
            };

            // Підписуємося на зміни SelectedItem і оновлюємо контент
            this.WhenAnyValue(x => x.SelectedItem)
                .Subscribe(selected =>
                {
                    if (selected is FirstLevelNavigationItem item)
                    {
                        if (item.Label == "Dashboard")
                        {
                            CurrentContentViewModel = dashboardVm;
                        }
                        else if (item.Label == "Arena")
                        {
                            CurrentContentViewModel = arenaVm;
                        }
                    }
                });

            // Встановлюємо початковий вибраний елемент.
            // Це автоматично викличе підписку вище і встановить початковий контент.
            SelectedItem = NavigationItems.OfType<FirstLevelNavigationItem>().FirstOrDefault(item => item.IsSelected);
        }
    }
}