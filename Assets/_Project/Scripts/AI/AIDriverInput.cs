using System;
using RockyLeague.AI.States;
using RockyLeague.Arena;
using RockyLeague.Car;
using RockyLeague.Car.Inputs;
using RockyLeague.Core.StateMachines;
using UnityEngine;

namespace RockyLeague.AI
{
    /// <summary>
    /// Hand-written AI driver. To the car it is just another <see cref="ICarInput"/>
    /// (Strategy): the car does not know whether a person or a bot is driving it.
    ///
    /// Its brain is a state machine (State pattern, reusing the project's StateMachine)
    /// with four behaviours. Every decision, which happens once per reaction time,
    /// it looks at the ball and picks one:
    ///   Clear       ball close to our goal               -> go for it, whatever the angle
    ///   Reposition  car past the ball, ball in their half -> go round, get behind it
    ///   Defend      car past the ball, ball in our half   -> stand between ball and goal
    ///   Attack      otherwise (car behind the ball)       -> line up and shoot at their goal
    /// </summary>
    [RequireComponent(typeof(CarController))]
    public sealed class AIDriverInput : MonoBehaviour, ICarInput
    {
        [SerializeField] private Team _team = Team.Away;
        [SerializeField] private AIDifficulty _difficulty;
        [SerializeField] private Ball _ball;
        [SerializeField] private ArenaBuilder _arena;
        [SerializeField, Min(1f), Tooltip("Ball closer than this to our goal means Clear.")]
        private float _clearRadius = 14f;

        private AIContext _context;
        private AIStuckRecovery _stuckRecovery;
        private StateMachine _brain;
        private AIAttackState _attack;
        private AIDefendState _defend;
        private AIClearState _clear;
        private AIRepositionState _reposition;
        private IState _desiredState;
        private float _nextDecisionTime;
        private Func<bool> _canDrive = () => true;

        /// <summary>Name of the current behaviour, for debugging.</summary>
        public string CurrentBehaviour => _brain?.CurrentState?.GetType().Name ?? "None";

        /// <summary>
        /// Sets the bot up from code (for a factory that spawns cars).
        /// Must be called before the first Start.
        /// </summary>
        /// <param name="canDrive">
        /// Tells the bot whether its car is allowed to move (false during a kickoff countdown and
        /// after the final whistle). Meanwhile the bot does not think at all, so it never mistakes
        /// "not allowed to move" for "stuck". Null means it can always drive.
        /// </param>
        public void Initialize(Team team, AIDifficulty difficulty, Ball ball, ArenaBuilder arena, Func<bool> canDrive = null)
        {
            _team = team;
            _difficulty = difficulty;
            _ball = ball;
            _arena = arena;
            _canDrive = canDrive ?? (() => true);
        }

        public CarInputData ReadInput()
        {
            if (_context == null || !isActiveAndEnabled)
            {
                return CarInputData.None;
            }

            return _context.Command;
        }

        // Start: the arena and the ball are set up in their own Awake.
        private void Start()
        {
            if (_difficulty == null || _ball == null || _arena == null)
            {
                Debug.LogError($"{nameof(AIDriverInput)} on '{name}' is missing its difficulty, ball or arena.", this);
                enabled = false;
                return;
            }

            _context = new AIContext(transform, GetComponent<CarController>(), _ball, _arena, _team, _difficulty);
            _stuckRecovery = new AIStuckRecovery();
            _brain = BuildBrain();
        }

        private void FixedUpdate()
        {
            if (_context == null)
            {
                return;
            }

            // While the car may not move (countdown, end of the match) the bot does nothing at
            // all, and decides again as soon as it can drive.
            if (!_canDrive())
            {
                _stuckRecovery.Reset();
                _context.Command = CarInputData.None;
                _nextDecisionTime = 0f;
                return;
            }

            if (Time.time >= _nextDecisionTime)
            {
                _context.Perceive();
                _desiredState = ChooseBehaviour();
                _nextDecisionTime = Time.time + _difficulty.ReactionTime;
            }

            _brain.Tick(Time.fixedDeltaTime);
            _context.Command = _stuckRecovery.Apply(_context.Command, _context.CarSpeed, Time.fixedDeltaTime);
        }

        private StateMachine BuildBrain()
        {
            _attack = new AIAttackState(_context);
            _defend = new AIDefendState(_context);
            _clear = new AIClearState(_context);
            _reposition = new AIRepositionState(_context);

            // Every behaviour can be reached from any other: the decision is taken in
            // ChooseBehaviour and the transitions only follow it.
            StateMachine brain = new StateMachine();
            brain.AddAnyStateTransition(_clear, () => _desiredState == _clear);
            brain.AddAnyStateTransition(_reposition, () => _desiredState == _reposition);
            brain.AddAnyStateTransition(_defend, () => _desiredState == _defend);
            brain.AddAnyStateTransition(_attack, () => _desiredState == _attack);

            _desiredState = _attack;
            brain.ChangeState(_attack);
            return brain;
        }

        private IState ChooseBehaviour()
        {
            if (_context.BallDistanceToOwnGoal < _clearRadius)
            {
                return _clear;
            }

            if (!_context.IsCarBehindBall)
            {
                return _context.IsBallInOwnHalf ? _defend : _reposition;
            }

            return _attack;
        }
    }
}
