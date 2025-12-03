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
using RSLBot.Core.Scenarios.Dungeons.Shogun;
using RSLBot.Core.Scenarios.Dungeons.SandDevil;

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

            // Arena & Tag Arena (Explicit instances to separate them)
            var arenaSettings = new ArenaFarmingSettings();
            var tagArenaSettings = new ArenaFarmingSettings();

            // Register other settings as Singletons
            services.AddSingleton<TwinsFarmingSettings>();
            services.AddSingleton<MinotaurFarmingSettings>();
            services.AddSingleton<ShogunFarmingSettings>();
            services.AddSingleton<SandDevilFarmingSettings>();

            // --- СЦЕНАРІЇ ---
            services.AddSingleton<ArenaFarmingScenario>(sp =>
            {
                return new ArenaFarmingScenario(
                    sp.GetRequiredService<INavigator>(),
                    arenaSettings, // Explicit instance
                    sp.GetRequiredService<ILoggingService>(),
                    sp.GetRequiredService<Tools>(),
                    sp.GetRequiredService<ImageAnalyzer>(),
                    sp.GetRequiredService<SharedSettings>(),
                    sp.GetRequiredService<ImageResourceManager>()
                );
            });

            services.AddSingleton<ArenaTagFarmingScenario>(sp =>
            {
                return new ArenaTagFarmingScenario(
                    sp.GetRequiredService<INavigator>(),
                    tagArenaSettings, // Explicit instance
                    sp.GetRequiredService<ILoggingService>(),
                    sp.GetRequiredService<Tools>(),
                    sp.GetRequiredService<ImageAnalyzer>(),
                    sp.GetRequiredService<SharedSettings>(),
                    sp.GetRequiredService<ImageResourceManager>()
                );
            });

            services.AddSingleton<TwinsScenario>(sp =>
            {
                return new TwinsScenario(
                    sp.GetRequiredService<INavigator>(),
                    sp.GetRequiredService<TwinsFarmingSettings>(),
                    sp.GetRequiredService<ILoggingService>(),
                    sp.GetRequiredService<Tools>(),
                    sp.GetRequiredService<ImageAnalyzer>(),
                    sp.GetRequiredService<SharedSettings>(),
                    sp.GetRequiredService<ImageResourceManager>()
                );
            });

            services.AddSingleton<MinotaurScenario>(sp =>
            {
                return new MinotaurScenario(
                    sp.GetRequiredService<INavigator>(),
                    sp.GetRequiredService<MinotaurFarmingSettings>(),
                    sp.GetRequiredService<ILoggingService>(),
                    sp.GetRequiredService<Tools>(),
                    sp.GetRequiredService<ImageAnalyzer>(),
                    sp.GetRequiredService<SharedSettings>(),
                    sp.GetRequiredService<ImageResourceManager>()
                );
            });

            services.AddSingleton<ShogunScenario>(sp =>
            {
                return new ShogunScenario(
                    sp.GetRequiredService<INavigator>(),
                    sp.GetRequiredService<ShogunFarmingSettings>(),
                    sp.GetRequiredService<ILoggingService>(),
                    sp.GetRequiredService<Tools>(),
                    sp.GetRequiredService<ImageAnalyzer>(),
                    sp.GetRequiredService<SharedSettings>(),
                    sp.GetRequiredService<ImageResourceManager>()
                );
            });

            services.AddSingleton<SandDevilScenario>(sp =>
            {
                return new SandDevilScenario(
                    sp.GetRequiredService<INavigator>(),
                    sp.GetRequiredService<SandDevilFarmingSettings>(),
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
                sp.GetRequiredService<MinotaurScenario>(),
                sp.GetRequiredService<ShogunScenario>(),
                sp.GetRequiredService<SandDevilScenario>()
            });

            // --- VIEW MODELS ---
            services.AddSingleton<LogViewModel>();
            services.AddSingleton<DashboardViewModel>();

            // Окремі ViewModel з власними налаштуваннями
            services.AddSingleton<ArenaSettingsViewModel>(sp =>
            {
                var scenario = sp.GetRequiredService<ArenaFarmingScenario>();
                return new ArenaSettingsViewModel(
                    scenario,
                    arenaSettings, // Explicit instance
                    sp.GetRequiredService<Tools>(),
                    sp.GetRequiredService<ScreenCaptureManager>(),
                    sp.GetRequiredService<SharedSettings>(),
                    sp.GetRequiredService<SettingsService>()
                );
            });

            services.AddSingleton<TagArenaSettingsViewModel>(sp =>
            {
                var scenario = sp.GetRequiredService<ArenaTagFarmingScenario>();
                return new TagArenaSettingsViewModel(
                    scenario,
                    tagArenaSettings, // Explicit instance
                    sp.GetRequiredService<Tools>(),
                    sp.GetRequiredService<ScreenCaptureManager>(),
                    sp.GetRequiredService<SharedSettings>(),
                    sp.GetRequiredService<SettingsService>()
                );
            });

            services.AddSingleton<TwinsSettingsViewModel>(sp =>
            {
                var scenario = sp.GetRequiredService<TwinsScenario>();
                return new TwinsSettingsViewModel(
                    scenario,
                    sp.GetRequiredService<TwinsFarmingSettings>(),
                    sp.GetRequiredService<Tools>(),
                    sp.GetRequiredService<ScreenCaptureManager>(),
                    sp.GetRequiredService<SharedSettings>(),
                    sp.GetRequiredService<SettingsService>()
                );
            });

            services.AddSingleton<MinotaurSettingsViewModel>(sp =>
            {
                var scenario = sp.GetRequiredService<MinotaurScenario>();
                return new MinotaurSettingsViewModel(
                    scenario,
                    sp.GetRequiredService<MinotaurFarmingSettings>(),
                    sp.GetRequiredService<Tools>(),
                    sp.GetRequiredService<ScreenCaptureManager>(),
                    sp.GetRequiredService<SharedSettings>(),
                    sp.GetRequiredService<SettingsService>()
                );
            });

            services.AddSingleton<ShogunSettingsViewModel>(sp =>
            {
                var scenario = sp.GetRequiredService<ShogunScenario>();
                return new ShogunSettingsViewModel(
                    scenario,
                    sp.GetRequiredService<ShogunFarmingSettings>(),
                    sp.GetRequiredService<Tools>(),
                    sp.GetRequiredService<ScreenCaptureManager>(),
                    sp.GetRequiredService<SharedSettings>(),
                    sp.GetRequiredService<SettingsService>()
                );
            });

            services.AddSingleton<SandDevilSettingsViewModel>(sp =>
            {
                var scenario = sp.GetRequiredService<SandDevilScenario>();
                return new SandDevilSettingsViewModel(
                    scenario,
                    sp.GetRequiredService<SandDevilFarmingSettings>(),
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