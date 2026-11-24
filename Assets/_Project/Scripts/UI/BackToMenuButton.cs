using RockyLeague.Core.Game;
using UnityEngine;
using UnityEngine.UI;

namespace RockyLeague.UI
{
    /// <summary>
    /// Button that returns to the main menu. Reusable in any scene that
    /// needs a way back (match, training...).
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class BackToMenuButton : MonoBehaviour
    {
        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            _button.onClick.AddListener(OnClicked);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(OnClicked);
        }

        private void OnClicked()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogWarning("No GameManager found: start the game from the main menu scene.", this);
                return;
            }

            GameManager.Instance.GoToMainMenu();
        }
    }
}
