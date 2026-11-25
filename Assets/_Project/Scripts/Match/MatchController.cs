using System;
using System.Collections;
using System.Collections.Generic;
using RockyLeague.Arena;
using RockyLeague.Core;
using RockyLeague.Core.Game;
using RockyLeague.Core.StateMachines;
using RockyLeague.Match.States;
using UnityEngine;

namespace RockyLeague.Match
{
    /// <summary>
    /// Match-level flow (lives in the Match scene):
    ///
    ///   Countdown -> Playing -> GoalScored -> Countdown -> ...  and finally  MatchEnd
    ///
    /// It builds the match from the <see cref="MatchConfig"/> chosen in the menu, owns the
    /// state machine and the <see cref="MatchClock"/>, and ticks them every frame.
    /// When a goal is scored the ball disappears, the cars keep moving during a celebration
    /// (a coroutine, so goal effects can be added to it later), and then everything that is
    /// <see cref="IResettable"/> goes back to its kickoff spot before the next countdown.
    ///
    /// The score is NOT kept here: the <see cref="ScoreKeeper"/> counts the goals and this class
    /// only asks it who is ahead when the match needs to know (SRP, no duplicated state).
    ///
    /// It is separate from the GameManager on purpose (SRP): the GameManager decides WHICH
    /// scene is active for the whole application; this class decides what happens INSIDE a match.
    /// It only exists while the Match scene is loaded, so every match starts from a clean state.
    /// </summary>
    public sealed class MatchController : MonoBehaviour
    {
        [SerializeField] private ScoreKeeper _scoreKeeper;
        [SerializeField, Tooltip("Provides the goals whose GoalScored event starts the celebration.")]
        private ArenaBuilder _arena;
        [SerializeField, Tooltip("Hidden while a goal is celebrated.")]
        private Ball _ball;

        private readonly List<IResettable> _resettables = new List<IResettable>();

        private StateMachine _stateMachine;
        private CountdownState _countdown;
        private GoalScoredState _goalScored;
        private Dictionary<IState, MatchPhase> _phaseByState;
        private Goal _blueGoal;
        private Goal _orangeGoal;
        private Team? _pendingGoal;

        public MatchConfig Config { get; private set; }

        public MatchClock Clock { get; private set; }

        public MatchPhase Phase { get; private set; } = MatchPhase.Countdown;

        /// <summary>Seconds left before kickoff, or 0 when the match is not counting down.</summary>
        public float CountdownRemaining => Phase == MatchPhase.Countdown ? _countdown.SecondsRemaining : 0f;

        /// <summary>The team that scored the last goal, or null if there has been none.</summary>
        public Team? LastScorer { get; private set; }

        /// <summary>The team that is ahead, or null if the score is tied.</summary>
        public Team? Winner
        {
            get
            {
                if (_scoreKeeper == null || _scoreKeeper.IsTied)
                {
                    return null;
                }

                return _scoreKeeper.Leader;
            }
        }

        /// <summary>Raised every time the match enters a new phase.</summary>
        public event Action<MatchPhase> PhaseChanged;

        private void Awake()
        {
            if (_scoreKeeper == null)
            {
                Debug.LogError($"{nameof(MatchController)} on '{name}' has no {nameof(ScoreKeeper)} assigned: the winner cannot be decided.", this);
            }
            if (_arena == null || _ball == null)
            {
                Debug.LogError($"{nameof(MatchController)} on '{name}' needs the {nameof(ArenaBuilder)} and the {nameof(Ball)} assigned: goals will not be detected.", this);
            }

            Config = ResolveConfig();
            Clock = new MatchClock(Config.MatchDuration);

            _countdown = new CountdownState(Config.CountdownSeconds);
            PlayingState playing = new PlayingState(Clock);
            _goalScored = new GoalScoredState();
            MatchEndState matchEnd = new MatchEndState();

            _phaseByState = new Dictionary<IState, MatchPhase>
            {
                { _countdown, MatchPhase.Countdown },
                { playing, MatchPhase.Playing },
                { _goalScored, MatchPhase.GoalScored },
                { matchEnd, MatchPhase.MatchEnd }
            };

            // Transitions are plain predicates registered here, so the states stay independent
            // from each other. For states with several exits, the first one that holds wins.
            _stateMachine = new StateMachine();
            _stateMachine.AddTransition(_countdown, playing, () => _countdown.IsFinished);

            // A goal wins over the final whistle if both happen in the same step.
            _stateMachine.AddTransition(playing, _goalScored, () => _pendingGoal.HasValue);
            _stateMachine.AddTransition(playing, matchEnd, () => Clock.IsTimeUp);

            // A goal in the last moment ends the match after the celebration, with no new kickoff.
            // Overtime will be added here in its own feature.
            _stateMachine.AddTransition(_goalScored, matchEnd, () => _goalScored.IsFinished && Clock.IsTimeUp);
            _stateMachine.AddTransition(_goalScored, _countdown, () => _goalScored.IsFinished);
        }

        private void OnEnable()
        {
            _stateMachine.StateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            _stateMachine.StateChanged -= OnStateChanged;
        }

        // Start instead of OnEnable: the arena creates its goals in its own Awake, and Unity
        // runs every Awake before any Start (the ScoreKeeper does the same for the same reason).
        private void Start()
        {
            SubscribeToGoals();
            CollectResettables();
            _stateMachine.ChangeState(_countdown);
        }

        private void Update()
        {
            _stateMachine.Tick(Time.deltaTime);
        }

        // Always unsubscribe: a forgotten subscription keeps references alive between matches.
        private void OnDestroy()
        {
            if (_blueGoal != null)
            {
                _blueGoal.GoalScored -= OnGoalScored;
            }
            if (_orangeGoal != null)
            {
                _orangeGoal.GoalScored -= OnGoalScored;
            }
        }

        // Without a GameManager (the Match scene was opened directly in the editor) or without
        // a chosen config, fall back to a 1v1 so the scene can be tested on its own.
        private static MatchConfig ResolveConfig()
        {
            GameManager gameManager = GameManager.Instance;
            if (gameManager != null && gameManager.CurrentMatchConfig != null)
            {
                return gameManager.CurrentMatchConfig;
            }

            Debug.Log("No match configuration received: using a default 1v1.");
            return MatchConfig.OneVsOne();
        }

        private void SubscribeToGoals()
        {
            if (_arena == null)
            {
                return;
            }

            _blueGoal = _arena.GetGoal(Team.Blue);
            _orangeGoal = _arena.GetGoal(Team.Orange);
            _blueGoal.GoalScored += OnGoalScored;
            _orangeGoal.GoalScored += OnGoalScored;
        }

        // Looked up once when the match starts. When a MatchFactory spawns the cars it will
        // register them here instead (YAGNI: there is nothing to register until then).
        private void CollectResettables()
        {
            MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is IResettable resettable)
                {
                    _resettables.Add(resettable);
                }
            }
        }

        private void OnGoalScored(object sender, GoalScoredEventArgs e)
        {
            // Only a goal with the ball in play counts for the match flow.
            if (Phase != MatchPhase.Playing || _pendingGoal.HasValue)
            {
                return;
            }

            _pendingGoal = e.ScoringTeam;

            if (_ball != null)
            {
                _ball.Hide();
            }
        }

        private void OnStateChanged(IState previous, IState next)
        {
            Phase = _phaseByState[next];

            if (next == _goalScored)
            {
                LastScorer = _pendingGoal;
                _pendingGoal = null;
                StartCoroutine(GoalCelebration());
            }

            PhaseChanged?.Invoke(Phase);
        }

        // A coroutine (not a timer inside the state) because it is a sequence of steps spread
        // over time: celebrate, then reset. Goal effects (particles, sound) can be added here
        // later, and the cars keep being driven while it waits.
        private IEnumerator GoalCelebration()
        {
            yield return new WaitForSeconds(Config.GoalCelebrationSeconds);

            ResetField();
            _goalScored.Finish();
        }

        private void ResetField()
        {
            foreach (IResettable resettable in _resettables)
            {
                resettable.ResetToKickoff();
            }
        }
    }
}
