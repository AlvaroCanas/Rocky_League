using System;
using RockyLeague.Arena;
using UnityEngine;

namespace RockyLeague.Match
{
    /// <summary>
    /// Keeps the score of the match and publishes <see cref="ScoreChanged"/> (Observer) for
    /// whoever needs it (scoreboard, stadium screens).
    ///
    /// It does not listen to the goals itself: the referee (<see cref="MatchController"/>)
    /// decides which goals count and calls <see cref="AddGoal"/>. So a ball that rolls into a
    /// goal when the ball is not in play (after the final whistle...) never reaches the score:
    /// there is a single place that decides what a valid goal is (SRP, no duplicated rules).
    /// </summary>
    public sealed class ScoreKeeper : MonoBehaviour
    {
        private int _homeGoals;
        private int _awayGoals;

        /// <summary>Raised after every change with (home goals, away goals).</summary>
        public event Action<int, int> ScoreChanged;

        public int GetGoals(Team team)
        {
            return team == Team.Home ? _homeGoals : _awayGoals;
        }

        public bool IsTied => _homeGoals == _awayGoals;

        /// <summary>Team with more goals. Only meaningful when not <see cref="IsTied"/>.</summary>
        public Team Leader => _homeGoals > _awayGoals ? Team.Home : Team.Away;

        /// <summary>Counts a valid goal for <paramref name="scoringTeam"/>.</summary>
        public void AddGoal(Team scoringTeam)
        {
            if (scoringTeam == Team.Home)
            {
                _homeGoals++;
            }
            else
            {
                _awayGoals++;
            }

            ScoreChanged?.Invoke(_homeGoals, _awayGoals);
        }

        /// <summary>Back to 0 - 0, for a new match.</summary>
        public void ResetScore()
        {
            _homeGoals = 0;
            _awayGoals = 0;
            ScoreChanged?.Invoke(_homeGoals, _awayGoals);
        }
    }
}
