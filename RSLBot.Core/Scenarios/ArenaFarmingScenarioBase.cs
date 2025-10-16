using RSLBot.Core.CoreHelpers;
using RSLBot.Core.Interfaces;
using RSLBot.Core.Services;
using RSLBot.Shared.Models;
using RSLBot.Shared.Settings;
using RSLBot.Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading.Tasks;
using RSLBot.Core.Extensions;

namespace RSLBot.Core.Scenarios
{
    /// <summary>
    /// Конфігурація для координат та ресурсів арени
    /// </summary>
    public class ArenaConfiguration
    {
        // Координати прокрутки
        public int ScrollDragX { get; set; } = 542;
        public int ScrollDragStartYBottom { get; set; } = 613;
        public int ScrollDragStartYBottomUp { get; set; } = 208;
        public int ScrollDragEndYBottomUp { get; set; } = 534;
        public int ScrollDragEndYTop { get; set; } = 155;
        
        // Координати кнопки старту
        public int StartButtonX { get; set; } = 940;
        public int StartButtonWidth { get; set; } = 200;
        
        // Область для перевірки прокрутки
        public Rectangle ScrollCheckRect { get; set; } = new Rectangle(228, 540, 207, 88);
        
        // Контрольна точка для знімка
        public Point ControlSnapshotPoint { get; set; } = new Point(228, 540);
        
        // Область опонента
        public int OpponentAreaX { get; set; } = 228;
        public int OpponentAreaWidth { get; set; } = 207;
        public int OpponentAreaHeight { get; set; } = 85;
        public int OpponentAreaOffsetY { get; set; } = 21;
        
        // Шляхи до ресурсів
        public string StartButtonImagePath { get; set; } = @"Configuration\Ukr\Templates\ArenaClassic\arena_classic_start.png";
        public string AddResourcesImagePath { get; set; } = @"Configuration\ScreenDefinition\Templates\add_resources.png";
        
        // Координати для читання токенів
        public Rectangle TokenFullArea { get; set; } = new Rectangle(818, 13, 16, 20);
        public Rectangle TokenTextAreaFull { get; set; } = new Rectangle(841, 13, 52, 22);
        public Rectangle TokenTextAreaNormal { get; set; } = new Rectangle(852, 13, 41, 22);
        
        // ScreenDefinitionId для різних екранів
        public ScreenDefinitionId MainScreenId { get; set; } = ScreenDefinitionId.ClassicArena;
        public ScreenDefinitionId FreeTokensScreenId { get; set; } = ScreenDefinitionId.ClassicArenaFreeTokens;
        public ScreenDefinitionId BuyTokensScreenId { get; set; } = ScreenDefinitionId.ClassicArenaBuyTokens;
        public ScreenDefinitionId PreparingScreenId { get; set; } = ScreenDefinitionId.ClassicArenaPreparing;
        public ScreenDefinitionId DefeatScreenId { get; set; } = ScreenDefinitionId.ClassicArenaDefeat;
        public ScreenDefinitionId VictoryScreenId { get; set; } = ScreenDefinitionId.ClassicArenaVin;
        public ScreenDefinitionId FightScreenId { get; set; } = ScreenDefinitionId.ClassicArenaFight;
    }

    /// <summary>
    /// Базовий клас для всіх сценаріїв арени з загальною логікою
    /// </summary>
    public abstract class ArenaFarmingScenarioBase<T> : BaseFarmingScenario<T>
        where T : ArenaFarmingSettings
    {
        protected abstract ArenaConfiguration Configuration { get; }
        
        // Template image of the "Fight" button.
        private Bitmap? _startButtonImage;
        
        // A list to keep track of all discovered opponents in the current list.
        private readonly List<Opponent> _opponents = new List<Opponent>();
        private Bitmap? _firstOpponentSnapshot;
        
        private int TokenPurchased = 0;
        private int won = 0;
        private int lost = 0;

        protected ArenaFarmingScenarioBase(INavigator navigator, T settings, ILoggingService logger, Tools tools, ImageAnalyzer imageAnalyzer, SharedSettings sharedSettings, ImageResourceManager imageResourceManager)
            : base(navigator, settings, sharedSettings, tools, imageAnalyzer, imageResourceManager, logger)
        {
        }
        
        protected override async Task Prepare()
        {
            await base.Prepare();
            _startButtonImage = ImageResourceManager[Configuration.StartButtonImagePath];
        }

        protected override async Task Loop()
        {
            if (CancellationTokenSource.IsCancellationRequested)
            {
                return;
            }
            
            LoggingService.InfoUi($"Starting Arena scenario for {settings.TokenPurchases} runs.");

            TokenPurchased = !settings.BuyTokensWithGems ? 0 : settings.TokenPurchases == 0 ? 100 : settings.TokenPurchases;
            LoggingService.InfoUi($"Token purchases: {TokenPurchased} tokens.");
            
            var tokens = await GetTokenCount();
            LoggingService.InfoUi($"Identified {tokens}/10 tokens.");
            
            if (tokens == 0)
            {
                if (!await TryGetTokens())
                {
                    await sharedSettings.CancellationTokenSource.CancelAsync();
                    return;
                }
                
                tokens = await GetTokenCount();
                
                LoggingService.InfoUi($"Identified {tokens}/10 tokens.");
            }
            
            var needRefresh = settings.RefreshOpponentsOnStart;
            var internalNeedRefresh = false;
            
            while (true)
            {
                LoggingService.InfoUi($"Starting new cycle through opponent list.");
                _firstOpponentSnapshot = null;
                
                if (IsCancellationRequested()) break;

                if (needRefresh)
                {
                    var ca = navigator.GetScreenDefinitionById(Configuration.MainScreenId);
                    
                    if (!await navigator.IsElementVisibleAsync(ca["Refresh"]))
                    {
                        await WaitImage(ca["Refresh"], 105, 1000 * 10);
                    }

                    await Click(ca["Refresh"], ca["Refresh_dis"]);
                }

                internalNeedRefresh = true;
                
                // Крок 1: Прокрутити на самий верх
                await ScrollToTop();
                
                // Крок 2: Очистити старі дані про опонентів
                CleanupOpponents();

                // Встановити контрольний знімок початку списку замість першого опонента
                await SyncWindow();
                if (Window != null)
                {
                    using (var controlArea = Window.Clone(new Rectangle(Configuration.ControlSnapshotPoint, new Size(Configuration.OpponentAreaWidth, Configuration.OpponentAreaHeight)), Window.PixelFormat))
                    {
                        _firstOpponentSnapshot = new Bitmap(controlArea);
                    }
                }
                
                // Крок 3: Почати обробку списку по "екранам"
                int currentScrollDepth = 0; // Скільки разів прокрутили від початку (0 = перший екран)
                bool endOfListReached = false;

                while (!endOfListReached)
                {
                    if (IsCancellationRequested()) break;

                    LoggingService.Info($"Processing screen at scroll depth: {currentScrollDepth}");

                    // Крок 4: Сканувати опонентів на поточному екрані
                    await SyncWindow();
                    var newOpponents = await ScanVisibleOpponents();
                    
                    if (!newOpponents.Any())
                    {
                        LoggingService.Info("No new opponents found on this screen.");
                    }
                    else
                    {
                        LoggingService.Info($"Found {newOpponents.Count} new opponent(s) on this screen.");
                        _opponents.AddRange(newOpponents);
                        
                        // Контрольний знімок вже встановлено від початку списку (controlSnapShot)
                    }

                    // Крок 5: Битися з усіма опонентами на цьому екрані
                    var opponentsOnThisScreen = newOpponents.ToList(); // Бережемо локальну копію
                    
                    for (int i = 0; i < opponentsOnThisScreen.Count; i++)
                    {
                        if (IsCancellationRequested()) break;

                        var opponent = opponentsOnThisScreen[i];
                        
                        LoggingService.Info($"Fighting opponent {i + 1}/{opponentsOnThisScreen.Count} on screen (depth: {currentScrollDepth})");

                        // Перевірити чи опонент ще видимий перед боєм
                        await SyncWindow();
                        if (!await IsOpponentVisible(opponent.Snapshot))
                        {
                            LoggingService.Warning("Opponent is not visible before fight. Skipping.");
                            opponent.Status = FightStatus.Lost;
                            continue;
                        }

                        // Битися з опонентом
                        await FightOpponent(opponent, currentScrollDepth);

                        if (IsCancellationRequested()) break;

                        // Крок 6: Після бою перевірити чи список не скинувся повністю
                        await SyncWindow();
                        if (_firstOpponentSnapshot != null && !await IsOpponentVisible(_firstOpponentSnapshot))
                        {
                            LoggingService.WarningUi("List was completely reset (league change). Restarting from top.");
                            internalNeedRefresh = false;
                            goto restart_full_cycle;
                        }

                        // Крок 7: Прокрутити назад до потрібної глибини (список скинувся на початок після бою)
                        if (currentScrollDepth > 0)
                        {
                            LoggingService.Info($"Scrolling back to depth {currentScrollDepth} after fight...");
                            await ScrollToDepth(currentScrollDepth);
                        }
                        else
                        {
                            // Якщо на першому екрані, просто трохи почекати
                            await Task.Delay(300);
                        }
                    }

                    if (IsCancellationRequested()) break;

                    // Крок 8: Спробувати прокрутити на наступний екран
                    LoggingService.Info("Screen cleared. Attempting to scroll to next screen...");

                    await SyncWindow();
                    if (Window != null)
                    {
                        using (var areaBeforeScroll = Window.Clone(Configuration.ScrollCheckRect, Window.PixelFormat))
                        {
                            // Прокрутити вниз
                            MouseDrag(new Point(Configuration.ScrollDragX, Configuration.ScrollDragStartYBottom), new Point(Configuration.ScrollDragX, Configuration.ScrollDragEndYTop), 200, 500);
                            await Task.Delay(1000);

                            // Перевірити чи змінився екран
                            await SyncWindow();
                            if (Window != null)
                            {
                                using (var areaAfterScroll = Window.Clone(Configuration.ScrollCheckRect, Window.PixelFormat))
                                {
                                    var match = ImageAnalyzer.FindImage(areaBeforeScroll, areaAfterScroll, accuracy: 0.98);
                                    if (match != default)
                                    {
                                        LoggingService.Info("End of list reached - content did not change after scroll.");
                                        endOfListReached = true;
                                    }
                                    else
                                    {
                                        currentScrollDepth++;
                                        LoggingService.Info($"Scrolled successfully. New depth: {currentScrollDepth}");
                                    }
                                }
                            }
                        }
                    }
                }

                restart_full_cycle:
                LoggingService.Info("Finished full cycle through opponent list. Will refresh and start over.");
                
                // Перевірити токени перед новим циклом
                tokens = await GetTokenCount();
                LoggingService.Info($"Tokens remaining: {tokens}/10");
                
                if (tokens == 0)
                {
                    LoggingService.Info("No tokens left. Attempting to get more...");
                    if (!await TryGetTokens())
                    {
                        LoggingService.Info("Cannot get more tokens. Ending scenario.");
                        await sharedSettings.CancellationTokenSource.CancelAsync();
                        break;
                    }
                }

                needRefresh = internalNeedRefresh && _opponents.Any(op => op.Status == FightStatus.Lost);
            }

            LoggingService.InfoUi($"Arena scenario finished. Виграли: {won} Програли: {lost}. Коефіцієнт: {((won+lost) == 0 ? 0 : (double)won/(won+lost)*100)}%");
        }

        private async Task<bool> TryGetTokens()
        {
            var result = false;
            
            await ClickAndProcessScreens(navigator.GetScreenDefinitionById(Configuration.MainScreenId)["add_tokens"],
                [Configuration.FreeTokensScreenId, Configuration.BuyTokensScreenId],
                async definition =>
                {
                    switch (definition.Id)
                    {
                        case var id when id == Configuration.FreeTokensScreenId:
                            await Click(definition["BuyTokens"], navigator.GetScreenDefinitionById(Configuration.MainScreenId));
                            LoggingService.InfoUi("Додаткові монети Арени отримано");
                            result = true;
                            break;
                        case var id when id == Configuration.BuyTokensScreenId:
                            if (TokenPurchased > 0)
                            {
                                await Click(definition["BuyTokens"],
                                    navigator.GetScreenDefinitionById(Configuration.MainScreenId));
                                result = true;
                                
                                TokenPurchased--;

                                LoggingService.InfoUi($"Куплено разів {(settings.TokenPurchases == 0 ? 100 - TokenPurchased : settings.TokenPurchases - TokenPurchased)}");
                            }
                            break;                         
                        default:
                            return true;
                    }

                    return false;
                }, async _ => await CancellationTokenSource.CancelAsync(), 3000 );

            return result;
        }

        private async Task<int> GetTokenCount()
        {
            await SyncWindow();
            
            var full = ImageAnalyzer.FindImage(Window, ImageResourceManager[Configuration.AddResourcesImagePath], Configuration.TokenFullArea) != default;

            var text = ImageAnalyzer.FindText(Window, true, full ? Configuration.TokenTextAreaFull : Configuration.TokenTextAreaNormal);
            
           return int.Parse(text.Split('/').First());
        }

        /// <summary>
        /// Point 1: Scrolls the opponent list to the very top.
        /// </summary>
        private async Task ScrollToTop()
        {
            LoggingService.Info("Scrolling to the top of the list.");
            int maxScrolls = 5; // Safety break
            Bitmap? previousArea = null;

            for (int i = 0; i < maxScrolls; i++)
            {
                await SyncWindow();
                if (Window != null)
                {
                    var currentArea = Window.Clone(Configuration.ScrollCheckRect, Window.PixelFormat);

                    if (previousArea != null && ImageAnalyzer.FindImage(previousArea, currentArea, default, 0.95) != default)
                    {
                        LoggingService.Info("Top of the list reached.");
                        previousArea.Dispose();
                        currentArea.Dispose();
                        return;
                    }
                    
                    previousArea?.Dispose();
                    previousArea = currentArea;
                }

                MouseDrag(new Point(Configuration.ScrollDragX, Configuration.ScrollDragStartYBottomUp), new Point(Configuration.ScrollDragX, Configuration.ScrollDragEndYBottomUp), 200, 500);
            }
            
            LoggingService.WarningUi("Could not determine if top of the list was reached.");
        }

        /// <summary>
        /// Scans the screen for fight buttons and creates snapshots for new, unknown opponents.
        /// </summary>
        private async Task<List<Opponent>> ScanVisibleOpponents()
        {
            var newOpponents = new List<Opponent>();
            if (_startButtonImage == null) return newOpponents;
            
            var fightButtons = await FindAllImages(_startButtonImage);

            LoggingService.Info($"Found {fightButtons.Count} fight button(s) on screen.");

            foreach (var buttonRect in fightButtons.OrderBy(r => r.Y))
            {
                var opponentArea = new Rectangle(Configuration.OpponentAreaX, buttonRect.Y - Configuration.OpponentAreaOffsetY, Configuration.OpponentAreaWidth, Configuration.OpponentAreaHeight);

                // Перевірити чи цей опонент вже є в нашому глобальному списку
                bool isAlreadyKnown = false;
                
                foreach (var knownOpponent in _opponents)
                {
                    // Шукаємо відповідність у всьому вікні, а не тільки в opponentArea
                    // Це дозволить знайти опонента навіть якщо він трохи зсунувся
                    var matchLocation = ImageAnalyzer.FindImage(Window, knownOpponent.Snapshot, default, accuracy: 0.98);
                    
                    if (matchLocation != default)
                    {
                        // Перевірити чи цей знайдений опонент перекривається з поточною областю
                        var knownRect = new Rectangle(matchLocation.X, matchLocation.Y, Configuration.OpponentAreaWidth, Configuration.OpponentAreaHeight);
                        if (knownRect.IntersectsWith(opponentArea))
                        {
                            isAlreadyKnown = true;
                            break;
                        }
                    }
                }

                if (!isAlreadyKnown && Window != null)
                {
                    // Це новий опонент - створити знімок
                    using var opponentSnapshot = Window.Clone(opponentArea, Window.PixelFormat);
                    var newOpponent = new Opponent 
                    { 
                        Snapshot = new Bitmap(opponentSnapshot), 
                        Status = FightStatus.NotFought 
                    };
                    newOpponents.Add(newOpponent);
                    LoggingService.Info($"New opponent discovered at Y={buttonRect.Y}");
                }
            }
            
            return newOpponents;
        }

        /// <summary>
        /// Placeholder for the fight sequence.
        /// </summary>
        private async Task FightOpponent(Opponent opponent, int currentScrollDepth)
        {
            LoggingService.InfoUi("Starting fight...");
            await SyncWindow();
            var opponentRect = ImageAnalyzer.FindImage(Window, opponent.Snapshot);
            if (opponentRect == default)
            {
                LoggingService.WarningUi("Could not find opponent to fight. Skipping.");
                opponent.Status = FightStatus.Lost; // Mark as lost to avoid retries
                return;
            }

            // Find the fight button next to the opponent snapshot
            var searchArea = new Rectangle(Configuration.StartButtonX, opponentRect.Top, Configuration.StartButtonWidth, opponentRect.Height);
            var fightButtonRect = ImageAnalyzer.FindImage(Window, _startButtonImage, searchArea);

            if (fightButtonRect != default)
            {
                // Після кліку можливі різні вікна: підготовка бою, попередження/діалог, сам бій тощо.
                // Використовуємо узагальнену обробку екранів із пріоритетом у заданому порядку.
                await ClickAndProcessScreens(
                    fightButtonRect.ToPoint(),
                    [
                        Configuration.PreparingScreenId,
                        Configuration.FreeTokensScreenId,
                        Configuration.BuyTokensScreenId
                    ],
                    async screenDefinition =>
                    {
                        switch (screenDefinition.Id)
                        {
                            case var id when id == Configuration.FreeTokensScreenId:
                                // Далі ведемо стандартний сценарій запуску бою
                                await navigator.GoToScreenAsync(screenDefinition, Configuration.MainScreenId);
                                LoggingService.InfoUi("Додано безкоштовних токенів Арени");
                                LoggingService.Info($"Scrolling back to depth {currentScrollDepth} after fight...");
                                await ScrollToDepth(currentScrollDepth);
                                Click(fightButtonRect.ToPoint());
                                await ExecuteSingleRun(opponent);
                                return false; // завершити очікування після запуску підпроцесу

                            case var id when id == Configuration.BuyTokensScreenId:
                                if (TokenPurchased > 0)
                                {
                                    await Click(screenDefinition["BuyTokens"],
                                        navigator.GetScreenDefinitionById(Configuration.MainScreenId));
                                    
                                    TokenPurchased--;

                                    LoggingService.InfoUi($"Куплено разів {(settings.TokenPurchases == 0 ? 100 - TokenPurchased : settings.TokenPurchases - TokenPurchased)}");
                                    
                                    await ScrollToDepth(currentScrollDepth);
                                }
                                else
                                {
                                    await sharedSettings.CancellationTokenSource.CancelAsync();
                                }
                                return false;         

                            case var id when id == Configuration.PreparingScreenId:
                                // Якщо одразу потрапили у бій, очікуємо його завершення у підпроцесі
                                await ExecuteSingleRun(opponent);
                                return false;

                            default:
                                return true;
                        }
                    },
                    async definition =>
                    {
                        // Таймаут: спробуємо повернутись на головний екран Арени
                        //LoggingService.Info($"Timeout after clicking Fight. Current: {definition}.");
                        //await navigator.GoToScreenAsync(ScreenDefinitionId.ClassicArena);
                    },
                    timeOutMilsec: 6000,
                    interval: 500
                );
            }
            else
            {
                LoggingService.Warning("Could not find fight button for the opponent. It might have been defeated already.");
                opponent.Status = FightStatus.Won; // Assume it's already won
            }
        }

        private async Task ExecuteSingleRun(Opponent opponent)
        {
            await ProcessScreen([Configuration.DefeatScreenId, Configuration.VictoryScreenId, Configuration.PreparingScreenId],
                async screenDefinition =>
                {
                    switch (screenDefinition.Id)
                    {
                        case var id when id == Configuration.DefeatScreenId:
                            opponent.Status = FightStatus.Lost;
                            LoggingService.InfoUi($"Програли :(. Разів: {++lost}");
                            break;
                        case var id when id == Configuration.VictoryScreenId:
                            LoggingService.InfoUi($"Перемога! Разів: {++won}");
                            opponent.Status = FightStatus.Won;
                            break;
                        case var id when id == Configuration.PreparingScreenId:
                            if (await navigator.IsElementVisibleAsync(screenDefinition["UncheckedAuto"]!))
                            {
                                await Click(screenDefinition["UncheckedAuto"]!, screenDefinition["CheckedAuto"]!);
                            }
                            
                            await Click(screenDefinition["Start"]!, navigator.GetScreenDefinitionById(Configuration.FightScreenId));
                            
                            opponent.Status = FightStatus.Fighting;
                            LoggingService.Info($"Fight status {opponent.Status}.");
                            
                            return true;
                        default:
                            return true;
                    }

                    LoggingService.Info($"Fight status {opponent.Status}.");
                    await navigator.GoToScreenAsync(screenDefinition, Configuration.MainScreenId);
                    
                    return false;
                }, async definition =>
                {
                    if (definition.Id == Configuration.FightScreenId)
                    {
                        await navigator.GoToScreenAsync(definition, Configuration.MainScreenId);
                        
                        LoggingService.WarningUi($"Fight time out.");
                        
                        return;
                    }
                    
                    LoggingService.Info($"Fight status unknown. Definition: {definition}.");
                });
        }

        /// <summary>
        /// Прокручує список до заданої глибини від початку.
        /// Глибина 0 = перший екран (без прокрутки), 1 = один скрол, 2 = два скроли і т.д.
        /// </summary>
        private async Task ScrollToDepth(int targetDepth)
        {
            if (targetDepth <= 0)
            {
                return; // Вже на початку
            }

            // Спочатку прокрутити на самий верх
            await ScrollToTop();

            // Потім прокрутити вниз потрібну кількість разів
            for (int i = 0; i < targetDepth; i++)
            {
                LoggingService.Info($"Scrolling {i + 1}/{targetDepth}...");
                MouseDrag(new Point(Configuration.ScrollDragX, Configuration.ScrollDragStartYBottom), new Point(Configuration.ScrollDragX, Configuration.ScrollDragEndYTop), 200, 500);
                await Task.Delay(800); // Дати час на прокрутку
            }

            LoggingService.Info($"Reached target depth: {targetDepth}");
        }

        /// <summary>
        /// Scrolls down the list until the specified opponent's snapshot is visible.
        /// </summary>
        private async Task ScrollToOpponent(Opponent opponent)
        {
            var scrollAttempts = 0;
            
            while (!await IsOpponentVisible(opponent.Snapshot))
            {
                if (scrollAttempts++ > 3)
                {
                    LoggingService.Error("Failed to scroll to opponent. Aborting this run.");
                    throw new InvalidOperationException("Could not find opponent after scrolling.");
                }
                
                // Perform a standard scroll down from the bottom
                MouseDrag(new Point(Configuration.ScrollDragX, Configuration.ScrollDragStartYBottom), new Point(Configuration.ScrollDragX, Configuration.ScrollDragEndYTop), 200, 500);
                await Task.Delay(800);
            }
        }

        #region Helper Methods

        private async Task<bool> IsOpponentVisible(Bitmap snapshot)
        {
            if (snapshot == null) return false;
            
            await SyncWindow();
            
            return ImageAnalyzer.FindImage(Window, snapshot, accuracy: 0.98) != default;
        }

        private async Task<List<Opponent>> GetVisibleUnfoughtOpponents()
        {
            var result = new List<Opponent>();
            await SyncWindow();
            foreach (var opponent in _opponents.Where(o => o.Status == FightStatus.NotFought))
            {
                if (ImageAnalyzer.FindImage(Window, opponent.Snapshot, accuracy: 0.95) != default)
                {
                    result.Add(opponent);
                }
            }
            return result;
        }
        
        private async Task<Opponent> FindLastVisibleOpponentOnScreen()
        {
            var result = new List<Opponent>();
            await SyncWindow();
            // Find ANY opponent, not just unfought ones, to get the last item on screen
            foreach (var opponent in _opponents) 
            {
                if (ImageAnalyzer.FindImage(Window, opponent.Snapshot, accuracy: 0.95) != default)
                {
                    result.Add(opponent);
                }
            }
            if (!result.Any()) return null;

            return result.OrderByDescending(o => ImageAnalyzer.FindImage(Window, o.Snapshot).Y).First();
        }

        private void CleanupOpponents()
        {
            _opponents.ForEach(o => o.Dispose());
            _opponents.Clear();
            _firstOpponentSnapshot?.Dispose();
            _firstOpponentSnapshot = null;
        }

        public void Dispose()
        {
            CleanupOpponents();
        }

        #endregion
    }

    /// <summary>
    /// Моделі для сценаріїв арени
    /// </summary>
    public enum FightStatus
    {
        NotFought,
        Fighting,
        Won,
        Lost
    }

    public class Opponent : IDisposable
    {
        /// <summary>
        /// A unique bitmap snapshot of the opponent's area.
        /// </summary>
        public Bitmap Snapshot { get; set; }

        /// <summary>
        /// The status of the fight against this opponent.
        /// </summary>
        public FightStatus Status { get; set; }

        public void Dispose()
        {
            // Safely dispose the bitmap to free up memory.
            Snapshot?.Dispose();
            Snapshot = null;
        }
    }
}
