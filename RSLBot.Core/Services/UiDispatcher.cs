namespace RSLBot.Core.Services;
using System;
using System.Threading.Tasks;
using System.Windows.Threading;



/// <summary>
/// Надає глобальний доступ до UI Dispatcher'а.
/// Має бути ініціалізований один раз при старті додатку.
/// </summary>
public static class UiDispatcher
{
    /// <summary>
    /// Головний Dispatcher додатку.
    /// </summary>
    public static Dispatcher? Dispatcher { get; private set; }

    /// <summary>
    /// Метод для ініціалізації. Викликається один раз з App.xaml.cs.
    /// </summary>
    public static void Initialize(Dispatcher dispatcher)
    {
        Dispatcher = dispatcher;
    }

    /// <summary>
    /// Виконує дію в потоці UI асинхронно.
    /// </summary>
    public static Task InvokeAsync(Action action)
    {
        return Dispatcher?.InvokeAsync(action).Task ?? Task.CompletedTask;
    }

    /// <summary>
    /// Виконує функцію в потоці UI асинхронно і повертає результат.
    /// </summary>
    public static Task<TResult> InvokeAsync<TResult>(Func<TResult> func)
    {
        return Dispatcher?.InvokeAsync(func).Task ?? Task.FromResult(default(TResult))!;
    }
}
