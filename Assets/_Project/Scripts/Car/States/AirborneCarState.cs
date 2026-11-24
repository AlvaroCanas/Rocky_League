using System;
using RockyLeague.Car.Inputs;
using RockyLeague.Core.StateMachines;

namespace RockyLeague.Car.States
{
    /// <summary>
    /// The car is in the air: no driving forces are applied, only gravity.
    /// Boost and jump cannot be used here (the game has no flying), but boost keeps
    /// recharging and the jump keeps tracking the button.
    /// </summary>
    public sealed class AirborneCarState : IState
    {
        private readonly CarBoost _boost;
        private readonly CarJump _jump;
        private readonly Func<CarInputData> _readInput;

        public AirborneCarState(CarBoost boost, CarJump jump, Func<CarInputData> readInput)
        {
            _boost = boost ?? throw new ArgumentNullException(nameof(boost));
            _jump = jump ?? throw new ArgumentNullException(nameof(jump));
            _readInput = readInput ?? throw new ArgumentNullException(nameof(readInput));
        }

        public void Enter() { }

        public void Tick(float deltaTime)
        {
            CarInputData input = _readInput();
            _boost.Recharge(deltaTime);
            _jump.Tick(input.Jump, false, deltaTime);
        }

        public void Exit() { }
    }
}
