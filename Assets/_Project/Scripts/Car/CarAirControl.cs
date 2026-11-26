using System;
using RockyLeague.Car.Inputs;
using UnityEngine;

namespace RockyLeague.Car
{
    /// <summary>
    /// Rotation of the car while it is in the air: pitch (nose down / up), yaw (nose right / left)
    /// and roll (lean right / left). Each one turns around the CAR's own axis, so "pitch" is always
    /// nose up or down for the car, however it is tilted.
    ///
    /// Every axis moves its angular speed towards a target set by the input. With no input on an
    /// axis the car is not stopped dead: it keeps most of its spin (a hit or a flip leaves it
    /// spinning) and only a small damping slows it down.
    ///
    /// Like the other car parts it knows HOW the car turns; <see cref="States.AirborneCarState"/>
    /// decides WHEN. It reads the same <see cref="CarInputData"/> for any driver (player, bot, ML).
    /// All methods are meant to be called from FixedUpdate.
    /// </summary>
    public sealed class CarAirControl
    {
        private readonly Rigidbody _body;
        private readonly CarSettings _settings;

        public CarAirControl(Rigidbody body, CarSettings settings)
        {
            _body = body ?? throw new ArgumentNullException(nameof(body));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public void Apply(in CarInputData input, float deltaTime)
        {
            Transform carTransform = _body.transform;

            // Work in the car's own axes: X is pitch, Y is yaw, Z is roll.
            Vector3 current = carTransform.InverseTransformDirection(_body.angularVelocity);

            Vector3 next = new Vector3(
                Approach(current.x, input.Pitch * _settings.AirPitchSpeed, input.Pitch, deltaTime),
                Approach(current.y, input.Steer * _settings.AirYawSpeed, input.Steer, deltaTime),
                // Rolling right is a turn around the negative Z axis.
                Approach(current.z, -input.Roll * _settings.AirRollSpeed, input.Roll, deltaTime));

            _body.angularVelocity = carTransform.TransformDirection(next);
        }

        // With input: speed up towards the target. Without it: slow down towards zero.
        private float Approach(float current, float target, float axisInput, float deltaTime)
        {
            bool hasInput = !Mathf.Approximately(axisInput, 0f);
            float rate = hasInput ? _settings.AirControlAcceleration : _settings.AirControlDamping;

            return Mathf.MoveTowards(current, hasInput ? target : 0f, rate * deltaTime);
        }
    }
}
