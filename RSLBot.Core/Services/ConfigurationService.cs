using System.IO;
using RSLBot.Core.Interfaces;
using RSLBot.Core.Models.Dto;
using RSLBot.Shared.Models;
using RSLBot.Shared.Settings;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Linq;

namespace RSLBot.Core.Services
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly IReadOnlyDictionary<ScreenDefinitionId, ScreenDefinition> screenGraph;
        private readonly ArenaFarmingSettings arenaSettings;

        public ConfigurationService()
        {
            screenGraph = InitializeScreenGraph("Configuration\\ScreenDefinition");
            arenaSettings = LoadSettings<ArenaFarmingSettings>("Configuration\\arena_settings.json");
        }

        public IReadOnlyDictionary<ScreenDefinitionId, ScreenDefinition> GetScreenDefinitions()
        {
            return screenGraph;
        }

        public ArenaFarmingSettings GetArenaSettings()
        {
            return arenaSettings;
        }

        private static IReadOnlyDictionary<ScreenDefinitionId, ScreenDefinition> InitializeScreenGraph(string directoryPath)
        {
            // --- Етап 1: Десеріалізація JSON у DTO ---
            var screenDtos = LoadScreenDefinitionDtos(directoryPath);

            // Створюємо словник для швидкого доступу до створених вузлів графа
            var screenNodes = screenDtos.ToDictionary(
                dto => dto.Id,
                dto => new ScreenDefinition
                {
                    ParentId = dto.ParentId,
                    Id = dto.Id,
                    WindowHeight = dto.WindowHeight,
                    WindowWidth = dto.WindowWidth,
                    VerificationImages = dto.VerificationImages,
                    UIElements = dto.UIElements,
                });

            // --- Етап 2: Побудова графа та зв'язування посилань ---
            foreach (var dto in screenDtos)
            {
                var sourceNode = screenNodes[dto.Id];

                // Зв'язуємо переходи (Transitions)
                foreach (var transitionDto in dto.Transitions)
                {
                    if (screenNodes.TryGetValue(transitionDto.TargetScreenId, out var targetNode))
                    {
                        sourceNode.Transitions.Add(new Transition
                        {
                            TargetScreen = targetNode,
                            HorizontalSearch = transitionDto.HorizontalSearch,
                            TriggerElement = transitionDto.TriggerElement
                        });
                    }
                }

                // Зв'язуємо внутрішні екрани (InnerScreenDefinitions)
                foreach (var innerScreenId in dto.InnerScreenDefinitions)
                {
                    if (screenNodes.TryGetValue(innerScreenId, out var innerScreenNode))
                    {
                        sourceNode.InnerScreenDefinitions.Add(innerScreenNode);
                    }
                }
            }

            return screenNodes;
        }

        private static List<ScreenDefinitionDto> LoadScreenDefinitionDtos(string directoryPath)
        {
            var dtos = new List<ScreenDefinitionDto>();
            var files = Directory.GetFiles(directoryPath, "*.json");

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };

            foreach (var file in files)
            {
                var json = File.ReadAllText(file);
                var dto = JsonSerializer.Deserialize<ScreenDefinitionDto>(json, options);
                if (dto != null)
                {
                    dtos.Add(dto);
                }
            }
            return dtos;
        }

        private static T LoadSettings<T>(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Configuration file not found: {path}");
            }
            var json = File.ReadAllText(path);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };

            return JsonSerializer.Deserialize<T>(json, options);
        }
    }
}