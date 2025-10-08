using System;
using System.Reactive.Subjects;

namespace RSLBot.WPF.Services
{
    public enum UILogLevel
    {
        Info,
        Warning,
        Error
    }

    public class UILogEvent
    {
        public DateTime Timestamp { get; set; }
        public UILogLevel Level { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Exception { get; set; }
    }

    /// <summary>
    /// Міст між системою логування Serilog та UI.
    /// Надає потік повідомлень, на який може підписатися ViewModel.
    /// </summary>
    public interface IUILoggingBridge
    {
        IObservable<UILogEvent> LogStream { get; }
        void PassMessage(UILogEvent logEvent);
    }

    public class UILoggingBridge : IUILoggingBridge
    {
        private readonly ISubject<UILogEvent> logSubject = new ReplaySubject<UILogEvent>(1);

        public IObservable<UILogEvent> LogStream => logSubject;

        public void PassMessage(UILogEvent logEvent)
        {
            logSubject.OnNext(logEvent);
        }
    }
}
