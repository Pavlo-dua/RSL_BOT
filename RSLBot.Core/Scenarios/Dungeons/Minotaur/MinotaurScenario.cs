using System;
using System.Threading.Tasks;
using RSLBot.Core.CoreHelpers;
using RSLBot.Core.Interfaces;
using RSLBot.Core.Services;
using RSLBot.Shared.Interfaces;
using RSLBot.Shared.Models;
using RSLBot.Shared.Settings;
using System.Drawing;
using DynamicData;

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
            FightingScreenId = ScreenDefinitionId.MinotaurFighting,
            BuyTokensScreenId = ScreenDefinitionId.MinotaurBuyTokens,
            VictoryScreenId = ScreenDefinitionId.MinotaurVin,
            DefeatScreenId = ScreenDefinitionId.MinotaurDefeat,

            // Інші налаштування
            KeysImageElementName = "energy",
            FightImageElementName = "fight",
            MaxScrollAttempts = 5,

            // Перевірки на можливість поповнення енергії
            CanUseFreeRefill = () => settings.UseFreeRefill,
            CanUseGemRefill = () => settings.UseGemRefill,

            AdditionalScreens = [ScreenDefinitionId.MinotaurMaxScroll]
        };

        public MinotaurScenario(INavigator navigator, MinotaurFarmingSettings settings, ILoggingService logger, Tools tools, ImageAnalyzer imageAnalyzer, SharedSettings sharedSettings, ImageResourceManager imageResourceManager)
            : base(navigator, settings, logger, tools, imageAnalyzer, sharedSettings, imageResourceManager)
        {
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

        protected override async Task<bool> ShouldRerun(ScreenDefinition screenDefinition)
        {
            var isBaseRollVisible = await navigator.IsElementVisibleAsync(screenDefinition["base_roll"]!);
            var isGreenRollVisible = await navigator.IsElementVisibleAsync(screenDefinition["green_roll"]!);
            var isRedRollVisible = await navigator.IsElementVisibleAsync(screenDefinition["red_roll"]!);

            switch (settings.Scrolls)
            {
                case MinotaurScrollType.Basic:
                    // Stop if Green or Red scrolls are visible (meaning Basic are maxed)
                    if (isGreenRollVisible || isRedRollVisible) return false;
                    break;
                case MinotaurScrollType.Green:
                    // Stop if Red scrolls are visible (meaning Green are maxed)
                    if (isRedRollVisible) return false;
                    break;
                case MinotaurScrollType.Red:
                    // Stop if NO scrolls are visible (meaning everything is maxed)
                    if (!isBaseRollVisible && !isGreenRollVisible && !isRedRollVisible)
                    {
                        return false;
                    }

                    break;
            }

            return await base.ShouldRerun(screenDefinition);
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

        protected override async Task<bool> ProcessCustomScreen(ScreenDefinition screenDefinition)
        {
            if (screenDefinition.Id == ScreenDefinitionId.MinotaurMaxScroll)
            {
                await Click(screenDefinition["Cancel"]!);
                return false;
            }

            return await base.ProcessCustomScreen(screenDefinition);
        }
    }
}
