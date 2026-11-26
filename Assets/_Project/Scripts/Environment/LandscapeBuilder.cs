using System;
using RockyLeague.Arena;
using RockyLeague.Core.Building;
using TMPro;
using UnityEngine;

namespace RockyLeague.Environment
{
    /// <summary>
    /// Everything outside the stadium: ground, a ring road, a lake, round trees and pines,
    /// a city skyline with lit windows, mountains, clouds and a blimp. Placement is random
    /// but seeded, so the landscape is the same in every match.
    /// </summary>
    public sealed class LandscapeBuilder
    {
        private const float GroundSize = 2400f;
        private const float RoadWidth = 8f;
        private const float TreeRingInner = 150f;
        private const float TreeRingOuter = 380f;
        private const float MountainRing = 700f;
        // The city sits towards +X, inside this angle on each side.
        private const float CityHalfAngle = 28f;
        private const float CityNear = 430f;
        private const float CityFar = 560f;
        private static readonly Vector3 LakeCentre = new Vector3(-250f, 0f, 150f);
        private static readonly Vector2 LakeHalfSize = new Vector2(85f, 55f);

        private readonly BlockBuilder _scenery;
        private readonly Transform _dynamicRoot;
        private readonly Palette _palette;
        private readonly StadiumLayout _layout;
        private readonly EnvironmentColours _colours;
        private readonly System.Random _random;
        private readonly Material _lampMaterial;
        private readonly Material _waterMaterial;

        public LandscapeBuilder(BlockBuilder scenery, Transform dynamicRoot, Palette palette, StadiumLayout layout,
            EnvironmentColours colours, System.Random random, Material lampMaterial, Material waterMaterial)
        {
            _scenery = scenery ?? throw new ArgumentNullException(nameof(scenery));
            _dynamicRoot = dynamicRoot;
            _palette = palette ?? throw new ArgumentNullException(nameof(palette));
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _colours = colours ?? throw new ArgumentNullException(nameof(colours));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _lampMaterial = lampMaterial;
            _waterMaterial = waterMaterial;
        }

        public void BuildGround()
        {
            // Just below the arena floor, whose top is at y = 0.
            _scenery.Decoration("Ground", new Vector3(0f, -0.55f, 0f), new Vector3(GroundSize, 1f, GroundSize), _palette.Paint(_colours.Ground));

            Vector2 outer = _layout.OuterHalfSize;
            Vector2 plaza = outer + new Vector2(12f, 12f);
            _scenery.Decoration("Plaza", new Vector3(0f, -0.52f, 0f), new Vector3(2f * plaza.x, 1f, 2f * plaza.y), _palette.Paint(_colours.Concrete * 0.85f));

            // A ring road around the plaza.
            Material road = _palette.Paint(_colours.Road);
            float roadX = plaza.x + RoadWidth * 0.5f;
            float roadZ = plaza.y + RoadWidth * 0.5f;
            _scenery.Decoration("Road", new Vector3(-roadX, -0.51f, 0f), new Vector3(RoadWidth, 1f, 2f * roadZ + RoadWidth), road);
            _scenery.Decoration("Road", new Vector3(roadX, -0.51f, 0f), new Vector3(RoadWidth, 1f, 2f * roadZ + RoadWidth), road);
            _scenery.Decoration("Road", new Vector3(0f, -0.51f, -roadZ), new Vector3(2f * roadX, 1f, RoadWidth), road);
            _scenery.Decoration("Road", new Vector3(0f, -0.51f, roadZ), new Vector3(2f * roadX, 1f, RoadWidth), road);
        }

        public void BuildLake()
        {
            Material water = _waterMaterial != null ? _waterMaterial : _palette.Paint(new Color(0.2f, 0.4f, 0.6f));
            _scenery.Decoration(PrimitiveType.Cylinder, "Lake", LakeCentre + Vector3.up * 0.01f,
                new Vector3(2f * LakeHalfSize.x, 0.05f, 2f * LakeHalfSize.y), Quaternion.identity, water);
            _scenery.Decoration(PrimitiveType.Cylinder, "Lake_Shore", LakeCentre + Vector3.up * -0.01f,
                new Vector3(2f * LakeHalfSize.x + 8f, 0.05f, 2f * LakeHalfSize.y + 8f), Quaternion.identity, _palette.Paint(new Color(0.78f, 0.72f, 0.55f)));
        }

        public void BuildTrees(int count)
        {
            Material trunk = _palette.Paint(_colours.Trunk);
            Material pine = _palette.Paint(_colours.Pine);

            for (int i = 0; i < count; i++)
            {
                Vector3 spot = RandomPointInRing(TreeRingInner, TreeRingOuter);
                if (IsInLake(spot, 6f))
                {
                    continue;
                }

                if (_random.NextDouble() < 0.5)
                {
                    BuildRoundTree(spot, trunk);
                }
                else
                {
                    BuildPine(spot, trunk, pine);
                }
            }
        }

        private void BuildRoundTree(Vector3 spot, Material trunk)
        {
            float height = Range(5f, 10f);
            float crown = Range(4f, 7.5f);
            Material leaves = _palette.Paint(_colours.Leaves[_random.Next(_colours.Leaves.Length)]);
            _scenery.Decoration(PrimitiveType.Cylinder, "Tree_Trunk", spot + Vector3.up * (height * 0.5f), new Vector3(0.7f, height * 0.5f, 0.7f), Quaternion.identity, trunk);
            _scenery.Decoration(PrimitiveType.Sphere, "Tree_Crown", spot + Vector3.up * (height + crown * 0.3f), new Vector3(crown, crown * 1.15f, crown), Quaternion.identity, leaves);
        }

        /// <summary>Three stacked boxes, each smaller and turned 45 degrees: a low-poly pine.</summary>
        private void BuildPine(Vector3 spot, Material trunk, Material needles)
        {
            float size = Range(5f, 8f);
            _scenery.Decoration(PrimitiveType.Cylinder, "Pine_Trunk", spot + Vector3.up * 1.5f, new Vector3(0.6f, 1.5f, 0.6f), Quaternion.identity, trunk);
            for (int layer = 0; layer < 3; layer++)
            {
                float width = size * (1f - layer * 0.3f);
                float y = 3f + layer * size * 0.55f;
                Quaternion turn = Quaternion.Euler(0f, 45f * layer + Range(0f, 30f), 0f);
                _scenery.Decoration(PrimitiveType.Cube, "Pine_Layer", spot + Vector3.up * y, new Vector3(width, size * 0.6f, width), turn, needles);
            }
        }

        /// <summary>Towers of different heights with bands of lit windows.</summary>
        public void BuildCity(int buildings)
        {
            Material body = _palette.Paint(_colours.Building);
            Material darker = _palette.Paint(_colours.Building * 0.75f);
            Material windows = _lampMaterial != null ? _lampMaterial : _palette.Paint(Color.white);

            for (int i = 0; i < buildings; i++)
            {
                float angle = Range(-CityHalfAngle, CityHalfAngle) * Mathf.Deg2Rad;
                float distance = Range(CityNear, CityFar);
                Vector3 spot = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * distance;

                // Taller towers in the middle of the skyline.
                float centreWeight = 1f - Mathf.Abs(angle) / (CityHalfAngle * Mathf.Deg2Rad);
                float height = Range(30f, 70f) + centreWeight * Range(30f, 110f);
                float width = Range(14f, 30f);
                float depth = Range(14f, 30f);

                _scenery.Decoration($"Building_{i}", spot + Vector3.up * (height * 0.5f), new Vector3(width, height, depth), i % 2 == 0 ? body : darker);

                for (float y = 8f; y < height - 4f; y += 7f)
                {
                    if (_random.NextDouble() < 0.35)
                    {
                        continue;
                    }

                    _scenery.Decoration($"Building_{i}_Windows", spot + Vector3.up * y, new Vector3(width + 0.3f, 1.2f, depth + 0.3f), windows);
                }
            }
        }

        public void BuildMountains(int count)
        {
            Material rock = _palette.Paint(_colours.Mountain);
            Material snow = _palette.Paint(Color.Lerp(_colours.Mountain, Color.white, 0.85f));

            for (int i = 0; i < count; i++)
            {
                float angle = (i + (float)_random.NextDouble() * 0.6f) / count * Mathf.PI * 2f;
                float distance = MountainRing + Range(-50f, 150f);
                Vector3 spot = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * distance;
                float width = Range(160f, 300f);
                float height = Range(80f, 190f);

                // Half-buried stretched spheres make soft peaks; the tallest get a snowy cap.
                Quaternion turn = Quaternion.Euler(0f, Range(0f, 180f), 0f);
                _scenery.Decoration(PrimitiveType.Sphere, "Mountain", spot, new Vector3(width, 2f * height, width * 0.8f), turn, rock);
                if (height > 140f)
                {
                    _scenery.Decoration(PrimitiveType.Sphere, "Mountain_Snow", spot + Vector3.up * (height * 0.82f),
                        new Vector3(width * 0.33f, height * 0.42f, width * 0.27f), turn, snow);
                }
            }
        }

        public void BuildClouds(int count)
        {
            Material cloud = _palette.Paint(_colours.Cloud);
            for (int i = 0; i < count; i++)
            {
                Vector3 spot = RandomPointInRing(120f, 650f) + Vector3.up * Range(110f, 200f);
                Quaternion turn = Quaternion.Euler(0f, Range(0f, 180f), 0f);
                float size = Range(18f, 42f);

                for (int part = 0; part < 3; part++)
                {
                    Vector3 shift = turn * new Vector3((part - 1) * size * 0.55f, Range(-2f, 3f), Range(-4f, 4f));
                    float scale = size * (part == 1 ? 1f : 0.7f);
                    _scenery.Decoration(PrimitiveType.Sphere, "Cloud", spot + shift, new Vector3(scale, scale * 0.35f, scale * 0.6f), turn, cloud);
                }
            }
        }

        /// <summary>A blimp in the team colours that circles the stadium. It moves, so it is not batched.</summary>
        public void BuildBlimp()
        {
            GameObject blimpObject = new GameObject("Blimp");
            blimpObject.transform.SetParent(_dynamicRoot, false);
            BlockBuilder parts = new BlockBuilder(blimpObject.transform);

            // Capsules stand upright: turn the body so it lies along the direction of travel (Z).
            Quaternion lying = Quaternion.Euler(90f, 0f, 0f);
            parts.Decoration(PrimitiveType.Capsule, "Body", Vector3.zero, new Vector3(9f, 15f, 9f), lying, _palette.Paint(_colours.Blimp));
            parts.Decoration(PrimitiveType.Cylinder, "Stripe_Blue", new Vector3(0f, 0f, 4f), new Vector3(9.2f, 1.2f, 9.2f), lying, _palette.Paint(_colours.SeatColour(Team.Blue)));
            parts.Decoration(PrimitiveType.Cylinder, "Stripe_Orange", new Vector3(0f, 0f, -4f), new Vector3(9.2f, 1.2f, 9.2f), lying, _palette.Paint(_colours.SeatColour(Team.Orange)));
            parts.Decoration("Fin_Vertical", new Vector3(0f, 3.5f, -12f), new Vector3(0.4f, 6f, 5f), _palette.Paint(_colours.Tower));
            parts.Decoration("Fin_Horizontal", new Vector3(0f, 0f, -12f), new Vector3(11f, 0.4f, 5f), _palette.Paint(_colours.Tower));
            parts.Decoration("Gondola", new Vector3(0f, -5f, 1f), new Vector3(2.5f, 1.8f, 6f), _palette.Paint(_colours.Tower));

            foreach (float side in new[] { -1f, 1f })
            {
                TextMeshPro text = WorldText.Create(blimpObject.transform, "Blimp_Text", new Vector3(side * 4.7f, 0.5f, 0f),
                    new Vector3(side, 0f, 0f), new Vector2(20f, 4f), "ROCKY LEAGUE", 30f, _colours.SeatColour(Team.Blue));
                text.alpha = 1f;
            }

            Blimp blimp = blimpObject.AddComponent<Blimp>();
            blimp.Initialize(Vector3.zero, 170f, 75f, 4f, Range(0f, 360f));
        }

        private bool IsInLake(Vector3 point, float margin)
        {
            Vector3 local = point - LakeCentre;
            float x = local.x / (LakeHalfSize.x + margin);
            float z = local.z / (LakeHalfSize.y + margin);
            return x * x + z * z < 1f;
        }

        private Vector3 RandomPointInRing(float inner, float outer)
        {
            float angle = Range(0f, Mathf.PI * 2f);
            float distance = Mathf.Sqrt(Range(inner * inner, outer * outer));
            return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * distance;
        }

        private float Range(float min, float max)
        {
            return min + (float)_random.NextDouble() * (max - min);
        }
    }
}
