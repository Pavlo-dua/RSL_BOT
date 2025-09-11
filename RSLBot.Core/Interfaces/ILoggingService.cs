using System;

namespace RSLBot.Core.Interfaces
{
    /// <summary>
    /// Універсальний інтерфейс для системи логування.
    /// </summary>
    public interface ILoggingService
    {
        /// <summary>
        /// Логує інформаційне повідомлення.
        /// </summary>
        void Info(string message);

        /// <summary>
        /// Логує попередження.
        /// </summary>
        void Warning(string message);

        /// <summary>
        /// Логує помилку.
        /// </summary>
        void Error(Exception ex, string message);

        void Error(string message);
    }
}
