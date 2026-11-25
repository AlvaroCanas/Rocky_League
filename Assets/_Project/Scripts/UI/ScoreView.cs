using RockyLeague.Arena;
using RockyLeague.Match;
using TMPro;
using UnityEngine;

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
        [SerializeField] private Color _blueColor = new Color(0.25f, 0.5f, 1f);
        [SerializeField] private Color _orangeColor = new Color(1f, 0.55f, 0.1f);

        private void OnEnable()
        {
            if (_scoreKeeper == null || _text == null)
            {
                Debug.LogError($"{nameof(ScoreView)} on '{name}' is missing its references.", this);
                enabled = false;
                return;
            }

            _scoreKeeper.ScoreChanged += OnScoreChanged;
            OnScoreChanged(_scoreKeeper.GetGoals(Team.Blue), _scoreKeeper.GetGoals(Team.Orange));
        }

        private void OnDisable()
        {
            if (_scoreKeeper != null)
            {
                _scoreKeeper.ScoreChanged -= OnScoreChanged;
            }
        }

        private void OnScoreChanged(int blueGoals, int orangeGoals)
        {
            string blue = ColorUtility.ToHtmlStringRGB(_blueColor);
            string orange = ColorUtility.ToHtmlStringRGB(_orangeColor);
            _text.text = $"<color=#{blue}>{blueGoals}</color>  -  <color=#{orange}>{orangeGoals}</color>";
        }
    }
}
