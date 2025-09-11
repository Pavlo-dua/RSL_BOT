using RSLBot.Core.Interfaces;
namespace RSLBot.Core.Services;


using RSLBot.Shared.Interfaces;

public class ScenarioExecutor
{
    private readonly Dictionary<IScenario.ScenarioId, IScenario> scenarios;
    private readonly INavigator navigator;

    public ScenarioExecutor( INavigator navigator)
    {
        scenarios = new ()
        {
            // Тут будуть додаватися реалізації сценаріїв
            // new DungeonFarmScenario(_screenIdentifier, _navigator),
        };
    }

    public Task Run(IScenario.ScenarioId scenarioId)
    {
        //var scenario = scenarios.Find(s => s.Name == settings.ScenarioName);

        //if (scenario != null)
        //{
        //    Console.WriteLine($"Executing scenario: {scenario.Name}");

        //    return scenario.Execute();
        //}

        return Task.CompletedTask;
    }
}