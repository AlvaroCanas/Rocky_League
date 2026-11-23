using UnityEngine;

namespace RockyLeague.Car.Inputs
{
    /// <summary>
    /// Immutable snapshot of the commands a car receives in one physics step.
    /// It is the contract shared by every <see cref="ICarInput"/> (player, classic AI, ML agent).
    /// Axes are clamped to [-1, 1] here, so no implementation can break what the car
    /// expects (keeps implementations substitutable, LSP).
    ///
    /// A struct because it is small, copied every step and has no identity.
    /// Buttons are "held" flags: the car detects presses itself, so input sources stay simple.
    /// </summary>
    public readonly struct CarInputData
    {
        /// <summary>Forward (+1) / reverse (-1).</summary>
        public float Throttle { get; }

        /// <summary>Right (+1) / left (-1). Also used as yaw while airborne.</summary>
        public float Steer { get; }

        /// <summary>Nose down (+1) / nose up (-1) while airborne.</summary>
        public float Pitch { get; }

        /// <summary>Roll right (+1) / left (-1) while airborne.</summary>
        public float Roll { get; }

        public bool Jump { get; }
        public bool Boost { get; }
        public bool Handbrake { get; }

        public CarInputData(float throttle, float steer, float pitch, float roll, bool jump, bool boost, bool handbrake)
        {
            Throttle = Mathf.Clamp(throttle, -1f, 1f);
            Steer = Mathf.Clamp(steer, -1f, 1f);
            Pitch = Mathf.Clamp(pitch, -1f, 1f);
            Roll = Mathf.Clamp(roll, -1f, 1f);
            Jump = jump;
            Boost = boost;
            Handbrake = handbrake;
        }

        /// <summary>No input at all (all axes at 0, no buttons held).</summary>
        public static CarInputData None => default;
    }
}
