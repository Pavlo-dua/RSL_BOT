using RSLBot.Core.Interfaces;
using RSLBot.Shared.Models;
using System.Collections.Generic;
using System.Linq;
using RSLBot.Core.CoreHelpers;
using System.Drawing;
using System;
using RSLBot.Shared.Settings;

namespace RSLBot.Core.Services
{
    /// <summary>
    /// Реалізація сервісу навігації. Використовує граф екранів та алгоритм BFS для пошуку шляху.
    /// </summary>
    public class Navigator : Manipulation, INavigator
    {

        private readonly IConfigurationService _configurationService;
        private readonly ILoggingService _logger;
        private readonly Tools _tools;
        private readonly ScreenCaptureManager _captureManager;
        private readonly ImageAnalyzer _imageAnalyzer;
        private readonly IReadOnlyDictionary<ScreenDefinitionId, ScreenDefinition> _screenGraph;

        public Navigator(
            IConfigurationService configurationService,
            ILoggingService logger,
            Tools tools,
            ScreenCaptureManager captureManager,
            ImageAnalyzer imageAnalyzer,
            ImageResourceManager imageResourceManager,
            SharedSettings sharedSettings) : base(tools, sharedSettings, imageAnalyzer, imageResourceManager, logger)
        {
            _configurationService = configurationService;
            _logger = logger;
            _tools = tools;
            _captureManager = captureManager;
            _imageAnalyzer = imageAnalyzer;
            _screenGraph = configurationService.GetScreenDefinitions();
        }

        /// <summary>
        /// Знаходить найкоротший шлях до цільового екрана за допомогою BFS та виконує його.
        /// </summary>
        public async Task<ScreenDefinition> GoToScreenAsync(ScreenDefinitionId targetScreenId, bool navigateByChild = false)
        {
            var startState = await GetCurrentStateAsync();

            if (startState == null)
            {
                _logger.Error("Could not identify the current screen.");
                return null;
            }

            if (startState.Id == targetScreenId)
            {
                _logger.Info("Already on the target screen.");
                return _screenGraph[targetScreenId];
            }

            if (!_screenGraph.ContainsKey(startState.Id) || !_screenGraph.ContainsKey(targetScreenId))
            {
                _logger.Error(null, $"Start or target screen not found in graph. Start: {startState.Id}, Target: {targetScreenId}");
                return null;
            }

            var startNode = _screenGraph[startState.Id];
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
                await ClickOnElementAsync(transition);
                _logger.Info("Clicked on " + transition.TriggerElement.Name);
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
            
            using var screenshot = await _captureManager.CaptureFrameAsync();
            if (screenshot == null)
            {
                _logger.Warning("Failed to capture screenshot.");
                return null;
            }

            foreach (var screen in _screenGraph.Values)
            {
                if (screen.VerificationImage == null || string.IsNullOrEmpty(screen.VerificationImage.ImageTemplatePath))
                {
                    continue;
                }

                try
                {
                    var template = ImageResourceManager[screen.VerificationImage.ImageTemplatePath];
                    var foundRect = _imageAnalyzer.FindImage(screenshot, template, screen.VerificationImage.Area);

                    if (foundRect != default(Rectangle))
                    {
                        _logger.Info($"Current screen identified as {screen.Id}");
                        return screen;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, $"Failed to process verification image for screen {screen.Id} at path {screen.VerificationImage.ImageTemplatePath}");
                }
            }

            _logger.Warning("Could not identify current screen.");
            return null;
        }

        public Task ClickOnElementAsync(Transition transition)
        {
            return Click(transition.TriggerElement, transition.TargetScreen.VerificationImage);
        }

        public async Task<bool> IsElementVisibleAsync(UIElement elementName)
        {
            var currentScreen = await GetCurrentStateAsync();
            if (currentScreen == null || currentScreen.Id == ScreenDefinitionId.Unknown) return false;

            if (_screenGraph.TryGetValue(currentScreen.Id, out var screen))
            {
                var element = screen.UIElements.FirstOrDefault(e => e == elementName);
                if (element != null)
                {
                    _logger.Info($"Checking visibility of '{elementName}' on screen '{screen.Id}'");
                    // Тут буде логіка перевірки видимості елемента
                    return true;
                }
            }
            
            _logger.Info($"Element '{elementName}' not found on screen '{currentScreen.Id}'");
            return false;
        }

        /// <inheritdoc />
        public bool IsElementVisible(string elementName)
        {
            throw new NotImplementedException();
        }
    }
}
