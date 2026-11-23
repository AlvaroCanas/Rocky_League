namespace RockyLeague.Car.Inputs
{
    /// <summary>
    /// Null Object: a car without a driver simply receives no input,
    /// so the car never has to check for a missing input source.
    /// Stateless, so sharing one instance is safe between matches.
    /// </summary>
    public sealed class NullCarInput : ICarInput
    {
        public static readonly NullCarInput Instance = new NullCarInput();

        private NullCarInput() { }

        public CarInputData ReadInput() => CarInputData.None;
    }
}
