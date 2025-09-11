using RSLBot.Core.CoreHelpers;
using RSLBot.Core.Interfaces;
using RSLBot.Core.Services;
using RSLBot.Shared.Interfaces;
using RSLBot.Shared.Models;

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
        protected readonly T settings = settings;
        private readonly Tools tools = tools;

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

        /// <summary>
        /// Основний цикл, що виконує забіги.
        /// </summary>
        protected abstract Task Loop();

        /// <summary>
        /// Перевіряє, чи можна продовжувати фарм (наявність ресурсів).
        /// </summary>
        protected abstract bool CanContinue();

        /// <summary>
        /// Запускає один бій/рівень.
        /// </summary>
        protected abstract Task ExecuteSingleRun();

        /// <summary>
        /// Обробляє всі екрани після завершення бою.
        /// </summary>
        protected abstract void HandleRunCompletion();

        /// <inheritdoc />
        public async Task ExecuteAsync()
        {
            await Prepare();
            // Логіка для визначення кількості запусків має бути в конкретному сценарії
            await Loop();
        }
    }
}
