using RSLBot.Shared.Models;
using RSLBot.Shared.Settings;
using System.Collections.Generic;

namespace RSLBot.Core.Interfaces
{
    /// <summary>
    /// Відповідає за завантаження та надання конфігураційних даних з файлів.
    /// </summary>
    public interface IConfigurationService
    {
        /// <summary>
        /// Повертає словник усіх визначених екранів гри.
        /// </summary>
        IReadOnlyDictionary<ScreenDefinitionId, ScreenDefinition> GetScreenDefinitions();

        /// <summary>
        /// Завантажує та повертає налаштування для сценарію Арени.
        /// </summary>
        ArenaFarmingSettings GetArenaSettings();
    }
}