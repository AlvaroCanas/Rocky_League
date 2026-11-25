using RockyLeague.Car.Inputs;

namespace RockyLeague.AI
{
    /// <summary>
    /// Safety net: if the bot keeps pressing the throttle but does not move (stuck against a
    /// wall or a goal post), it backs up for a moment. Kept apart from the decision states
    /// because it is not a decision, just a recovery that applies to all of them.
    /// </summary>
    public sealed class AIStuckRecovery
    {
        private const float StuckSpeed = 1f;
        private const float StuckTimeToReact = 1.2f;
        private const float ReverseDuration = 0.8f;

        private float _stuckTime;
        private float _reverseTimeLeft;

        public CarInputData Apply(CarInputData command, float carSpeed, float deltaTime)
        {
            if (_reverseTimeLeft > 0f)
            {
                _reverseTimeLeft -= deltaTime;
                return AISteering.Command(-command.Throttle, -command.Steer, boost: false, handbrake: false);
            }

            bool tryingToMove = command.Throttle > 0.5f;
            bool notMoving = carSpeed < StuckSpeed && carSpeed > -StuckSpeed;
            _stuckTime = tryingToMove && notMoving ? _stuckTime + deltaTime : 0f;

            if (_stuckTime > StuckTimeToReact)
            {
                _stuckTime = 0f;
                _reverseTimeLeft = ReverseDuration;
            }

            return command;
        }
    }
}
