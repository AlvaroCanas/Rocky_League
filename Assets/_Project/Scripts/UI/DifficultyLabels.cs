using RockyLeague.Core.Game;

namespace RockyLeague.UI
{
    /// <summary>
    /// How each difficulty is written on screen. Kept in the UI: the game logic only knows the
    /// enum, and the words the player reads can change without touching it.
    /// </summary>
    public static class DifficultyLabels
    {
        public static string ToLabel(this DifficultyLevel level)
        {
            return level switch
            {
                DifficultyLevel.Easy => "EASY",
                DifficultyLevel.Normal => "MEDIUM",
                DifficultyLevel.Hard => "HARD",
                _ => level.ToString().ToUpperInvariant()
            };
        }
    }
}
