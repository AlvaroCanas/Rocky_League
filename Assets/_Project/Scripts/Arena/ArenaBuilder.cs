using RockyLeague.Core.Building;
using UnityEngine;

namespace RockyLeague.Arena
{
    /// <summary>
    /// Builds the arena from an <see cref="ArenaSettings"/> asset when the scene starts:
    /// floor, glass walls with an opening for each goal, corners cut at 45 degrees,
    /// curved ramps where the floor meets the walls, tinted goal boxes with glowing frames,
    /// an invisible ceiling that keeps the ball inside and frames along the glass.
    /// The ramps and the field markings are built by their own classes.
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
        [SerializeField, Tooltip("Lighter grass for the mowing stripes.")]
        private Material _stripeMaterial;
        [SerializeField, Tooltip("Curved ramps at the foot of the walls.")]
        private Material _rampMaterial;
        [SerializeField, Tooltip("Glowing blue: goal frame and rails on the blue half.")]
        private Material _blueGlowMaterial;
        [SerializeField, Tooltip("Glowing orange: goal frame and rails on the orange half.")]
        private Material _orangeGlowMaterial;
        [SerializeField, Tooltip("Show the ceiling (useful for debugging only).")]
        private bool _showCeiling;

        private const int RampSlats = 8;
        private const float RampThickness = 0.5f;
        private const float GoalFrameRadius = 0.3f;
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
            BuildCorners();
            BuildRamps();
            BuildGoalFrame(Team.Blue);
            BuildGoalFrame(Team.Orange);
            BuildFrames();

            FieldMarkingsBuilder markings = new FieldMarkingsBuilder(_blocks, _settings, _lineMaterial, _stripeMaterial);
            markings.Build();

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

            // Each side rail is split in a blue half and an orange half.
            float halfRail = (l + 2f * t + 0.3f) * 0.5f;
            foreach (float x in new[] { -sideX, sideX })
            {
                _blocks.Decoration("Rail_Side", new Vector3(x, railY, -halfRail * 0.5f), new Vector3(frame, 0.4f, halfRail), RailMaterial(-1f));
                _blocks.Decoration("Rail_Side", new Vector3(x, railY, halfRail * 0.5f), new Vector3(frame, 0.4f, halfRail), RailMaterial(1f));
            }
            _blocks.Decoration("Rail_BlueEnd", new Vector3(0f, railY, -endZ), new Vector3(w + 2f * t + 0.3f, 0.4f, frame), RailMaterial(-1f));
            _blocks.Decoration("Rail_OrangeEnd", new Vector3(0f, railY, endZ), new Vector3(w + 2f * t + 0.3f, 0.4f, frame), RailMaterial(1f));

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

        /// <summary>
        /// Cuts each corner at 45 degrees with a diagonal glass wall, so the ball never gets
        /// stuck in a right-angled corner. The square walls behind it stay, out of reach.
        /// </summary>
        private void BuildCorners()
        {
            float c = _settings.CornerSize;
            if (c <= 0f)
            {
                return;
            }

            float h = _settings.WallHeight;
            float t = _settings.WallThickness;
            foreach (Vector3 corner in Corners())
            {
                CornerLine(corner, out Vector3 start, out Vector3 end, out Vector3 inward);
                Vector3 middle = (start + end) * 0.5f - inward * (t * 0.5f);
                float length = (end - start).magnitude + t;
                // Facing outwards: the box's long side runs along the diagonal.
                Quaternion facing = Quaternion.LookRotation(-inward, Vector3.up);

                _blocks.Solid("Corner_Wall", middle + Vector3.up * (h * 0.5f), new Vector3(length, h, t), facing, _wallMaterial, _wallPhysics);
                _blocks.Decoration(PrimitiveType.Cube, "Corner_Rail", middle + Vector3.up * (h + 0.2f), new Vector3(length, 0.4f, t + 0.3f), facing, RailMaterial(corner.z));
            }
        }

        /// <summary>Quarter pipes along every wall: the sides, both halves of each end and the corners.</summary>
        private void BuildRamps()
        {
            float r = _settings.RampRadius;
            if (r <= 0f)
            {
                return;
            }

            Material material = _rampMaterial != null ? _rampMaterial : _floorMaterial;
            RampBuilder ramps = new RampBuilder(_blocks, material, _wallPhysics, r, RampSlats, RampThickness);

            float halfWidth = _settings.Width * 0.5f;
            float halfLength = _settings.Length * 0.5f;
            float c = _settings.CornerSize;
            float goalGap = _settings.GoalWidth * 0.5f + 0.5f;

            ramps.Build("Ramp_Left", new Vector3(-halfWidth, 0f, -(halfLength - c)), new Vector3(-halfWidth, 0f, halfLength - c), Vector3.right);
            ramps.Build("Ramp_Right", new Vector3(halfWidth, 0f, -(halfLength - c)), new Vector3(halfWidth, 0f, halfLength - c), Vector3.left);

            foreach (float side in new[] { -1f, 1f })
            {
                Vector3 inward = new Vector3(0f, 0f, -side);
                float z = side * halfLength;
                // Left and right of the goal mouth: the opening itself stays flat.
                ramps.Build("Ramp_End", new Vector3(-(halfWidth - c), 0f, z), new Vector3(-goalGap, 0f, z), inward);
                ramps.Build("Ramp_End", new Vector3(goalGap, 0f, z), new Vector3(halfWidth - c, 0f, z), inward);
            }

            if (c <= 0f)
            {
                return;
            }

            foreach (Vector3 corner in Corners())
            {
                CornerLine(corner, out Vector3 start, out Vector3 end, out Vector3 inward);
                ramps.Build("Ramp_Corner", start, end, inward);
            }
        }

        /// <summary>Two posts and a crossbar that glow in the colour of the team that defends the goal.</summary>
        private void BuildGoalFrame(Team team)
        {
            Material glow = team == Team.Blue ? _blueGlowMaterial : _orangeGlowMaterial;
            if (glow == null)
            {
                return;
            }

            float gh = _settings.GoalHeight;
            float halfGoal = _settings.GoalWidth * 0.5f;
            float z = GoalSide(team) * _settings.Length * 0.5f;
            Vector3 thickness = new Vector3(2f * GoalFrameRadius, 0f, 2f * GoalFrameRadius);

            // A cylinder is 2 units tall at scale 1, so its height scale is half the length.
            _blocks.Decoration(PrimitiveType.Cylinder, $"{team}Goal_PostLeft", new Vector3(-halfGoal, gh * 0.5f, z),
                thickness + Vector3.up * (gh * 0.5f), Quaternion.identity, glow);
            _blocks.Decoration(PrimitiveType.Cylinder, $"{team}Goal_PostRight", new Vector3(halfGoal, gh * 0.5f, z),
                thickness + Vector3.up * (gh * 0.5f), Quaternion.identity, glow);
            _blocks.Decoration(PrimitiveType.Cylinder, $"{team}Goal_Crossbar", new Vector3(0f, gh, z),
                thickness + Vector3.up * (halfGoal + GoalFrameRadius), Quaternion.Euler(0f, 0f, 90f), glow);
        }

        /// <summary>The four corners as (x sign, 0, z sign).</summary>
        private static Vector3[] Corners()
        {
            return new[] { new Vector3(-1f, 0f, -1f), new Vector3(1f, 0f, -1f), new Vector3(-1f, 0f, 1f), new Vector3(1f, 0f, 1f) };
        }

        /// <summary>Where the diagonal wall of a corner meets the floor, and which way the field is.</summary>
        private void CornerLine(Vector3 corner, out Vector3 start, out Vector3 end, out Vector3 inward)
        {
            float halfWidth = _settings.Width * 0.5f;
            float halfLength = _settings.Length * 0.5f;
            float c = _settings.CornerSize;
            start = new Vector3(corner.x * (halfWidth - c), 0f, corner.z * halfLength);
            end = new Vector3(corner.x * halfWidth, 0f, corner.z * (halfLength - c));
            inward = new Vector3(-corner.x, 0f, -corner.z).normalized;
        }

        /// <summary>Rails glow blue on the blue half (negative Z) and orange on the other half.</summary>
        private Material RailMaterial(float z)
        {
            Material glow = z < 0f ? _blueGlowMaterial : _orangeGlowMaterial;
            return glow != null ? glow : _trimMaterial;
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
