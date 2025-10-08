using RSLBot.Core.Interfaces;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace RSLBot.WPF.Services
{

    /// <summary>
    /// Реалізація сервісу логування на базі Serilog.
    /// </summary>
    public class SerilogLoggingService : ILoggingService
    {
        private readonly ILogger logger;
        private readonly IUILoggingBridge loggingBridge;

        public SerilogLoggingService(IUILoggingBridge loggingBridge)
        {
            this.loggingBridge = loggingBridge;
            logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.WithProperty("Process", Process.GetCurrentProcess().ProcessName)
                .Enrich.FromLogContext()
                .WriteTo.File(
                    path: "logs/bot_log.txt",
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] ({SourceContext}) {Message:lj}{NewLine}{Exception}")
                .WriteTo.Sink(new ObservableSink(loggingBridge))
                .CreateLogger();
        }

        // UI-intended logs: go to both UI and file
        public void Info(string message)
        {
            PushUi(UILogLevel.Info, message);
            logger.ForContext("SourceContext", GetCaller()).Information(message);
        }

        public void Warning(string message)
        {
            PushUi(UILogLevel.Warning, message);
            logger.ForContext("SourceContext", GetCaller()).Warning(message);
        }

        public void Error(Exception ex, string message)
        {
            PushUi(UILogLevel.Error, message + (ex != null ? "\n" + ex : string.Empty));
            logger.ForContext("SourceContext", GetCaller()).Error(ex, message);
        }

        public void Error(string message)
        {
            PushUi(UILogLevel.Error, message);
            logger.ForContext("SourceContext", GetCaller()).Error(message);
        }

        private void PushUi(UILogLevel level, string message)
        {
            loggingBridge.PassMessage(new UILogEvent
            {
                Timestamp = DateTime.Now,
                Level = level,
                Message = message
            });
        }

        private static string GetCaller([CallerFilePath] string file = "", [CallerMemberName] string member = "", [CallerLineNumber] int line = 0)
            => System.IO.Path.GetFileName(file) + ":" + line + " " + member;
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
            var uiLevel = logEvent.Level switch
            {
                LogEventLevel.Information => UILogLevel.Info,
                LogEventLevel.Warning => UILogLevel.Warning,
                LogEventLevel.Error => UILogLevel.Error,
                LogEventLevel.Fatal => UILogLevel.Error,
                _ => UILogLevel.Info
            };
            loggingBridge.PassMessage(new UILogEvent
            {
                Timestamp = logEvent.Timestamp.ToLocalTime().DateTime,
                Level = uiLevel,
                Message = logEvent.RenderMessage(),
                Exception = logEvent.Exception?.ToString()
            });
        }
    }
}