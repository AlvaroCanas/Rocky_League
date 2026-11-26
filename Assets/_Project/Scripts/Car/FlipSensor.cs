using UnityEngine;

namespace RockyLeague.Car
{
    /// <summary>
    /// Detects a car that is lying on something other than its wheels: upside down, on its
    /// side, on its nose or on its tail. The <see cref="GroundSensor"/> only looks below the
    /// wheels, so it cannot see this. Here a small probe sphere sits on each TOP corner of the
    /// car's box (the bottom corners are the wheels).
    ///
    /// Which corners touch tells which face is lying on the surface:
    ///   3 or 4 corners                  -> the roof
    ///   2 corners on the same side      -> that side, the nose or the tail
    ///   2 opposite corners (diagonal)   -> unclear, ignored
    ///
    /// Only scenery counts: colliders that have a Rigidbody (this car, other cars, the ball)
    /// are ignored. That way no layers are needed, and a new arena works as long as its
    /// walls and floor are static colliders.
    ///
    /// Plain C# class refreshed by its owner every physics step (same idea as GroundSensor).
    /// </summary>
    public sealed class FlipSensor
    {
        // Order of the probes, also used when resolving the face.
        private const int FrontLeft = 0;
        private const int FrontRight = 1;
        private const int RearLeft = 2;
        private const int RearRight = 3;

        private const int MaxOverlaps = 8;

        private readonly Transform _transform;
        private readonly Vector3[] _localCorners;
        private readonly bool[] _cornerContacts;
        private readonly Collider[] _overlaps = new Collider[MaxOverlaps];
        private readonly float _probeRadius;
        private readonly float _minContactSeconds;
        private readonly LayerMask _layers;

        private float _contactTime;

        /// <summary>The face that rests on a surface right now, or None.</summary>
        public CarFace TouchingFace { get; private set; }

        /// <summary>
        /// True when the same face has been resting on a surface for long enough. The delay
        /// avoids reacting to a bounce or to a brush against a wall in mid-air.
        /// </summary>
        public bool IsFlipped => TouchingFace != CarFace.None && _contactTime >= _minContactSeconds;

        private FlipSensor(Transform transform, Vector3[] localCorners, CarSettings settings)
        {
            _transform = transform;
            _localCorners = localCorners;
            _cornerContacts = new bool[localCorners.Length];
            _probeRadius = settings.FlippedProbeRadius;
            _minContactSeconds = settings.FlippedDelay;
            _layers = settings.GroundLayers;
        }

        /// <summary>Factory method: places one probe at each top corner of the car's box collider.</summary>
        public static FlipSensor FromBoxCollider(BoxCollider box, CarSettings settings)
        {
            Vector3 c = box.center;
            Vector3 half = box.size * 0.5f;
            float top = c.y + half.y;

            var corners = new[]
            {
                new Vector3(c.x - half.x, top, c.z + half.z), // front left
                new Vector3(c.x + half.x, top, c.z + half.z), // front right
                new Vector3(c.x - half.x, top, c.z - half.z), // rear left
                new Vector3(c.x + half.x, top, c.z - half.z), // rear right
            };

            return new FlipSensor(box.transform, corners, settings);
        }

        public void Refresh(float deltaTime)
        {
            for (int i = 0; i < _localCorners.Length; i++)
            {
                _cornerContacts[i] = TouchesScenery(_transform.TransformPoint(_localCorners[i]));
            }

            CarFace face = ResolveFace();

            // The timer only grows while the SAME face keeps resting on a surface.
            _contactTime = face != CarFace.None && face == TouchingFace ? _contactTime + deltaTime : 0f;
            TouchingFace = face;
        }

        /// <summary>Editor helper to visualise the probes (red = touching scenery).</summary>
        public void DrawGizmos()
        {
            for (int i = 0; i < _localCorners.Length; i++)
            {
                Gizmos.color = _cornerContacts[i] ? Color.red : Color.gray;
                Gizmos.DrawWireSphere(_transform.TransformPoint(_localCorners[i]), _probeRadius);
            }
        }

        private bool TouchesScenery(Vector3 point)
        {
            int count = Physics.OverlapSphereNonAlloc(point, _probeRadius, _overlaps, _layers, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                if (_overlaps[i].attachedRigidbody == null)
                {
                    return true;
                }
            }

            return false;
        }

        private CarFace ResolveFace()
        {
            int touching = 0;
            for (int i = 0; i < _cornerContacts.Length; i++)
            {
                if (_cornerContacts[i])
                {
                    touching++;
                }
            }

            if (touching >= 3) return CarFace.Top;
            if (touching != 2) return CarFace.None;

            bool frontLeft = _cornerContacts[FrontLeft];
            bool frontRight = _cornerContacts[FrontRight];
            bool rearLeft = _cornerContacts[RearLeft];
            bool rearRight = _cornerContacts[RearRight];

            if (frontLeft && frontRight) return CarFace.Front;
            if (rearLeft && rearRight) return CarFace.Back;
            if (frontLeft && rearLeft) return CarFace.Left;
            if (frontRight && rearRight) return CarFace.Right;

            return CarFace.None; // diagonal
        }
    }
}
