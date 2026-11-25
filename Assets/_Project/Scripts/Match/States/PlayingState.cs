using System;
using RockyLeague.Core.StateMachines;

namespace RockyLeague.Match.States
{
    /// <summary>
    /// The ball is in play and the clock runs. The state only makes time pass;
    /// deciding that the match is over when time is up is the job of the transition
    /// registered in the MatchController, so this state does not know what comes next (OCP).
    /// </summary>
    public sealed class PlayingState : IState
    {
        private readonly MatchScore _score;

        public PlayingState(MatchScore score)
        {
            _score = score ?? throw new ArgumentNullException(nameof(score));
        }

        public void Enter()
        {
        }

        public void Tick(float deltaTime)
        {
            _score.TickTime(deltaTime);
        }

        public void Exit()
        {
        }
    }
}
