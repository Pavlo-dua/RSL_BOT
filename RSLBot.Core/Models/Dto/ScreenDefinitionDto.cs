using RSLBot.Shared.Models;
using System.Collections.Generic;

namespace RSLBot.Core.Models.Dto
{
    // DTO для десеріалізації з JSON
    public class ScreenDefinitionDto
    {
        public ScreenDefinitionId ParentId { get; set; }
        public ScreenDefinitionId Id { get; set; }
        public int WindowHeight { get; set; }
        public int WindowWidth { get; set; }
        public List<UIElement> VerificationImages { get; set; } = [];
        public List<UIElement> UIElements { get; set; } = [];
        public List<ScreenDefinitionId> InnerScreenDefinitions { get; set; } = [];
        public List<TransitionDto> Transitions { get; set; } = [];
    }

    public class TransitionDto
    {
        public ScreenDefinitionId TargetScreenId { get; set; }
        public UIElement TriggerElement { get; set; }
    }
}
