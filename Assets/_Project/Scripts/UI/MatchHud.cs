using RockyLeague.Arena;
using RockyLeague.Match;
using TMPro;
using UnityEngine;

namespace RockyLeague.UI
{
    /// <summary>
    /// On-screen display of the match clock and the central message (countdown number,
    /// goal announcement or final result). It only READS from the <see cref="MatchController"/> and holds no game logic.
    ///
    /// The score is not drawn here: <see cref="ScoreView"/> already shows it, listening to the
    /// ScoreKeeper, so each piece of information has exactly one view (SRP, DRY).
    ///
    /// Text is rebuilt only when the displayed value changes (a new second), not every frame,
    /// so it does not create garbage strings in Update.
    /// </summary>
    public sealed class MatchHud : MonoBehaviour
    {
        [SerializeField] private MatchController _match;
        [SerializeField] private TMP_Text _timerText;
        [SerializeField] private TMP_Text _messageText;

        private int _shownSeconds = -1;
        private int _shownCountdown = -1;

        private void OnEnable()
        {
            if (_match == null)
            {
                Debug.LogError($"{nameof(MatchHud)} on '{name}' has no {nameof(MatchController)} assigned.", this);
                enabled = false;
                return;
            }

            _match.PhaseChanged += OnPhaseChanged;
        }

        private void OnDisable()
        {
            if (_match != null)
            {
                _match.PhaseChanged -= OnPhaseChanged;
            }
        }

        private void Start()
        {
            // The controller has built the clock in its Awake, so it can be read here.
            OnPhaseChanged(_match.Phase);
        }

        private void Update()
        {
            RefreshTimer();
            RefreshCountdown();
        }

        private void OnPhaseChanged(MatchPhase phase)
        {
            _shownCountdown = -1;

            switch (phase)
            {
                case MatchPhase.Playing:
                    _messageText.text = string.Empty;
                    break;
                case MatchPhase.GoalScored:
                    _messageText.text = BuildGoalText();
                    break;
                case MatchPhase.MatchEnd:
                    _messageText.text = BuildResultText();
                    break;
            }
        }

        private void RefreshTimer()
        {
            int seconds = Mathf.CeilToInt(_match.Clock.TimeRemaining);
            if (seconds == _shownSeconds)
            {
                return;
            }

            _shownSeconds = seconds;
            _timerText.text = $"{seconds / 60}:{seconds % 60:00}";
        }

        private void RefreshCountdown()
        {
            if (_match.Phase != MatchPhase.Countdown)
            {
                return;
            }

            int seconds = Mathf.CeilToInt(_match.CountdownRemaining);
            if (seconds == _shownCountdown)
            {
                return;
            }

            _shownCountdown = seconds;
            _messageText.text = seconds > 0 ? seconds.ToString() : string.Empty;
        }

        private string BuildGoalText()
        {
            Team? scorer = _match.LastScorer;
            return scorer.HasValue ? $"GOAL!\n<size=40%>{scorer.Value.ToString().ToUpperInvariant()}</size>" : "GOAL!";
        }

        private string BuildResultText()
        {
            Team? winner = _match.Winner;
            return winner.HasValue ? $"{winner.Value.ToString().ToUpperInvariant()} WINS" : "DRAW";
        }
    }
}
