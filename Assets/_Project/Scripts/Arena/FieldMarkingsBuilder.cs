using System;
using RockyLeague.Core.Building;
using UnityEngine;

namespace RockyLeague.Arena
{
    /// <summary>
    /// Paints the field: mowing stripes on the grass, the halfway line, the centre circle and
    /// the area in front of each goal. Only looks: nothing here has a collider.
    /// </summary>
    public sealed class FieldMarkingsBuilder
    {
        private const float StripeWidth = 7.5f;
        private const float StripeThickness = 0.004f;
        private const float LineWidth = 0.3f;
        private const float LineThickness = 0.02f;
        private const float CenterCircleRadius = 8f;
        private const int CenterCircleSegments = 40;
        private const float GoalAreaMargin = 5f;
        private const float GoalAreaDepth = 10f;

        private readonly BlockBuilder _blocks;
        private readonly ArenaSettings _settings;
        private readonly Material _lineMaterial;
        private readonly Material _stripeMaterial;

        public FieldMarkingsBuilder(BlockBuilder blocks, ArenaSettings settings, Material lineMaterial, Material stripeMaterial)
        {
            _blocks = blocks ?? throw new ArgumentNullException(nameof(blocks));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _lineMaterial = lineMaterial;
            _stripeMaterial = stripeMaterial;
        }

        public void Build()
        {
            BuildStripes();
            BuildLines();
        }

        /// <summary>Every other band across the field gets a lighter green, like freshly mown grass.</summary>
        private void BuildStripes()
        {
            if (_stripeMaterial == null)
            {
                return;
            }

            int bands = Mathf.CeilToInt(_settings.Length / StripeWidth);
            float bandLength = _settings.Length / bands;
            for (int i = 1; i < bands; i += 2)
            {
                float z = -_settings.Length * 0.5f + (i + 0.5f) * bandLength;
                _blocks.Decoration($"Stripe_{i}", new Vector3(0f, StripeThickness * 0.5f, z),
                    new Vector3(_settings.Width, StripeThickness, bandLength), _stripeMaterial);
            }
        }

        private void BuildLines()
        {
            // Above the stripes.
            float y = StripeThickness + LineThickness * 0.5f + 0.002f;
            _blocks.Decoration("Line_Halfway", new Vector3(0f, y, 0f), new Vector3(_settings.Width, LineThickness, LineWidth), _lineMaterial);

            float segmentLength = 2f * Mathf.PI * CenterCircleRadius / CenterCircleSegments + 0.05f;
            for (int i = 0; i < CenterCircleSegments; i++)
            {
                float angle = i * 360f / CenterCircleSegments;
                float radians = angle * Mathf.Deg2Rad;
                Vector3 point = new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians)) * CenterCircleRadius;
                // Each piece lies along the tangent of the circle at its point.
                Quaternion along = Quaternion.Euler(0f, -(angle + 90f), 0f);
                _blocks.Decoration(PrimitiveType.Cube, "Line_Circle", point + Vector3.up * y, new Vector3(segmentLength, LineThickness, LineWidth), along, _lineMaterial);
            }

            BuildGoalArea(Team.Home, y);
            BuildGoalArea(Team.Away, y);
        }

        private void BuildGoalArea(Team team, float y)
        {
            float side = team == Team.Home ? -1f : 1f;
            float halfWidth = _settings.GoalWidth * 0.5f + GoalAreaMargin;
            float goalLineZ = side * _settings.Length * 0.5f;
            float frontZ = goalLineZ - side * GoalAreaDepth;
            float middleZ = (goalLineZ + frontZ) * 0.5f;

            _blocks.Decoration($"Line_{team}Area_Front", new Vector3(0f, y, frontZ), new Vector3(2f * halfWidth, LineThickness, LineWidth), _lineMaterial);
            _blocks.Decoration($"Line_{team}Area_Left", new Vector3(-halfWidth, y, middleZ), new Vector3(LineWidth, LineThickness, GoalAreaDepth), _lineMaterial);
            _blocks.Decoration($"Line_{team}Area_Right", new Vector3(halfWidth, y, middleZ), new Vector3(LineWidth, LineThickness, GoalAreaDepth), _lineMaterial);
        }
    }
}
