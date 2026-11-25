using RockyLeague.Core.Game;
using UnityEngine;
using UnityEngine.UI;

namespace RockyLeague.UI
{
    /// <summary>
    /// Main menu screen. It only translates button clicks into requests to the
    /// <see cref="GameManager"/>; it holds no game logic.
    ///
    /// Buttons are wired by code (not in the Inspector) because the GameManager
    /// lives in another scene and cannot be referenced from here.
    /// Subscribed in OnEnable and unsubscribed in OnDisable to leave no dangling listeners.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _trainingButton;

        private void OnEnable()
        {
            _playButton.onClick.AddListener(OnPlayClicked);
            _trainingButton.onClick.AddListener(OnTrainingClicked);
        }

        private void OnDisable()
        {
            _playButton.onClick.RemoveListener(OnPlayClicked);
            _trainingButton.onClick.RemoveListener(OnTrainingClicked);
        }

        private void OnPlayClicked() => GameManager.Instance.GoToPlayMode();

        private void OnTrainingClicked() => GameManager.Instance.StartTraining();
    }
}
