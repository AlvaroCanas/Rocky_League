using System;
using RockyLeague.Car.Inputs;
using UnityEngine;

namespace RockyLeague.Car
{
    /// <summary>
    /// Puts a car that is lying on a face that is not its wheels back on its wheels.
    /// It knows HOW to do it; <see cref="States.FlippedCarState"/> decides WHEN (the player pressed jump).
    ///
    /// The flip is a small hop away from the face that rests on the surface plus a spin that
    /// brings the wheels towards that surface. Both are worked out in the CAR's own axes, so it
    /// is the same on the floor, on a wall or on the ceiling:
    ///   - hop direction: opposite to the outward direction of the resting face;
    ///   - spin axis: outward x up, which rotates the car's up towards the surface normal.
    ///
    /// The spin is not a tuning value: it is the angle to cover (90 degrees from a side, nose or
    /// tail, 180 from the roof) divided by the time the hop keeps the car in the air, so the
    /// car lands about upright whatever the hop speed is. The only knob is the hop speed.
    /// All methods are meant to be called from FixedUpdate.
    /// </summary>
    public sealed class CarRecovery
    {
        private readonly Rigidbody _body;
        private readonly CarSettings _settings;
        private readonly FlipSensor _flipSensor;

        public CarRecovery(Rigidbody body, CarSettings settings, FlipSensor flipSensor)
        {
            _body = body ?? throw new ArgumentNullException(nameof(body));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _flipSensor = flipSensor ?? throw new ArgumentNullException(nameof(flipSensor));
        }

        /// <summary>Hops and spins the car back towards its wheels. Does nothing if no face rests on a surface.</summary>
        public void Flip(in CarInputData input)
        {
            CarFace face = _flipSensor.TouchingFace;
            if (face == CarFace.None)
            {
                return;
            }

            Vector3 outward = OutwardOf(face);
            Transform carTransform = _body.transform;

            // VelocityChange: changes the speeds directly, ignoring the mass and the inertia.
            _body.AddForce(carTransform.TransformDirection(-outward) * _settings.RecoveryHopSpeed, ForceMode.VelocityChange);

            Vector3 spinAxis = GetSpinAxis(face, outward, input);
            _body.AddTorque(carTransform.TransformDirection(spinAxis) * GetSpinSpeed(face), ForceMode.VelocityChange);
        }

        // Outward direction of each face, in the car's local space.
        private static Vector3 OutwardOf(CarFace face)
        {
            switch (face)
            {
                case CarFace.Top: return Vector3.up;
                case CarFace.Left: return Vector3.left;
                case CarFace.Right: return Vector3.right;
                case CarFace.Front: return Vector3.forward;
                case CarFace.Back: return Vector3.back;
                default: return Vector3.zero;
            }
        }

        private static Vector3 GetSpinAxis(CarFace face, Vector3 outward, in CarInputData input)
        {
            if (face == CarFace.Top)
            {
                // Upside down, rolling either way works: let the player choose with the steering.
                return input.Steer < 0f ? Vector3.back : Vector3.forward;
            }

            return Vector3.Cross(outward, Vector3.up);
        }

        private float GetSpinSpeed(CarFace face)
        {
            float gravity = Mathf.Max(Mathf.Abs(Physics.gravity.y), 0.01f);
            float airTime = 2f * _settings.RecoveryHopSpeed / gravity;
            float angle = face == CarFace.Top ? Mathf.PI : Mathf.PI * 0.5f;

            return angle / airTime;
        }
    }
}
