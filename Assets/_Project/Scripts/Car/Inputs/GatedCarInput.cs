using System;

namespace RockyLeague.Car.Inputs
{
    /// <summary>
    /// Decorator around any <see cref="ICarInput"/>: it passes the commands through while the
    /// gate is open and sends no input while it is closed (for example during a kickoff countdown).
    ///
    /// The car, the player input and the bots do not change at all: whoever builds the car wraps
    /// its input in a gate and decides when the gate is open with a function (OCP, DIP).
    /// To the car it is just one more <see cref="ICarInput"/> (LSP).
    /// </summary>
    public sealed class GatedCarInput : ICarInput
    {
        private readonly ICarInput _inner;
        private readonly Func<bool> _isOpen;

        public GatedCarInput(ICarInput inner, Func<bool> isOpen)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _isOpen = isOpen ?? throw new ArgumentNullException(nameof(isOpen));
        }

        public CarInputData ReadInput()
        {
            return _isOpen() ? _inner.ReadInput() : CarInputData.None;
        }
    }
}
