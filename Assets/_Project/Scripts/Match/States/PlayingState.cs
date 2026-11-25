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
        private readonly MatchClock _clock;

        public PlayingState(MatchClock clock)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public void Enter()
        {
        }

        public void Tick(float deltaTime)
        {
            _clock.Tick(deltaTime);
        }

        public void Exit()
        {
        }
    }
}
