using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    /// It asks the <see cref="MatchFactory"/> to build the match from the <see cref="MatchConfig"/>
    /// chosen in the menu, owns the state machine and the <see cref="MatchClock"/>, and ticks
    /// them every frame. Before every kickoff (the first one included) everything that is
    /// <see cref="IResettable"/> goes back to its kickoff spot: the ball to the centre and the
    /// cars to a random, fair set of spots. Cars can only move while the ball is in play and
    /// during the goal celebration (<see cref="CarsCanDrive"/>), never during the countdown.
    ///
    /// It is also the referee: it is the only listener of the goals, and decides whether a goal
    /// counts (only while the ball is in play). A valid goal is given to the <see cref="ScoreKeeper"/>
    /// and announced again through <see cref="GoalScored"/> for effects and UI (<see cref="IGoalSource"/>).
    /// When a goal is scored the ball disappears and the cars keep moving during a celebration
    /// (a coroutine, so goal effects can be added later). At the final whistle the match is
    /// closed: the ball disappears and every car is frozen, so nothing happens behind the results.
    ///
    /// The score is NOT kept here: the <see cref="ScoreKeeper"/> counts the goals and this class
    /// only asks it who is ahead when the match needs to know (SRP, no duplicated state).
    ///
    /// It is separate from the GameManager on purpose (SRP): the GameManager decides WHICH
    /// scene is active for the whole application; this class decides what happens INSIDE a match.
    /// It only exists while the Match scene is loaded, so every match starts from a clean state.
    /// </summary>
    public sealed class MatchController : MonoBehaviour, IGoalSource
    {
        [SerializeField] private ScoreKeeper _scoreKeeper;
        [SerializeField, Tooltip("Creates the cars of the match from the chosen configuration.")]
        private MatchFactory _factory;
        [SerializeField, Tooltip("Provides the goals whose GoalScored event starts the celebration.")]
        private ArenaBuilder _arena;
        [SerializeField, Tooltip("Hidden while a goal is celebrated and after the final whistle.")]
        private Ball _ball;
        [SerializeField, Min(0f), Tooltip("Testing only: if above 0, the match lasts this many seconds instead of the chosen duration.")]
        private float _debugMatchDuration;

        private readonly List<IResettable> _resettables = new List<IResettable>();

        private StateMachine _stateMachine;
        private CountdownState _countdown;
        private GoalScoredState _goalScored;
        private MatchEndState _matchEnd;
        private Team _playerTeam = Team.Home;
        private Dictionary<IState, MatchPhase> _phaseByState;
        private Goal _homeGoal;
        private Goal _awayGoal;
        private Team? _pendingGoal;

        public MatchConfig Config { get; private set; }

        public MatchClock Clock { get; private set; }

        public MatchPhase Phase { get; private set; } = MatchPhase.Countdown;

        /// <summary>Seconds left before kickoff, or 0 when the match is not counting down.</summary>
        public float CountdownRemaining => Phase == MatchPhase.Countdown ? _countdown.SecondsRemaining : 0f;

        /// <summary>True while cars may move: with the ball in play and during a goal celebration.</summary>
        public bool CarsCanDrive => Phase == MatchPhase.Playing || Phase == MatchPhase.GoalScored;

        /// <summary>The team that scored the last goal, or null if there has been none.</summary>
        public Team? LastScorer { get; private set; }

        /// <summary>How the match is going (or ended) for the human player's team.</summary>
        public MatchResult PlayerResult => ResultFor(_playerTeam);

        /// <summary>Win, loss or draw for <paramref name="team"/> with the current score.</summary>
        public MatchResult ResultFor(Team team)
        {
            if (_scoreKeeper == null || _scoreKeeper.IsTied)
            {
                return MatchResult.Draw;
            }

            return _scoreKeeper.Leader == team ? MatchResult.Win : MatchResult.Loss;
        }

        /// <summary>Raised every time the match enters a new phase.</summary>
        public event Action<MatchPhase> PhaseChanged;

        /// <summary>Raised for every goal that counts, after the score has been updated. The sender is this referee.</summary>
        public event EventHandler<GoalScoredEventArgs> GoalScored;

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
            if (_factory == null)
            {
                Debug.LogError($"{nameof(MatchController)} on '{name}' has no {nameof(MatchFactory)} assigned: no cars will be created.", this);
            }

            Config = ResolveConfig();
            if (_debugMatchDuration > 0f)
            {
                Config = Config.WithDuration(_debugMatchDuration);
                Debug.Log($"Testing: the match lasts {_debugMatchDuration} s.");
            }
            Clock = new MatchClock(Config.MatchDuration);

            _countdown = new CountdownState(Config.CountdownSeconds);
            PlayingState playing = new PlayingState(Clock);
            _goalScored = new GoalScoredState();
            _matchEnd = new MatchEndState();

            _phaseByState = new Dictionary<IState, MatchPhase>
            {
                { _countdown, MatchPhase.Countdown },
                { playing, MatchPhase.Playing },
                { _goalScored, MatchPhase.GoalScored },
                { _matchEnd, MatchPhase.MatchEnd }
            };

            // Transitions are plain predicates registered here, so the states stay independent
            // from each other. For states with several exits, the first one that holds wins.
            _stateMachine = new StateMachine();
            _stateMachine.AddTransition(_countdown, playing, () => _countdown.IsFinished);

            // A goal wins over the final whistle if both happen in the same step.
            _stateMachine.AddTransition(playing, _goalScored, () => _pendingGoal.HasValue);
            _stateMachine.AddTransition(playing, _matchEnd, () => Clock.IsTimeUp);

            // A goal in the last moment ends the match after the celebration, with no new kickoff.
            // Overtime will be added here in its own feature.
            _stateMachine.AddTransition(_goalScored, _matchEnd, () => _goalScored.IsFinished && Clock.IsTimeUp);
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
        // runs every Awake before any Start.
        private void Start()
        {
            SubscribeToGoals();
            BuildMatch();
            ResetField();
            _stateMachine.ChangeState(_countdown);
        }

        private void Update()
        {
            _stateMachine.Tick(Time.deltaTime);
        }

        // Always unsubscribe: a forgotten subscription keeps references alive between matches.
        private void OnDestroy()
        {
            if (_homeGoal != null)
            {
                _homeGoal.GoalScored -= OnGoalScored;
            }
            if (_awayGoal != null)
            {
                _awayGoal.GoalScored -= OnGoalScored;
            }
        }

        // Without a GameManager (the Match scene was opened directly in the editor) or without
        // a chosen config, fall back to a 1v1 so the scene can be tested on its own.
        private static MatchConfig ResolveConfig()
        {
            GameManager gameManager = GameManager.Instance;
            if (gameManager != null && gameManager.CurrentMatchConfig != null)
            {
                Debug.Log($"Starting match: {gameManager.CurrentMatchConfig}");
                return gameManager.CurrentMatchConfig;
            }

            MatchConfig fallback = MatchConfig.OneVsOne();
            Debug.Log($"No match configuration received: using a default {fallback}.");
            return fallback;
        }

        private void SubscribeToGoals()
        {
            if (_arena == null)
            {
                return;
            }

            _homeGoal = _arena.GetGoal(Team.Home);
            _awayGoal = _arena.GetGoal(Team.Away);
            _homeGoal.GoalScored += OnGoalScored;
            _awayGoal.GoalScored += OnGoalScored;
        }

        // Everything that goes back to its spot before a kickoff is registered here explicitly,
        // instead of searching the scene: the factory says which cars exist (DIP, no hidden lookups).
        private void BuildMatch()
        {
            if (_ball != null)
            {
                _resettables.Add(_ball);
            }

            if (_factory != null)
            {
                _resettables.Add(_factory.Build(Config, () => CarsCanDrive));

                // Looked up once, so LINQ is fine here.
                MatchCar player = _factory.Cars.FirstOrDefault(car => car.IsPlayer);
                _playerTeam = player != null ? player.Team : Team.Home;
            }
        }

        private void OnGoalScored(object sender, GoalScoredEventArgs e)
        {
            // The referee's rule: only a goal with the ball in play counts. Anything else
            // (the ball rolling in after the final whistle...) is ignored by score, effects and UI.
            if (Phase != MatchPhase.Playing || _pendingGoal.HasValue)
            {
                return;
            }

            _pendingGoal = e.ScoringTeam;

            if (_ball != null)
            {
                _ball.Hide();
            }

            if (_scoreKeeper != null)
            {
                _scoreKeeper.AddGoal(e.ScoringTeam);
            }

            GoalScored?.Invoke(this, e);
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
            else if (next == _matchEnd)
            {
                StopPlay();
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

        // The final whistle: nothing may move or score any more. The ball disappears (so it cannot
        // roll into a goal) and every car stops where it is. Bots stop thinking on their own,
        // because CarsCanDrive is now false.
        private void StopPlay()
        {
            if (_ball != null)
            {
                _ball.Hide();
            }

            if (_factory != null)
            {
                foreach (MatchCar matchCar in _factory.Cars)
                {
                    matchCar.Car.Freeze();
                }
            }
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
