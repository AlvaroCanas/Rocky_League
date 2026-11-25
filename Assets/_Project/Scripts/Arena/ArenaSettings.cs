using UnityEngine;

namespace RockyLeague.Arena
{
    /// <summary>
    /// Dimensions of the arena, in meters, kept apart from the code that builds it.
    /// Changing the field size or the goals is a matter of editing this asset.
    /// The field is centred on the origin: X is the width, Z the length (goal to goal).
    /// </summary>
    [CreateAssetMenu(fileName = "ArenaSettings", menuName = "Rocky League/Arena Settings")]
    public sealed class ArenaSettings : ScriptableObject
    {
        [Header("Field")]
        [SerializeField, Min(5f), Tooltip("Side to side (X).")]
        private float _width = 40f;
        [SerializeField, Min(5f), Tooltip("Goal line to goal line (Z).")]
        private float _length = 60f;
        [SerializeField, Min(1f)] private float _wallHeight = 12f;
        [SerializeField, Min(0.1f)] private float _wallThickness = 2f;

        [Header("Goals")]
        [SerializeField, Min(1f)] private float _goalWidth = 10f;
        [SerializeField, Min(1f)] private float _goalHeight = 4f;
        [SerializeField, Min(0.5f), Tooltip("How deep the net is behind the goal line.")]
        private float _goalDepth = 5f;
        [SerializeField, Min(0f), Tooltip("How far behind the goal line a goal starts counting. Use the ball's diameter so the whole ball must cross.")]
        private float _goalLineMargin = 2f;

        [Header("Surfaces")]
        [SerializeField, Range(0f, 1f), Tooltip("How much the ball bounces off walls.")]
        private float _wallBounciness = 0.5f;

        public float Width => _width;
        public float Length => _length;
        public float WallHeight => _wallHeight;
        public float WallThickness => _wallThickness;

        public float GoalWidth => _goalWidth;
        public float GoalHeight => _goalHeight;
        public float GoalDepth => _goalDepth;
        public float GoalLineMargin => _goalLineMargin;

        public float WallBounciness => _wallBounciness;

        // Keeps the goals smaller than the walls they are cut into.
        private void OnValidate()
        {
            _goalWidth = Mathf.Min(_goalWidth, _width - 2f * _wallThickness);
            _goalHeight = Mathf.Min(_goalHeight, _wallHeight - 1f);
            _goalLineMargin = Mathf.Min(_goalLineMargin, _wallThickness + _goalDepth - 0.5f);
        }
    }
}
