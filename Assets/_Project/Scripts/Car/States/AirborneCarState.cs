using System;
using RockyLeague.Car.Inputs;
using RockyLeague.Core.StateMachines;

namespace RockyLeague.Car.States
{
    /// <summary>
    /// The car is in the air: no driving forces are applied, only gravity, but the driver can
    /// rotate it (pitch, yaw and roll) and use the air jump once (<see cref="CarDodge"/>): a
    /// double jump, or a dodge if a direction is held. A dodge moves the car to the Dodging state.
    ///
    /// Boost cannot be used in the air (the game has no flying), but it keeps recharging.
    /// </summary>
    public sealed class AirborneCarState : IState
    {
        private readonly CarBoost _boost;
        private readonly CarJump _jump;
        private readonly CarDodge _dodge;
        private readonly CarAirControl _airControl;
        private readonly Func<CarInputData> _readInput;

        public AirborneCarState(CarBoost boost, CarJump jump, CarDodge dodge, CarAirControl airControl, Func<CarInputData> readInput)
        {
            _boost = boost ?? throw new ArgumentNullException(nameof(boost));
            _jump = jump ?? throw new ArgumentNullException(nameof(jump));
            _dodge = dodge ?? throw new ArgumentNullException(nameof(dodge));
            _airControl = airControl ?? throw new ArgumentNullException(nameof(airControl));
            _readInput = readInput ?? throw new ArgumentNullException(nameof(readInput));
        }

        public void Enter() { }

        public void Tick(float deltaTime)
        {
            CarInputData input = _readInput();
            _boost.Recharge(deltaTime);

            // The ground jump is not allowed here, but CarJump still detects new presses.
            _jump.Tick(input.Jump, false, deltaTime);
            if (_jump.JustPressed && _dodge.TryAirJump(input) && _dodge.IsDodging)
            {
                // A dodge has just started: its spin must not be overwritten by air control.
                return;
            }

            _airControl.Apply(input, deltaTime);
        }

        public void Exit() { }
    }
}
