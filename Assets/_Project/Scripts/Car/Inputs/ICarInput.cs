namespace RockyLeague.Car.Inputs
{
    /// <summary>
    /// Source of commands for a car (Strategy pattern).
    /// The car depends on this abstraction, not on who drives it (DIP), so the same
    /// car works with the player, the classic AI or an ML-Agents agent.
    /// </summary>
    public interface ICarInput
    {
        CarInputData ReadInput();
    }
}
