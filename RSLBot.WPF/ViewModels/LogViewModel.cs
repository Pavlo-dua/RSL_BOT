using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using RSLBot.WPF.Services;

namespace RSLBot.WPF.ViewModels
{
    using ReactiveUI;

    /// <summary>
    /// ViewModel для відображення логів.
    /// </summary>
    public class LogViewModel : ReactiveViewModelBase
    {
        public ObservableCollection<UILogEvent> LogMessages { get; } = new();

        public LogViewModel(IUILoggingBridge loggingBridge)
        {
            // Підписуємося на потік повідомлень від сервісу логування
            loggingBridge.LogStream
                .ObserveOn(RxApp.MainThreadScheduler) // Переконуємося, що оновлення UI відбувається в головному потоці
                .Subscribe(message =>
                {
                    LogMessages.Insert(0, message); // Додаємо нові повідомлення на початок списку
                    if (LogMessages.Count > 200)
                    {
                        LogMessages.RemoveAt(LogMessages.Count - 1); // Обмежуємо розмір логу
                    }
                });
        }
    }
}
