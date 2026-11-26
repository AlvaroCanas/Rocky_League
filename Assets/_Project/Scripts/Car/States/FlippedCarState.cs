using System;
using RockyLeague.Car.Inputs;
using RockyLeague.Core.StateMachines;

namespace RockyLeague.Car.States
{
    /// <summary>
    /// The car is lying on a face that is not its wheels (upside down, on its side, on its
    /// nose or on its tail). It cannot drive or boost; pressing jump flips it back onto
    /// its wheels (<see cref="CarRecovery"/>). It is never automatic: the driver decides.
    ///
    /// The jump is still ticked every step so its press detection stays up to date: a button
    /// held while falling does not count as a new press when the car lands on its side.
    /// </summary>
    public sealed class FlippedCarState : IState
    {
        private readonly CarBoost _boost;
        private readonly CarJump _jump;
        private readonly CarRecovery _recovery;
        private readonly Func<CarInputData> _readInput;

        public FlippedCarState(CarBoost boost, CarJump jump, CarRecovery recovery, Func<CarInputData> readInput)
        {
            _boost = boost ?? throw new ArgumentNullException(nameof(boost));
            _jump = jump ?? throw new ArgumentNullException(nameof(jump));
            _recovery = recovery ?? throw new ArgumentNullException(nameof(recovery));
            _readInput = readInput ?? throw new ArgumentNullException(nameof(readInput));
        }

        public void Enter() { }

        public void Tick(float deltaTime)
        {
            CarInputData input = _readInput();
            _boost.Recharge(deltaTime);
            _jump.Tick(input.Jump, false, deltaTime);

            if (_jump.JustPressed)
            {
                _recovery.Flip(input);
            }
        }

        public void Exit() { }
    }
}
