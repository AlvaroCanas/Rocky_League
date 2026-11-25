using System;

namespace RockyLeague.Match
{
    /// <summary>
    /// The match clock: how much time is left. Plain C# (no Unity types), so it can be
    /// tested without opening Unity.
    ///
    /// It deliberately knows nothing about goals: the score lives in a single place, the
    /// <see cref="ScoreKeeper"/>, so there are never two counters that can disagree (DRY, SRP).
    /// What was decided BEFORE the match (its duration, rules...) lives in MatchConfig.
    /// </summary>
    public sealed class MatchClock
    {
        public float TimeRemaining { get; private set; }

        public bool IsTimeUp => TimeRemaining <= 0f;

        public MatchClock(float matchDuration)
        {
            if (matchDuration <= 0f) throw new ArgumentOutOfRangeException(nameof(matchDuration), "The match must last more than zero seconds.");

            TimeRemaining = matchDuration;
        }

        public void Tick(float deltaTime)
        {
            TimeRemaining = Math.Max(0f, TimeRemaining - deltaTime);
        }
    }
}
