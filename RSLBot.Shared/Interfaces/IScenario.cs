namespace RSLBot.Core.Interfaces;

public interface IScenario
{
    public enum ScenarioId
    {
        ClassicArena,
        TagArena,
        Twins,
        Minotaur,
        Shogun,
        SandDevil
    }

    ScenarioId Id { get; }

    Task ExecuteAsync();
}