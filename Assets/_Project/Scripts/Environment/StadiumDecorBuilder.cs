using System;
using RockyLeague.Arena;
using RockyLeague.Core.Building;
using RockyLeague.Match;
using TMPro;
using UnityEngine;

namespace RockyLeague.Environment
{
    /// <summary>
    /// What makes the stadium look like a real one: floodlight towers with glowing lamps,
    /// a giant screen above each end stand that shows the live score, and ad boards
    /// around the field. Texts and screens go under the dynamic parent (they change).
    /// </summary>
    public sealed class StadiumDecorBuilder
    {
        private const float TowerHeight = 34f;
        private const float ScreenWidth = 22f;
        private const float ScreenHeight = 10f;
        private const float BoardLength = 9f;
        private const float BoardSpacing = 10f;

        private readonly BlockBuilder _scenery;
        private readonly Transform _dynamicRoot;
        private readonly Palette _palette;
        private readonly StadiumLayout _layout;
        private readonly EnvironmentColours _colours;
        private readonly Material _lampMaterial;

        public StadiumDecorBuilder(BlockBuilder scenery, Transform dynamicRoot, Palette palette, StadiumLayout layout,
            EnvironmentColours colours, Material lampMaterial)
        {
            _scenery = scenery ?? throw new ArgumentNullException(nameof(scenery));
            _dynamicRoot = dynamicRoot;
            _palette = palette ?? throw new ArgumentNullException(nameof(palette));
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _colours = colours ?? throw new ArgumentNullException(nameof(colours));
            _lampMaterial = lampMaterial;
        }

        public void BuildFloodlights()
        {
            Vector2 outer = _layout.OuterHalfSize;
            Material tower = _palette.Paint(_colours.Tower);
            Material lamp = _lampMaterial != null ? _lampMaterial : _palette.Paint(Color.white);

            foreach (Vector3 corner in new[] { new Vector3(-1f, 0f, -1f), new Vector3(1f, 0f, -1f), new Vector3(-1f, 0f, 1f), new Vector3(1f, 0f, 1f) })
            {
                Vector3 foot = new Vector3(corner.x * (outer.x + 4f), 0f, corner.z * (outer.y + 4f));
                _scenery.Decoration("Floodlight_Mast", foot + Vector3.up * (TowerHeight * 0.5f), new Vector3(1.2f, TowerHeight, 1.2f), tower);

                // The lamp panel leans towards the centre of the field.
                Quaternion facing = Quaternion.LookRotation(-foot.normalized, Vector3.up) * Quaternion.Euler(30f, 0f, 0f);
                Vector3 head = foot + Vector3.up * (TowerHeight + 2f);
                _scenery.Decoration(PrimitiveType.Cube, "Floodlight_Frame", head, new Vector3(8f, 4.4f, 0.8f), facing, tower);
                _scenery.Decoration(PrimitiveType.Cube, "Floodlight_Lamps", head + facing * new Vector3(0f, 0f, 0.45f), new Vector3(7.2f, 3.6f, 0.2f), facing, lamp);
            }
        }

        /// <summary>A screen above each end stand, facing the field, that follows the score.</summary>
        public void BuildScreens(ScoreKeeper scoreKeeper, IGoalSource goals)
        {
            foreach (Vector3 outward in new[] { Vector3.back, Vector3.forward })
            {
                float distance = _layout.EndStandStart + _layout.StandDepth + 0.5f;
                Vector3 centre = outward * distance + Vector3.up * (_layout.BackWallHeight + 11f);
                Vector3 towardsField = -outward;

                Material frame = _palette.Paint(_colours.ScreenFrame);
                _scenery.Decoration("Screen_Frame", centre, new Vector3(ScreenWidth, ScreenHeight, 1.2f), frame);
                foreach (float side in new[] { -1f, 1f })
                {
                    Vector3 leg = new Vector3(side * ScreenWidth * 0.35f, 0f, 0f);
                    float legHeight = 11f - ScreenHeight * 0.5f;
                    _scenery.Decoration("Screen_Leg", outward * distance + leg + Vector3.up * (_layout.BackWallHeight + legHeight * 0.5f),
                        new Vector3(0.8f, legHeight, 0.8f), _palette.Paint(_colours.Tower));
                }

                if (scoreKeeper == null || goals == null)
                {
                    continue;
                }

                Vector3 textSpot = centre + towardsField * 0.65f;
                TextMeshPro title = WorldText.Create(_dynamicRoot, "Screen_Title", textSpot + Vector3.up * 3.2f, towardsField,
                    new Vector2(ScreenWidth - 2f, 2.5f), "ROCKY LEAGUE", 16f, Color.white);
                title.alpha = 0.8f;

                TextMeshPro score = WorldText.Create(_dynamicRoot, "Screen_Score", textSpot - Vector3.up * 1f, towardsField,
                    new Vector2(ScreenWidth - 2f, 6f), "0 - 0", 60f, Color.white);
                StadiumScreen screen = score.gameObject.AddComponent<StadiumScreen>();
                screen.Initialize(scoreKeeper, goals, score, _colours.SeatColour(Team.Home), _colours.SeatColour(Team.Away));
            }
        }

        /// <summary>Boards along both side walls, just outside the glass, in the colour of each half.</summary>
        public void BuildAdBoards()
        {
            float halfLength = _layout.SideStandLength * 0.5f - 4f;
            int boards = Mathf.FloorToInt(2f * halfLength / BoardSpacing);
            string[] slogans = { "ROCKY LEAGUE", "ICAI GAME JAM 26/27" };

            foreach (Vector3 outward in new[] { Vector3.left, Vector3.right })
            {
                float x = _layout.WallOuterX + 0.8f;
                for (int i = 0; i < boards; i++)
                {
                    float z = -halfLength + (i + 0.5f) * BoardSpacing;
                    Vector3 centre = outward * x + new Vector3(0f, 0.7f, z);
                    Team team = z < 0f ? Team.Home : Team.Away;

                    _scenery.Decoration("AdBoard", centre, new Vector3(0.2f, 1.2f, BoardLength), _palette.Paint(_colours.SeatColour(team)));
                    WorldText.Create(_dynamicRoot, "AdBoard_Text", centre - outward * 0.12f, -outward,
                        new Vector2(BoardLength - 0.5f, 1f), slogans[i % slogans.Length], 8f, Color.white);
                }
            }
        }
    }
}
