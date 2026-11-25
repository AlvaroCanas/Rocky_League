using System;
using RockyLeague.Car.Inputs;
using RockyLeague.Core.StateMachines;
using UnityEngine;

namespace RockyLeague.AI.States
{
    /// <summary>
    /// Attack: get to a spot just behind the ball, lined up with the rival goal, and then
    /// drive through the ball so it goes towards the goal. Jumps for balls in the air.
    /// </summary>
    public sealed class AIAttackState : IState
    {
        // How far behind the ball the bot lines up before hitting it.
        private const float ApproachDistance = 4f;
        // How lined up (degrees) the bot must be to go for the hit.
        private const float AlignedAngle = 30f;
        // Drive this far past the ball centre so the hit is firm.
        private const float FollowThrough = 2f;
        // Jump when the ball is this high and this close.
        private const float JumpBallHeight = 2.5f;
        private const float JumpMaxDistance = 5f;

        private readonly AIContext _context;

        public AIAttackState(AIContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public void Enter()
        {
        }

        public void Tick(float deltaTime)
        {
            Vector3 ball = _context.PredictedBall;
            Vector3 ballToGoal = (_context.RivalGoal - ball).normalized;
            Vector3 carToBall = ball - _context.CarPosition;
            bool aligned = Vector3.Angle(carToBall, ballToGoal) < AlignedAngle;

            Vector3 target = aligned
                ? ball + ballToGoal * FollowThrough
                : _context.ClampToField(ball - ballToGoal * ApproachDistance);

            CarInputData drive = AISteering.DriveTo(_context, target, arrive: false, allowBoost: aligned);

            bool jump = _context.Difficulty.CanJump
                && aligned
                && _context.BallHeight > JumpBallHeight
                && _context.DistanceToBall < JumpMaxDistance;

            _context.Command = jump
                ? AISteering.Command(drive.Throttle, drive.Steer, drive.Boost, drive.Handbrake, jump: true)
                : drive;
        }

        public void Exit()
        {
        }
    }
}
