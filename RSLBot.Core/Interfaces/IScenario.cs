namespace RSLBot.Core.Interfaces;

public interface IScenario
{
    public enum ScenarioId
    {
        ClassicArena,
        TagArena
    }

    ScenarioId Id { get; }

    Task ExecuteAsync();
}