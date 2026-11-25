using RockyLeague.Arena;
using RockyLeague.Match;
using TMPro;
using UnityEngine;

namespace RockyLeague.UI
{
    /// <summary>
    /// On-screen display of the match: clock, score and the central message (countdown
    /// number or final result). It only READS from the <see cref="MatchController"/> and
    /// holds no game logic.
    ///
    /// Text is rebuilt only when the displayed value changes (a new second, a new goal),
    /// not every frame, so it does not create garbage strings in Update.
    /// </summary>
    public sealed class MatchHud : MonoBehaviour
    {
        [SerializeField] private MatchController _match;
        [SerializeField] private TMP_Text _timerText;
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private TMP_Text _messageText;

        private int _shownSeconds = -1;
        private int _shownBlueGoals = -1;
        private int _shownOrangeGoals = -1;
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
            // The controller has built the score in its Awake, so it can be read here.
            OnPhaseChanged(_match.Phase);
        }

        private void Update()
        {
            RefreshTimer();
            RefreshScore();
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
                case MatchPhase.MatchEnd:
                    _messageText.text = BuildResultText();
                    break;
            }
        }

        private void RefreshTimer()
        {
            int seconds = Mathf.CeilToInt(_match.Score.TimeRemaining);
            if (seconds == _shownSeconds)
            {
                return;
            }

            _shownSeconds = seconds;
            _timerText.text = $"{seconds / 60}:{seconds % 60:00}";
        }

        private void RefreshScore()
        {
            int blue = _match.Score.GetGoals(Team.Blue);
            int orange = _match.Score.GetGoals(Team.Orange);
            if (blue == _shownBlueGoals && orange == _shownOrangeGoals)
            {
                return;
            }

            _shownBlueGoals = blue;
            _shownOrangeGoals = orange;
            _scoreText.text = $"{blue} - {orange}";
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

        private string BuildResultText()
        {
            Team? winner = _match.Score.Winner;
            return winner.HasValue ? $"{winner.Value.ToString().ToUpperInvariant()} WINS" : "DRAW";
        }
    }
}
