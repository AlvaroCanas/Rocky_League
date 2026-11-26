using System;
using RockyLeague.Car.Inputs;
using RockyLeague.Core.StateMachines;

namespace RockyLeague.Car.States
{
    /// <summary>
    /// The car is in the air: no driving forces are applied, only gravity, but the driver
    /// can rotate it (pitch, yaw and roll).
    /// Boost and jump cannot be used here yet, but boost keeps recharging and the jump
    /// keeps tracking the button.
    /// </summary>
    public sealed class AirborneCarState : IState
    {
        private readonly CarBoost _boost;
        private readonly CarJump _jump;
        private readonly CarAirControl _airControl;
        private readonly Func<CarInputData> _readInput;

        public AirborneCarState(CarBoost boost, CarJump jump, CarAirControl airControl, Func<CarInputData> readInput)
        {
            _boost = boost ?? throw new ArgumentNullException(nameof(boost));
            _jump = jump ?? throw new ArgumentNullException(nameof(jump));
            _airControl = airControl ?? throw new ArgumentNullException(nameof(airControl));
            _readInput = readInput ?? throw new ArgumentNullException(nameof(readInput));
        }

        public void Enter() { }

        public void Tick(float deltaTime)
        {
            CarInputData input = _readInput();
            _boost.Recharge(deltaTime);
            _jump.Tick(input.Jump, false, deltaTime);
            _airControl.Apply(input, deltaTime);
        }

        public void Exit() { }
    }
}
