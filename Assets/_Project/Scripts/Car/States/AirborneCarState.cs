using System;
using RockyLeague.Core.StateMachines;

namespace RockyLeague.Car.States
{
    /// <summary>
    /// The car is in the air: no driving forces are applied, only gravity.
    /// Boost cannot be used here (the game has no flying), but it keeps recharging.
    /// </summary>
    public sealed class AirborneCarState : IState
    {
        private readonly CarBoost _boost;

        public AirborneCarState(CarBoost boost)
        {
            _boost = boost ?? throw new ArgumentNullException(nameof(boost));
        }

        public void Enter() { }

        public void Tick(float deltaTime)
        {
            _boost.Recharge(deltaTime);
        }

        public void Exit() { }
    }
}
