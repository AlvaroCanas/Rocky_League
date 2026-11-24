namespace RockyLeague.Core.StateMachines
{
    /// <summary>
    /// A single state of a <see cref="StateMachine"/>.
    /// States do not know about each other: transitions are owned by the machine,
    /// so new states can be added without modifying existing ones (OCP).
    /// </summary>
    public interface IState
    {
        void Enter();

        /// <param name="deltaTime">Time step of the loop that drives the machine (Update or FixedUpdate).</param>
        void Tick(float deltaTime);

        void Exit();
    }
}
