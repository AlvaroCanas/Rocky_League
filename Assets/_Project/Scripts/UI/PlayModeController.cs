using RockyLeague.Core.Game;
using UnityEngine;
using UnityEngine.UI;

namespace RockyLeague.UI
{
    /// <summary>
    /// Mode selection screen (the PlayMode scene). It only turns a click on "1v1" or
    /// "2v2" into a <see cref="MatchConfig"/> and hands it to the <see cref="GameManager"/>;
    /// it holds no game logic and does not create the match itself.
    ///
    /// Buttons are wired by code (the GameManager lives in another scene and cannot be
    /// referenced in the Inspector) and unsubscribed in OnDisable to leave no dangling listeners.
    /// </summary>
    public sealed class PlayModeController : MonoBehaviour
    {
        [SerializeField] private Button _oneVsOneButton;
        [SerializeField] private Button _twoVsTwoButton;

        private void OnEnable()
        {
            _oneVsOneButton.onClick.AddListener(OnOneVsOneClicked);
            _twoVsTwoButton.onClick.AddListener(OnTwoVsTwoClicked);
        }

        private void OnDisable()
        {
            _oneVsOneButton.onClick.RemoveListener(OnOneVsOneClicked);
            _twoVsTwoButton.onClick.RemoveListener(OnTwoVsTwoClicked);
        }

        private void OnOneVsOneClicked() => StartMatch(MatchConfig.OneVsOne());

        private void OnTwoVsTwoClicked() => StartMatch(MatchConfig.TwoVsTwo());

        private void StartMatch(MatchConfig config)
        {
            if (GameManager.Instance == null)
            {
                Debug.LogWarning("No GameManager found: start the game from the main menu scene.", this);
                return;
            }

            GameManager.Instance.StartMatch(config);
        }
    }
}
