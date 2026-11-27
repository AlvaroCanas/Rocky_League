using RockyLeague.Arena;
using RockyLeague.Match;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace RockyLeague.UI
{
    /// <summary>
    /// Shows the score on screen. Last link of the Observer chain
    /// (Goal -> ScoreKeeper -> ScoreView): it only listens and draws, it never
    /// changes the score, so the UI can be replaced without touching game logic.
    /// </summary>
    public sealed class ScoreView : MonoBehaviour
    {
        [SerializeField] private ScoreKeeper _scoreKeeper;
        [SerializeField] private TMP_Text _text;
        [SerializeField, FormerlySerializedAs("_blueColor")] private Color _homeColor = new Color(0.25f, 0.5f, 1f);
        [SerializeField, FormerlySerializedAs("_orangeColor")] private Color _awayColor = new Color(1f, 0.55f, 0.1f);

        private void OnEnable()
        {
            if (_scoreKeeper == null || _text == null)
            {
                Debug.LogError($"{nameof(ScoreView)} on '{name}' is missing its references.", this);
                enabled = false;
                return;
            }

            _scoreKeeper.ScoreChanged += OnScoreChanged;
            OnScoreChanged(_scoreKeeper.GetGoals(Team.Home), _scoreKeeper.GetGoals(Team.Away));
        }

        private void OnDisable()
        {
            if (_scoreKeeper != null)
            {
                _scoreKeeper.ScoreChanged -= OnScoreChanged;
            }
        }

        private void OnScoreChanged(int homeGoals, int awayGoals)
        {
            string home = ColorUtility.ToHtmlStringRGB(_homeColor);
            string away = ColorUtility.ToHtmlStringRGB(_awayColor);
            _text.text = $"<color=#{home}>{homeGoals}</color>  -  <color=#{away}>{awayGoals}</color>";
        }
    }
}
