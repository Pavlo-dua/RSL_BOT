namespace RSLBot.Core.Interfaces;

public interface IScenario
{
    public enum ScenarioId
    {
        ClassicArena,
        TagArena,
        Twins
    }

    ScenarioId Id { get; }

    Task ExecuteAsync();
}