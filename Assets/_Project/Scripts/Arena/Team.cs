namespace RockyLeague.Arena
{
    /// <summary>
    /// The two sides of a match. Home defends the goal at negative Z,
    /// Away defends the goal at positive Z. The human player always plays at Home.
    ///
    /// A team is an identity, not a colour: the colours of each side are a separate,
    /// visual setting, so they can be made customisable without touching any game logic.
    /// The values keep the old order (Home = 0, Away = 1) so teams already saved in
    /// scenes and prefabs are still read correctly.
    /// </summary>
    public enum Team
    {
        Home,
        Away
    }
}
