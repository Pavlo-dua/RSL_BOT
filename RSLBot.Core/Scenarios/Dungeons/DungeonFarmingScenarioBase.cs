using RSLBot.Core.CoreHelpers;
using RSLBot.Core.Interfaces;
using RSLBot.Core.Services;
using RSLBot.Shared.Models;
using System.Drawing;
using RSLBot.Core.Extensions;

namespace RSLBot.Core.Scenarios.Dungeons
{
    using RSLBot.Shared.Interfaces;
    using RSLBot.Shared.Settings;

    /// <summary>
    /// Конфігурація для координат та ресурсів данджонів
    /// </summary>
    public class DungeonConfiguration
    {
        // Координати прокрутки
        public int ScrollDragX { get; set; } = 542;
        public int ScrollDragStartYBottom { get; set; } = 613;
        public int ScrollDragEndYTop { get; set; } = 155;
        
        // ScreenDefinitionId для різних екранів
        public ScreenDefinitionId MainScreenId { get; set; }
        public ScreenDefinitionId PreparingScreenId { get; set; }
        public ScreenDefinitionId BuyTokensScreenId { get; set; }
        public ScreenDefinitionId VictoryScreenId { get; set; }
        public ScreenDefinitionId DefeatScreenId { get; set; }
        
        // Інші налаштування
        public string KeysImageElementName { get; set; } = "keys";
        public string FightImageElementName { get; set; } = "fight";
        public int MaxScrollAttempts { get; set; } = 5;
        
        // Функція для отримання області перевірки (кожен данджон може мати свою)
        public Func<Rectangle>? GetCheckArea { get; set; }
    }

    /// <summary>
    /// Базовий клас для всіх сценаріїв данджонів з загальною логікою
    /// </summary>
    public abstract class DungeonFarmingScenarioBase<T> : BaseFarmingScenario<T>
        where T : IScenarioSettings
    {
        protected abstract DungeonConfiguration Configuration { get; }
        
        protected DungeonFarmingScenarioBase(INavigator navigator, T settings, ILoggingService logger, Tools tools, ImageAnalyzer imageAnalyzer, SharedSettings sharedSettings, ImageResourceManager imageResourceManager)
            : base(navigator, settings, sharedSettings, tools, imageAnalyzer, imageResourceManager, logger)
        {
        }
        
        protected override async Task Prepare()
        {
            await base.Prepare();
        }
        
        protected override async Task Loop()
        {
            if (CancellationTokenSource.IsCancellationRequested)
            {
                return;
            }
            
            LoggingService.InfoUi($"Starting Dungeon scenario.");
            
            var countOfDefeat = 0;
            var countOfVin = 0;
            
            var tokens = await GetTokenCount();

            if (tokens == 0)
            {
                LoggingService.InfoUi("Немає ключів, тому згортаємося :(");
                return;
            }

            await ScrollToEnd();
            
            var fights = await FindAllImages(ImageResourceManager[MainScreenDefinition[Configuration.FightImageElementName].ImageTemplatePath]);
            var fight = fights.OrderBy(f => f.Y).Last();
            
            await ClickAndProcessScreens(
                fight.ToPoint(),
                [
                    Configuration.PreparingScreenId,
                    Configuration.BuyTokensScreenId,
                    Configuration.VictoryScreenId,
                    Configuration.DefeatScreenId
                ],
                async (screenDefinition, timeoutReset) =>
                {
                    switch (screenDefinition.Id)
                    {
                        case var id when id == Configuration.PreparingScreenId:
                            await ProcessPreparingScreen(screenDefinition);
                            return true;
                        case var id when id == Configuration.BuyTokensScreenId:
                            await ProcessBuyTokensScreen(screenDefinition);
                            return false; 
                        case var id when id == Configuration.VictoryScreenId:
                            countOfVin++;
                            LoggingService.InfoUi($"Перемога! Разів: {countOfVin}");
                            timeoutReset();
                            await Click(screenDefinition["Rerun"]);
                            return true;
                        case var id when id == Configuration.DefeatScreenId:
                            countOfDefeat++;
                            LoggingService.InfoUi($"Програли :(. Разів: {countOfDefeat}");
                            if (await ProcessDefeatScreen(screenDefinition, countOfDefeat, timeoutReset))
                            {
                                return true;
                            }
                            await sharedSettings.CancellationTokenSource.CancelAsync();
                            return false;
                        default:
                            return true;
                    }
                },
                async definition =>
                {
                    await HandleTimeout(definition);
                },
                600000
            );
            
            LoggingService.InfoUi($"Dungeon scenario finished. Виграли: {countOfVin} Програли: {countOfDefeat}. Коефіцієнт: {((countOfVin+countOfDefeat) == 0 ? 0 : (double)countOfVin/(countOfVin+countOfDefeat)*100)}%");
        }
        
        /// <summary>
        /// Обробка екрану підготовки бою
        /// </summary>
        protected virtual async Task ProcessPreparingScreen(ScreenDefinition screenDefinition)
        {
            LoggingService.InfoUi($"Поїхали...");
            await Click(screenDefinition["Start"]);
        }
        
        /// <summary>
        /// Обробка екрану покупки токенів
        /// </summary>
        protected virtual async Task ProcessBuyTokensScreen(ScreenDefinition screenDefinition)
        {
            LoggingService.InfoUi($"Схоже більше немає ключів, тому зупиняємося");
            await sharedSettings.CancellationTokenSource.CancelAsync();
        }
        
        /// <summary>
        /// Обробка екрану поразки
        /// </summary>
        protected virtual async Task<bool> ProcessDefeatScreen(ScreenDefinition screenDefinition, int countOfDefeat, Action timeoutReset)
        {
            // Базова реалізація - переназначається в підкласах
            return false;
        }
        
        protected virtual async Task HandleTimeout(ScreenDefinition definition)
        {
            LoggingService.WarningUi("Схоже бій затягнувся, тому зупиняємося...");
            LoggingService.Error("Зупинка бою не реалізована!");
        }
        
        private async Task<int> GetTokenCount()
        {
            await SyncWindow();

            var keys = await WaitImage(MainScreenDefinition[Configuration.KeysImageElementName]);

            var keysTextArea = new Rectangle(keys.X - 36, keys.Y + 3, 36, keys.Height - 5);

            var text = ImageAnalyzer.FindText(Window, true, keysTextArea);
                
            return int.Parse(text.Split('/').First());
        }
        
        private async Task ScrollToEnd()
        {
            await SyncWindow();
            
            for (var def = 0; def < Configuration.MaxScrollAttempts; def++)
            {
                var checkArea = await GetCheckArea();
                using var areaBeforeScroll = Window.Clone(checkArea, Window.PixelFormat);
                
                // Прокрутити вниз
                MouseDrag(new Point(Configuration.ScrollDragX, Configuration.ScrollDragStartYBottom),
                    new Point(Configuration.ScrollDragX, Configuration.ScrollDragEndYTop), 200, 500);
                await Task.Delay(1000);

                // Перевірити чи змінився екран
                await SyncWindow();
                
                using var areaAfterScroll = Window.Clone(checkArea, Window.PixelFormat);
                var match = ImageAnalyzer.FindImage(areaBeforeScroll, areaAfterScroll, accuracy: 0.98);
                
                if (match != default)
                {
                    LoggingService.Info("End of list reached - content did not change after scroll.");
                    break;
                }
            }
        }

        private async Task<Rectangle> GetCheckArea()
        {
            if (Configuration.GetCheckArea != null)
            {
                return Configuration.GetCheckArea();
            }
            
            var fights = await FindAllImages(ImageResourceManager[MainScreenDefinition[Configuration.FightImageElementName].ImageTemplatePath]);
            var fight = fights.OrderBy(f => f.Y).Last();

            return new Rectangle(325, fight.Y, 770, 50);
        }
    }
}
