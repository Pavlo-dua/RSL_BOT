using RSLBot.Core.CoreHelpers;
using RSLBot.Core.Interfaces;
using RSLBot.Core.Services;
using RSLBot.Shared.Models;
using RSLBot.Shared.Settings;
using System;
using System.Drawing;
using RSLBot.Core.Extensions;

namespace RSLBot.Core.Scenarios.ArenaTag
{
    public partial class ArenaTagFarmingScenario : ArenaFarmingScenarioBase<ArenaFarmingSettings>
    {
        // Note: ILoggingService is available as 'logger' in the base class 'Manipulation'
        protected override ScreenDefinitionId MainFarmingScreenId => ScreenDefinitionId.TagArena;
        public override IScenario.ScenarioId Id => IScenario.ScenarioId.TagArena;

        protected override ArenaConfiguration Configuration => new ArenaConfiguration
        {
            // Координати прокрутки (приклад різних координат для ArenaTag)
            ScrollDragX = 504,
            ScrollDragStartYBottom = 521, // Трохи нижче для ArenaTag
            ScrollDragStartYBottomUp = 210, // Трохи нижче для ArenaTag
            ScrollDragEndYBottomUp = 540, // Трохи нижче для ArenaTag
            ScrollDragEndYTop = 150, // Трохи нижче для ArenaTag
            
            // Координати кнопки старту
            StartButtonX = 940,
            StartButtonWidth = 200,
            
            // Область для перевірки прокрутки
            ScrollCheckRect = new Rectangle(228, 450, 207, 88), // Трохи нижче для ArenaTag
            
            // Контрольна точка для знімка
            ControlSnapshotPoint = new Point(228, 450), // Трохи нижче для ArenaTag
            
            // Область опонента
            OpponentAreaX = 228,
            OpponentAreaWidth = 207,
            OpponentAreaHeight = 85,
            OpponentAreaOffsetY = 20,
            
            // Шляхи до ресурсів (може бути різний для ArenaTag)
            StartButtonImagePath = @"Configuration\ScreenDefinition\Templates\ArenaTag\start_fight.png",
            AddResourcesImagePath = @"Configuration\ScreenDefinition\Templates\add_resources.png",
            
            // Координати для читання токенів
            TokenFullArea = new Rectangle(818, 13, 16, 20),
            TokenTextAreaFull = new Rectangle(841, 13, 52, 22),
            TokenTextAreaNormal = new Rectangle(852, 13, 41, 22),
            
            // ScreenDefinitionId для різних екранів (може бути різний для ArenaTag)
            MainScreenId = ScreenDefinitionId.TagArena, // Можна змінити на ArenaTag якщо є такий екран
            FreeTokensScreenId = ScreenDefinitionId.TagArenaFreeTokens,
            BuyTokensScreenId = ScreenDefinitionId.TagArenaBuyTokens,
            PreparingScreenId = ScreenDefinitionId.TagArenaPreparing,
            DefeatScreenId = ScreenDefinitionId.TagArenaDefeat,
            VictoryScreenId = ScreenDefinitionId.TagArenaVin,
            FightScreenId = ScreenDefinitionId.TagArenaFight
        };
        
        public ArenaTagFarmingScenario(INavigator navigator, ArenaFarmingSettings settings, ILoggingService logger, Tools tools, ImageAnalyzer imageAnalyzer, SharedSettings sharedSettings, ImageResourceManager imageResourceManager)
            : base(navigator, settings, logger, tools, imageAnalyzer, sharedSettings, imageResourceManager)
        {
        }

    }
}