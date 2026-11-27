using System.Collections;
using RockyLeague.Arena;
using RockyLeague.Core.Game;
using RockyLeague.Match;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RockyLeague.UI
{
    /// <summary>
    /// End-of-match screen: result for the player (YOU WIN / YOU LOSE / DRAW), final score,
    /// mode and difficulty, and the PLAY AGAIN and MENU buttons (MENU goes back to the mode
    /// selection screen, to play again with other settings).
    ///
    /// It only READS the match (Observer of PhaseChanged) and asks the GameManager to change
    /// scene; it holds no game logic. It waits a moment after the final whistle (a coroutine)
    /// so the player sees "FULL TIME!" before the panel appears.
    /// </summary>
    public sealed class MatchResultsView : MonoBehaviour
    {
        [SerializeField] private MatchController _match;
        [SerializeField] private ScoreKeeper _scoreKeeper;
        [SerializeField] private UIPanel _panel;

        [Header("Texts")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private TMP_Text _detailsText;

        [Header("Buttons")]
        [SerializeField] private Button _playAgainButton;
        [SerializeField] private Button _menuButton;

        [Header("Looks")]
        [SerializeField, Min(0f)] private float _delaySeconds = 2f;
        [SerializeField] private Color _winColor = new Color(1f, 0.76f, 0.2f);
        [SerializeField] private Color _lossColor = new Color(1f, 0.45f, 0.42f);
        [SerializeField] private Color _drawColor = Color.white;
        [SerializeField] private Color _homeColor = new Color(0.25f, 0.5f, 1f);
        [SerializeField] private Color _awayColor = new Color(1f, 0.55f, 0.1f);

        private void Awake()
        {
            if (_match == null || _scoreKeeper == null || _panel == null || _titleText == null || _scoreText == null
                || _detailsText == null || _playAgainButton == null || _menuButton == null)
            {
                Debug.LogError($"{nameof(MatchResultsView)} on '{name}' is missing some references.", this);
                enabled = false;
            }
        }

        // Null checks: OnDisable also runs when the scene unloads after Awake switched this off.
        private void OnEnable()
        {
            if (_match == null || _playAgainButton == null || _menuButton == null)
            {
                return;
            }

            _match.PhaseChanged += OnPhaseChanged;
            _playAgainButton.onClick.AddListener(OnPlayAgainClicked);
            _menuButton.onClick.AddListener(OnMenuClicked);
        }

        private void OnDisable()
        {
            if (_match != null)
            {
                _match.PhaseChanged -= OnPhaseChanged;
            }
            if (_playAgainButton != null)
            {
                _playAgainButton.onClick.RemoveListener(OnPlayAgainClicked);
            }
            if (_menuButton != null)
            {
                _menuButton.onClick.RemoveListener(OnMenuClicked);
            }
        }

        private void OnPhaseChanged(MatchPhase phase)
        {
            if (phase == MatchPhase.MatchEnd)
            {
                StartCoroutine(ShowAfterDelay());
            }
        }

        private IEnumerator ShowAfterDelay()
        {
            yield return new WaitForSeconds(_delaySeconds);

            Fill();
            _panel.Show();
            EventSystem.current?.SetSelectedGameObject(_playAgainButton.gameObject);
        }

        private void Fill()
        {
            MatchResult result = _match.PlayerResult;
            _titleText.text = result switch
            {
                MatchResult.Win => "YOU WIN!",
                MatchResult.Loss => "YOU LOSE",
                _ => "DRAW"
            };
            _titleText.color = result switch
            {
                MatchResult.Win => _winColor,
                MatchResult.Loss => _lossColor,
                _ => _drawColor
            };

            string home = ColorUtility.ToHtmlStringRGB(_homeColor);
            string away = ColorUtility.ToHtmlStringRGB(_awayColor);
            _scoreText.text = $"<color=#{home}>{_scoreKeeper.GetGoals(Team.Home)}</color>  -  <color=#{away}>{_scoreKeeper.GetGoals(Team.Away)}</color>";

            MatchConfig config = _match.Config;
            _detailsText.text = $"{config.CarsPerTeam} vs {config.CarsPerTeam}  \u00B7  {config.Difficulty.ToLabel()}";
        }

        private void OnPlayAgainClicked()
        {
            if (TryGetGameManager(out GameManager gameManager))
            {
                _panel.SetInteractable(false);
                gameManager.RestartMatch();
            }
        }

        private void OnMenuClicked()
        {
            if (TryGetGameManager(out GameManager gameManager))
            {
                _panel.SetInteractable(false);
                gameManager.GoToPlayMode();
            }
        }

        private bool TryGetGameManager(out GameManager gameManager)
        {
            gameManager = GameManager.Instance;
            if (gameManager == null)
            {
                Debug.LogWarning("No GameManager found: start the game from the main menu scene.", this);
            }

            return gameManager != null;
        }
    }
}
