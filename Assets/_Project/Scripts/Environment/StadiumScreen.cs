using RockyLeague.Arena;
using RockyLeague.Match;
using TMPro;
using UnityEngine;

namespace RockyLeague.Environment
{
    /// <summary>
    /// Giant screen above the stands. It listens to the score (ScoreChanged) and to the
    /// goals (GoalScored), like the on-screen score: another subscriber that the publishers
    /// do not know about. After a goal it shows "GOAL!" in the scorer's colour for a moment.
    /// </summary>
    public sealed class StadiumScreen : MonoBehaviour
    {
        private const float GoalMessageSeconds = 3f;

        private ScoreKeeper _scoreKeeper;
        private ArenaBuilder _arena;
        private TMP_Text _text;
        private Color _blueColour;
        private Color _orangeColour;
        private Goal _blueGoal;
        private Goal _orangeGoal;
        private float _goalMessageLeft;

        public void Initialize(ScoreKeeper scoreKeeper, ArenaBuilder arena, TMP_Text text, Color blueColour, Color orangeColour)
        {
            _scoreKeeper = scoreKeeper;
            _arena = arena;
            _text = text;
            _blueColour = blueColour;
            _orangeColour = orangeColour;
        }

        // Start: the score keeper and the goals are ready after every Awake has run.
        private void Start()
        {
            if (_scoreKeeper == null || _arena == null || _text == null)
            {
                enabled = false;
                return;
            }

            _scoreKeeper.ScoreChanged += OnScoreChanged;
            _blueGoal = _arena.GetGoal(Team.Blue);
            _orangeGoal = _arena.GetGoal(Team.Orange);
            _blueGoal.GoalScored += OnGoalScored;
            _orangeGoal.GoalScored += OnGoalScored;
            ShowScore();
        }

        private void OnDestroy()
        {
            if (_scoreKeeper != null)
            {
                _scoreKeeper.ScoreChanged -= OnScoreChanged;
            }
            if (_blueGoal != null)
            {
                _blueGoal.GoalScored -= OnGoalScored;
            }
            if (_orangeGoal != null)
            {
                _orangeGoal.GoalScored -= OnGoalScored;
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

        private void OnScoreChanged(int blueGoals, int orangeGoals)
        {
            if (_goalMessageLeft <= 0f)
            {
                ShowScore();
            }
        }

        private void OnGoalScored(object sender, GoalScoredEventArgs e)
        {
            Color colour = e.ScoringTeam == Team.Blue ? _blueColour : _orangeColour;
            _text.text = $"{WorldText.ColourTag(colour)}GOAL!</color>";
            _goalMessageLeft = GoalMessageSeconds;
        }

        private void ShowScore()
        {
            int blue = _scoreKeeper.GetGoals(Team.Blue);
            int orange = _scoreKeeper.GetGoals(Team.Orange);
            _text.text = $"{WorldText.ColourTag(_blueColour)}{blue}</color>  -  {WorldText.ColourTag(_orangeColour)}{orange}</color>";
        }
    }
}
