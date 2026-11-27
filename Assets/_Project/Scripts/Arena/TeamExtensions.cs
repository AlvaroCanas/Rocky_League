namespace RockyLeague.Arena
{
    /// <summary>
    /// Helpers for <see cref="Team"/>. An enum cannot have methods, so C# extension
    /// methods let us write <c>team.Opponent()</c> as if it were one.
    /// </summary>
    public static class TeamExtensions
    {
        public static Team Opponent(this Team team)
        {
            return team == Team.Home ? Team.Away : Team.Home;
        }
    }
}
