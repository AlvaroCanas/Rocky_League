using System;
using RockyLeague.Arena;
using UnityEngine;

namespace RockyLeague.Match
{
    /// <summary>
    /// Keeps the score of the match. It is a subscriber (Observer pattern):
    /// it listens to both <see cref="Goal"/>s and, after counting a goal,
    /// publishes <see cref="ScoreChanged"/> for whoever needs the score (UI, match flow).
    ///
    /// It only counts (SRP): deciding what happens after a goal (celebration,
    /// kickoff, end of match) belongs to the match flow.
    /// </summary>
    public sealed class ScoreKeeper : MonoBehaviour
    {
        [SerializeField] private ArenaBuilder _arena;

        private Goal _blueGoal;
        private Goal _orangeGoal;
        private int _blueGoals;
        private int _orangeGoals;

        /// <summary>Raised after every change with (blue goals, orange goals).</summary>
        public event Action<int, int> ScoreChanged;

        public int GetGoals(Team team)
        {
            return team == Team.Blue ? _blueGoals : _orangeGoals;
        }

        public bool IsTied => _blueGoals == _orangeGoals;

        /// <summary>Team with more goals. Only meaningful when not <see cref="IsTied"/>.</summary>
        public Team Leader => _blueGoals > _orangeGoals ? Team.Blue : Team.Orange;

        /// <summary>Back to 0 - 0, for a new match.</summary>
        public void ResetScore()
        {
            _blueGoals = 0;
            _orangeGoals = 0;
            ScoreChanged?.Invoke(_blueGoals, _orangeGoals);
        }

        // Start instead of Awake/OnEnable: the arena creates its goals in its own Awake,
        // and Unity runs every Awake before any Start.
        private void Start()
        {
            if (_arena == null)
            {
                Debug.LogError($"{nameof(ScoreKeeper)} on '{name}' has no {nameof(ArenaBuilder)} assigned.", this);
                enabled = false;
                return;
            }

            _blueGoal = _arena.GetGoal(Team.Blue);
            _orangeGoal = _arena.GetGoal(Team.Orange);
            _blueGoal.GoalScored += OnGoalScored;
            _orangeGoal.GoalScored += OnGoalScored;

            ScoreChanged?.Invoke(_blueGoals, _orangeGoals);
        }

        // Always unsubscribe: a forgotten subscription keeps references alive between matches.
        private void OnDestroy()
        {
            if (_blueGoal != null)
            {
                _blueGoal.GoalScored -= OnGoalScored;
            }
            if (_orangeGoal != null)
            {
                _orangeGoal.GoalScored -= OnGoalScored;
            }
        }

        private void OnGoalScored(object sender, GoalScoredEventArgs e)
        {
            if (e.ScoringTeam == Team.Blue)
            {
                _blueGoals++;
            }
            else
            {
                _orangeGoals++;
            }

            ScoreChanged?.Invoke(_blueGoals, _orangeGoals);
        }
    }
}
