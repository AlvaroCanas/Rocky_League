using UnityEngine;

namespace RockyLeague.Cameras
{
    /// <summary>
    /// Simple chase camera that follows a target from behind.
    /// Uses the target's heading projected on the horizontal plane, so the camera
    /// does not spin when the car flips or drives on walls.
    /// A "ball cam" mode will be added once the ball exists.
    /// </summary>
    public sealed class CarFollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField, Min(0f)] private float _distance = 5f;
        [SerializeField] private float _height = 2f;
        [SerializeField, Tooltip("Point above the target the camera looks at.")]
        private float _lookAtHeight = 1f;
        [SerializeField, Min(0.01f)] private float _positionSmoothTime = 0.12f;

        private Vector3 _velocity;
        private Vector3 _lastHeading = Vector3.forward;

        public void SetTarget(Transform target) => _target = target;

        private void LateUpdate()
        {
            if (_target == null) return;

            Vector3 heading = Vector3.ProjectOnPlane(_target.forward, Vector3.up);
            if (heading.sqrMagnitude > 0.01f)
            {
                _lastHeading = heading.normalized;
            }

            Vector3 desiredPosition = _target.position - _lastHeading * _distance + Vector3.up * _height;
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _velocity, _positionSmoothTime);
            transform.LookAt(_target.position + Vector3.up * _lookAtHeight);
        }
    }
}
