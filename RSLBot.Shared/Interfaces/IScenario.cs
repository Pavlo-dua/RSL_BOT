namespace RSLBot.Core.Interfaces;

public interface IScenario
{
    public enum ScenarioId
    {
        ClassicArena,
        TagArena,
        Twins,
        Minotaur
    }

    ScenarioId Id { get; }

    Task ExecuteAsync();
}