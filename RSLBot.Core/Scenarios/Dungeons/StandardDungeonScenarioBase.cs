using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using RSLBot.Core.CoreHelpers;
using RSLBot.Core.Interfaces;
using RSLBot.Core.Services;
using RSLBot.Shared.Interfaces;
using RSLBot.Shared.Models;
using RSLBot.Shared.Settings;

namespace RSLBot.Core.Scenarios.Dungeons
{
    public abstract class StandardDungeonScenarioBase<T> : DungeonFarmingScenarioBase<T>
        where T : StandardDungeonSettings
    {
        private int _battlesFought = 0;
        private int _gemRefillsUsed = 0;
        private int _freeRefillsUsed = 0;

        protected StandardDungeonScenarioBase(INavigator navigator, T settings, ILoggingService logger, Tools tools, ImageAnalyzer imageAnalyzer, SharedSettings sharedSettings, ImageResourceManager imageResourceManager)
            : base(navigator, settings, logger, tools, imageAnalyzer, sharedSettings, imageResourceManager)
        {
        }

        private int _tournamentPointsAccumulated = 0;

        protected override async Task<bool> ShouldRerun(ScreenDefinition screenDefinition)
        {
            _battlesFought++;

            if (settings.TournamentPoints > 0)
            {
                var tournamentPointsElement = screenDefinition["tournament_points"];
                if (tournamentPointsElement != null)
                {
                    // Шукаємо елемент tournament_points на екрані
                    await SyncWindow();

                    if (Window == null)
                    {
                        LoggingService.Error("Window is null in ShouldRerun");
                        return false;
                    }

                    var match = ImageAnalyzer.FindImage(Window, ImageResourceManager[tournamentPointsElement.ImageTemplatePath]);

                    if (match != default)
                    {
                        // Якщо елемент присутній, то тоді читаємо цифри
                        // починаючи з низу знайденого елемента - 2 пікселя вниз і -1 піксель вліво. Ширина 65. Висота 16.
                        var textArea = new Rectangle(match.X - 1, match.Y + match.Height + 2, 65, 16);
                        var text = ImageAnalyzer.FindText(Window, true, textArea);

                        if (int.TryParse(text, out int points))
                        {
                            _tournamentPointsAccumulated += points;
                            LoggingService.InfoUi($"Турнірні бали: {_tournamentPointsAccumulated}/{settings.TournamentPoints} (+{points})");

                            if (_tournamentPointsAccumulated >= settings.TournamentPoints)
                            {
                                LoggingService.InfoUi($"Ціль по турнірним балам досягнута: {_tournamentPointsAccumulated}");
                                return false;
                            }
                        }
                        else
                        {
                            LoggingService.WarningUi($"Не вдалося розпізнати турнірні бали. Text: '{text}'");
                            // Якщо не вийшло отримати це число, то зупиняєш бій.
                            return false;
                        }
                    }
                    else
                    {
                        // Якщо елемент не знайдено, але налаштування увімкнено - можливо це не турнірний час або помилка розпізнавання
                        // Користувач просив: "Якщо не вийшло отримати це число, то зупиняєш бій."
                        // Якщо самого значка немає, то ми не можемо отримати число.
                        LoggingService.WarningUi("Елемент турнірних балів не знайдено, хоча налаштування увімкнено.");
                        return false;
                    }
                }
            }

            if (settings.MaxBattles > 0 && _battlesFought >= settings.MaxBattles)
            {
                if (settings.OptimizeResources)
                {
                    // Перевіряємо, чи є ще енергія для бою
                    var energy = await GetTokenCount();
                    // Припускаємо, що вартість бою десь 14-16, але краще перевірити точно.
                    // Для стандартних данжів це зазвичай 16 на високих рівнях, або менше.
                    // Якщо енергії > 16 (з запасом), то продовжуємо.
                    // Але GetTokenCount повертає поточну енергію.
                    // Якщо ми тут, значить бій закінчився.
                    // Якщо енергії достатньо для ще одного бою, то продовжуємо, АЛЕ
                    // ми не повинні більше поповнювати енергію.
                    // Тому ми можемо просто дозволити rerun, але заблокувати поповнення.
                    // Але логіка поповнення в ProcessBuyTokensScreen.
                    // Тому тут просто повертаємо true, якщо є енергія.

                    // TODO: Отримати вартість бою з конфігурації або налаштувань.
                    // Поки що хардкод 16 як найпоширеніша вартість для 20+ поверхів.
                    if (energy >= 16)
                    {
                        LoggingService.Info($"[Optimize] Max battles reached ({_battlesFought}), but energy ({energy}) is enough for another run. Continuing.");
                        return true;
                    }
                }

                LoggingService.Info($"Max battles limit reached: {_battlesFought}");
                return false;
            }

            return await base.ShouldRerun(screenDefinition);
        }

        protected override Task<bool> ProcessDefeatScreen(ScreenDefinition screenDefinition, int countOfDefeat, Action timeoutReset)
        {
            return Task.FromResult(settings.MaxDefeat == -1 || settings.MaxDefeat >= countOfDefeat);
        }

        protected override async Task<int> GetTokenCount()
        {
            await SyncWindow();

            var energy = await WaitImage(MainScreenDefinition["energy"]);

            var addResources = await WaitImage(ImageResourceManager[MainScreenDefinition["add_resources"].ImageTemplatePath], new System.Drawing.Rectangle(0, 0, energy.X, energy.Height));

            var keysTextArea = new Rectangle(addResources.X + addResources.Width + 3, addResources.Y + 3, energy.X - addResources.X - addResources.Width - 3, addResources.Height - 5);

            var text = ImageAnalyzer.FindText(Window, true, keysTextArea);

            return int.Parse(text.Split('/').First().Replace(" ", ""));
        }

        protected override async Task ProcessPreparingScreen(ScreenDefinition screenDefinition)
        {
            var isUncheckedSuperRaidVisible = await navigator.IsElementVisibleAsync(screenDefinition["UncheckedSuperRaid"]!);

            if (isUncheckedSuperRaidVisible)
            {
                await Click(screenDefinition["UncheckedSuperRaid"]!);
            }

            await base.ProcessPreparingScreen(screenDefinition);
        }

        protected override async Task<bool> ProcessBuyTokensScreen(ScreenDefinition screenDefinition)
        {
            if (settings.MaxBattles > 0 && _battlesFought >= settings.MaxBattles && settings.OptimizeResources)
            {
                LoggingService.Info("[Optimize] Max battles reached. Skipping gem refill.");
                await Click(screenDefinition["CloseTokens"]!);
                return false;
            }

            if (settings.MaxGemRefills > 0 && _gemRefillsUsed >= settings.MaxGemRefills)
            {
                LoggingService.Info($"Max gem refills limit reached: {_gemRefillsUsed}");
                await Click(screenDefinition["CloseTokens"]!);
                return false;
            }

            var result = await base.ProcessBuyTokensScreen(screenDefinition);
            if (result)
            {
                _gemRefillsUsed++;
                LoggingService.Info($"Gem refill used. Total: {_gemRefillsUsed}");
            }
            return result;
        }

        protected override async Task<bool> ProcessFreeTokensScreen(ScreenDefinition screenDefinition)
        {
            if (settings.MaxBattles > 0 && _battlesFought >= settings.MaxBattles && settings.OptimizeResources)
            {
                LoggingService.Info("[Optimize] Max battles reached. Skipping free refill.");
                // Тут треба закрити вікно або натиснути "ні", якщо є така кнопка.
                // Але зазвичай FreeTokensScreen це просто кнопка "взяти".
                // Якщо ми не хочемо брати, то що робити?
                // Мабуть, просто ігноруємо або закриваємо.
                // У базовому класі немає кнопки закриття для FreeTokens, зазвичай це просто клік по "BuyTokens" (який насправді Free).
                // Якщо ми не хочемо брати, то ми повинні якось вийти з цього екрану або зупинити сценарій.
                // Повернемо false, щоб зупинити цикл, якщо не можемо пропустити.
                return false;
            }

            if (settings.MaxFreeRefills > 0 && _freeRefillsUsed >= settings.MaxFreeRefills)
            {
                LoggingService.Info($"Max free refills limit reached: {_freeRefillsUsed}");
                return false;
            }

            var result = await base.ProcessFreeTokensScreen(screenDefinition);
            if (result)
            {
                _freeRefillsUsed++;
                LoggingService.Info($"Free refill used. Total: {_freeRefillsUsed}");
            }
            return result;
        }
    }
}
