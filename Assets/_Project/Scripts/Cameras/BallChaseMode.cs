using System;
using UnityEngine;

namespace RockyLeague.Cameras
{
    /// <summary>
    /// Ball cam: keeps the ball in the middle of the screen without ever losing sight of the car.
    ///
    /// The camera moves on a "halo": a horizontal circle around the car, at a fixed height above
    /// it. Where on the circle depends only on where the ball is: the camera goes to the side
    /// opposite to the ball, so the car is always between the camera and the ball. The height is
    /// measured with the WORLD up, not the car's, so the halo does not tilt when the car drives on
    /// a wall or turns upside down.
    ///
    /// Three details make it work in every situation:
    ///  - What gets smoothed is the angle on the halo, not the position, so when the ball changes
    ///    side the camera goes ROUND the car instead of through it.
    ///  - When the ball is almost straight above the car its horizontal direction means nothing
    ///    (tiny moves would swing the camera around), so the last good angle is kept.
    ///  - The camera looks at the ball, unless that would leave the car out of the picture (ball
    ///    high above or very close): then it turns towards the car just enough to keep the car on
    ///    screen, with a margin. Seeing the car always wins over centring the ball.
    ///
    /// Plain C# class marked [Serializable], so its tuning is edited in the CameraRig's inspector.
    /// </summary>
    [Serializable]
    public sealed class BallChaseMode : ICameraMode
    {
        [Header("Halo")]
        [SerializeField, Min(0.5f), Tooltip("Radius of the halo: horizontal distance from the car to the camera (m).")]
        private float _distance = 5.5f;
        [SerializeField, Tooltip("Height of the halo above the car (m).")]
        private float _height = 2f;
        [SerializeField, Min(0.01f), Tooltip("Time the camera takes to follow the ball around the halo (s).")]
        private float _angleSmoothTime = 0.15f;
        [SerializeField, Min(1f), Tooltip("Fastest the camera can go round the halo (degrees per second).")]
        private float _maxAngularSpeed = 540f;
        [SerializeField, Min(0f), Tooltip("Below this horizontal distance between car and ball (m) the camera keeps its angle.")]
        private float _minBallDistance = 1.5f;

        [Header("Framing")]
        [SerializeField, Tooltip("Point above the car's pivot that must stay on screen (m).")]
        private float _carLookHeight = 0.5f;
        [SerializeField, Range(0f, 30f), Tooltip("How far from the edge of the screen the car is kept (degrees).")]
        private float _carScreenMargin = 6f;
        [SerializeField, Min(0.01f), Tooltip("Time the view takes to follow the ball (s). Small: the ball changes direction often.")]
        private float _aimSmoothTime = 0.06f;

        // Angle on the halo in degrees around the world up: 0 = +Z, 90 = +X.
        private float _angle;
        private float _targetAngle;
        private float _angleVelocity;
        private Vector3 _aim;
        private Vector3 _aimVelocity;
        private bool _initialized;

        public CameraPose Evaluate(in CameraContext context, float deltaTime)
        {
            UpdateTargetAngle(context);

            if (_initialized)
            {
                _angle = Mathf.SmoothDampAngle(_angle, _targetAngle, ref _angleVelocity, _angleSmoothTime, _maxAngularSpeed, deltaTime);
                _aim = Vector3.SmoothDamp(_aim, context.BallPosition, ref _aimVelocity, _aimSmoothTime, Mathf.Infinity, deltaTime);
            }
            else
            {
                _angle = _targetAngle;
                _aim = context.BallPosition;
                _initialized = true;
            }

            Vector3 onHalo = Quaternion.Euler(0f, _angle, 0f) * Vector3.forward * _distance;
            Vector3 position = context.CarPosition + onHalo + Vector3.up * _height;

            Vector3 carPoint = context.CarPosition + Vector3.up * _carLookHeight;
            Vector3 view = KeepCarInView(_aim - position, carPoint - position, context.VerticalFieldOfView);
            return CameraPose.LookingAlong(position, view);
        }

        private void UpdateTargetAngle(in CameraContext context)
        {
            Vector3 toBall = Vector3.ProjectOnPlane(context.BallPosition - context.CarPosition, Vector3.up);
            if (toBall.sqrMagnitude > _minBallDistance * _minBallDistance)
            {
                // The camera goes to the side opposite to the ball.
                _targetAngle = AngleOf(-toBall);
            }
            else if (!_initialized)
            {
                // No useful ball direction on the very first frame: start behind the car.
                _targetAngle = AngleOf(-Vector3.ProjectOnPlane(context.CarForward, Vector3.up));
            }
        }

        // Looks along toAim unless the car would end up nearer than the margin to the edge of the
        // screen: then the view turns towards the car just enough to leave it right at that limit.
        private Vector3 KeepCarInView(Vector3 toAim, Vector3 toCar, float verticalFieldOfView)
        {
            if (toCar.sqrMagnitude < 1e-6f)
            {
                return toAim;
            }
            if (toAim.sqrMagnitude < 1e-6f)
            {
                return toCar;
            }

            // The vertical field of view is the narrow one, so it sets the limit.
            float maxAngle = Mathf.Max(1f, verticalFieldOfView * 0.5f - _carScreenMargin);
            float angle = Vector3.Angle(toAim, toCar);
            if (angle <= maxAngle)
            {
                return toAim;
            }

            // Slerp from the car towards the ball, stopping maxAngle away from the car.
            return Vector3.Slerp(toCar.normalized, toAim.normalized, maxAngle / angle);
        }

        // Angle in degrees around the world up of a horizontal direction (0 = +Z, 90 = +X).
        private static float AngleOf(Vector3 direction) => Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
    }
}
