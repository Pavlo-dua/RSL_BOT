namespace RSLBot.Core.Interfaces;

public interface IScenario
{
    public enum ScenarioId
    {
        Arena
    }

    ScenarioId Id { get; }

    Task ExecuteAsync();
}