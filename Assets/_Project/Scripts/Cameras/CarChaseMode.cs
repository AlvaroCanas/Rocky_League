using System;
using UnityEngine;

namespace RockyLeague.Cameras
{
    /// <summary>
    /// Chase camera: behind the car and a bit above it, looking at the car.
    /// It uses the car's heading projected on the horizontal plane, so the camera does not tilt
    /// when the car drives on walls or ramps.
    ///
    /// In the air the heading is only followed while the car is roughly upright (a yaw in the air
    /// still turns the camera). During a flip, or any tumble, the car's nose points down, up or
    /// backwards for a moment, and following it would swing the camera round; so the camera keeps
    /// the last heading until the car is level again.
    ///
    /// Plain C# class marked [Serializable], so its tuning is edited in the CameraRig's inspector.
    /// </summary>
    [Serializable]
    public sealed class CarChaseMode : ICameraMode
    {
        [SerializeField, Min(0f)] private float _distance = 5f;
        [SerializeField] private float _height = 2f;
        [SerializeField, Tooltip("Point above the car the camera looks at.")]
        private float _lookAtHeight = 1f;
        [SerializeField, Min(0.01f)] private float _positionSmoothTime = 0.12f;
        [SerializeField, Range(0f, 1f), Tooltip("In the air, how upright the car must be for the camera to follow its heading (1 = perfectly level). Below it the camera keeps its direction, e.g. during a flip.")]
        private float _minUprightInAir = 0.7f;

        private Vector3 _position;
        private Vector3 _velocity;
        private Vector3 _lastHeading = Vector3.forward;
        private bool _hasPosition;

        public CameraPose Evaluate(in CameraContext context, float deltaTime)
        {
            bool followCar = context.CarIsGrounded || Vector3.Dot(context.CarUp, Vector3.up) >= _minUprightInAir;
            Vector3 heading = Vector3.ProjectOnPlane(context.CarForward, Vector3.up);
            if (followCar && heading.sqrMagnitude > 0.01f)
            {
                _lastHeading = heading.normalized;
            }

            Vector3 desiredPosition = context.CarPosition - _lastHeading * _distance + Vector3.up * _height;

            // The first frame starts in place instead of flying in from wherever the camera was.
            _position = _hasPosition
                ? Vector3.SmoothDamp(_position, desiredPosition, ref _velocity, _positionSmoothTime, Mathf.Infinity, deltaTime)
                : desiredPosition;
            _hasPosition = true;

            Vector3 lookAt = context.CarPosition + Vector3.up * _lookAtHeight;
            return CameraPose.LookingAlong(_position, lookAt - _position);
        }
    }
}
