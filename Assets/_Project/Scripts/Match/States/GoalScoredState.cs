using RockyLeague.Core.StateMachines;

namespace RockyLeague.Match.States
{
    /// <summary>
    /// A goal has just been scored: the ball is gone, a message is on screen and the cars keep
    /// moving. The state does not measure the pause itself: whoever runs the celebration (a
    /// coroutine in the MatchController, which can later add effects) calls <see cref="Finish"/>
    /// when it is over, and the match state machine moves on to the next kickoff.
    /// </summary>
    public sealed class GoalScoredState : IState
    {
        public bool IsFinished { get; private set; }

        public void Finish()
        {
            IsFinished = true;
        }

        public void Enter()
        {
            IsFinished = false;
        }

        public void Tick(float deltaTime)
        {
        }

        public void Exit()
        {
        }
    }
}
