using UnityEngine;

namespace RockyLeague.Car
{
    /// <summary>
    /// Detects whether the car is resting on a surface (floor, wall or ceiling)
    /// by casting one short ray per wheel along the car's local down axis.
    /// Plain C# class: only responsible for detection (SRP), refreshed by its owner.
    /// </summary>
    public sealed class GroundSensor
    {
        // Rays start slightly inside the collider; Physics.Raycast ignores the collider it starts in.
        private const float RayStartInset = 0.05f;
        // Probes are placed a bit inside the box corners, where the wheels would be.
        private const float WheelInsetFactor = 0.8f;

        private readonly Transform _transform;
        private readonly Vector3[] _localWheelPoints;
        private readonly bool[] _wheelContacts;
        private readonly float _checkDistance;
        private readonly int _minGroundedWheels;
        private readonly LayerMask _groundLayers;

        public bool IsGrounded { get; private set; }

        /// <summary>Average normal of the touched surface; the car's up when not grounded.</summary>
        public Vector3 GroundNormal { get; private set; }

        public int WheelContactCount { get; private set; }

        private GroundSensor(Transform transform, Vector3[] localWheelPoints, CarSettings settings)
        {
            _transform = transform;
            _localWheelPoints = localWheelPoints;
            _wheelContacts = new bool[localWheelPoints.Length];
            _checkDistance = settings.GroundCheckDistance;
            _minGroundedWheels = settings.MinGroundedWheels;
            _groundLayers = settings.GroundLayers;
            GroundNormal = transform.up;
        }

        /// <summary>Factory method: places one probe at each bottom corner of the car's box collider.</summary>
        public static GroundSensor FromBoxCollider(BoxCollider box, CarSettings settings)
        {
            Vector3 c = box.center;
            Vector3 half = box.size * 0.5f;
            float x = half.x * WheelInsetFactor;
            float z = half.z * WheelInsetFactor;
            float bottom = c.y - half.y;

            var points = new[]
            {
                new Vector3(c.x - x, bottom, c.z + z), // front left
                new Vector3(c.x + x, bottom, c.z + z), // front right
                new Vector3(c.x - x, bottom, c.z - z), // rear left
                new Vector3(c.x + x, bottom, c.z - z), // rear right
            };

            return new GroundSensor(box.transform, points, settings);
        }

        public void Refresh()
        {
            Vector3 down = -_transform.up;
            Vector3 normalSum = Vector3.zero;
            int contacts = 0;

            for (int i = 0; i < _localWheelPoints.Length; i++)
            {
                Vector3 origin = GetRayOrigin(i);
                bool hit = Physics.Raycast(origin, down, out RaycastHit info, RayStartInset + _checkDistance,
                    _groundLayers, QueryTriggerInteraction.Ignore);

                _wheelContacts[i] = hit;
                if (hit)
                {
                    contacts++;
                    normalSum += info.normal;
                }
            }

            WheelContactCount = contacts;
            IsGrounded = contacts >= _minGroundedWheels;
            GroundNormal = contacts > 0 ? normalSum.normalized : _transform.up;
        }

        /// <summary>Editor helper to visualise the probes (green = touching).</summary>
        public void DrawGizmos()
        {
            Vector3 down = -_transform.up;
            for (int i = 0; i < _localWheelPoints.Length; i++)
            {
                Gizmos.color = _wheelContacts[i] ? Color.green : Color.red;
                Vector3 origin = GetRayOrigin(i);
                Gizmos.DrawLine(origin, origin + down * (RayStartInset + _checkDistance));
            }
        }

        private Vector3 GetRayOrigin(int wheelIndex) =>
            _transform.TransformPoint(_localWheelPoints[wheelIndex]) + _transform.up * RayStartInset;
    }
}
