using System;
using System.Collections.Generic;

namespace RSLBot.Shared.Models
{
    /// <summary>
    /// Описує один ігровий екран як вузол графа.
    /// </summary>
    public class ScreenDefinition
    {
        public bool ThereIsPopup { get; set; }
        public ScreenDefinitionId ParentId { get; set; }
        public ScreenDefinitionId Id { get; set; }
        public int WindowHeight { get; set; }
        public int WindowWidth { get; set; }
        public required List<UIElement> VerificationImages { get; set; } = [];
        public List<UIElement> UIElements { get; set; } = [];
        public List<ScreenDefinition> InnerScreenDefinitions { get; set; } = [];

        public UIElement? this[string name]
        {
            get
            {
                var n = name.ToUpper();
                return UIElements.FirstOrDefault(el => el.Name.Equals(n, StringComparison.CurrentCultureIgnoreCase));
            }
        }

        public Transition GetTransition(ScreenDefinitionId screenDefinitionId)
        {
            return Transitions.Find(tr => tr.TargetScreen.Id.Equals(screenDefinitionId))!;
        }

        /// <summary>
        /// Список переходів, що містить прямі посилання на цільові екрани.
        /// </summary>
        public List<Transition> Transitions { get; set; } = [];
    }
}
