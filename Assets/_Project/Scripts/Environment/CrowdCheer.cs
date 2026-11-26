using System.Collections.Generic;
using RockyLeague.Arena;
using UnityEngine;

namespace RockyLeague.Environment
{
    /// <summary>
    /// Makes the crowd jump when a goal is scored. It is one more subscriber of the
    /// goals' GoalScored event (Observer): the goals do not know the crowd exists.
    /// Fans of the scoring team jump high; the others barely move.
    ///
    /// The fans are not animated one by one with their own scripts: this single component
    /// moves all of them in one loop, and only while a celebration is running.
    /// </summary>
    public sealed class CrowdCheer : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _cheerSeconds = 4f;
        [SerializeField, Min(0f)] private float _jumpHeight = 0.7f;
        [SerializeField, Min(0f), Tooltip("Jumps per second, roughly.")]
        private float _jumpSpeed = 9f;
        [SerializeField, Range(0f, 1f), Tooltip("How much the other team's fans move.")]
        private float _rivalShare = 0.15f;

        private readonly List<Fan> _fans = new List<Fan>();
        private ArenaBuilder _arena;
        private Goal _blueGoal;
        private Goal _orangeGoal;
        private Team _cheeringTeam;
        private float _cheerTimeLeft;

        public void Initialize(ArenaBuilder arena)
        {
            _arena = arena;
        }

        public void AddFan(Transform fan, Team team, float phase)
        {
            _fans.Add(new Fan(fan, team, phase));
        }

        // Start: the arena creates its goals in its own Awake.
        private void Start()
        {
            if (_arena == null)
            {
                enabled = false;
                return;
            }

            _blueGoal = _arena.GetGoal(Team.Blue);
            _orangeGoal = _arena.GetGoal(Team.Orange);
            _blueGoal.GoalScored += OnGoalScored;
            _orangeGoal.GoalScored += OnGoalScored;
        }

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
            _cheeringTeam = e.ScoringTeam;
            _cheerTimeLeft = _cheerSeconds;
        }

        private void Update()
        {
            if (_cheerTimeLeft <= 0f)
            {
                return;
            }

            _cheerTimeLeft = Mathf.Max(0f, _cheerTimeLeft - Time.deltaTime);
            // Fade out during the last second so nobody freezes in the air.
            float fade = Mathf.Clamp01(_cheerTimeLeft);

            foreach (Fan fan in _fans)
            {
                float share = fan.Team == _cheeringTeam ? 1f : _rivalShare;
                float jump = Mathf.Abs(Mathf.Sin(Time.time * _jumpSpeed + fan.Phase)) * _jumpHeight * share * fade;
                Vector3 position = fan.Body.localPosition;
                position.y = fan.BaseHeight + jump;
                fan.Body.localPosition = position;
            }
        }

        private readonly struct Fan
        {
            public Fan(Transform body, Team team, float phase)
            {
                Body = body;
                Team = team;
                Phase = phase;
                BaseHeight = body.localPosition.y;
            }

            public Transform Body { get; }
            public Team Team { get; }
            public float Phase { get; }
            public float BaseHeight { get; }
        }
    }
}
