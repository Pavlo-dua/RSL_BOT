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

            services.AddSingleton<INavigator, Navigator>();
            services.AddSingleton<ImageAnalyzer>();
            services.AddSingleton<ScreenCaptureManager>();
            services.AddSingleton<ImageResourceManager>();

            // --- НАЛАШТУВАННЯ ---
            services.AddSingleton(provider => 
                provider.GetRequiredService<IConfigurationService>().GetArenaSettings());
            services.AddSingleton<SharedSettings>();

            // --- СЦЕНАРІЇ ---
            services.AddTransient<ArenaFarmingScenario>();

            // --- VIEW MODELS ---
            services.AddSingleton<LogViewModel>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<ArenaSettingsViewModel>();
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