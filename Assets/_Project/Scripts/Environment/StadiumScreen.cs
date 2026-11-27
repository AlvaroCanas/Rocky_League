using RockyLeague.Arena;
using RockyLeague.Match;
using TMPro;
using UnityEngine;

namespace RockyLeague.Environment
{
    /// <summary>
    /// Giant screen above the stands. It listens to the score (ScoreChanged) and to the
    /// referee's valid goals (GoalScored), like the on-screen score: another subscriber that
    /// the publishers do not know about. After a goal it shows "GOAL!" in the scorer's colour for a moment.
    /// </summary>
    public sealed class StadiumScreen : MonoBehaviour
    {
        private const float GoalMessageSeconds = 3f;

        private ScoreKeeper _scoreKeeper;
        private IGoalSource _goals;
        private TMP_Text _text;
        private Color _homeColour;
        private Color _awayColour;
        private float _goalMessageLeft;

        public void Initialize(ScoreKeeper scoreKeeper, IGoalSource goals, TMP_Text text, Color homeColour, Color awayColour)
        {
            _scoreKeeper = scoreKeeper;
            _goals = goals;
            _text = text;
            _homeColour = homeColour;
            _awayColour = awayColour;
        }

        // Start: the score keeper and the goals are ready after every Awake has run.
        private void Start()
        {
            if (_scoreKeeper == null || _goals == null || _text == null)
            {
                enabled = false;
                return;
            }

            _scoreKeeper.ScoreChanged += OnScoreChanged;
            _goals.GoalScored += OnGoalScored;
            ShowScore();
        }

        private void OnDestroy()
        {
            if (_scoreKeeper != null)
            {
                _scoreKeeper.ScoreChanged -= OnScoreChanged;
            }
            if (_goals != null)
            {
                _goals.GoalScored -= OnGoalScored;
            }
        }

        private void Update()
        {
            if (_goalMessageLeft <= 0f)
            {
                return;
            }

            _goalMessageLeft -= Time.deltaTime;
            if (_goalMessageLeft <= 0f)
            {
                ShowScore();
            }
        }

        private void OnScoreChanged(int homeGoals, int awayGoals)
        {
            if (_goalMessageLeft <= 0f)
            {
                ShowScore();
            }
        }

        private void OnGoalScored(object sender, GoalScoredEventArgs e)
        {
            Color colour = e.ScoringTeam == Team.Home ? _homeColour : _awayColour;
            _text.text = $"{WorldText.ColourTag(colour)}GOAL!</color>";
            _goalMessageLeft = GoalMessageSeconds;
        }

        private void ShowScore()
        {
            int home = _scoreKeeper.GetGoals(Team.Home);
            int away = _scoreKeeper.GetGoals(Team.Away);
            _text.text = $"{WorldText.ColourTag(_homeColour)}{home}</color>  -  {WorldText.ColourTag(_awayColour)}{away}</color>";
        }
    }
}
