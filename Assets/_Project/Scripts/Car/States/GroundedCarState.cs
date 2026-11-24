using System;
using RockyLeague.Car.Inputs;
using RockyLeague.Core.StateMachines;

namespace RockyLeague.Car.States
{
    /// <summary>The car rests on a surface: it can drive, steer and boost.</summary>
    public sealed class GroundedCarState : IState
    {
        private readonly CarMotor _motor;
        private readonly CarBoost _boost;
        private readonly Func<CarInputData> _readInput;

        public GroundedCarState(CarMotor motor, CarBoost boost, Func<CarInputData> readInput)
        {
            _motor = motor ?? throw new ArgumentNullException(nameof(motor));
            _boost = boost ?? throw new ArgumentNullException(nameof(boost));
            _readInput = readInput ?? throw new ArgumentNullException(nameof(readInput));
        }

        public void Enter() { }

        public void Tick(float deltaTime)
        {
            CarInputData input = _readInput();
            _motor.Drive(input, deltaTime);
            _boost.Tick(input.Boost, deltaTime);
        }

        public void Exit() { }
    }
}
