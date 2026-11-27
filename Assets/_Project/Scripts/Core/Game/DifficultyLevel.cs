namespace RockyLeague.Core.Game
{
    /// <summary>
    /// How good the bots are, as chosen in the menu.
    ///
    /// It is a plain enum, not a reference to the AIDifficulty assets, so the
    /// <see cref="MatchConfig"/> stays plain C# with no Unity types. Turning a level into
    /// the matching AIDifficulty asset is the job of whoever builds the match (the factory).
    /// </summary>
    public enum DifficultyLevel
    {
        Easy,
        Normal,
        Hard
    }
}
