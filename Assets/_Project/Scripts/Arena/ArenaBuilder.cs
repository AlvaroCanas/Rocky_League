using UnityEngine;

namespace RockyLeague.Arena
{
    /// <summary>
    /// Builds the arena from an <see cref="ArenaSettings"/> asset when the scene starts:
    /// floor, side walls, end walls with an opening for each goal, the goal boxes
    /// and an invisible ceiling that keeps the ball inside.
    ///
    /// Building it by code (instead of placing every wall by hand) keeps all the
    /// geometry consistent with the settings: changing the field size moves walls,
    /// goals and kickoff spots together. In the editor the layout is shown with gizmos.
    ///
    /// It also answers questions about the layout (goal centres, kickoff spots),
    /// which the match flow and the AI will need.
    /// </summary>
    public sealed class ArenaBuilder : MonoBehaviour
    {
        [SerializeField] private ArenaSettings _settings;

        [Header("Looks (optional)")]
        [SerializeField] private Material _floorMaterial;
        [SerializeField] private Material _wallMaterial;
        [SerializeField] private Material _blueGoalMaterial;
        [SerializeField] private Material _orangeGoalMaterial;
        [SerializeField, Tooltip("Show the ceiling (useful for debugging only).")]
        private bool _showCeiling;

        private PhysicsMaterial _wallPhysics;

        public ArenaSettings Settings => _settings;

        /// <summary>Centre of the opening of the goal that <paramref name="team"/> defends.</summary>
        public Vector3 GetGoalCenter(Team team)
        {
            float z = GoalSide(team) * _settings.Length * 0.5f;
            return transform.TransformPoint(new Vector3(0f, _settings.GoalHeight * 0.5f, z));
        }

        /// <summary>Where a car of <paramref name="team"/> starts each kickoff, half way to its own goal.</summary>
        public Vector3 GetKickoffPosition(Team team)
        {
            float z = GoalSide(team) * _settings.Length * 0.25f;
            return transform.TransformPoint(new Vector3(0f, 0.5f, z));
        }

        /// <summary>Rotation that makes a car of <paramref name="team"/> face the opponent's goal.</summary>
        public Quaternion GetKickoffRotation(Team team)
        {
            Vector3 towardsRival = new Vector3(0f, 0f, -GoalSide(team));
            return transform.rotation * Quaternion.LookRotation(towardsRival, Vector3.up);
        }

        private void Awake()
        {
            if (_settings == null)
            {
                Debug.LogError($"{nameof(ArenaBuilder)} on '{name}' has no {nameof(ArenaSettings)} assigned.", this);
                enabled = false;
                return;
            }

            _wallPhysics = CreateWallPhysics();
            Build();
        }

        // -1 for the Blue goal (negative Z), +1 for the Orange goal (positive Z).
        private static float GoalSide(Team team)
        {
            return team == Team.Blue ? -1f : 1f;
        }

        private PhysicsMaterial CreateWallPhysics()
        {
            PhysicsMaterial material = new PhysicsMaterial("ArenaWall");
            material.bounciness = _settings.WallBounciness;
            material.bounceCombine = PhysicsMaterialCombine.Average;
            material.dynamicFriction = 0.2f;
            material.staticFriction = 0.2f;
            return material;
        }

        private void Build()
        {
            float w = _settings.Width;
            float l = _settings.Length;
            float h = _settings.WallHeight;
            float t = _settings.WallThickness;

            // Floor: top face at y = 0, long enough to cover the inside of both goals.
            float floorLength = l + 2f * (t + _settings.GoalDepth + t);
            CreateBlock("Floor", new Vector3(0f, -t * 0.5f, 0f), new Vector3(w + 2f * t, t, floorLength), _floorMaterial);

            // Ceiling: invisible lid so the ball cannot leave the arena.
            GameObject ceiling = CreateBlock("Ceiling", new Vector3(0f, h + t * 0.5f, 0f), new Vector3(w + 2f * t, t, l + 2f * t), _wallMaterial);
            ceiling.GetComponent<MeshRenderer>().enabled = _showCeiling;

            // Side walls, along the whole length.
            float sideX = w * 0.5f + t * 0.5f;
            Vector3 sideSize = new Vector3(t, h, l + 2f * t);
            CreateBlock("Wall_Left", new Vector3(-sideX, h * 0.5f, 0f), sideSize, _wallMaterial);
            CreateBlock("Wall_Right", new Vector3(sideX, h * 0.5f, 0f), sideSize, _wallMaterial);

            BuildEnd(Team.Blue, _blueGoalMaterial);
            BuildEnd(Team.Orange, _orangeGoalMaterial);
        }

        /// <summary>End wall with the goal opening, plus the goal box behind it.</summary>
        private void BuildEnd(Team team, Material goalMaterial)
        {
            float w = _settings.Width;
            float l = _settings.Length;
            float h = _settings.WallHeight;
            float t = _settings.WallThickness;
            float gw = _settings.GoalWidth;
            float gh = _settings.GoalHeight;
            float gd = _settings.GoalDepth;
            float side = GoalSide(team);
            string prefix = $"{team}End_";

            // End wall: two pieces at the sides of the opening and a lintel above it.
            float wallZ = side * (l * 0.5f + t * 0.5f);
            float pieceWidth = (w - gw) * 0.5f;
            float pieceX = gw * 0.5f + pieceWidth * 0.5f;
            Vector3 pieceSize = new Vector3(pieceWidth, h, t);
            CreateBlock(prefix + "WallLeft", new Vector3(-pieceX, h * 0.5f, wallZ), pieceSize, _wallMaterial);
            CreateBlock(prefix + "WallRight", new Vector3(pieceX, h * 0.5f, wallZ), pieceSize, _wallMaterial);
            CreateBlock(prefix + "Lintel", new Vector3(0f, gh + (h - gh) * 0.5f, wallZ), new Vector3(gw, h - gh, t), _wallMaterial);

            // Goal box: from the goal line to the back of the net.
            float boxDepth = t + gd;
            float boxZ = side * (l * 0.5f + boxDepth * 0.5f);
            float goalSideX = gw * 0.5f + t * 0.5f;
            Vector3 goalSideSize = new Vector3(t, gh, boxDepth);
            CreateBlock(prefix + "GoalLeft", new Vector3(-goalSideX, gh * 0.5f, boxZ), goalSideSize, goalMaterial);
            CreateBlock(prefix + "GoalRight", new Vector3(goalSideX, gh * 0.5f, boxZ), goalSideSize, goalMaterial);
            CreateBlock(prefix + "GoalRoof", new Vector3(0f, gh + t * 0.5f, boxZ), new Vector3(gw + 2f * t, t, boxDepth), goalMaterial);

            float backZ = side * (l * 0.5f + boxDepth + t * 0.5f);
            CreateBlock(prefix + "GoalBack", new Vector3(0f, (gh + t) * 0.5f, backZ), new Vector3(gw + 2f * t, gh + t, t), goalMaterial);
        }

        private GameObject CreateBlock(string blockName, Vector3 localCenter, Vector3 size, Material material)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = blockName;
            block.transform.SetParent(transform, false);
            block.transform.localPosition = localCenter;
            block.transform.localScale = size;

            if (material != null)
            {
                block.GetComponent<MeshRenderer>().sharedMaterial = material;
            }

            block.GetComponent<BoxCollider>().sharedMaterial = _wallPhysics;
            return block;
        }

        // Editor preview: the field outline and both goals, visible without pressing Play.
        private void OnDrawGizmos()
        {
            if (_settings == null)
            {
                return;
            }

            Gizmos.matrix = transform.localToWorldMatrix;
            float l = _settings.Length;
            float h = _settings.WallHeight;
            float gh = _settings.GoalHeight;
            float boxDepth = _settings.WallThickness + _settings.GoalDepth;

            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(new Vector3(0f, h * 0.5f, 0f), new Vector3(_settings.Width, h, l));

            Vector3 goalSize = new Vector3(_settings.GoalWidth, gh, boxDepth);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(new Vector3(0f, gh * 0.5f, -(l * 0.5f + boxDepth * 0.5f)), goalSize);
            Gizmos.color = new Color(1f, 0.5f, 0f);
            Gizmos.DrawWireCube(new Vector3(0f, gh * 0.5f, l * 0.5f + boxDepth * 0.5f), goalSize);
        }
    }
}
