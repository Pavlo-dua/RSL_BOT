using RSLBot.Core.CoreHelpers;
using RSLBot.Core.Interfaces;
using RSLBot.Core.Scenarios.Dungeons;
using RSLBot.Core.Services;
using RSLBot.Shared.Interfaces;
using RSLBot.Shared.Models;
using RSLBot.Shared.Settings;

namespace RSLBot.Core.Scenarios.Dungeons.Twins;

public class TwinsScenario : DungeonFarmingScenarioBase<TwinsFarmingSettings>
{
    protected override ScreenDefinitionId MainFarmingScreenId => ScreenDefinitionId.Twins;
    public override IScenario.ScenarioId Id => IScenario.ScenarioId.Twins;

    protected override DungeonConfiguration Configuration => new DungeonConfiguration
    {
        // Координати прокрутки
        ScrollDragX = 542,
        ScrollDragStartYBottom = 613,
        ScrollDragEndYTop = 155,

        // ScreenDefinitionId для різних екранів
        MainScreenId = ScreenDefinitionId.Twins,
        PreparingScreenId = ScreenDefinitionId.TwinsPreparing,
        FightingScreenId = ScreenDefinitionId.TwinsFighting,
        BuyTokensScreenId = ScreenDefinitionId.TwinsBuyTokens,
        VictoryScreenId = ScreenDefinitionId.TwinsVin,
        DefeatScreenId = ScreenDefinitionId.TwinsDefeat,

        // Інші налаштування
        KeysImageElementName = "keys",
        FightImageElementName = "fight",
        MaxScrollAttempts = 5,

        // Перевірки на можливість поповнення енергії
        CanUseGemRefill = () => settings.BuyTokensWithGems
    };

    public TwinsScenario(INavigator navigator, TwinsFarmingSettings settings, ILoggingService logger, Tools tools, ImageAnalyzer imageAnalyzer, SharedSettings sharedSettings, ImageResourceManager imageResourceManager)
        : base(navigator, settings, logger, tools, imageAnalyzer, sharedSettings, imageResourceManager)
    {
    }

    protected override Task<bool> ProcessDefeatScreen(ScreenDefinition screenDefinition, int countOfDefeat, Action timeoutReset)
    {
        return Task.FromResult(settings.MaxDefeat == -1 || settings.MaxDefeat >= countOfDefeat);
    }
}
