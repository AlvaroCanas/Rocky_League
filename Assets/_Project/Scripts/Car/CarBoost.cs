using System;
using UnityEngine;

namespace RockyLeague.Car
{
    /// <summary>
    /// Boost of one car. It holds how much boost this car has left (its own state)
    /// and pushes the car forward while boost is used. Tuning values come from the
    /// shared <see cref="CarSettings"/> (Flyweight), so they are not duplicated per car.
    ///
    /// Like <see cref="CarMotor"/>, it knows HOW boosting works; the car states decide
    /// WHEN it is allowed (e.g. only on the ground, since the game has no flying).
    /// All methods are meant to be called from FixedUpdate.
    /// </summary>
    public sealed class CarBoost
    {
        private readonly Rigidbody _body;
        private readonly CarSettings _settings;
        private readonly GroundSensor _groundSensor;

        public CarBoost(Rigidbody body, CarSettings settings, GroundSensor groundSensor)
        {
            if (body == null)
            {
                throw new ArgumentNullException(nameof(body));
            }
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }
            if (groundSensor == null)
            {
                throw new ArgumentNullException(nameof(groundSensor));
            }

            _body = body;
            _settings = settings;
            _groundSensor = groundSensor;
            Amount = _settings.StartingBoost;
        }

        /// <summary>Boost left, from 0 to <see cref="CarSettings.MaxBoost"/>.</summary>
        public float Amount { get; private set; }

        /// <summary>Boost left from 0 to 1, ready for a UI bar.</summary>
        public float NormalizedAmount => Amount / _settings.MaxBoost;

        /// <summary>True during the physics steps in which boost is actually being used.</summary>
        public bool IsBoosting { get; private set; }

        /// <summary>Uses boost if it is requested and there is some left; otherwise recharges.</summary>
        public void Tick(bool wantsBoost, float deltaTime)
        {
            if (wantsBoost && Amount > 0f)
            {
                IsBoosting = true;
                Amount = Mathf.Max(0f, Amount - _settings.BoostConsumption * deltaTime);
                ApplyBoostForce();
            }
            else
            {
                Recharge(deltaTime);
            }
        }

        /// <summary>Back to the starting amount of boost (used when the match restarts).</summary>
        public void Refill()
        {
            IsBoosting = false;
            Amount = _settings.StartingBoost;
        }

        /// <summary>Only recharges. For states where boosting is not allowed (airborne).</summary>
        public void Recharge(float deltaTime)
        {
            IsBoosting = false;
            Amount = Mathf.Min(_settings.MaxBoost, Amount + _settings.BoostRecharge * deltaTime);
        }

        private void ApplyBoostForce()
        {
            // Push along the car's nose, kept parallel to the surface it drives on.
            Vector3 forward = Vector3.ProjectOnPlane(_body.transform.forward, _groundSensor.GroundNormal).normalized;
            float forwardSpeed = Vector3.Dot(_body.linearVelocity, forward);

            if (forwardSpeed < _settings.MaxBoostSpeed)
            {
                _body.AddForce(forward * _settings.BoostAcceleration, ForceMode.Acceleration);
            }
        }
    }
}
