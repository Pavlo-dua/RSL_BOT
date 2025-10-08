using RSLBot.Shared.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RSLBot.Core.Interfaces
{
    /// <summary>
    /// Відповідає за всю навігацію та взаємодію з UI гри.
    /// </summary>
    public interface INavigator
    {
        /// <summary>
        /// Переходить до вказаного екрана, автоматично знаходячи шлях.
        /// </summary>
        Task<ScreenDefinition> GoToScreenAsync(ScreenDefinitionId targetScreen, bool navigateByChild = false);

        Task<ScreenDefinition> GoToScreenAsync(ScreenDefinition currentScreen, ScreenDefinitionId targetScreenId);

        /// <summary>
        /// Визначає поточний екран гри.
        /// </summary>
        Task<ScreenDefinition> GetCurrentStateAsync();

        //void ClickOnElement(string elementName);

        /// <summary>
        /// Перевіряє видимість елемента на поточному екрані.
        /// </summary>
        Task<bool> IsElementVisibleAsync(UIElement elementName);
        //bool IsElementVisible(string elementName);

        Task<ScreenDefinition> GetCurrentStateAsync(List<ScreenDefinitionId> screenDefinitionIds);
        
        ScreenDefinition GetScreenDefinitionById(ScreenDefinitionId id);
    }
}
