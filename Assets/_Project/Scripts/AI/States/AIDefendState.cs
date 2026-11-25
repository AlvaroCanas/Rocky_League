using System;
using RockyLeague.Core.StateMachines;
using UnityEngine;

namespace RockyLeague.AI.States
{
    /// <summary>
    /// Defend: the ball is in our half and the bot is out of position, so it goes back
    /// to stand between the ball and its own goal, and waits there.
    /// </summary>
    public sealed class AIDefendState : IState
    {
        // Share of the goal-to-ball line where the bot stands (0 = on the goal line).
        private const float GuardFraction = 0.35f;
        private const float MaxGuardDistance = 12f;

        private readonly AIContext _context;

        public AIDefendState(AIContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public void Enter()
        {
        }

        public void Tick(float deltaTime)
        {
            Vector3 goalToBall = _context.BallPosition - _context.OwnGoal;
            float guardDistance = Mathf.Min(goalToBall.magnitude * GuardFraction, MaxGuardDistance);
            Vector3 guardSpot = _context.ClampToField(_context.OwnGoal + goalToBall.normalized * guardDistance);

            _context.Command = AISteering.DriveTo(_context, guardSpot, arrive: true, allowBoost: true);
        }

        public void Exit()
        {
        }
    }
}
