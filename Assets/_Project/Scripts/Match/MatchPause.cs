using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RockyLeague.Match
{
    /// <summary>
    /// Pauses and resumes the match (Esc, or the on-screen button). Pausing is <c>Time.timeScale = 0</c>: physics, the clock, the countdown and the
    /// goal celebration all stop at once without any of them knowing about the pause.
    ///
    /// It only decides WHEN the game is paused and publishes <see cref="PausedChanged"/>
    /// (Observer); showing the pause menu is the job of the UI (SRP).
    /// The match cannot be paused after the final whistle (the results panel is on screen),
    /// and the time scale is always restored when the Match scene is left, so a pause can never
    /// leak into the menu or into the next match.
    /// </summary>
    public sealed class MatchPause : MonoBehaviour
    {
        [SerializeField] private MatchController _match;

        private InputAction _pauseAction;

        public bool IsPaused { get; private set; }

        public bool CanPause => _match != null && _match.Phase != MatchPhase.MatchEnd;

        /// <summary>Raised with true when the game pauses and false when it resumes.</summary>
        public event Action<bool> PausedChanged;

        public void Toggle()
        {
            if (IsPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }

        public void Pause()
        {
            if (IsPaused || !CanPause)
            {
                return;
            }

            IsPaused = true;
            Time.timeScale = 0f;
            PausedChanged?.Invoke(true);
        }

        public void Resume()
        {
            if (!IsPaused)
            {
                return;
            }

            IsPaused = false;
            Time.timeScale = 1f;
            PausedChanged?.Invoke(false);
        }

        private void Awake()
        {
            if (_match == null)
            {
                Debug.LogError($"{nameof(MatchPause)} on '{name}' has no {nameof(MatchController)} assigned.", this);
            }

            // Defined in code: it is the only action of the match screen and does not belong to the car.
            _pauseAction = new InputAction("Pause", InputActionType.Button);
            _pauseAction.AddBinding("<Keyboard>/escape");
        }

        private void OnEnable()
        {
            _pauseAction.performed += OnPausePressed;
            _pauseAction.Enable();
        }

        private void OnDisable()
        {
            _pauseAction.performed -= OnPausePressed;
            _pauseAction.Disable();
        }

        // Leaving the scene (menu, restart) while paused must not leave the whole game frozen.
        private void OnDestroy()
        {
            if (IsPaused)
            {
                Time.timeScale = 1f;
            }

            _pauseAction?.Dispose();
        }

        private void OnPausePressed(InputAction.CallbackContext context) => Toggle();
    }
}
