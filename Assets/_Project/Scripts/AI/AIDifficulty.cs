using UnityEngine;

namespace RockyLeague.AI
{
    /// <summary>
    /// How good a bot is. The decision logic is the same for every level: only these
    /// numbers change (shared as assets, like CarSettings), so a new difficulty is a new
    /// asset, not new code.
    /// </summary>
    [CreateAssetMenu(fileName = "AIDifficulty", menuName = "Rocky League/AI Difficulty")]
    public sealed class AIDifficulty : ScriptableObject
    {
        [SerializeField, Min(0.02f), Tooltip("Seconds between decisions. The bot reacts to what it saw this long ago.")]
        private float _reactionTime = 0.25f;
        [SerializeField, Min(0f), Tooltip("Random error, in meters, added to where the bot thinks the ball will be.")]
        private float _aimError = 0.7f;
        [SerializeField, Min(0f), Tooltip("How far ahead in time (s) the bot predicts the ball.")]
        private float _maxPredictionTime = 1f;
        [SerializeField, Range(0f, 1f), Tooltip("Top throttle the bot uses.")]
        private float _maxThrottle = 0.9f;
        [SerializeField, Range(0f, 1f), Tooltip("Minimum boost left (0..1) for the bot to use it.")]
        private float _minBoostToUse = 0.25f;
        [SerializeField, Tooltip("Whether the bot jumps to reach balls in the air.")]
        private bool _canJump = true;

        public float ReactionTime => _reactionTime;
        public float AimError => _aimError;
        public float MaxPredictionTime => _maxPredictionTime;
        public float MaxThrottle => _maxThrottle;
        public float MinBoostToUse => _minBoostToUse;
        public bool CanJump => _canJump;
    }
}
