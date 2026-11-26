using RockyLeague.Core.Building;
using UnityEngine;

namespace RockyLeague.Arena
{
    /// <summary>
    /// Builds the arena from an <see cref="ArenaSettings"/> asset when the scene starts:
    /// floor, glass side walls, glass end walls with an opening for each goal, the goal
    /// boxes, an invisible ceiling that keeps the ball inside, dark frames along the
    /// edges of the glass and the lines painted on the field.
    ///
    /// Building it by code (instead of placing every wall by hand) keeps all the
    /// geometry consistent with the settings: changing the field size moves walls,
    /// goals and kickoff spots together. In the editor the layout is shown with gizmos.
    ///
    /// It also creates one <see cref="Goal"/> trigger per goal and answers questions
    /// about the layout (goals, goal centres, kickoff spots) for the match flow and the AI.
    /// </summary>
    public sealed class ArenaBuilder : MonoBehaviour
    {
        [SerializeField] private ArenaSettings _settings;

        [Header("Looks (optional)")]
        [SerializeField] private Material _floorMaterial;
        [SerializeField] private Material _wallMaterial;
        [SerializeField] private Material _blueGoalMaterial;
        [SerializeField] private Material _orangeGoalMaterial;
        [SerializeField, Tooltip("Frames along the edges of the glass walls.")]
        private Material _trimMaterial;
        [SerializeField, Tooltip("Lines painted on the field.")]
        private Material _lineMaterial;
        [SerializeField, Tooltip("Show the ceiling (useful for debugging only).")]
        private bool _showCeiling;

        // Field markings, in meters.
        private const float LineWidth = 0.3f;
        private const float LineThickness = 0.02f;
        private const float CenterCircleRadius = 8f;
        private const int CenterCircleSegments = 40;
        private const float GoalAreaMargin = 5f;
        private const float GoalAreaDepth = 10f;
        // Glass panels: a frame post at least every this many meters along the side walls.
        private const float MaxPanelLength = 15f;

        private PhysicsMaterial _wallPhysics;
        private BlockBuilder _blocks;
        private Goal _blueGoal;
        private Goal _orangeGoal;

        public ArenaSettings Settings => _settings;

        /// <summary>The goal that <paramref name="team"/> defends. Available after Awake.</summary>
        public Goal GetGoal(Team team)
        {
            return team == Team.Blue ? _blueGoal : _orangeGoal;
        }

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
            _blocks = new BlockBuilder(transform);
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
            _blocks.Solid("Floor", new Vector3(0f, -t * 0.5f, 0f), new Vector3(w + 2f * t, t, floorLength), _floorMaterial, _wallPhysics);

            // Ceiling: invisible lid so the ball cannot leave the arena.
            GameObject ceiling = _blocks.Solid("Ceiling", new Vector3(0f, h + t * 0.5f, 0f), new Vector3(w + 2f * t, t, l + 2f * t), _wallMaterial, _wallPhysics);
            ceiling.GetComponent<MeshRenderer>().enabled = _showCeiling;

            // Side walls, along the whole length.
            float sideX = w * 0.5f + t * 0.5f;
            Vector3 sideSize = new Vector3(t, h, l + 2f * t);
            _blocks.Solid("Wall_Left", new Vector3(-sideX, h * 0.5f, 0f), sideSize, _wallMaterial, _wallPhysics);
            _blocks.Solid("Wall_Right", new Vector3(sideX, h * 0.5f, 0f), sideSize, _wallMaterial, _wallPhysics);

            BuildEnd(Team.Blue, _blueGoalMaterial);
            BuildEnd(Team.Orange, _orangeGoalMaterial);
            BuildFrames();
            BuildFieldLines();

            _blueGoal = CreateGoalTrigger(Team.Blue);
            _orangeGoal = CreateGoalTrigger(Team.Orange);
        }

        /// <summary>Invisible volume inside the goal box, from the goal line margin to the back of the net.</summary>
        private Goal CreateGoalTrigger(Team defendingTeam)
        {
            float l = _settings.Length;
            float boxDepth = _settings.WallThickness + _settings.GoalDepth;
            float margin = _settings.GoalLineMargin;
            float depth = boxDepth - margin;
            float z = GoalSide(defendingTeam) * (l * 0.5f + margin + depth * 0.5f);

            GameObject zone = new GameObject($"{defendingTeam}Goal");
            zone.transform.SetParent(transform, false);
            zone.transform.localPosition = new Vector3(0f, _settings.GoalHeight * 0.5f, z);

            BoxCollider box = zone.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(_settings.GoalWidth, _settings.GoalHeight, depth);

            Goal goal = zone.AddComponent<Goal>();
            goal.Initialize(defendingTeam);
            return goal;
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
            _blocks.Solid(prefix + "WallLeft", new Vector3(-pieceX, h * 0.5f, wallZ), pieceSize, _wallMaterial, _wallPhysics);
            _blocks.Solid(prefix + "WallRight", new Vector3(pieceX, h * 0.5f, wallZ), pieceSize, _wallMaterial, _wallPhysics);
            _blocks.Solid(prefix + "Lintel", new Vector3(0f, gh + (h - gh) * 0.5f, wallZ), new Vector3(gw, h - gh, t), _wallMaterial, _wallPhysics);

            // Goal box: from the goal line to the back of the net.
            float boxDepth = t + gd;
            float boxZ = side * (l * 0.5f + boxDepth * 0.5f);
            float goalSideX = gw * 0.5f + t * 0.5f;
            Vector3 goalSideSize = new Vector3(t, gh, boxDepth);
            _blocks.Solid(prefix + "GoalLeft", new Vector3(-goalSideX, gh * 0.5f, boxZ), goalSideSize, goalMaterial, _wallPhysics);
            _blocks.Solid(prefix + "GoalRight", new Vector3(goalSideX, gh * 0.5f, boxZ), goalSideSize, goalMaterial, _wallPhysics);
            _blocks.Solid(prefix + "GoalRoof", new Vector3(0f, gh + t * 0.5f, boxZ), new Vector3(gw + 2f * t, t, boxDepth), goalMaterial, _wallPhysics);

            float backZ = side * (l * 0.5f + boxDepth + t * 0.5f);
            _blocks.Solid(prefix + "GoalBack", new Vector3(0f, (gh + t) * 0.5f, backZ), new Vector3(gw + 2f * t, gh + t, t), goalMaterial, _wallPhysics);
        }

        /// <summary>
        /// Dark frames on the edges of the glass: a rail on top of every wall, posts along the
        /// side walls that split them into panels and posts beside each goal. Without them a
        /// transparent wall is hard to see.
        /// </summary>
        private void BuildFrames()
        {
            float w = _settings.Width;
            float l = _settings.Length;
            float h = _settings.WallHeight;
            float t = _settings.WallThickness;
            float frame = t + 0.3f;
            float railY = h + 0.2f;
            float sideX = w * 0.5f + t * 0.5f;
            float endZ = l * 0.5f + t * 0.5f;

            _blocks.Decoration("Rail_Left", new Vector3(-sideX, railY, 0f), new Vector3(frame, 0.4f, l + 2f * t + 0.3f), _trimMaterial);
            _blocks.Decoration("Rail_Right", new Vector3(sideX, railY, 0f), new Vector3(frame, 0.4f, l + 2f * t + 0.3f), _trimMaterial);
            _blocks.Decoration("Rail_BlueEnd", new Vector3(0f, railY, -endZ), new Vector3(w + 2f * t + 0.3f, 0.4f, frame), _trimMaterial);
            _blocks.Decoration("Rail_OrangeEnd", new Vector3(0f, railY, endZ), new Vector3(w + 2f * t + 0.3f, 0.4f, frame), _trimMaterial);

            int panels = Mathf.CeilToInt(l / MaxPanelLength);
            for (int i = 0; i <= panels; i++)
            {
                float z = -endZ + i * (2f * endZ / panels);
                _blocks.Decoration($"Post_Left_{i}", new Vector3(-sideX, h * 0.5f, z), new Vector3(frame, h, 0.5f), _trimMaterial);
                _blocks.Decoration($"Post_Right_{i}", new Vector3(sideX, h * 0.5f, z), new Vector3(frame, h, 0.5f), _trimMaterial);
            }

            float goalPostX = _settings.GoalWidth * 0.5f;
            foreach (float z in new[] { -endZ, endZ })
            {
                _blocks.Decoration("GoalPost", new Vector3(-goalPostX, h * 0.5f, z), new Vector3(0.5f, h, frame), _trimMaterial);
                _blocks.Decoration("GoalPost", new Vector3(goalPostX, h * 0.5f, z), new Vector3(0.5f, h, frame), _trimMaterial);
            }
        }

        /// <summary>Halfway line, centre circle and the area in front of each goal.</summary>
        private void BuildFieldLines()
        {
            float y = LineThickness * 0.5f + 0.005f;
            _blocks.Decoration("Line_Halfway", new Vector3(0f, y, 0f), new Vector3(_settings.Width, LineThickness, LineWidth), _lineMaterial);

            float segmentLength = 2f * Mathf.PI * CenterCircleRadius / CenterCircleSegments + 0.05f;
            for (int i = 0; i < CenterCircleSegments; i++)
            {
                float angle = i * 360f / CenterCircleSegments;
                float radians = angle * Mathf.Deg2Rad;
                Vector3 point = new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians)) * CenterCircleRadius;
                // Each piece lies along the tangent of the circle at its point.
                Quaternion along = Quaternion.Euler(0f, -(angle + 90f), 0f);
                _blocks.Decoration(PrimitiveType.Cube, "Line_Circle", point + Vector3.up * y, new Vector3(segmentLength, LineThickness, LineWidth), along, _lineMaterial);
            }

            BuildGoalArea(Team.Blue, y);
            BuildGoalArea(Team.Orange, y);
        }

        private void BuildGoalArea(Team team, float y)
        {
            float side = GoalSide(team);
            float halfWidth = _settings.GoalWidth * 0.5f + GoalAreaMargin;
            float goalLineZ = side * _settings.Length * 0.5f;
            float frontZ = goalLineZ - side * GoalAreaDepth;
            float middleZ = (goalLineZ + frontZ) * 0.5f;

            _blocks.Decoration($"Line_{team}Area_Front", new Vector3(0f, y, frontZ), new Vector3(2f * halfWidth, LineThickness, LineWidth), _lineMaterial);
            _blocks.Decoration($"Line_{team}Area_Left", new Vector3(-halfWidth, y, middleZ), new Vector3(LineWidth, LineThickness, GoalAreaDepth), _lineMaterial);
            _blocks.Decoration($"Line_{team}Area_Right", new Vector3(halfWidth, y, middleZ), new Vector3(LineWidth, LineThickness, GoalAreaDepth), _lineMaterial);
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
