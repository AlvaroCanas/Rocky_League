using System;
using System.Collections.Generic;
using RockyLeague.Core.Game;
using RockyLeague.Core.StateMachines;
using RockyLeague.Match.States;
using UnityEngine;

namespace RockyLeague.Match
{
    /// <summary>
    /// Match-level flow (lives in the Match scene): Countdown -> Playing -> MatchEnd.
    /// It builds the match from the <see cref="MatchConfig"/> chosen in the menu, owns the
    /// state machine and the <see cref="MatchScore"/>, and ticks them every frame.
    ///
    /// It is separate from the GameManager on purpose (SRP): the GameManager decides WHICH
    /// scene is active for the whole application; this class decides what happens INSIDE a match.
    /// It only exists while the Match scene is loaded, so every match starts from a clean state.
    /// </summary>
    public sealed class MatchController : MonoBehaviour
    {
        private StateMachine _stateMachine;
        private CountdownState _countdown;
        private Dictionary<IState, MatchPhase> _phaseByState;

        public MatchConfig Config { get; private set; }

        public MatchScore Score { get; private set; }

        public MatchPhase Phase { get; private set; } = MatchPhase.Countdown;

        /// <summary>Seconds left before kickoff, or 0 when the match is not counting down.</summary>
        public float CountdownRemaining => Phase == MatchPhase.Countdown ? _countdown.SecondsRemaining : 0f;

        /// <summary>Raised every time the match enters a new phase.</summary>
        public event Action<MatchPhase> PhaseChanged;

        private void Awake()
        {
            Config = ResolveConfig();
            Score = new MatchScore(Config.MatchDuration);

            _countdown = new CountdownState(Config.CountdownSeconds);
            PlayingState playing = new PlayingState(Score);
            MatchEndState matchEnd = new MatchEndState();

            _phaseByState = new Dictionary<IState, MatchPhase>
            {
                { _countdown, MatchPhase.Countdown },
                { playing, MatchPhase.Playing },
                { matchEnd, MatchPhase.MatchEnd }
            };

            // Transitions are plain predicates registered here, so the states stay
            // independent from each other. Overtime will be added between Playing and
            // MatchEnd in its own feature without touching these states.
            _stateMachine = new StateMachine();
            _stateMachine.AddTransition(_countdown, playing, () => _countdown.IsFinished);
            _stateMachine.AddTransition(playing, matchEnd, () => Score.IsTimeUp);
        }

        private void OnEnable()
        {
            _stateMachine.StateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            _stateMachine.StateChanged -= OnStateChanged;
        }

        private void Start()
        {
            _stateMachine.ChangeState(_countdown);
        }

        private void Update()
        {
            _stateMachine.Tick(Time.deltaTime);
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

        private void OnStateChanged(IState previous, IState next)
        {
            Phase = _phaseByState[next];
            PhaseChanged?.Invoke(Phase);
        }
    }
}
