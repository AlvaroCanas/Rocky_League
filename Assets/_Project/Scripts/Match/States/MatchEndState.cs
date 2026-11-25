using RockyLeague.Core.StateMachines;

namespace RockyLeague.Match.States
{
    /// <summary>
    /// The match is over. It is a final state: nothing leaves it. Whoever is interested
    /// (results screen, "play again") reacts to the match phase changing to MatchEnd.
    /// </summary>
    public sealed class MatchEndState : IState
    {
        public void Enter()
        {
        }

        public void Tick(float deltaTime)
        {
        }

        public void Exit()
        {
        }
    }
}
