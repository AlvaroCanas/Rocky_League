using RockyLeague.Core.Game;
using RockyLeague.Match;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RockyLeague.UI
{
    /// <summary>
    /// Pause menu: RESUME, RESTART and MENU (back to the mode selection screen), plus a
    /// reminder of the controls, and the small
    /// PAUSE button shown during the match. It listens to <see cref="MatchPause"/> (Observer)
    /// and shows or hides its panel; it never pauses the game by itself, it only asks
    /// MatchPause to (SRP: deciding about the pause and drawing the menu are separate jobs).
    /// The PAUSE button disappears at the final whistle, when the results take over.
    /// </summary>
    public sealed class PauseMenuView : MonoBehaviour
    {
        [SerializeField] private MatchPause _pause;
        [SerializeField] private MatchController _match;
        [SerializeField] private UIPanel _panel;

        [Header("Buttons")]
        [SerializeField, Tooltip("On-screen button that opens the pause menu during the match.")]
        private Button _pauseButton;
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _menuButton;

        private void Awake()
        {
            if (_pause == null || _match == null || _panel == null || _pauseButton == null
                || _resumeButton == null || _restartButton == null || _menuButton == null)
            {
                Debug.LogError($"{nameof(PauseMenuView)} on '{name}' is missing some references.", this);
                enabled = false;
            }
        }

        // Null checks: OnDisable also runs when the scene unloads after Awake switched this off.
        private void OnEnable()
        {
            if (_pause == null || _match == null || _pauseButton == null || _resumeButton == null
                || _restartButton == null || _menuButton == null)
            {
                return;
            }

            _pause.PausedChanged += OnPausedChanged;
            _match.PhaseChanged += OnPhaseChanged;
            _pauseButton.onClick.AddListener(_pause.Pause);
            _resumeButton.onClick.AddListener(_pause.Resume);
            _restartButton.onClick.AddListener(OnRestartClicked);
            _menuButton.onClick.AddListener(OnMenuClicked);
        }

        private void OnDisable()
        {
            if (_pause != null)
            {
                _pause.PausedChanged -= OnPausedChanged;
                if (_pauseButton != null)
                {
                    _pauseButton.onClick.RemoveListener(_pause.Pause);
                }
                if (_resumeButton != null)
                {
                    _resumeButton.onClick.RemoveListener(_pause.Resume);
                }
            }
            if (_match != null)
            {
                _match.PhaseChanged -= OnPhaseChanged;
            }
            if (_restartButton != null)
            {
                _restartButton.onClick.RemoveListener(OnRestartClicked);
            }
            if (_menuButton != null)
            {
                _menuButton.onClick.RemoveListener(OnMenuClicked);
            }
        }

        private void OnPausedChanged(bool paused)
        {
            if (paused)
            {
                _panel.Show();
                EventSystem.current?.SetSelectedGameObject(_resumeButton.gameObject);
            }
            else
            {
                _panel.Hide();
            }
        }

        private void OnPhaseChanged(MatchPhase phase)
        {
            _pauseButton.gameObject.SetActive(phase != MatchPhase.MatchEnd);
        }

        // The game stays paused while the scene loads; MatchPause restores the time scale
        // when the scene is unloaded.
        private void OnRestartClicked()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogWarning("No GameManager found: start the game from the main menu scene.", this);
                return;
            }

            _panel.SetInteractable(false);
            GameManager.Instance.RestartMatch();
        }

        private void OnMenuClicked()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogWarning("No GameManager found: start the game from the main menu scene.", this);
                return;
            }

            _panel.SetInteractable(false);
            GameManager.Instance.GoToPlayMode();
        }
    }
}
