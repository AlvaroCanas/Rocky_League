using System;
using RockyLeague.Car.Inputs;
using UnityEngine;

namespace RockyLeague.Car
{
    /// <summary>
    /// The car's second jump, done in the air: a double jump or a dodge (the "flip").
    ///
    /// Every time the car touches the ground it gets one air jump (<see cref="Refill"/>), with no
    /// time limit (KISS). When it is used, the driver's direction decides what happens:
    ///   - no direction (pitch and steer inside the dead zone) -> double jump: a second push
    ///     along the car's own up;
    ///   - a direction -> dodge: a push along the ground in that direction (relative to where
    ///     the car points) and one full spin around the matching axis: forward / back flip
    ///     around the car's X axis, side flip around its Z axis, a mix for diagonals.
    ///
    /// The spin follows a planned curve: fast at the start and slowing down evenly to zero just as
    /// the dodge ends, so the flip starts sharp and ends softly, with no sudden stop. Every step
    /// the spin around the flip axis is set to that curve, so touching the ball (or another car)
    /// cannot stop the flip half way; any other spin a touch adds is slowed down at the same rate.
    /// The start speed is not a tuning value: it comes from the dodge duration so that the car
    /// turns one full turn, and ends the way up it started (same idea as <see cref="CarRecovery"/>).
    /// While the car spins, gravity is weakened (a "hang" like in Rocket League): without it the
    /// car would land before finishing the flip, and on landing the wheels' grip would eat the
    /// sideways push, so the flip would barely change the car's path.
    /// While the car spins, the Rigidbody's angular speed limit is raised just enough, and set
    /// back when the dodge ends or is interrupted.
    ///
    /// Like the other car parts it knows HOW; <see cref="States.AirborneCarState"/> decides WHEN
    /// the air jump is used and <see cref="States.DodgingCarState"/> runs the spin.
    /// All methods are meant to be called from FixedUpdate.
    /// </summary>
    public sealed class CarDodge
    {
        private const float FullTurn = 2f * Mathf.PI;

        private readonly Rigidbody _body;
        private readonly CarSettings _settings;

        private Vector3 _localSpinAxis;
        private float _startSpinSpeed;
        private float _spinBrake;
        private float _timeLeft;

        public CarDodge(Rigidbody body, CarSettings settings)
        {
            _body = body ?? throw new ArgumentNullException(nameof(body));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>True while the car still has its air jump (until it uses it or lands again).</summary>
        public bool HasAirJump { get; private set; }

        /// <summary>True while the dodge spin is running.</summary>
        public bool IsDodging => _timeLeft > 0f;

        /// <summary>Gives the air jump back. Called when the car touches the ground.</summary>
        public void Refill()
        {
            HasAirJump = true;
        }

        /// <summary>
        /// Uses the air jump, if the car still has it: a double jump or a dodge depending on the
        /// direction in <paramref name="input"/>. Returns false if there was no air jump left.
        /// </summary>
        public bool TryAirJump(in CarInputData input)
        {
            if (!HasAirJump || _body.isKinematic)
            {
                return false;
            }

            HasAirJump = false;

            // Pitch +1 is "nose down" (W), which is a forward flip; steer +1 (D) is a flip to the right.
            Vector2 direction = new Vector2(input.Steer, input.Pitch);
            if (direction.magnitude < _settings.DodgeDeadZone)
            {
                DoubleJump();
            }
            else
            {
                StartDodge(direction.normalized);
            }

            return true;
        }

        /// <summary>
        /// While the dodge lasts, keeps the flip spin on its curve and gravity weakened. By the end
        /// the spin has faded out, and any spin left over is handled by the air control.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!IsDodging)
            {
                return;
            }

            _timeLeft -= deltaTime;
            if (_timeLeft <= 0f)
            {
                Stop();
                return;
            }

            FollowSpinCurve(deltaTime);
            ReduceGravity();
        }

        /// <summary>
        /// Ends the dodge where it is (landing in the middle of a flip, a kickoff reset...) and
        /// gives the Rigidbody its normal angular speed limit back. Safe to call at any time.
        /// </summary>
        public void Stop()
        {
            _timeLeft = 0f;
            _body.maxAngularVelocity = _settings.MaxAngularSpeed;
        }

        private void DoubleJump()
        {
            // VelocityChange: changes the speed directly, ignoring the mass (like the first jump).
            _body.AddForce(_body.transform.up * _settings.DoubleJumpSpeed, ForceMode.VelocityChange);
        }

        private void StartDodge(Vector2 direction)
        {
            Transform car = _body.transform;
            Vector3 forward = Flatten(car.forward, car.up);
            Vector3 right = Flatten(car.right, Vector3.Cross(Vector3.up, forward));

            // The push is along the ground; a falling car also stops falling, so the flip "floats".
            Vector3 velocity = _body.linearVelocity;
            velocity.y = Mathf.Max(0f, velocity.y);
            _body.linearVelocity = velocity + (forward * direction.y + right * direction.x) * _settings.DodgeSpeed;

            // Same signs as CarAirControl: nose down is +X, rolling right is -Z.
            _localSpinAxis = new Vector3(direction.y, 0f, -direction.x).normalized;

            // Slowing down evenly from the start speed to zero covers half of startSpeed * duration,
            // so a full turn needs a start speed of two turns per duration.
            float duration = _settings.DodgeDuration;
            _startSpinSpeed = 2f * FullTurn / duration;
            _spinBrake = _startSpinSpeed / duration;
            _timeLeft = duration;

            _body.maxAngularVelocity = Mathf.Max(_settings.MaxAngularSpeed, _startSpinSpeed);
            _body.angularVelocity = car.TransformDirection(_localSpinAxis) * _startSpinSpeed;
        }

        // The spin around the flip axis is put back on its curve; the rest of the spin (what a
        // touch may have added) is slowed down at the same rate, so the car does not tumble.
        private void FollowSpinCurve(float deltaTime)
        {
            if (_body.isKinematic)
            {
                return;
            }

            Vector3 axis = _body.transform.TransformDirection(_localSpinAxis);
            Vector3 angular = _body.angularVelocity;
            Vector3 otherSpin = angular - Vector3.Project(angular, axis);

            float plannedSpeed = _startSpinSpeed * (_timeLeft / _settings.DodgeDuration);
            otherSpin = Vector3.MoveTowards(otherSpin, Vector3.zero, _spinBrake * deltaTime);

            _body.angularVelocity = axis * plannedSpeed + otherSpin;
        }

        // Pushes up against part of gravity, so the car falls slower while it flips.
        private void ReduceGravity()
        {
            if (_body.isKinematic || !_body.useGravity)
            {
                return;
            }

            _body.AddForce(-Physics.gravity * (1f - _settings.DodgeGravityScale), ForceMode.Acceleration);
        }

        // Direction projected on the ground; if it points almost straight up or down, the fallback
        // is used instead (for example the car's up when its nose points at the sky).
        private static Vector3 Flatten(Vector3 direction, Vector3 fallback)
        {
            Vector3 flat = Vector3.ProjectOnPlane(direction, Vector3.up);
            if (flat.sqrMagnitude < 0.01f)
            {
                flat = Vector3.ProjectOnPlane(fallback, Vector3.up);
            }

            return flat.sqrMagnitude > 1e-6f ? flat.normalized : Vector3.zero;
        }
    }
}
