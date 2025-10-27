using RSLBot.Shared.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RSLBot.Core.Interfaces
{
    public interface INavigator
    {
        Task<ScreenDefinition> GoToScreenAsync(ScreenDefinitionId targetScreenId, bool navigateByChild = false);
        Task<ScreenDefinition> GoToScreenAsync(ScreenDefinition currentScreen, ScreenDefinitionId targetScreenId);
        Task<ScreenDefinition> GetCurrentStateAsync();
        Task<ScreenDefinition> GetCurrentStateAsync(List<ScreenDefinitionId> screenDefinitionIds);
        ScreenDefinition GetScreenDefinitionById(ScreenDefinitionId id);
        Task<bool> IsElementVisibleAsync(UIElement elementName);
        Task CloseAllPopUp();
    }
}