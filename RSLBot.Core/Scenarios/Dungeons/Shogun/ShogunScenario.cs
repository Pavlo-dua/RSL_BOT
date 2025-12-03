using RSLBot.Core.CoreHelpers;
using RSLBot.Core.Interfaces;
using RSLBot.Core.Services;
using RSLBot.Shared.Interfaces;
using RSLBot.Shared.Models;
using RSLBot.Shared.Settings;

namespace RSLBot.Core.Scenarios.Dungeons.Shogun
{
    public class ShogunScenario : StandardDungeonScenarioBase<ShogunFarmingSettings>
    {
        protected override ScreenDefinitionId MainFarmingScreenId => ScreenDefinitionId.Shogun;
        public override IScenario.ScenarioId Id => IScenario.ScenarioId.Shogun;

        protected override DungeonConfiguration Configuration => new DungeonConfiguration
        {
            // Координати прокрутки
            ScrollDragX = 542,
            ScrollDragStartYBottom = 613,
            ScrollDragEndYTop = 155,

            // ScreenDefinitionId для різних екранів
            MainScreenId = ScreenDefinitionId.Shogun,
            PreparingScreenId = ScreenDefinitionId.ShogunPreparing,
            FightingScreenId = ScreenDefinitionId.ShogunFighting,
            BuyTokensScreenId = ScreenDefinitionId.ShogunBuyTokens,
            VictoryScreenId = ScreenDefinitionId.ShogunVin,
            DefeatScreenId = ScreenDefinitionId.ShogunDefeat,
            FreeTokensScreenId = ScreenDefinitionId.ShogunFreeTokens,

            // Інші налаштування
            KeysImageElementName = "energy",
            FightImageElementName = "fight",
            MaxScrollAttempts = 5,

            // Перевірки на можливість поповнення енергії
            CanUseFreeRefill = () => settings.UseFreeRefill,
            CanUseGemRefill = () => settings.UseGemRefill
        };

        public ShogunScenario(INavigator navigator, ShogunFarmingSettings settings, ILoggingService logger, Tools tools, ImageAnalyzer imageAnalyzer, SharedSettings sharedSettings, ImageResourceManager imageResourceManager)
            : base(navigator, settings, logger, tools, imageAnalyzer, sharedSettings, imageResourceManager)
        {
        }
    }
}
