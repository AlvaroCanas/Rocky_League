using System;

namespace RockyLeague.Core.Game
{
    /// <summary>
    /// Everything that is decided BEFORE a match starts: how many cars each team has,
    /// how good the bots are, how long it lasts, how long the countdown and the goal celebration are...
    ///
    /// It is plain, immutable data (no MonoBehaviour, no Unity types): the menu builds it,
    /// the <see cref="GameManager"/> carries it across scenes and the match reads it.
    /// Being immutable means nobody can change the rules in the middle of a match, and
    /// validating in the constructor means an invalid configuration can never exist.
    ///
    /// 1v1 and 2v2 are not different code, just different configurations (OCP):
    /// a new mode is a new factory method here, not a new scene or a new class.
    /// What changes DURING the match is not here: the clock is the MatchClock and the goals are the ScoreKeeper.
    /// </summary>
    public sealed class MatchConfig
    {
        public const float DefaultMatchDuration = 180f;
        public const float DefaultCountdownSeconds = 3f;
        public const float DefaultGoalCelebrationSeconds = 3f;

        /// <summary>Length of the regular time, in seconds.</summary>
        public float MatchDuration { get; }

        /// <summary>Cars in each team (human player and bots together).</summary>
        public int CarsPerTeam { get; }

        /// <summary>How good the bots are (team-mate and rivals alike).</summary>
        public DifficultyLevel Difficulty { get; }

        /// <summary>Seconds of countdown before kickoff (at the start and after every goal).</summary>
        public float CountdownSeconds { get; }

        /// <summary>Seconds between a goal and the reset of the field, while the cars keep moving.</summary>
        public float GoalCelebrationSeconds { get; }

        /// <summary>If true, a tie at the end of the regular time goes to a golden-goal overtime.</summary>
        public bool OvertimeEnabled { get; }

        public MatchConfig(
            int carsPerTeam,
            DifficultyLevel difficulty = DifficultyLevel.Normal,
            float matchDuration = DefaultMatchDuration,
            float countdownSeconds = DefaultCountdownSeconds,
            float goalCelebrationSeconds = DefaultGoalCelebrationSeconds,
            bool overtimeEnabled = true)
        {
            if (carsPerTeam < 1) throw new ArgumentOutOfRangeException(nameof(carsPerTeam), "A team needs at least one car.");
            if (!Enum.IsDefined(typeof(DifficultyLevel), difficulty)) throw new ArgumentOutOfRangeException(nameof(difficulty), "Unknown difficulty level.");
            if (matchDuration <= 0f) throw new ArgumentOutOfRangeException(nameof(matchDuration), "The match must last more than zero seconds.");
            if (countdownSeconds < 0f) throw new ArgumentOutOfRangeException(nameof(countdownSeconds), "The countdown cannot be negative.");
            if (goalCelebrationSeconds < 0f) throw new ArgumentOutOfRangeException(nameof(goalCelebrationSeconds), "The celebration cannot be negative.");

            CarsPerTeam = carsPerTeam;
            Difficulty = difficulty;
            MatchDuration = matchDuration;
            CountdownSeconds = countdownSeconds;
            GoalCelebrationSeconds = goalCelebrationSeconds;
            OvertimeEnabled = overtimeEnabled;
        }

        public static MatchConfig OneVsOne(DifficultyLevel difficulty = DifficultyLevel.Normal) =>
            new MatchConfig(carsPerTeam: 1, difficulty: difficulty);

        public static MatchConfig TwoVsTwo(DifficultyLevel difficulty = DifficultyLevel.Normal) =>
            new MatchConfig(carsPerTeam: 2, difficulty: difficulty);

        /// <summary>
        /// A copy of this configuration with another duration. The config is immutable, so
        /// "changing" it means building a new one (the original is left untouched).
        /// </summary>
        public MatchConfig WithDuration(float matchDuration) =>
            new MatchConfig(CarsPerTeam, Difficulty, matchDuration, CountdownSeconds, GoalCelebrationSeconds, OvertimeEnabled);

        /// <summary>Short summary for logs, e.g. "2v2 | Hard | 180 s".</summary>
        public override string ToString() => $"{CarsPerTeam}v{CarsPerTeam} | {Difficulty} | {MatchDuration:0} s";
    }
}
