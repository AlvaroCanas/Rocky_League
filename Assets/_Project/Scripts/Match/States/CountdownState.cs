using System;
using RockyLeague.Core.StateMachines;

namespace RockyLeague.Match.States
{
    /// <summary>
    /// Count-down before kickoff ("3, 2, 1"). It is a plain timer driven by Tick:
    /// there is nothing else to do while waiting, so a coroutine would add nothing here (KISS).
    /// The match state machine reads <see cref="IsFinished"/> to decide when to start playing.
    /// </summary>
    public sealed class CountdownState : IState
    {
        private readonly float _duration;

        public float SecondsRemaining { get; private set; }

        public bool IsFinished => SecondsRemaining <= 0f;

        public CountdownState(float durationSeconds)
        {
            if (durationSeconds < 0f) throw new ArgumentOutOfRangeException(nameof(durationSeconds), "The countdown cannot be negative.");

            _duration = durationSeconds;
        }

        public void Enter()
        {
            SecondsRemaining = _duration;
        }

        public void Tick(float deltaTime)
        {
            SecondsRemaining = Math.Max(0f, SecondsRemaining - deltaTime);
        }

        public void Exit()
        {
        }
    }
}
