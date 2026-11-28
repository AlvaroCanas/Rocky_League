using RockyLeague.Car.Inputs;
using UnityEngine;

namespace RockyLeague.Car
{
    /// <summary>
    /// Knows HOW the car moves: it turns commands into forces on the Rigidbody.
    /// States decide WHEN each behaviour applies (grounded, airborne...), the motor
    /// only implements it. Arcade model (not realistic), tuned via <see cref="CarSettings"/>.
    /// All methods are meant to be called from FixedUpdate.
    /// </summary>
    public sealed class CarMotor
    {
        // Below this forward speed (m/s) the car is considered stopped when deciding brake vs accelerate.
        private const float StoppedSpeed = 0.5f;

        private readonly Rigidbody _body;
        private readonly CarSettings _settings;
        private readonly GroundSensor _groundSensor;

        public CarMotor(Rigidbody body, CarSettings settings, GroundSensor groundSensor)
        {
            _body = body;
            _settings = settings;
            _groundSensor = groundSensor;
        }

        /// <summary>Forward speed along the car's nose (negative when reversing).</summary>
        public float ForwardSpeed => Vector3.Dot(_body.linearVelocity, _body.transform.forward);

        /// <summary>Driving on a surface: throttle, steering, grip and sticking to the surface.</summary>
        public void Drive(in CarInputData input, float deltaTime)
        {
            Vector3 normal = _groundSensor.GroundNormal;
            Vector3 forward = Vector3.ProjectOnPlane(_body.transform.forward, normal).normalized;
            Vector3 right = Vector3.Cross(normal, forward);
            float forwardSpeed = Vector3.Dot(_body.linearVelocity, forward);

            ApplyThrottle(input.Throttle, forward, forwardSpeed, deltaTime);
            ApplySteering(input.Steer, normal, forwardSpeed, deltaTime);
            ApplyLateralGrip(input.Handbrake, right);

            _body.AddForce(-normal * _settings.StickyAcceleration, ForceMode.Acceleration);
        }

        private void ApplyThrottle(float throttle, Vector3 forward, float forwardSpeed, float deltaTime)
        {
            float acceleration;

            if (Mathf.Approximately(throttle, 0f))
            {
                // Coasting: slow down without overshooting past zero.
                float maxChange = Mathf.Abs(forwardSpeed) / deltaTime;
                acceleration = -Mathf.Sign(forwardSpeed) * Mathf.Min(_settings.CoastDeceleration, maxChange);
            }
            else if (Mathf.Abs(forwardSpeed) > StoppedSpeed && Mathf.Sign(throttle) != Mathf.Sign(forwardSpeed))
            {
                // Throttle against the movement: brake.
                acceleration = throttle * _settings.BrakeDeceleration;
            }
            else
            {
                float normalizedSpeed = Mathf.Abs(forwardSpeed) / _settings.MaxDriveSpeed;
                acceleration = normalizedSpeed < 1f
                    ? throttle * _settings.ThrottleAcceleration * Mathf.Lerp(1f, _settings.AccelerationAtMaxSpeed, normalizedSpeed)
                    : 0f;
            }

            _body.AddForce(forward * acceleration, ForceMode.Acceleration);
        }

        private void ApplySteering(float steer, Vector3 normal, float forwardSpeed, float deltaTime)
        {
            float steerFactor = _settings.EvaluateSteerFactor(Mathf.Abs(forwardSpeed) / _settings.MaxDriveSpeed);
            float direction = forwardSpeed >= 0f ? 1f : -1f; // steering is mirrored when reversing
            float targetYawRate = steer * _settings.MaxYawRate * steerFactor * direction;

            // The turning speed moves towards the target instead of jumping to it, so a key press
            // (0 to full steer at once) turns the car in smoothly, and releasing it straightens it
            // out smoothly too. Only the rotation around the surface normal is touched;
            // pitch/roll stay physics-driven.
            Vector3 angular = _body.angularVelocity;
            float currentYawRate = Vector3.Dot(angular, normal);
            float yawRate = Mathf.MoveTowards(currentYawRate, targetYawRate, _settings.SteerResponse * deltaTime);
            _body.angularVelocity = angular + normal * (yawRate - currentYawRate);
        }

        private void ApplyLateralGrip(bool handbrake, Vector3 right)
        {
            float grip = handbrake ? _settings.HandbrakeLateralGrip : _settings.LateralGrip;
            float lateralSpeed = Vector3.Dot(_body.linearVelocity, right);
            _body.AddForce(-right * lateralSpeed * grip, ForceMode.Acceleration);
        }
    }
}
