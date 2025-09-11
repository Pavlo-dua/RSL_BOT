using RSLBot.Core.Services;
using System;

namespace RSLBot.Telegram;

public class TelegramBotManager
{
    private readonly ScenarioExecutor scenarioExecutor;

    public TelegramBotManager(ScenarioExecutor scenarioExecutor)
    {
        this.scenarioExecutor = scenarioExecutor;
    }

    public void Start()
    {
        // TODO: Додати логіку ініціалізації Telegram.Bot клієнта
        // Потрібно буде додати NuGet пакет Telegram.Bot
        Console.WriteLine("Telegram Bot Manager started.");
        // bot.OnMessage += HandleMessage;
        // bot.StartReceiving();
    }

    private void HandleMessage(object sender, EventArgs e)
    {
        // TODO: Обробка команд від користувача
        // var message = e.Message;
        // if (message.Text.StartsWith("/start_arena")) {
        //     var settings = new ScenarioSettings { Name = "Arena", IsEnabled = true };
        //     _scenarioExecutor.Run(settings);
        // }
        // if (message.Text.StartsWith("/screenshot")) {
        //     // Логіка відправки скріншота
        // }
    }

    public void Stop()
    {
        Console.WriteLine("Telegram Bot Manager stopped.");
    }
}