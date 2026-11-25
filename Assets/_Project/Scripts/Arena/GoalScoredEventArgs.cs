using System;

namespace RockyLeague.Arena
{
    /// <summary>Data sent with <see cref="Goal.GoalScored"/>: who scored and who conceded.</summary>
    public sealed class GoalScoredEventArgs : EventArgs
    {
        public GoalScoredEventArgs(Team scoringTeam)
        {
            ScoringTeam = scoringTeam;
        }

        public Team ScoringTeam { get; }
        public Team ConcedingTeam => ScoringTeam.Opponent();
    }
}
