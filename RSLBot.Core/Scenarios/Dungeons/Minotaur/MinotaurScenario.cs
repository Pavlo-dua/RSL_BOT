using System;
using System.Threading.Tasks;
using RSLBot.Core.CoreHelpers;
using RSLBot.Core.Interfaces;
using RSLBot.Core.Services;
using RSLBot.Shared.Interfaces;
using RSLBot.Shared.Models;
using RSLBot.Shared.Settings;

namespace RSLBot.Core.Scenarios.Dungeons.Minotaur
{
    public class MinotaurScenario : DungeonFarmingScenarioBase<MinotaurFarmingSettings>
    {
        protected override ScreenDefinitionId MainFarmingScreenId => ScreenDefinitionId.Minotaur;
        public override IScenario.ScenarioId Id => IScenario.ScenarioId.Minotaur;

        protected override DungeonConfiguration Configuration => new DungeonConfiguration
        {
            // Координати прокрутки
            ScrollDragX = 542,
            ScrollDragStartYBottom = 613,
            ScrollDragEndYTop = 155,

            // ScreenDefinitionId для різних екранів
            MainScreenId = ScreenDefinitionId.Minotaur,
            PreparingScreenId = ScreenDefinitionId.MinotaurPreparing,
            BuyTokensScreenId = ScreenDefinitionId.MinotaurBuyTokens,
            VictoryScreenId = ScreenDefinitionId.MinotaurVin,
            DefeatScreenId = ScreenDefinitionId.MinotaurDefeat,

            // Інші налаштування
            KeysImageElementName = "energy",
            FightImageElementName = "fight",
            MaxScrollAttempts = 5
        };

        public MinotaurScenario(INavigator navigator, MinotaurFarmingSettings settings, ILoggingService logger, Tools tools, ImageAnalyzer imageAnalyzer, SharedSettings sharedSettings, ImageResourceManager imageResourceManager)
            : base(navigator, settings, logger, tools, imageAnalyzer, sharedSettings, imageResourceManager)
        {
        }

        protected override Task<bool> ProcessDefeatScreen(ScreenDefinition screenDefinition, int countOfDefeat, Action timeoutReset)
        {
            return Task.FromResult(settings.MaxDefeat == -1 || settings.MaxDefeat >= countOfDefeat);
        }
    }
}
