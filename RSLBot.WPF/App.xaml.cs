using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RSLBot.Core.Interfaces;
using RSLBot.Core.Services;
using RSLBot.WPF.Services;
using RSLBot.WPF.ViewModels;
using RSLBot.WPF.ViewModels.Tabs;
using System.Windows;
using ReactiveUI;
using System.Reactive.Concurrency;
using RSLBot.Core.CoreHelpers;
using RSLBot.Shared.Settings;
using RSLBot.Core.Scenarios.ArenaClassic;
using RSLBot.Core.Scenarios.ArenaTag;
using RSLBot.Core.Scenarios.Dungeons.Minotaur;
using RSLBot.Core.Scenarios.Dungeons.Twins;

namespace RSLBot.WPF
{
    public partial class App : Application
    {
        private readonly IHost host;

        public App()
        {
            RxApp.MainThreadScheduler = new DispatcherScheduler(Current.Dispatcher);

            host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    ConfigureServices(services);
                })
                .Build();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // --- СЕРВІСИ ---
            services.AddSingleton<IUILoggingBridge, UILoggingBridge>();
            services.AddSingleton<ILoggingService, SerilogLoggingService>();
            services.AddSingleton<IConfigurationService, ConfigurationService>();
            services.AddSingleton<SettingsService>();

            services.AddSingleton<INavigator, Navigator>();
            services.AddSingleton<ImageAnalyzer>();
            services.AddSingleton<ScreenCaptureManager>();
            services.AddSingleton<ImageResourceManager>();

            // --- НАЛАШТУВАННЯ ---
            services.AddSingleton<SharedSettings>();
            
            // Окремі налаштування для кожного сценарію
            // Для Classic Arena
            services.AddSingleton<ArenaFarmingSettings>(sp => new ArenaFarmingSettings());
            
            // Для Twins
            services.AddSingleton<TwinsFarmingSettings>(sp => new TwinsFarmingSettings());
            
            services.AddSingleton<MinotaurFarmingSettings>(sp => new MinotaurFarmingSettings());
            
            // --- СЦЕНАРІЇ ---
            services.AddSingleton<ArenaFarmingScenario>(sp =>
            {
                var settings = new ArenaFarmingSettings(); // Окремі налаштування для Classic Arena
                return new ArenaFarmingScenario(
                    sp.GetRequiredService<INavigator>(),
                    settings,
                    sp.GetRequiredService<ILoggingService>(),
                    sp.GetRequiredService<Tools>(),
                    sp.GetRequiredService<ImageAnalyzer>(),
                    sp.GetRequiredService<SharedSettings>(),
                    sp.GetRequiredService<ImageResourceManager>()
                );
            });
            
            services.AddSingleton<ArenaTagFarmingScenario>(sp =>
            {
                var settings = new ArenaFarmingSettings(); // Окремі налаштування для Tag Arena
                return new ArenaTagFarmingScenario(
                    sp.GetRequiredService<INavigator>(),
                    settings,
                    sp.GetRequiredService<ILoggingService>(),
                    sp.GetRequiredService<Tools>(),
                    sp.GetRequiredService<ImageAnalyzer>(),
                    sp.GetRequiredService<SharedSettings>(),
                    sp.GetRequiredService<ImageResourceManager>()
                );
            });
            
            services.AddSingleton<TwinsScenario>(sp =>
            {
                var settings = new TwinsFarmingSettings(); // Окремі налаштування для Twins
                return new TwinsScenario(
                    sp.GetRequiredService<INavigator>(),
                    settings,
                    sp.GetRequiredService<ILoggingService>(),
                    sp.GetRequiredService<Tools>(),
                    sp.GetRequiredService<ImageAnalyzer>(),
                    sp.GetRequiredService<SharedSettings>(),
                    sp.GetRequiredService<ImageResourceManager>()
                );
            });
            
            services.AddSingleton<MinotaurScenario>(sp =>
            {
                var settings = new MinotaurFarmingSettings(); // Окремі налаштування для Twins
                return new MinotaurScenario(
                    sp.GetRequiredService<INavigator>(),
                    settings,
                    sp.GetRequiredService<ILoggingService>(),
                    sp.GetRequiredService<Tools>(),
                    sp.GetRequiredService<ImageAnalyzer>(),
                    sp.GetRequiredService<SharedSettings>(),
                    sp.GetRequiredService<ImageResourceManager>()
                );
            });
            
            // Реєстрація всіх сценаріїв як IScenario для DashboardViewModel
            services.AddSingleton<IEnumerable<IScenario>>(sp => new List<IScenario>
            {
                sp.GetRequiredService<ArenaFarmingScenario>(),
                sp.GetRequiredService<ArenaTagFarmingScenario>(),
                sp.GetRequiredService<TwinsScenario>(),
                sp.GetRequiredService<MinotaurScenario>()
            });

            // --- VIEW MODELS ---
            services.AddSingleton<LogViewModel>();
            services.AddSingleton<DashboardViewModel>();
            
            // Окремі ViewModel з власними налаштуваннями
            services.AddSingleton<ArenaSettingsViewModel>(sp =>
            {
                var scenario = sp.GetRequiredService<ArenaFarmingScenario>();
                var settings = new ArenaFarmingSettings(); // Окремі налаштування
                return new ArenaSettingsViewModel(
                    scenario,
                    settings,
                    sp.GetRequiredService<Tools>(),
                    sp.GetRequiredService<ScreenCaptureManager>(),
                    sp.GetRequiredService<SharedSettings>(),
                    sp.GetRequiredService<SettingsService>()
                );
            });
            
            services.AddSingleton<TagArenaSettingsViewModel>(sp =>
            {
                var scenario = sp.GetRequiredService<ArenaTagFarmingScenario>();
                var settings = new ArenaFarmingSettings(); // Окремі налаштування
                return new TagArenaSettingsViewModel(
                    scenario,
                    settings,
                    sp.GetRequiredService<Tools>(),
                    sp.GetRequiredService<ScreenCaptureManager>(),
                    sp.GetRequiredService<SharedSettings>(),
                    sp.GetRequiredService<SettingsService>()
                );
            });
            
            services.AddSingleton<TwinsSettingsViewModel>(sp =>
            {
                var scenario = sp.GetRequiredService<TwinsScenario>();
                var settings = new TwinsFarmingSettings(); // Окремі налаштування
                return new TwinsSettingsViewModel(
                    scenario,
                    settings,
                    sp.GetRequiredService<Tools>(),
                    sp.GetRequiredService<ScreenCaptureManager>(),
                    sp.GetRequiredService<SharedSettings>(),
                    sp.GetRequiredService<SettingsService>()
                );
            });
            
            services.AddSingleton<MinotaurSettingsViewModel>(sp =>
            {
                var scenario = sp.GetRequiredService<MinotaurScenario>();
                var settings = new MinotaurFarmingSettings(); // Окремі налаштування
                return new MinotaurSettingsViewModel(
                    scenario,
                    settings,
                    sp.GetRequiredService<Tools>(),
                    sp.GetRequiredService<ScreenCaptureManager>(),
                    sp.GetRequiredService<SharedSettings>(),
                    sp.GetRequiredService<SettingsService>()
                );
            });
            
            services.AddSingleton<SettingsViewModel>();
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<MainWindow>();

            services.AddSingleton<Tools>();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await host.StartAsync();

            var mainWindow = host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            using (host)
            {
                await host.StopAsync();
            }
            base.OnExit(e);
        }
    }
}