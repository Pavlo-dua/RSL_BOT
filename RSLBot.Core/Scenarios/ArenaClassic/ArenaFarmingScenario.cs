using RSLBot.Core.CoreHelpers;
using RSLBot.Core.Interfaces;
using RSLBot.Shared.Models;
using RSLBot.Shared.Settings;
using System.Drawing;
using System.IO;
using RSLBot.Core.Services;

namespace RSLBot.Core.Scenarios.ArenaClassic
{
    using RSLBot.Core.Extensions;

    /// <summary>
    /// Реалізація сценарію для фарму Арени.
    /// </summary>
    public partial class ArenaFarmingScenario : BaseFarmingScenario<ArenaFarmingSettings>
    {
        private readonly ILoggingService logger;
        protected override ScreenDefinitionId MainFarmingScreenId => ScreenDefinitionId.ClassicArena;

        /// <inheritdoc />
        public override IScenario.ScenarioId Id => IScenario.ScenarioId.Arena;

        public ArenaFarmingScenario(INavigator navigator, ArenaFarmingSettings settings, ILoggingService logger, Tools tools, ImageAnalyzer imageAnalyzer, SharedSettings sharedSettings, ImageResourceManager imageResourceManager)
            : base(navigator, settings, sharedSettings, tools, imageAnalyzer, imageResourceManager, logger)
        {
            this.logger = logger;
        }

        private readonly List<Opponent> _allOpponents = new List<Opponent>();
        private Opponent _currentOpponent;
        private const string StartButtonImagePath = @"C:\Home\My_work\RSL_Bot\RSLBot.Shared\Configuration\Ukr\Templates\ArenaClassic\arena_classic_start.png";
        private Bitmap _startButtonImage;

        protected override async Task Loop()
        {
                logger.Info($"Starting Arena scenario for {settings.NumberOfFights} runs.");
                
                if (settings.NumberOfFights == 0)
                {
                    logger.Info("NumberOfFights is 0, will farm until the end of the list.");
                }

               // if (!LoadResources()) return;

                int fightCounter = 0;

                // 1. Scan and fight opponents on the first screen
                logger.Info("Scanning for opponents on the first screen.");
                var initialOpponents = await ScanVisibleOpponents();
                foreach (var opponent in initialOpponents.Where(opponent => !_allOpponents.Contains(opponent)))
                {
                    _allOpponents.Add(opponent);
                }

                foreach (var opponent in initialOpponents)
                {
                    if (!ShouldContinueFarming(fightCounter)) break;

                    _currentOpponent = opponent;
                    logger.Info($"Fighting initial opponent at Y:{_currentOpponent.Area.Y}");
                    await ExecuteSingleRun();
                    HandleRunCompletion();
                    fightCounter++;
                }

                // 2. Scroll and fight remaining opponents
                int scrollsPerformed = 0;
                int endOfListDetectionCounter = 0;

                while (ShouldContinueFarming(fightCounter))
                {
                    var visibleBeforeScroll = await ScanVisibleOpponents();
                    if (!visibleBeforeScroll.Any())
                    {
                        logger.Warning("No opponents visible to scroll, stopping.");
                        break;
                    }
                    var lastOpponentBeforeScroll = visibleBeforeScroll.Last();

                    scrollsPerformed++;
                    logger.Info($"Scrolling down ({scrollsPerformed} time(s)).");

                    if (! await PerformScrolls(1))
                    {
                        logger.Info("Could not scroll further, assuming end of list.");
                        break;
                    }

                    var visibleAfterScroll = await ScanVisibleOpponents();
                    if (!visibleAfterScroll.Any())
                    {
                        logger.Warning("No opponents visible after scroll, stopping.");
                        break;
                    }
                    var lastOpponentAfterScroll = visibleAfterScroll.Last();

                    // End of list check
                    if (lastOpponentAfterScroll.Equals(lastOpponentBeforeScroll))
                    {
                        endOfListDetectionCounter++;
                        logger.Info($"List position unchanged. End of list attempt {endOfListDetectionCounter}/2.");
                        if (endOfListDetectionCounter >= 2)
                        {
                            logger.Info("End of list reached.");
                            break;
                        }
                    }
                    else
                    {
                        endOfListDetectionCounter = 0; // Reset counter if list moved
                    }

                    var newOpponents = visibleAfterScroll.Where(o => !_allOpponents.Contains(o)).ToList();

                    if (!newOpponents.Any())
                    {
                        logger.Info("No new opponents found after scrolling. Continuing to scroll.");
                        continue;
                    }

                    foreach (var opponent in newOpponents)
                    {
                        _allOpponents.Add(opponent);
                    }

                    // Fight the newly found opponents
                    foreach (var newOpponent in newOpponents)
                    {
                        if (!ShouldContinueFarming(fightCounter)) break;

                        logger.Info($"Scrolling back to the page of the next opponent.");
                        if (!await PerformScrolls(scrollsPerformed))
                        {
                            logger.Warning("Failed to scroll back to the opponent's page. Aborting.");
                            _startButtonImage?.Dispose();
                            return; // Abort
                        }

                        _currentOpponent = newOpponent;
                        logger.Info($"Fighting new opponent at Y:{_currentOpponent.Area.Y}");
                        ExecuteSingleRun();
                        HandleRunCompletion();
                        fightCounter++;
                    }
                    if (!ShouldContinueFarming(fightCounter)) break;
                }

                logger.Info("Arena scenario finished.");
                _startButtonImage?.Dispose();
        }

        private bool LoadResources()
        {
            return true;
        }

        private async Task<List<Opponent>> ScanVisibleOpponents()
        {
            var visibleOpponents = new List<Opponent>();
            await SyncWindow();
            var buttons = await FindAllImages(_startButtonImage);

            foreach (var buttonRect in buttons.OrderBy(r => r.Y))
            {
                var opponentArea = new Rectangle(
                    229,
                    buttonRect.Y + buttonRect.Height / 2 - 40,
                    681,
                    80);

                visibleOpponents.Add(new Opponent { Area = opponentArea, Status = FightStatus.NotFought });
            }
            logger.Info($"Scan found {visibleOpponents.Count} opponents on screen.");
            return visibleOpponents;
        }

        private async Task<bool> PerformScrolls(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var visibleOpponents = await ScanVisibleOpponents();
                if (!visibleOpponents.Any())
                {
                    logger.Warning("Cannot scroll, no opponents visible.");
                    return false;
                }

                var lastOpponentBeforeScroll = visibleOpponents.Last();
                int scrollAttempts = 0;
                const int maxScrollAttempts = 3;

                while (scrollAttempts < maxScrollAttempts)
                {
                    // Find the current position of the opponent we want to scroll off-screen
                    var currentPositionOfLastOpponent = ( await ScanVisibleOpponents()).FirstOrDefault(o => o.Equals(lastOpponentBeforeScroll));

                    // If the opponent is no longer visible, the scroll was successful.
                    if (currentPositionOfLastOpponent == null)
                    {
                        logger.Info($"Scroll {i + 1}/{count} successful, opponent is off-screen.");
                        break; // Exit the while loop and proceed to the next scroll if any
                    }

                    Point dragFrom = new Point(currentPositionOfLastOpponent.Area.X + currentPositionOfLastOpponent.Area.Width / 2, currentPositionOfLastOpponent.Area.Y + currentPositionOfLastOpponent.Area.Height / 2);
                    Point dragTo = new Point(dragFrom.X, 140);

                    logger.Info($"Performing scroll {i + 1}/{count} from Y:{dragFrom.Y} (Attempt: {scrollAttempts + 1})");
                    MouseDrag(dragFrom, dragTo, 1000, 500);
                    Thread.Sleep(500); // Wait for scroll animation

                    scrollAttempts++;

                    if (scrollAttempts >= maxScrollAttempts)
                    {
                        // Final check after the last attempt
                        if ((await ScanVisibleOpponents()).Any(o => o.Equals(lastOpponentBeforeScroll)))
                        {
                            logger.Error($"Failed to scroll opponent off-screen after {maxScrollAttempts} attempts.");
                            return false; // Indicate that scrolling failed
                        }
                        else
                        {
                            logger.Info($"Scroll {i + 1}/{count} successful on the last attempt.");
                        }
                    }
                }
            }
            return true;
        }

        protected override bool CanContinue()
        {
            logger.Info("Checking for Arena keys...");
            return true;
        }

        private bool ShouldContinueFarming(int currentFightCount)
        {
            if (!CanContinue()) // This checks for keys, etc.
            {
                return false;
            }

            if (settings.NumberOfFights != 0 && currentFightCount >= settings.NumberOfFights)
            {
                logger.Info("Desired number of fights reached.");
                return false;
            }

            return true;
        }

        protected override async Task ExecuteSingleRun()
        {
            if (_currentOpponent == null)
            {
                logger.Error("ExecuteSingleRun called without a valid opponent.");
                return;
            }
            
            logger.Info($"Starting Arena battle against opponent at Y:{_currentOpponent.Area.Y}");

            // We assume we are already scrolled to the correct page
            var opponentButtonRect = (await FindAllImages(_startButtonImage))
                .OrderBy(r => Math.Abs((r.Y + r.Height / 2) - (_currentOpponent.Area.Y + _currentOpponent.Area.Height / 2)))
                .FirstOrDefault();

            if (opponentButtonRect == default)
            {
                logger.Warning($"Could not find fight button for opponent at Y:{_currentOpponent.Area.Y}. Skipping.");
                _currentOpponent.Status = FightStatus.Lost;
                return;
            }

            Click(opponentButtonRect.ToPoint());
            _currentOpponent.Status = FightStatus.Lost; // Assume lost until result is handled
        }

        protected override void HandleRunCompletion()
        {
            logger.Info("Waiting for battle completion...");
            // NOTE: This is a placeholder for battle result handling.
            // You need to implement logic to detect victory or defeat,
            // and then navigate back to the arena screen.

            // For now, we just wait a bit to simulate the fight time
            Thread.Sleep(5000);
            logger.Info("Battle finished, returning to lobby (simulated).");
        }
    }
}
