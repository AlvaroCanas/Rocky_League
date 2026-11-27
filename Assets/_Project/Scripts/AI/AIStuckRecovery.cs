using RockyLeague.Car.Inputs;

namespace RockyLeague.AI
{
    /// <summary>
    /// Safety net: if the bot keeps trying to drive but hardly moves (stuck against a wall,
    /// a goal post or in a corner), it backs up for a moment steering the other way, so it
    /// faces somewhere else when it drives forward again. Kept apart from the decision states
    /// because it is not a decision, just a recovery that applies to all of them.
    /// </summary>
    public sealed class AIStuckRecovery
    {
        private const float MinThrottleToCount = 0.3f;
        private const float StuckSpeed = 1.5f;
        private const float StuckTimeToReact = 1f;
        private const float ReverseDuration = 1f;

        private float _stuckTime;
        private float _reverseTimeLeft;
        private float _escapeSteer;

        /// <summary>Forgets any stuck time, e.g. while the car is not allowed to move.</summary>
        public void Reset()
        {
            _stuckTime = 0f;
            _reverseTimeLeft = 0f;
        }

        public CarInputData Apply(CarInputData command, float carSpeed, float deltaTime)
        {
            if (_reverseTimeLeft > 0f)
            {
                _reverseTimeLeft -= deltaTime;
                return AISteering.Command(-1f, _escapeSteer, boost: false, handbrake: false);
            }

            bool tryingToMove = command.Throttle > MinThrottleToCount;
            bool barelyMoving = carSpeed < StuckSpeed && carSpeed > -StuckSpeed;
            _stuckTime = tryingToMove && barelyMoving ? _stuckTime + deltaTime : 0f;

            if (_stuckTime > StuckTimeToReact)
            {
                _stuckTime = 0f;
                _reverseTimeLeft = ReverseDuration;
                // Reverse steering the opposite way to where it was trying to go (right if it had no steering).
                _escapeSteer = command.Steer > 0f ? -1f : 1f;
            }

            return command;
        }
    }
}
