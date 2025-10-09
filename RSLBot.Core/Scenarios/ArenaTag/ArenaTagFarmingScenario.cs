using RSLBot.Core.CoreHelpers;
using RSLBot.Core.Interfaces;
using RSLBot.Core.Services;
using RSLBot.Shared.Models;
using RSLBot.Shared.Settings;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading.Tasks;
using RSLBot.Core.Extensions;

namespace RSLBot.Core.Scenarios.ArenaTagClassic
{
    public partial class ArenaTagFarmingScenario : BaseFarmingScenario<ArenaFarmingSettings>
    {
        // Note: ILoggingService is available as 'logger' in the base class 'Manipulation'
        protected override ScreenDefinitionId MainFarmingScreenId => ScreenDefinitionId.ClassicArena;
        public override IScenario.ScenarioId Id => IScenario.ScenarioId.Arena;

        // EN: Template image of the "Fight" button.
        private Bitmap _startButtonImage;
        
        // EN: A list to keep track of all discovered opponents in the current list.
        private readonly List<Opponent> _opponents = new List<Opponent>();
        private Bitmap? _firstOpponentSnapshot;

        /// <summary>
        /// Контрольний знимок для ідентифікації того, що список не оновився повністю.
        /// </summary>
        private Point controlSnapShot = new (228, 540);

        // User-defined constants        
        private const int ScrollDragX = 542;
        private const int ScrollDragStartYBottom = 613;
        private const int ScrollDragStartYBottomUp = 208;
        private const int ScrollDragEndYBottomUp = 534;
        private const int ScrollDragEndYTop = 155;
        private const int startBottonX = 940;
        private const int startBottonWigth = 200;
        private readonly Rectangle _scrollCheckRect = new Rectangle(228, 540, 207, 88);
        private int TokenPuchesed = 0;
        
        public ArenaTagFarmingScenario(INavigator navigator, ArenaFarmingSettings settings, ILoggingService logger, Tools tools, ImageAnalyzer imageAnalyzer, SharedSettings sharedSettings, ImageResourceManager imageResourceManager)
            : base(navigator, settings, sharedSettings, tools, imageAnalyzer, imageResourceManager, logger)
        {
        }
        
        protected override async Task Prepare()
        {
            await base.Prepare();
            _startButtonImage = ImageResourceManager[@"Configuration\Ukr\Templates\ArenaClassic\arena_classic_start.png"];
        }

        protected override async Task Loop()
        {
            LoggingService.InfoUi($"Starting Arena scenario for {settings.TokenPurchases} runs.");

            TokenPuchesed = !settings.BuyTokensWithGems ? 0 : settings.TokenPurchases == 0 ? 100 : settings.TokenPurchases;
            LoggingService.InfoUi($"Token purchases: {TokenPuchesed} tokens.");
            
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
                
                LoggingService.Info($"Identified {tokens}/10 tokens.");
            }
            
            var needRefresh = settings.RefreshOpponentsOnStart;
            
            while (true)
            {
                LoggingService.Info($"Starting new cycle through opponent list.");
                _firstOpponentSnapshot = null;
                
                if (IsCancellationRequested()) break;

                if (needRefresh)
                {
                    var ca = navigator.GetScreenDefinitionById(ScreenDefinitionId.ClassicArena);
                    
                    if (!await navigator.IsElementVisibleAsync(ca["Refresh"]))
                    {
                        await WaitImage(ca["Refresh"], 80, 1000 * 10);
                    }

                    await Click(ca["Refresh"], ca["Refresh_dis"]);
                }
                
                // Крок 1: Прокрутити на самий верх
                await ScrollToTop();
                
                // Крок 2: Очистити старі дані про опонентів
                CleanupOpponents();

                // Встановити контрольний знімок початку списку замість першого опонента
                await SyncWindow();
                using (var controlArea = Window.Clone(new Rectangle(controlSnapShot, new Size(207, 85)), Window.PixelFormat))
                {
                    _firstOpponentSnapshot = new Bitmap(controlArea);
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

                        // Лічильник боїв видалено разом з налаштуванням кількості боїв

                        if (IsCancellationRequested()) break;

                        // Крок 6: Після бою перевірити чи список не скинувся повністю
                        await SyncWindow();
                        if (_firstOpponentSnapshot != null && !await IsOpponentVisible(_firstOpponentSnapshot))
                        {
                            LoggingService.Info("List was completely reset (league change). Restarting from top.");
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
                    using (var areaBeforeScroll = Window.Clone(_scrollCheckRect, Window.PixelFormat))
                    {
                        // Прокрутити вниз
                        MouseDrag(new Point(ScrollDragX, ScrollDragStartYBottom), new Point(ScrollDragX, ScrollDragEndYTop), 200, 500);
                        await Task.Delay(1000);

                        // Перевірити чи змінився екран
                        await SyncWindow();
                        using (var areaAfterScroll = Window.Clone(_scrollCheckRect, Window.PixelFormat))
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

                needRefresh = _opponents.Any(op => op.Status != FightStatus.Won);
            }

            LoggingService.Info("Arena scenario finished.");
        }

        private async Task<bool> TryGetTokens()
        {
            var result = false;
            
            await ClickAndProcessScreens(navigator.GetScreenDefinitionById(ScreenDefinitionId.ClassicArena)["add_tokens"],
                [ScreenDefinitionId.ClassicArenaFreeTokens, ScreenDefinitionId.ClassicArenaBuyTokens],
                async definition =>
                {
                    switch (definition.Id)
                    {
                        case ScreenDefinitionId.ClassicArenaFreeTokens:
                            await Click(definition["BuyTokens"], navigator.GetScreenDefinitionById(ScreenDefinitionId.ClassicArena));
                            result = true;
                            break;
                        case ScreenDefinitionId.ClassicArenaBuyTokens:
                            if (TokenPuchesed > 0)
                            {
                                await Click(definition["BuyTokens"],
                                    navigator.GetScreenDefinitionById(ScreenDefinitionId.ClassicArena));
                                result = true;
                                
                                TokenPuchesed--;

                                LoggingService.InfoUi($"Куплено разів {(settings.TokenPurchases == 0 ? 100 - TokenPuchesed : settings.TokenPurchases - TokenPuchesed)}");
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
            
            var full = ImageAnalyzer.FindImage(Window, ImageResourceManager[@"Configuration\ScreenDefinition\Templates\add_resources.png"], new (818, 13, 16, 20)) != default;

            var text = ImageAnalyzer.FindText(Window, true, full ? new (841, 13, 52, 22) : new (852, 13, 41, 22));
            
           return int.Parse(text.Split('/').First());
        }
        
        private async Task RefreshIfTokensIsEnough()
        {
            await SyncWindow(); 

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
                var currentArea = Window.Clone(_scrollCheckRect, Window.PixelFormat);

                if (previousArea != null && ImageAnalyzer.FindImage(previousArea, currentArea, default, 0.95) != default)
                {
                    LoggingService.Info("Top of the list reached.");
                    previousArea.Dispose();
                    currentArea.Dispose();
                    return;
                }
                
                previousArea?.Dispose();
                previousArea = currentArea;

                MouseDrag(new Point(ScrollDragX, ScrollDragStartYBottomUp), new Point(ScrollDragX, ScrollDragEndYBottomUp), 200, 500);
            }
            
            LoggingService.Warning("Could not determine if top of the list was reached.");
        }

        /// <summary>
        /// Scans the screen for fight buttons and creates snapshots for new, unknown opponents.
        /// </summary>
        private async Task<List<Opponent>> ScanVisibleOpponents()
        {
            var newOpponents = new List<Opponent>();
            var fightButtons = await FindAllImages(_startButtonImage);

            LoggingService.Info($"Found {fightButtons.Count} fight button(s) on screen.");

            foreach (var buttonRect in fightButtons.OrderBy(r => r.Y))
            {
                var opponentArea = new Rectangle(228, buttonRect.Y - 21, 207, 85);

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
                        var knownRect = new Rectangle(matchLocation.X, matchLocation.Y, 207, 85);
                        if (knownRect.IntersectsWith(opponentArea))
                        {
                            isAlreadyKnown = true;
                            break;
                        }
                    }
                }

                if (!isAlreadyKnown)
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
            LoggingService.Info("Starting fight...");
            await SyncWindow();
            var opponentRect = ImageAnalyzer.FindImage(Window, opponent.Snapshot);
            if (opponentRect == default)
            {
                LoggingService.Warning("Could not find opponent to fight. Skipping.");
                opponent.Status = FightStatus.Lost; // Mark as lost to avoid retries
                return;
            }

            // Find the fight button next to the opponent snapshot
            var searchArea = new Rectangle(startBottonX, opponentRect.Top, startBottonWigth, opponentRect.Height);
            var fightButtonRect = ImageAnalyzer.FindImage(Window, _startButtonImage, searchArea);

            if (fightButtonRect != default)
            {
                // Після кліку можливі різні вікна: підготовка бою, попередження/діалог, сам бій тощо.
                // Використовуємо узагальнену обробку екранів із пріоритетом у заданому порядку.
                await ClickAndProcessScreens(
                    fightButtonRect.ToPoint(),
                    [
                        ScreenDefinitionId.ClassicArenaPreparing,
                        ScreenDefinitionId.ClassicArenaFreeTokens,
                        ScreenDefinitionId.ClassicArenaBuyTokens
                    ],
                    async screenDefinition =>
                    {
                        switch (screenDefinition.Id)
                        {
                            case ScreenDefinitionId.ClassicArenaFreeTokens:
                                // Далі ведемо стандартний сценарій запуску бою
                                await navigator.GoToScreenAsync(screenDefinition, ScreenDefinitionId.ClassicArena);
                                LoggingService.Info($"Scrolling back to depth {currentScrollDepth} after fight...");
                                await ScrollToDepth(currentScrollDepth);
                                Click(fightButtonRect.ToPoint());
                                await ExecuteSingleRun(opponent);
                                return false; // завершити очікування після запуску підпроцесу
                            case ScreenDefinitionId.ClassicArenaBuyTokens:
                                if (TokenPuchesed > 0)
                                {
                                    await Click(screenDefinition["BuyTokens"],
                                        navigator.GetScreenDefinitionById(ScreenDefinitionId.ClassicArena));
                                    
                                    TokenPuchesed--;

                                    LoggingService.InfoUi($"Куплено разів {(settings.TokenPurchases == 0 ? 100 - TokenPuchesed : settings.TokenPurchases - TokenPuchesed)}");
                                    
                                    await ScrollToDepth(currentScrollDepth);
                                }
                                else
                                {
                                    await sharedSettings.CancellationTokenSource.CancelAsync();
                                }
                                return false;
                            case ScreenDefinitionId.ClassicArenaPreparing:
                                // Якщо одразу потрапили у бій, очікуємо його завершення у підпроцесі
                                await ExecuteSingleRun(opponent);
                                return false;
                            default:
                                // await sharedSettings.CancellationTokenSource.CancelAsync();
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
            await ProcessScreen([ScreenDefinitionId.ClassicArenaDefeat, ScreenDefinitionId.ClassicArenaVin, ScreenDefinitionId.ClassicArenaPreparing],
                async screenDefinition =>
                {
                    switch (screenDefinition.Id)
                    {
                        case ScreenDefinitionId.ClassicArenaDefeat:
                            opponent.Status = FightStatus.Lost;
                            break;
                        case ScreenDefinitionId.ClassicArenaVin:
                            opponent.Status = FightStatus.Won;
                            break;
                        case ScreenDefinitionId.ClassicArenaPreparing:
                            if (await navigator.IsElementVisibleAsync(screenDefinition["UncheckedAuto"]!))
                            {
                                await Click(screenDefinition["UncheckedAuto"]!, screenDefinition["CheckedAuto"]!);
                            }
                            
                            await Click(screenDefinition["Start"]!, navigator.GetScreenDefinitionById(ScreenDefinitionId.ClassicArenaFight));
                            
                            opponent.Status = FightStatus.Fighting;
                            LoggingService.Info($"Fight status {opponent.Status}.");
                            
                            return true;
                        default:
                            return true;
                    }

                    LoggingService.Info($"Fight status {opponent.Status}.");
                    await navigator.GoToScreenAsync(screenDefinition, ScreenDefinitionId.ClassicArena);
                    
                    return false;
                }, async definition =>
                {
                    if (definition.Id == ScreenDefinitionId.ClassicArenaFight)
                    {
                        await navigator.GoToScreenAsync(definition, ScreenDefinitionId.ClassicArena);
                        
                        LoggingService.Info($"Fight time out.");
                        
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
                MouseDrag(new Point(ScrollDragX, ScrollDragStartYBottom), new Point(ScrollDragX, ScrollDragEndYTop), 200, 500);
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
                MouseDrag(new Point(ScrollDragX, ScrollDragStartYBottom), new Point(ScrollDragX, ScrollDragEndYTop), 200, 500);
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
}