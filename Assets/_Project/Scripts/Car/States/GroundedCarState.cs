using System;
using RockyLeague.Car.Inputs;
using RockyLeague.Core.StateMachines;

namespace RockyLeague.Car.States
{
    /// <summary>The car rests on a surface: it can drive and steer.</summary>
    public sealed class GroundedCarState : IState
    {
        private readonly CarMotor _motor;
        private readonly Func<CarInputData> _readInput;

        public GroundedCarState(CarMotor motor, Func<CarInputData> readInput)
        {
            _motor = motor ?? throw new ArgumentNullException(nameof(motor));
            _readInput = readInput ?? throw new ArgumentNullException(nameof(readInput));
        }

        public void Enter() { }

        public void Tick(float deltaTime) => _motor.Drive(_readInput(), deltaTime);

        public void Exit() { }
    }
}
