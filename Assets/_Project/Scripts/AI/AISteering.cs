using RockyLeague.Car.Inputs;
using UnityEngine;

namespace RockyLeague.AI
{
    /// <summary>
    /// Turns "go to this point" into throttle and steering, like a driver would.
    /// It is the only place that knows how to drive, so the states only decide WHERE to go.
    /// </summary>
    public static class AISteering
    {
        // Angle (degrees) that gives full steering.
        private const float FullSteerAngle = 30f;
        // A target this far behind and this close is reached faster in reverse.
        private const float ReverseAngle = 110f;
        private const float ReverseMaxDistance = 12f;
        // Sharp turns at speed use the handbrake.
        private const float HandbrakeAngle = 75f;
        private const float HandbrakeMinSpeed = 10f;
        // Boost only when roughly facing a far target.
        private const float BoostMaxAngle = 12f;
        private const float BoostMinDistance = 12f;
        // Arrival: slow down inside this radius and stop inside the next one.
        private const float ArriveSlowRadius = 10f;
        private const float ArriveStopRadius = 1.5f;

        /// <param name="arrive">Slow down and stop at the target instead of driving through it.</param>
        /// <param name="allowBoost">Whether boost may be used to get there.</param>
        public static CarInputData DriveTo(AIContext context, Vector3 target, bool arrive, bool allowBoost)
        {
            Vector3 local = context.ToCarSpace(target);
            float distance = local.magnitude;
            float angle = Vector3.SignedAngle(Vector3.forward, local, Vector3.up); // + right, - left

            if (Mathf.Abs(angle) > ReverseAngle && distance < ReverseMaxDistance)
            {
                // Backing up mirrors the steering, so steer towards the side of the target.
                return Command(-1f, Mathf.Sign(angle), boost: false, handbrake: false);
            }

            float steer = Mathf.Clamp(angle / FullSteerAngle, -1f, 1f);
            float throttle = context.Difficulty.MaxThrottle;

            if (arrive)
            {
                throttle *= distance < ArriveStopRadius ? 0f : Mathf.Clamp01(distance / ArriveSlowRadius);
            }

            bool handbrake = Mathf.Abs(angle) > HandbrakeAngle && context.CarSpeed > HandbrakeMinSpeed;
            bool boost = allowBoost
                && Mathf.Abs(angle) < BoostMaxAngle
                && distance > BoostMinDistance
                && context.BoostLeft >= context.Difficulty.MinBoostToUse;

            return Command(throttle, steer, boost, handbrake);
        }

        public static CarInputData Command(float throttle, float steer, bool boost, bool handbrake, bool jump = false)
        {
            return new CarInputData(throttle, steer, pitch: 0f, roll: 0f, jump: jump, boost: boost, handbrake: handbrake);
        }
    }
}
