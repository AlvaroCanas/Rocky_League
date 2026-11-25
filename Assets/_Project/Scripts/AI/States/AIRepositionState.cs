using System;
using RockyLeague.Core.StateMachines;
using UnityEngine;

namespace RockyLeague.AI.States
{
    /// <summary>
    /// Reposition: the bot has gone past the ball (it is between the ball and the rival goal),
    /// so pushing now would send the ball the wrong way. It drives around the ball,
    /// on its own side, to get behind it again.
    /// </summary>
    public sealed class AIRepositionState : IState
    {
        // Where to go: this far behind the ball and this far to the side.
        private const float BehindDistance = 10f;
        private const float SideOffset = 5f;

        private readonly AIContext _context;

        public AIRepositionState(AIContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public void Enter()
        {
        }

        public void Tick(float deltaTime)
        {
            Vector3 ball = _context.BallPosition;
            Vector3 attack = _context.AttackDirection;
            Vector3 side = Vector3.Cross(Vector3.up, attack);

            // Go round the ball on the side where the car already is, so it does not hit it.
            float sideSign = Vector3.Dot(_context.CarPosition - ball, side) >= 0f ? 1f : -1f;
            Vector3 target = ball - attack * BehindDistance + side * (SideOffset * sideSign);

            _context.Command = AISteering.DriveTo(_context, _context.ClampToField(target), arrive: false, allowBoost: true);
        }

        public void Exit()
        {
        }
    }
}
