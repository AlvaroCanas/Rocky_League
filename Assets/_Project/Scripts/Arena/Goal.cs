using System;
using UnityEngine;

namespace RockyLeague.Arena
{
    /// <summary>
    /// Trigger volume inside a goal box. When the ball enters it, it raises
    /// <see cref="GoalScored"/> (Observer pattern): the goal does not know who is
    /// listening (score, match flow, UI, sound...), it only announces what happened.
    /// This keeps goal detection apart from what a goal means for the match (SRP).
    ///
    /// The volume starts a ball diameter behind the goal line, so a goal only
    /// counts when the whole ball has crossed the line (as in real football).
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class Goal : MonoBehaviour
    {
        [SerializeField] private Team _defendingTeam;

        /// <summary>Raised once each time the ball enters the goal. The sender is this goal.</summary>
        public event EventHandler<GoalScoredEventArgs> GoalScored;

        /// <summary>Team that defends this goal (and concedes when the ball goes in).</summary>
        public Team DefendingTeam => _defendingTeam;

        /// <summary>Used by the code that creates goals at runtime (<see cref="ArenaBuilder"/>).</summary>
        public void Initialize(Team defendingTeam)
        {
            _defendingTeam = defendingTeam;
        }

        private void Awake()
        {
            GetComponent<BoxCollider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            // The ball's collider may be on a child, so look at its Rigidbody.
            Rigidbody body = other.attachedRigidbody;
            if (body == null || !body.TryGetComponent(out Ball _))
            {
                return;
            }

            GoalScored?.Invoke(this, new GoalScoredEventArgs(_defendingTeam.Opponent()));
        }
    }
}
