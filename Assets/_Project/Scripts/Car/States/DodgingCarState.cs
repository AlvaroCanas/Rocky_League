using System;
using RockyLeague.Car.Inputs;
using RockyLeague.Core.StateMachines;

namespace RockyLeague.Car.States
{
    /// <summary>
    /// The car is doing a dodge (flip): it spins one full turn on its own and the driver cannot
    /// rotate it meanwhile (no air control). Boost keeps recharging and the jump keeps tracking
    /// the button, as in the other air states.
    ///
    /// The state machine leaves it when the dodge is over (back to Airborne) or when the car lands
    /// in the middle of it (Grounded). Either way <see cref="Exit"/> stops the dodge, so the
    /// Rigidbody never keeps the raised angular speed limit.
    /// </summary>
    public sealed class DodgingCarState : IState
    {
        private readonly CarBoost _boost;
        private readonly CarJump _jump;
        private readonly CarDodge _dodge;
        private readonly Func<CarInputData> _readInput;

        public DodgingCarState(CarBoost boost, CarJump jump, CarDodge dodge, Func<CarInputData> readInput)
        {
            _boost = boost ?? throw new ArgumentNullException(nameof(boost));
            _jump = jump ?? throw new ArgumentNullException(nameof(jump));
            _dodge = dodge ?? throw new ArgumentNullException(nameof(dodge));
            _readInput = readInput ?? throw new ArgumentNullException(nameof(readInput));
        }

        public void Enter() { }

        public void Tick(float deltaTime)
        {
            CarInputData input = _readInput();
            _boost.Recharge(deltaTime);
            _jump.Tick(input.Jump, false, deltaTime);
            _dodge.Tick(deltaTime);
        }

        public void Exit()
        {
            _dodge.Stop();
        }
    }
}
