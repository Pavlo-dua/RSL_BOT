using RSLBot.Shared.Models;

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
    }
}
