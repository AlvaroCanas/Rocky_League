using System;
using RockyLeague.Arena;
using RockyLeague.Core.Building;
using UnityEngine;

namespace RockyLeague.Environment
{
    /// <summary>
    /// The four stands: stepped tiers, seats in the colour of the nearest team, a back wall
    /// with flags, a roof over the side stands and the crowd. Fans go under a separate parent
    /// because they move when a goal is scored (the rest is static and gets batched).
    /// </summary>
    public sealed class StandsBuilder
    {
        private const float PersonSpacing = 1.1f;
        private const float FlagSpacing = 14f;
        private const float RoofTilt = 8f;

        private readonly BlockBuilder _scenery;
        private readonly BlockBuilder _crowdBlocks;
        private readonly CrowdCheer _crowd;
        private readonly Palette _palette;
        private readonly StadiumLayout _layout;
        private readonly EnvironmentColours _colours;
        private readonly System.Random _random;
        private readonly float _crowdDensity;

        public StandsBuilder(BlockBuilder scenery, BlockBuilder crowdBlocks, CrowdCheer crowd, Palette palette,
            StadiumLayout layout, EnvironmentColours colours, System.Random random, float crowdDensity)
        {
            _scenery = scenery ?? throw new ArgumentNullException(nameof(scenery));
            _crowdBlocks = crowdBlocks ?? throw new ArgumentNullException(nameof(crowdBlocks));
            _crowd = crowd;
            _palette = palette ?? throw new ArgumentNullException(nameof(palette));
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _colours = colours ?? throw new ArgumentNullException(nameof(colours));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _crowdDensity = crowdDensity;
        }

        public void Build()
        {
            BuildStand("Stand_Left", Vector3.left, _layout.SideStandStart, _layout.SideStandLength, isSideStand: true);
            BuildStand("Stand_Right", Vector3.right, _layout.SideStandStart, _layout.SideStandLength, isSideStand: true);
            BuildStand("Stand_BlueEnd", Vector3.back, _layout.EndStandStart, _layout.EndStandLength, isSideStand: false);
            BuildStand("Stand_OrangeEnd", Vector3.forward, _layout.EndStandStart, _layout.EndStandLength, isSideStand: false);
        }

        /// <param name="outward">Direction from the field to this stand.</param>
        /// <param name="start">Distance from the centre of the field to the first step.</param>
        /// <param name="isSideStand">Side stands are split in a blue half and an orange half, and get a roof.</param>
        private void BuildStand(string standName, Vector3 outward, float start, float length, bool isSideStand)
        {
            Vector3 along = Vector3.Cross(Vector3.up, outward);
            Material concrete = _palette.Paint(_colours.Concrete);

            for (int tier = 0; tier < _layout.Tiers; tier++)
            {
                float height = (tier + 1) * _layout.TierRise;
                float offset = start + tier * _layout.TierDepth + _layout.TierDepth * 0.5f;
                float rowLength = length + 2f * tier * _layout.TierDepth;

                _scenery.Decoration($"{standName}_Tier{tier}", outward * offset + Vector3.up * (height * 0.5f),
                    Size(along, rowLength, height, _layout.TierDepth), concrete);

                Vector3 seats = outward * (offset - _layout.TierDepth * 0.3f) + Vector3.up * (height + 0.2f);
                BuildSeats(standName, tier, seats, along, outward, rowLength, isSideStand);
                BuildCrowd(standName, tier, seats, along, outward, rowLength, isSideStand);
            }

            float backOffset = start + _layout.StandDepth + 0.5f;
            float backHeight = _layout.BackWallHeight;
            float backLength = length + 2f * _layout.StandDepth + 1f;
            _scenery.Decoration($"{standName}_Back", outward * backOffset + Vector3.up * (backHeight * 0.5f),
                Size(along, backLength, backHeight, 1f), concrete);

            BuildFlags(standName, outward * backOffset + Vector3.up * backHeight, along, outward, backLength, isSideStand);

            if (isSideStand)
            {
                BuildRoof(standName, start, backHeight, along, outward, backLength);
            }
        }

        private void BuildSeats(string standName, int tier, Vector3 centre, Vector3 along, Vector3 outward, float rowLength, bool isSideStand)
        {
            if (!isSideStand)
            {
                _scenery.Decoration($"{standName}_Seats{tier}", centre, Size(along, rowLength, 0.4f, 0.8f),
                    _palette.Paint(_colours.SeatColour(TeamOfEnd(outward))));
                return;
            }

            // Each half of a side stand takes the colour of the goal it is closer to.
            float half = rowLength * 0.5f;
            Vector3 towardsMinus = centre - along * (half * 0.5f);
            Vector3 towardsPlus = centre + along * (half * 0.5f);
            _scenery.Decoration($"{standName}_Seats{tier}a", towardsMinus, Size(along, half, 0.4f, 0.8f),
                _palette.Paint(_colours.SeatColour(TeamAt(towardsMinus))));
            _scenery.Decoration($"{standName}_Seats{tier}b", towardsPlus, Size(along, half, 0.4f, 0.8f),
                _palette.Paint(_colours.SeatColour(TeamAt(towardsPlus))));
        }

        /// <summary>People: small blocks on the seats, most of them wearing their team's colour.</summary>
        private void BuildCrowd(string standName, int tier, Vector3 seatsCentre, Vector3 along, Vector3 outward, float rowLength, bool isSideStand)
        {
            int seats = Mathf.FloorToInt(rowLength / PersonSpacing);
            for (int seat = 0; seat < seats; seat++)
            {
                if (_random.NextDouble() > _crowdDensity)
                {
                    continue;
                }

                float position = -rowLength * 0.5f + (seat + 0.5f) * PersonSpacing;
                Vector3 spot = seatsCentre + along * position;
                Team team = isSideStand ? TeamAt(spot) : TeamOfEnd(outward);
                Color shirt = _random.NextDouble() < 0.6
                    ? Color.Lerp(_colours.SeatColour(team), Color.white, (float)_random.NextDouble() * 0.3f)
                    : _colours.CrowdColours[_random.Next(_colours.CrowdColours.Length)];

                float height = 0.8f + (float)_random.NextDouble() * 0.3f;
                GameObject fan = _crowdBlocks.Decoration($"{standName}_Fan{tier}_{seat}", spot + Vector3.up * (0.2f + height * 0.5f),
                    Size(along, 0.55f, height, 0.45f), _palette.Paint(QuantizedColour(shirt)));

                if (_crowd != null)
                {
                    _crowd.AddFan(fan.transform, team, (float)_random.NextDouble() * Mathf.PI * 2f);
                }
            }
        }

        private void BuildFlags(string standName, Vector3 wallTop, Vector3 along, Vector3 outward, float wallLength, bool isSideStand)
        {
            Material pole = _palette.Paint(_colours.Tower);
            int flags = Mathf.FloorToInt(wallLength / FlagSpacing);
            for (int i = 0; i <= flags; i++)
            {
                float position = -flags * FlagSpacing * 0.5f + i * FlagSpacing;
                Vector3 foot = wallTop + along * position;
                Team team = isSideStand ? TeamAt(foot) : TeamOfEnd(outward);

                _scenery.Decoration(PrimitiveType.Cylinder, $"{standName}_FlagPole{i}", foot + Vector3.up * 3f,
                    new Vector3(0.2f, 3f, 0.2f), Quaternion.identity, pole);
                _scenery.Decoration($"{standName}_Flag{i}", foot + Vector3.up * 5.2f + along * 1.3f,
                    Size(along, 2.4f, 1.4f, 0.08f), _palette.Paint(_colours.SeatColour(team)));
            }
        }

        /// <summary>A slab from the top of the back wall over the seats, slightly raised towards the field.</summary>
        private void BuildRoof(string standName, float start, float backHeight, Vector3 along, Vector3 outward, float length)
        {
            float width = _layout.StandDepth + 4f;
            float centreOffset = start + _layout.StandDepth + 1f - width * 0.5f;
            Vector3 centre = outward * centreOffset + Vector3.up * (backHeight + 1.2f);
            // Tilt around the stand's long axis so the field edge of the roof is higher.
            Quaternion tilt = Quaternion.AngleAxis(-RoofTilt, along);
            Vector3 size = Size(along, length, 0.6f, width);
            _scenery.Decoration(PrimitiveType.Cube, $"{standName}_Roof", centre, size, tilt, _palette.Paint(_colours.Roof));
        }

        private static Team TeamAt(Vector3 point)
        {
            return point.z < 0f ? Team.Blue : Team.Orange;
        }

        private static Team TeamOfEnd(Vector3 outward)
        {
            return outward.z < 0f ? Team.Blue : Team.Orange;
        }

        /// <summary>Rounds a colour to a few steps so similar shirts share one material.</summary>
        private static Color QuantizedColour(Color colour)
        {
            const float steps = 6f;
            return new Color(Mathf.Round(colour.r * steps) / steps, Mathf.Round(colour.g * steps) / steps, Mathf.Round(colour.b * steps) / steps);
        }

        /// <summary>A box measured along a direction (length), up (height) and across it (depth).</summary>
        public static Vector3 Size(Vector3 along, float length, float height, float depth)
        {
            bool alongX = Mathf.Abs(along.x) > 0.5f;
            return alongX ? new Vector3(length, height, depth) : new Vector3(depth, height, length);
        }
    }
}
