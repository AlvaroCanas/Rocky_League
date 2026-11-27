using System;
using RockyLeague.Core.Building;
using UnityEngine;

namespace RockyLeague.Arena
{
    /// <summary>
    /// Builds the curved ramps (quarter pipes) where the floor meets a wall, so the ball
    /// and the cars roll up the wall instead of hitting a sharp corner.
    ///
    /// A curve cannot be a single box, so each ramp is a row of flat slats that follow a
    /// quarter of a circle: the first one lies on the floor and the last one stands
    /// against the wall. More slats = smoother curve.
    /// </summary>
    public sealed class RampBuilder
    {
        private readonly BlockBuilder _blocks;
        private readonly Material _material;
        private readonly PhysicsMaterial _physics;
        private readonly float _radius;
        private readonly int _slats;
        private readonly float _thickness;

        public RampBuilder(BlockBuilder blocks, Material material, PhysicsMaterial physics, float radius, int slats, float thickness)
        {
            _blocks = blocks ?? throw new ArgumentNullException(nameof(blocks));
            _material = material;
            _physics = physics;
            _radius = radius;
            _slats = Mathf.Max(1, slats);
            _thickness = thickness;
        }

        /// <summary>
        /// A ramp along the foot of a wall, from <paramref name="start"/> to <paramref name="end"/>
        /// (two points where the inner face of the wall meets the floor).
        /// </summary>
        /// <param name="inward">Horizontal direction from the wall towards the field.</param>
        public void Build(string rampName, Vector3 start, Vector3 end, Vector3 inward)
        {
            if (_radius <= 0f)
            {
                return;
            }

            Vector3 along = end - start;
            float length = along.magnitude;
            Vector3 middle = (start + end) * 0.5f;

            for (int i = 0; i < _slats; i++)
            {
                // Two points of the quarter circle, in (distance from the wall, height).
                Vector2 from = ArcPoint(i * 90f / _slats);
                Vector2 to = ArcPoint((i + 1) * 90f / _slats);
                Vector2 mid = (from + to) * 0.5f;
                Vector2 chord = to - from;
                Vector2 towardsCentre = (new Vector2(_radius, _radius) - mid).normalized;

                Vector3 tangent = (inward * chord.x + Vector3.up * chord.y).normalized;
                Vector3 normal = inward * towardsCentre.x + Vector3.up * towardsCentre.y;
                // Push the slat back by half its thickness so its inner face lies on the curve.
                Vector3 centre = middle + inward * mid.x + Vector3.up * mid.y - normal * (_thickness * 0.5f);

                Quaternion rotation = Quaternion.LookRotation(tangent, normal);
                Vector3 size = new Vector3(length, _thickness, chord.magnitude + 0.05f);
                _blocks.Solid($"{rampName}_{i}", centre, size, rotation, _material, _physics);
            }
        }

        /// <summary>0 degrees = on the floor, one radius away from the wall; 90 = against the wall, one radius up.</summary>
        private Vector2 ArcPoint(float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            return new Vector2(_radius - _radius * Mathf.Sin(radians), _radius - _radius * Mathf.Cos(radians));
        }
    }
}
