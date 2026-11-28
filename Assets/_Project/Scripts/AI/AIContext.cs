using RockyLeague.Arena;
using RockyLeague.Car;
using RockyLeague.Car.Inputs;
using UnityEngine;

namespace RockyLeague.AI
{
    /// <summary>
    /// What the bot knows about the game and what it wants to do, shared by all its states.
    ///
    /// The ball is seen through a snapshot taken only once per decision
    /// (<see cref="Perceive"/>), so slower bots really react later. The bot's own car is read
    /// live, as a driver always knows where they are. States write their decision in
    /// <see cref="Command"/>, which the car reads as its input.
    /// </summary>
    public sealed class AIContext
    {
        // Minimum speed used to estimate how long it takes to reach the ball.
        private const float MinApproachSpeed = 10f;

        private readonly Transform _car;
        private readonly CarController _controller;
        private readonly Ball _ball;
        private readonly ArenaSettings _arenaSettings;

        public AIContext(Transform car, CarController controller, Ball ball, ArenaBuilder arena, Team team, AIDifficulty difficulty)
        {
            _car = car;
            _controller = controller;
            _ball = ball;
            _arenaSettings = arena.Settings;
            Team = team;
            Difficulty = difficulty;

            OwnGoal = Flat(arena.GetGoalCenter(team));
            RivalGoal = Flat(arena.GetGoalCenter(team.Opponent()));
            FieldCenter = (OwnGoal + RivalGoal) * 0.5f;
            AttackDirection = (RivalGoal - OwnGoal).normalized;
        }

        public Team Team { get; }
        public AIDifficulty Difficulty { get; }

        // Field layout (on the ground plane, y = 0).
        public Vector3 OwnGoal { get; }
        public Vector3 RivalGoal { get; }
        public Vector3 FieldCenter { get; }
        public Vector3 AttackDirection { get; }

        // The bot's own car, always up to date.
        public Vector3 CarPosition => Flat(_car.position);
        public float CarSpeed => _controller.ForwardSpeed;
        public float BoostLeft => _controller.NormalizedBoost;
        public bool IsCarGrounded => _controller.IsGrounded;

        // The ball, as last perceived.
        public Vector3 BallPosition { get; private set; }
        public float BallHeight { get; private set; }
        public Vector3 BallVelocity { get; private set; }
        public Vector3 PredictedBall { get; private set; }

        /// <summary>What the bot does this physics step.</summary>
        public CarInputData Command { get; set; }

        public float DistanceToBall => Vector3.Distance(CarPosition, BallPosition);
        public float BallDistanceToOwnGoal => Vector3.Distance(BallPosition, OwnGoal);

        /// <summary>True if the ball is between the car and the rival goal.</summary>
        public bool IsCarBehindBall => Vector3.Dot(BallPosition - CarPosition, AttackDirection) > 0f;

        public bool IsBallInOwnHalf => Vector3.Dot(BallPosition - FieldCenter, AttackDirection) < 0f;

        /// <summary>Takes a new snapshot of the ball and guesses where it will be.</summary>
        public void Perceive()
        {
            BallPosition = Flat(_ball.Position);
            BallHeight = _ball.Position.y;
            BallVelocity = Flat(_ball.Velocity);

            // Look further ahead when the ball is far, but never more than the difficulty allows.
            float speed = Mathf.Max(Mathf.Abs(CarSpeed), MinApproachSpeed);
            float lookAhead = Mathf.Min(DistanceToBall / speed, Difficulty.MaxPredictionTime);
            Vector3 guess = BallPosition + BallVelocity * lookAhead;

            Vector2 error = Random.insideUnitCircle * Difficulty.AimError;
            guess += new Vector3(error.x, 0f, error.y);

            PredictedBall = ClampToField(guess);
        }

        /// <summary>Position of a world point as seen from the car (x = right, z = forward).</summary>
        public Vector3 ToCarSpace(Vector3 worldPoint)
        {
            Vector3 local = _car.InverseTransformPoint(new Vector3(worldPoint.x, _car.position.y, worldPoint.z));
            local.y = 0f;
            return local;
        }

        /// <summary>Keeps a target inside the field, a little away from the walls.</summary>
        public Vector3 ClampToField(Vector3 point)
        {
            const float wallMargin = 2f;
            Vector3 local = point - FieldCenter;
            float halfWidth = _arenaSettings.Width * 0.5f - wallMargin;
            float halfLength = _arenaSettings.Length * 0.5f - wallMargin;

            // The arena is aligned with the world axes: X is the width, Z the length.
            local.x = Mathf.Clamp(local.x, -halfWidth, halfWidth);
            local.z = Mathf.Clamp(local.z, -halfLength, halfLength);

            // Corners are cut at 45 degrees: pull points behind the diagonal wall back in front of it.
            float overshoot = CornerOvershoot(local, wallMargin);
            if (overshoot > 0f)
            {
                local.x -= Mathf.Sign(local.x) * overshoot * 0.5f;
                local.z -= Mathf.Sign(local.z) * overshoot * 0.5f;
            }

            return FieldCenter + local;
        }

        /// <summary>True if the point is closer than <paramref name="margin"/> to a side or end wall.</summary>
        public bool IsNearWall(Vector3 point, float margin)
        {
            Vector3 local = point - FieldCenter;
            float toSideWall = _arenaSettings.Width * 0.5f - Mathf.Abs(local.x);
            float toEndWall = _arenaSettings.Length * 0.5f - Mathf.Abs(local.z);
            return toSideWall < margin || toEndWall < margin || CornerOvershoot(local, margin) > 0f;
        }

        /// <summary>
        /// How far a point (relative to the centre) is past the line that stays
        /// <paramref name="margin"/> in front of a diagonal corner wall; 0 or less = in front of it.
        /// </summary>
        private float CornerOvershoot(Vector3 local, float margin)
        {
            float corner = _arenaSettings.CornerSize;
            if (corner <= 0f)
            {
                return 0f;
            }

            // Along the diagonal, x + z grows by sqrt(2) per meter away from the wall.
            float intoX = Mathf.Abs(local.x) - (_arenaSettings.Width * 0.5f - corner);
            float intoZ = Mathf.Abs(local.z) - (_arenaSettings.Length * 0.5f - corner);
            return intoX + intoZ - (corner - margin * Mathf.Sqrt(2f));
        }

        private static Vector3 Flat(Vector3 v)
        {
            return new Vector3(v.x, 0f, v.z);
        }
    }
}
