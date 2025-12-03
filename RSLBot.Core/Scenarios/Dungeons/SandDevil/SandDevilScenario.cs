using RSLBot.Core.CoreHelpers;
using RSLBot.Core.Interfaces;
using RSLBot.Core.Services;
using RSLBot.Shared.Interfaces;
using RSLBot.Shared.Models;
using RSLBot.Shared.Settings;

namespace RSLBot.Core.Scenarios.Dungeons.SandDevil
{
    public class SandDevilScenario : StandardDungeonScenarioBase<SandDevilFarmingSettings>
    {
        protected override ScreenDefinitionId MainFarmingScreenId => ScreenDefinitionId.SandDevil;
        public override IScenario.ScenarioId Id => IScenario.ScenarioId.SandDevil;

        protected override DungeonConfiguration Configuration => new DungeonConfiguration
        {
            // Координати прокрутки
            ScrollDragX = 542,
            ScrollDragStartYBottom = 613,
            ScrollDragEndYTop = 155,

            // ScreenDefinitionId для різних екранів
            MainScreenId = ScreenDefinitionId.SandDevil,
            PreparingScreenId = ScreenDefinitionId.SandDevilPreparing,
            FightingScreenId = ScreenDefinitionId.SandDevilFighting,
            BuyTokensScreenId = ScreenDefinitionId.SandDevilBuyTokens,
            VictoryScreenId = ScreenDefinitionId.SandDevilVin,
            DefeatScreenId = ScreenDefinitionId.SandDevilDefeat,
            FreeTokensScreenId = ScreenDefinitionId.SandDevilFreeTokens,

            // Інші налаштування
            KeysImageElementName = "energy",
            FightImageElementName = "fight",
            MaxScrollAttempts = 5,

            // Перевірки на можливість поповнення енергії
            CanUseFreeRefill = () => settings.UseFreeRefill,
            CanUseGemRefill = () => settings.UseGemRefill
        };

        public SandDevilScenario(INavigator navigator, SandDevilFarmingSettings settings, ILoggingService logger, Tools tools, ImageAnalyzer imageAnalyzer, SharedSettings sharedSettings, ImageResourceManager imageResourceManager)
            : base(navigator, settings, logger, tools, imageAnalyzer, sharedSettings, imageResourceManager)
        {
        }
    }
}
