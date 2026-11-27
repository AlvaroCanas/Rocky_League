using System;

namespace RockyLeague.Arena
{
    /// <summary>
    /// Something that announces goals. A <see cref="Goal"/> announces every time the ball goes
    /// in; the match referee (MatchController) only announces the goals that count. Effects and
    /// UI depend on this small interface (ISP, DIP), so they can listen to either one.
    /// </summary>
    public interface IGoalSource
    {
        event EventHandler<GoalScoredEventArgs> GoalScored;
    }
}
