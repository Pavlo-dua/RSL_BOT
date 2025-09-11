using RSLBot.Shared.Models;

namespace RSLBot.Core.Models.Dto
{
    // DTO для десеріалізації з JSON
    public class ScreenDefinitionDto
    {
        public ScreenDefinitionId ParentId { get; set; }
        public ScreenDefinitionId Id { get; set; }
        public UIElement VerificationImage { get; set; }
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