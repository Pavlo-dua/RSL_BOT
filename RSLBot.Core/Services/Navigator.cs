using RSLBot.Core.Interfaces;
using RSLBot.Shared.Models;
using System.Collections.Generic;
using System.Linq;
using RSLBot.Core.CoreHelpers;
using System.Drawing;
using System;
using System.Data.SqlTypes;
using RSLBot.Shared.Settings;
using System.Threading.Tasks;
using RSLBot.Core.Extensions;

namespace RSLBot.Core.Services
{
    /// <summary>
    /// Реалізація сервісу навігації. Використовує граф екранів та алгоритм BFS для пошуку шляху.
    /// </summary>
    public class Navigator(
        IConfigurationService configurationService,
        ILoggingService logger,
        Tools tools,
        ScreenCaptureManager captureManager,
        ImageAnalyzer imageAnalyzer,
        ImageResourceManager imageResourceManager,
        SharedSettings sharedSettings)
        : Manipulation(tools, sharedSettings, imageAnalyzer, imageResourceManager, logger), INavigator
    {

        private readonly IConfigurationService _configurationService = configurationService;
        private readonly ILoggingService _logger = logger;
        private readonly Tools _tools = tools;
        private readonly ScreenCaptureManager _captureManager = captureManager;
        private readonly ImageAnalyzer _imageAnalyzer = imageAnalyzer;
        private readonly IReadOnlyDictionary<ScreenDefinitionId, ScreenDefinition> _screenGraph = configurationService.GetScreenDefinitions();

        public async Task CloseAllPopUp()
        {
            await SyncWindow();

            while (true)
            {
                var close = ImageAnalyzer.FindImage(Window,
                    ImageResourceManager["Configuration\\ScreenDefinition\\Templates\\popup_close.png"]);

                if (close != default)
                {
                    Click(close.ToPoint());
                    await Task.Delay(3000, sharedSettings.CancellationTokenSource.Token);
                    await SyncWindow();
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Знаходить найкоротший шлях до цільового екрана за допомогою BFS та виконує його.
        /// </summary>
        public async Task<ScreenDefinition> GoToScreenAsync(ScreenDefinitionId targetScreenId, bool navigateByChild = false)
        {
            await CloseAllPopUp();
            
            var startState = await GetCurrentStateAsync();

            if (startState == null)
            {
                _logger.Error("Could not identify the current screen.");
                return null;
            }

            return await GoToScreenAsync(startState, targetScreenId);
        }

        public async Task<ScreenDefinition> GoToScreenAsync(ScreenDefinition currentScreen, ScreenDefinitionId targetScreenId)
        {
            await CloseAllPopUp();
            
            if (currentScreen.Id == targetScreenId)
            {
                _logger.Info("Already on the target screen.");
                return _screenGraph[targetScreenId];
            }

            if (!_screenGraph.ContainsKey(currentScreen.Id) || !_screenGraph.ContainsKey(targetScreenId))
            {
                _logger.Error(null, $"Start or target screen not found in graph. Start: {currentScreen.Id}, Target: {targetScreenId}");
                return null;
            }

            var startNode = _screenGraph[currentScreen.Id];
            var targetNode = _screenGraph[targetScreenId];

            var path = FindPathBfs(startNode, targetNode);

            if (path == null)
            {
                _logger.Error(new InvalidOperationException("Path not found"), $"Path from '{startNode.Id}' to '{targetNode.Id}' not found!");
                return null;
            }

            _logger.Info($"Path found: {string.Join(" -> ", path.Select(t => t.TargetScreen.Id))}. Executing...");
            
            foreach (var transition in path)
            {
                if (await ClickOnElementAsync(transition, async () =>
                    {
                        if (transition.TargetScreen.Id == ScreenDefinitionId.Bastion)
                            await CloseAllPopUp();
                    }))
                {
                    _logger.Info("Clicked on " + transition.TriggerElement.Name);
                }
                else
                {
                    _logger.Error(new InvalidOperationException("Path not found"),
                        $"Path from '{startNode.Id}' to '{targetNode.Id}' not found!");
                    return null;
                }
            }

            return path[^1].TargetScreen;
        }

        /// <summary>
        /// Реалізація алгоритму пошуку в ширину (BFS) для знаходження шляху в графі екранів.
        /// </summary>
        /// <returns>Список переходів (шлях) або null, якщо шлях не знайдено.</returns>
        private List<Transition> FindPathBfs(ScreenDefinition startNode, ScreenDefinition targetNode)
        {
            var queue = new Queue<List<Transition>>();
            var visited = new HashSet<ScreenDefinitionId> { startNode.Id };

            queue.Enqueue(new List<Transition>());

            while (queue.Count > 0)
            {
                var currentPath = queue.Dequeue();
                var currentNode = currentPath.Count > 0 ? currentPath.Last().TargetScreen : startNode;

                if (currentNode.Id == targetNode.Id)
                {
                    return currentPath;
                }

                // Ітеруємо по зв'язаних переходах
                foreach (var transition in currentNode.Transitions)
                {
                    if (!visited.Contains(transition.TargetScreen.Id))
                    {
                        visited.Add(transition.TargetScreen.Id);
                        
                        var newPath = new List<Transition>(currentPath) { transition };
                        queue.Enqueue(newPath);
                    }
                }
            }

            return null;
        }

        public async Task<ScreenDefinition> GetCurrentStateAsync()
        {
            _logger.Info("Identifying current screen...");

            await SyncWindow();

            foreach (var screen in _screenGraph.Values)
            {
                if (screen.VerificationImages == null || !screen.VerificationImages.Any())
                {
                    continue;
                }

                try
                {
                    bool allImagesFound = true;
                    foreach (var verificationImage in screen.VerificationImages)
                    {
                        if (string.IsNullOrEmpty(verificationImage.ImageTemplatePath))
                        {
                            allImagesFound = false;
                            _logger.Warning($"Verification image for screen {screen.Id} has an empty ImageTemplatePath.");
                            break;
                        }
                        var template = ImageResourceManager[verificationImage.ImageTemplatePath];
                        var foundRect = _imageAnalyzer.FindImage(Window, template, verificationImage.Area);

                        if (foundRect == default(Rectangle))
                        {
                            allImagesFound = false;
                            break;
                        }
                    }

                    if (allImagesFound)
                    {
                        _logger.Info($"Current screen identified as {screen.Id}");
                        return screen;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, $"Failed to process verification images for screen {screen.Id}");
                }
            }

            _logger.Warning("Could not identify current screen.");
            return null;
        }
        
        public async Task<ScreenDefinition> GetCurrentStateAsync(List<ScreenDefinitionId> screenDefinitionIds)
        {
            _logger.Info("Identifying current screen...");
            
            await SyncWindow();

            foreach (var screen in _screenGraph.Where(sg => screenDefinitionIds.Contains(sg.Key)).Select(sg => sg.Value))
            {
                if (screen.VerificationImages == null || !screen.VerificationImages.Any())
                {
                    continue;
                }

                try
                {
                    bool allImagesFound = true;
                    foreach (var verificationImage in screen.VerificationImages)
                    {
                        if (string.IsNullOrEmpty(verificationImage.ImageTemplatePath))
                        {
                            allImagesFound = false;
                            _logger.Warning($"Verification image for screen {screen.Id} has an empty ImageTemplatePath.");
                            break;
                        }
                        var template = ImageResourceManager[verificationImage.ImageTemplatePath];
                        var foundRect = _imageAnalyzer.FindImage(Window, template, verificationImage.Area);

                        if (foundRect == default(Rectangle))
                        {
                            allImagesFound = false;
                            break;
                        }
                    }

                    if (allImagesFound)
                    {
                        _logger.Info($"Current screen identified as {screen.Id}");
                        return screen;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, $"Failed to process verification images for screen {screen.Id}");
                }
            }

            _logger.Warning("Could not identify current screen.");
            
            return new ScreenDefinition{VerificationImages = [] };;
        }

        public ScreenDefinition GetScreenDefinitionById(ScreenDefinitionId id)
        {
            return _screenGraph[id];
        }

        public async Task<bool> ClickOnElementAsync(Transition transition, Func<Task>? misc = null)
        {
            var triggerElement = transition.TriggerElement;
            if (string.IsNullOrEmpty(triggerElement.ImageTemplatePath))
            {
                _logger.Error($"Trigger element '{triggerElement.Name}' for target '{transition.TargetScreen.Id}' has no image path.");
                return false;
            }
            
            return await Click(triggerElement, transition.TargetScreen, misc) != default;
        }

        public async Task<bool> IsElementVisibleAsync(UIElement elementName)
        {
            _logger.Info($"Checking visibility of '{elementName.Name}'");
            return ImageAnalyzer.FindImage((await SyncWindow())!, ImageResourceManager[elementName.ImageTemplatePath], elementName.Area) != default;
        }
    }
}