namespace RockyLeague.Match
{
    /// <summary>
    /// The stage a match is in, as seen from outside (UI, cars, rules...).
    /// It is a read-only summary of the match state machine: other systems ask
    /// "which phase are we in?" without knowing the state classes behind it (Law of Demeter).
    /// </summary>
    public enum MatchPhase
    {
        Countdown,
        Playing,
        MatchEnd
    }
}
