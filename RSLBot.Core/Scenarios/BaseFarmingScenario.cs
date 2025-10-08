using RSLBot.Core.CoreHelpers;
using RSLBot.Core.Interfaces;
using RSLBot.Core.Services;
using RSLBot.Shared.Interfaces;
using RSLBot.Shared.Models;
using System.Drawing;
using RSLBot.Core.Extensions;

namespace RSLBot.Core.Scenarios
{
    using RSLBot.Shared.Settings;

    /// <summary>
    /// Абстрактний базовий клас для всіх сценаріїв фарму.
    /// Визначає загальну структуру та логіку виконання.
    /// </summary>
    public abstract class BaseFarmingScenario<T>(
        INavigator navigator,
        T settings,
        SharedSettings sharedSettings,
        Tools tools,
        ImageAnalyzer imageAnalyzer,
        ImageResourceManager imageResourceManager,
        ILoggingService loggingService)
        : Manipulation(tools, sharedSettings, imageAnalyzer, imageResourceManager, loggingService), IScenario
        where T : IScenarioSettings
    {
        protected readonly INavigator navigator = navigator;
        protected readonly ILoggingService LoggingService = loggingService;
        protected readonly T settings = settings;
        private readonly Tools tools = tools;
        protected readonly SharedSettings sharedSettings = sharedSettings;

        protected CancellationTokenSource CancellationTokenSource => sharedSettings.CancellationTokenSource;

        /// <summary>
        /// Кожен дочірній сценарій визначає свій головний екран, з якого починається фарм.
        /// </summary>
        protected abstract ScreenDefinitionId MainFarmingScreenId { get; }

        public abstract IScenario.ScenarioId Id { get; }

        /// <summary>
        /// Переходить на стартовий екран фарму.
        /// </summary>
        protected virtual Task Prepare()
        {
            tools.Init();

            return navigator.GoToScreenAsync(MainFarmingScreenId);
        }

        private async Task MonitoringUnexpectedEvent()
        {
	        while (!CancellationTokenSource.IsCancellationRequested)
	        {
		        await Task.Delay(TimeSpan.FromSeconds(15), CancellationTokenSource.Token).ConfigureAwait(false);
		        var screen = await navigator.GetCurrentStateAsync();

		        switch (screen.Id)
		        {
			        case ScreenDefinitionId.NeedUpgradeGame:
				        await CancellationTokenSource.CancelAsync();
				        break;
			        case ScreenDefinitionId.ConnectionIssue:
				        Click(screen["retry"].Area.ToPoint());
				        break;
			        case ScreenDefinitionId.TechWork:
				        //await CancellationTokenSource.CancelAsync();
				        LoggingService.Info("Зафіксовані технічні роботи");
				        break;
			        default:
				        break;
		        }
	        }
        }

        private async Task CloseAllPopUp()
        {
	        await SyncWindow();

	        while (true)
	        {
		        var close = ImageAnalyzer.FindImage(Window,
			        ImageResourceManager["Configuration\\ScreenDefinition\\Templates\\popup_close.png"]);

		        if (close != default)
		        {
			        Click(close.ToPoint());
			        await Task.Delay(3000, CancellationTokenSource.Token);
			        await SyncWindow();		        
		        }
		        else
		        {
			        break;
		        }
	        }
        }
        
        /// <summary>
        /// Основний цикл, що виконує забіги.
        /// </summary>
        protected abstract Task Loop();

        /// <inheritdoc />
        public async Task ExecuteAsync()
        {
	        sharedSettings.CancellationTokenSource = new CancellationTokenSource();
	        
	        await CloseAllPopUp();
	        
            await Prepare();

            _ = MonitoringUnexpectedEvent();

            await Loop();
        }

        protected async Task ProcessScreen(List<ScreenDefinitionId> screenDefinitionIds, Func<ScreenDefinition, Task<bool>> processScreen, Func<ScreenDefinition, Task>? timeOutBack = null, int timeOutMilsec = 1000*60*5, int interval = 1000)
        {
            var timeout = DateTime.Now + TimeSpan.FromMilliseconds(timeOutMilsec);

            while (true)
            {
                if (timeout < DateTime.Now)
                {
                    if (timeOutBack != null)
                    {
                        await timeOutBack(await navigator.GetCurrentStateAsync());
                    }
                    
                    break;
                }
                
                var screen = await navigator.GetCurrentStateAsync(screenDefinitionIds);
                
                if (!await processScreen(screen))
                {
                    break;
                }
                
                await Task.Delay(interval);
            }
        }

		/// <summary>
		/// Виконує клік по точці та одразу запускає обробку екранів у заданому порядку.
		/// Порядок екранів у <paramref name="screenDefinitionIds"/> зберігається під час пошуку.
		/// </summary>
		/// <param name="clickPoint">Координати кліку.</param>
		/// <param name="screenDefinitionIds">Список можливих екранів після кліку (в порядку пріоритету).</param>
		/// <param name="processScreen">Обробник знайденого екрану. Повернути false, щоб завершити очікування.</param>
		/// <param name="timeOutBack">Опціонально: колбек при таймауті очікування.</param>
		/// <param name="timeOutMilsec">Таймаут очікування.</param>
		/// <param name="interval">Інтервал між перевірками.</param>
		protected async Task ClickAndProcessScreens(Point clickPoint, List<ScreenDefinitionId> screenDefinitionIds, Func<ScreenDefinition, Task<bool>> processScreen, Func<ScreenDefinition, Task>? timeOutBack = null, int timeOutMilsec = 1000*60*5, int interval = 1000)
		{
			await SyncWindow();
			
			var attempts = 0;
			var maxAttempts = 3;
			var perAttemptTimeout = Math.Max(2000, timeOutMilsec / maxAttempts);
			while (true)
			{
				Click(clickPoint);
				var completed = false;
				await ProcessScreen(screenDefinitionIds,
					async def =>
					{
						var cont = await processScreen(def);
						if (!cont) completed = true;
						return cont;
					},
					async current =>
					{
						// Таймаут однієї спроби, не фінальний – просто вихід зі спроби
						if (timeOutBack != null)
						{
							await timeOutBack(current);
						}
					},
					timeOutMilsec: perAttemptTimeout,
					interval: interval);

				if (completed)
				{
					break;
				}

				attempts++;
				if (attempts >= maxAttempts)
				{
					break;
				}
			}
		}

		/// <summary>
		/// Перевантаження: клік по елементу з подальшою обробкою можливих екранів у заданому порядку.
		/// </summary>
		protected async Task ClickAndProcessScreens(UIElement element, List<ScreenDefinitionId> screenDefinitionIds, Func<ScreenDefinition, Task<bool>> processScreen, Func<ScreenDefinition, Task>? timeOutBack = null, int timeOutMilsec = 1000*60*5, int interval = 1000)
		{
			await SyncWindow();
			var clickElement = ImageAnalyzer.FindImage(Window, ImageResourceManager[element.ImageTemplatePath], element.Area);
			if (clickElement == default)
			{
				LoggingService.Error($"Could not find element to click: {element.Name}");
				return;
			}
			await ClickAndProcessScreens(clickElement.ToPoint(), screenDefinitionIds, processScreen, timeOutBack, timeOutMilsec, interval);
		}
    }
}
