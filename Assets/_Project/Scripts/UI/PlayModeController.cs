using System;
using RockyLeague.Core.Game;
using UnityEngine;
using UnityEngine.UI;

namespace RockyLeague.UI
{
    /// <summary>
    /// Mode selection screen (the PlayMode scene). The player picks the mode and the
    /// difficulty with two <see cref="SegmentedSelector"/>s and presses "Play"; only then is a
    /// <see cref="MatchConfig"/> built and handed to the <see cref="GameManager"/>.
    ///
    /// It holds no game logic and does not create the match itself: building the cars and
    /// the bots from the config is the job of the match factory in the Match scene (SRP).
    ///
    /// The Play button is wired by code (the GameManager lives in another scene and cannot be
    /// referenced in the Inspector) and unsubscribed in OnDisable to leave no dangling listeners.
    /// </summary>
    public sealed class PlayModeController : MonoBehaviour
    {
        // What each position of the selectors means. The buttons in the scene must be in this order.
        // A mode is a function "difficulty -> config", so adding a mode (3v3...) is one more entry
        // here and one more button in the scene, with no new branches in the code (OCP).
        private static readonly Func<DifficultyLevel, MatchConfig>[] Modes =
        {
            MatchConfig.OneVsOne,
            MatchConfig.TwoVsTwo
        };

        private static readonly DifficultyLevel[] Difficulties =
        {
            DifficultyLevel.Easy,
            DifficultyLevel.Normal,
            DifficultyLevel.Hard
        };

        [SerializeField, Tooltip("Buttons in order: 1 vs 1, 2 vs 2.")]
        private SegmentedSelector _modeSelector;
        [SerializeField, Tooltip("Buttons in order: Easy, Normal, Hard.")]
        private SegmentedSelector _difficultySelector;
        [SerializeField] private Button _playButton;

        private void Awake()
        {
            if (!IsSetUpCorrectly())
            {
                enabled = false;
            }
        }

        // Null checks: Unity still calls OnDisable when the scene unloads, even if this component
        // switched itself off in Awake because the scene was not set up.
        private void OnEnable()
        {
            if (_playButton != null)
            {
                _playButton.onClick.AddListener(OnPlayClicked);
            }
        }

        private void OnDisable()
        {
            if (_playButton != null)
            {
                _playButton.onClick.RemoveListener(OnPlayClicked);
            }
        }

        private void OnPlayClicked()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogWarning("No GameManager found: start the game from the main menu scene.", this);
                return;
            }

            GameManager.Instance.StartMatch(BuildConfig());
        }

        private MatchConfig BuildConfig()
        {
            Func<DifficultyLevel, MatchConfig> mode = Modes[_modeSelector.SelectedIndex];
            DifficultyLevel difficulty = Difficulties[_difficultySelector.SelectedIndex];
            return mode(difficulty);
        }

        // The selectors only know positions, so their button count must match the tables above.
        private bool IsSetUpCorrectly()
        {
            if (_modeSelector == null || _difficultySelector == null || _playButton == null)
            {
                Debug.LogError($"{nameof(PlayModeController)} on '{name}' needs both selectors and the Play button assigned.", this);
                return false;
            }

            if (_modeSelector.OptionCount != Modes.Length || _difficultySelector.OptionCount != Difficulties.Length)
            {
                Debug.LogError($"{nameof(PlayModeController)} on '{name}': the mode selector needs {Modes.Length} buttons and the difficulty selector {Difficulties.Length}.", this);
                return false;
            }

            return true;
        }
    }
}
