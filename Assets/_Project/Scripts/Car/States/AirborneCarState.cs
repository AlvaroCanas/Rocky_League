using RockyLeague.Core.StateMachines;

namespace RockyLeague.Car.States
{
    /// <summary>
    /// The car is in the air: no driving forces are applied, only gravity.
    /// Air control (pitch / yaw / roll) and the double jump will live here.
    /// </summary>
    public sealed class AirborneCarState : IState
    {
        public void Enter() { }

        public void Tick(float deltaTime) { }

        public void Exit() { }
    }
}
