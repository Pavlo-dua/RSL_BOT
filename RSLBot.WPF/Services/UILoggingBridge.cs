using System.Reactive.Subjects;

namespace RSLBot.WPF.Services
{
    /// <summary>
    /// Міст між системою логування Serilog та UI.
    /// Надає потік повідомлень, на який може підписатися ViewModel.
    /// </summary>
    public interface IUILoggingBridge
    {
        IObservable<string> LogStream { get; }
        void PassMessage(string message);
    }

    public class UILoggingBridge : IUILoggingBridge
    {
        private readonly ISubject<string> logSubject = new ReplaySubject<string>(1);

        public IObservable<string> LogStream => logSubject;

        public void PassMessage(string message)
        {
            logSubject.OnNext(message);
        }
    }
}
