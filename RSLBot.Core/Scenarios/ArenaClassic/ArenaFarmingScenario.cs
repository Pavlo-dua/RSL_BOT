using RSLBot.Core.CoreHelpers;
using RSLBot.Core.Interfaces;
using RSLBot.Core.Services;
using RSLBot.Shared.Models;
using RSLBot.Shared.Settings;
using System;
using System.Drawing;
using RSLBot.Core.Extensions;

namespace RSLBot.Core.Scenarios.ArenaClassic
{
    public partial class ArenaFarmingScenario : ArenaFarmingScenarioBase<ArenaFarmingSettings>
    {
        // Note: ILoggingService is available as 'logger' in the base class 'Manipulation'
        protected override ScreenDefinitionId MainFarmingScreenId => ScreenDefinitionId.ClassicArena;
        public override IScenario.ScenarioId Id => IScenario.ScenarioId.ClassicArena;

        protected override ArenaConfiguration Configuration => new ArenaConfiguration
        {
            // Координати прокрутки
            ScrollDragX = 542,
            ScrollDragStartYBottom = 613,
            ScrollDragStartYBottomUp = 208,
            ScrollDragEndYBottomUp = 534,
            ScrollDragEndYTop = 155,
            
            // Координати кнопки старту
            StartButtonX = 940,
            StartButtonWidth = 200,
            
            // Область для перевірки прокрутки
            ScrollCheckRect = new Rectangle(228, 540, 207, 88),
            
            // Контрольна точка для знімка
            ControlSnapshotPoint = new Point(228, 540),
            
            // Область опонента
            OpponentAreaX = 228,
            OpponentAreaWidth = 207,
            OpponentAreaHeight = 85,
            OpponentAreaOffsetY = 21,
            
            // Шляхи до ресурсів
            StartButtonImagePath = @"Configuration\Ukr\Templates\ArenaClassic\arena_classic_start.png",
            AddResourcesImagePath = @"Configuration\ScreenDefinition\Templates\add_resources.png",
            
            // Координати для читання токенів
            TokenFullArea = new Rectangle(818, 13, 16, 20),
            TokenTextAreaFull = new Rectangle(841, 13, 52, 22),
            TokenTextAreaNormal = new Rectangle(852, 13, 41, 22),
            
            // ScreenDefinitionId для різних екранів
            MainScreenId = ScreenDefinitionId.ClassicArena,
            FreeTokensScreenId = ScreenDefinitionId.ClassicArenaFreeTokens,
            BuyTokensScreenId = ScreenDefinitionId.ClassicArenaBuyTokens,
            PreparingScreenId = ScreenDefinitionId.ClassicArenaPreparing,
            DefeatScreenId = ScreenDefinitionId.ClassicArenaDefeat,
            VictoryScreenId = ScreenDefinitionId.ClassicArenaVin,
            FightScreenId = ScreenDefinitionId.ClassicArenaFight
        };
        
        public ArenaFarmingScenario(INavigator navigator, ArenaFarmingSettings settings, ILoggingService logger, Tools tools, ImageAnalyzer imageAnalyzer, SharedSettings sharedSettings, ImageResourceManager imageResourceManager)
            : base(navigator, settings, logger, tools, imageAnalyzer, sharedSettings, imageResourceManager)
        {
        }

    }
}