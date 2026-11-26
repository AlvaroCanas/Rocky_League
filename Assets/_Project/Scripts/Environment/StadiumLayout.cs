using RockyLeague.Arena;
using UnityEngine;

namespace RockyLeague.Environment
{
    /// <summary>
    /// Where the stands go, worked out once from the arena's size so every builder
    /// agrees on the same numbers. Distances in meters from the centre of the field.
    /// </summary>
    public sealed class StadiumLayout
    {
        private const float StandGap = 3f;

        public StadiumLayout(ArenaSettings arena, int tiers, float tierDepth, float tierRise)
        {
            Tiers = tiers;
            TierDepth = tierDepth;
            TierRise = tierRise;
            WallHeight = arena.WallHeight;

            float wallOuterX = arena.Width * 0.5f + arena.WallThickness;
            // Behind the goals, the stands start after the goal boxes.
            float wallOuterZ = arena.Length * 0.5f + 2f * arena.WallThickness + arena.GoalDepth;

            WallOuterX = wallOuterX;
            SideStandStart = wallOuterX + StandGap;
            EndStandStart = wallOuterZ + StandGap;
            SideStandLength = 2f * wallOuterZ;
            EndStandLength = 2f * wallOuterX;
        }

        public int Tiers { get; }
        public float TierDepth { get; }
        public float TierRise { get; }
        public float WallHeight { get; }

        /// <summary>Outer face of the side walls.</summary>
        public float WallOuterX { get; }

        public float SideStandStart { get; }
        public float EndStandStart { get; }
        public float SideStandLength { get; }
        public float EndStandLength { get; }

        public float StandDepth => Tiers * TierDepth;
        public float BackWallHeight => (Tiers + 2) * TierRise;

        /// <summary>Half size (x, z) of everything the stadium takes, stands included.</summary>
        public Vector2 OuterHalfSize => new Vector2(SideStandStart + StandDepth + 1f, EndStandStart + StandDepth + 1f);
    }
}
