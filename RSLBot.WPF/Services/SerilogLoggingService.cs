using RSLBot.Core.Interfaces;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System.Text;

namespace RSLBot.WPF.Services
{

    /// <summary>
    /// Реалізація сервісу логування на базі Serilog.
    /// </summary>
    public class SerilogLoggingService : ILoggingService
    {
        private readonly ILogger logger;

        public SerilogLoggingService(IUILoggingBridge loggingBridge)
        {
            logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File("logs/bot_log.txt", rollingInterval: RollingInterval.Day)
                .WriteTo.Sink(new ObservableSink(loggingBridge)) // Кастомний приймач для UI
                .CreateLogger();
        }

        public void Info(string message) => logger.Information(message);
        public void Warning(string message) => logger.Warning(message);
        public void Error(Exception ex, string message) => logger.Error(ex, message);
        public void Error(string message) => logger.Error(message);
    }

    /// <summary>
    /// Кастомний "приймач" (Sink) для Serilog, що перенаправляє повідомлення в UI.
    /// </summary>
    public class ObservableSink : ILogEventSink
    {
        private readonly IUILoggingBridge loggingBridge;

        public ObservableSink(IUILoggingBridge loggingBridge)
        {
            this.loggingBridge = loggingBridge;
        }

        public void Emit(LogEvent logEvent)
        {
            var message = RenderLogEvent(logEvent);
            loggingBridge.PassMessage(message);
        }

        private static string RenderLogEvent(LogEvent logEvent)
        {
            var sb = new StringBuilder();
            sb.Append(logEvent.Timestamp.ToString("HH:mm:ss"));
            sb.Append($" [{logEvent.Level.ToString().ToUpper()}] ");
            sb.Append(logEvent.RenderMessage());
            if (logEvent.Exception != null)
            {
                sb.Append($"\n{logEvent.Exception}");
            }
            return sb.ToString();
        }
    }
}