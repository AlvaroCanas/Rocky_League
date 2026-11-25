using System;
using RockyLeague.Core.StateMachines;
using UnityEngine;

namespace RockyLeague.AI.States
{
    /// <summary>
    /// Clear: the ball is dangerously close to our goal. No time to line up:
    /// go straight at it, at full boost, and hit it away from the goal.
    /// </summary>
    public sealed class AIClearState : IState
    {
        private const float FollowThrough = 1.5f;

        private readonly AIContext _context;

        public AIClearState(AIContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public void Enter()
        {
        }

        public void Tick(float deltaTime)
        {
            Vector3 ball = _context.PredictedBall;
            Vector3 awayFromGoal = (ball - _context.OwnGoal).normalized;
            Vector3 target = ball + awayFromGoal * FollowThrough;

            _context.Command = AISteering.DriveTo(_context, target, arrive: false, allowBoost: true);
        }

        public void Exit()
        {
        }
    }
}
