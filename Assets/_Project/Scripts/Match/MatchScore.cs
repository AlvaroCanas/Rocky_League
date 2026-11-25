using System;
using RockyLeague.Arena;

namespace RockyLeague.Match
{
    /// <summary>
    /// What changes DURING a match: goals of each team and time left.
    /// Plain C# (no Unity types), so it can be tested without opening Unity.
    ///
    /// It only stores and answers questions (who is winning? is time up?); it does not
    /// decide what happens next. That decision belongs to the match state machine (SRP).
    /// What was decided BEFORE the match (its duration, rules...) lives in MatchConfig.
    /// </summary>
    public sealed class MatchScore
    {
        private int _blueGoals;
        private int _orangeGoals;

        public float TimeRemaining { get; private set; }

        public bool IsTimeUp => TimeRemaining <= 0f;

        public bool IsTied => _blueGoals == _orangeGoals;

        /// <summary>The team that is ahead, or null while the score is tied.</summary>
        public Team? Winner
        {
            get
            {
                if (IsTied)
                {
                    return null;
                }

                return _blueGoals > _orangeGoals ? Team.Blue : Team.Orange;
            }
        }

        public MatchScore(float matchDuration)
        {
            if (matchDuration <= 0f) throw new ArgumentOutOfRangeException(nameof(matchDuration), "The match must last more than zero seconds.");

            TimeRemaining = matchDuration;
        }

        public int GetGoals(Team team) => team == Team.Blue ? _blueGoals : _orangeGoals;

        /// <summary>Adds a goal for the team that SCORED it (not the one that conceded it).</summary>
        public void AddGoal(Team scorer)
        {
            if (scorer == Team.Blue)
            {
                _blueGoals++;
            }
            else
            {
                _orangeGoals++;
            }
        }

        public void TickTime(float deltaTime)
        {
            TimeRemaining = Math.Max(0f, TimeRemaining - deltaTime);
        }
    }
}
