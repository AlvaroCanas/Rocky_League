using RockyLeague.Arena;
using RockyLeague.Core.Building;
using RockyLeague.Match;
using UnityEngine;

namespace RockyLeague.Environment
{
    /// <summary>
    /// Builds the world around the arena when the scene starts. It only coordinates
    /// (a Facade over the builders): the stands, the stadium details and the landscape
    /// are each built by their own class, so no class grows into a God Object.
    ///
    /// Everything comes from the arena's size and a fixed seed, so a bigger arena gets
    /// bigger stands and the world is the same in every match. What never moves is merged
    /// with static batching; what moves or changes (fans, screens, texts, the blimp)
    /// lives under a separate parent that is left out of the merge.
    /// </summary>
    public sealed class EnvironmentBuilder : MonoBehaviour
    {
        private const float TierDepth = 2.6f;
        private const float TierRise = 1.6f;

        [SerializeField] private ArenaBuilder _arena;
        [SerializeField, Tooltip("Optional: the giant screens show its score.")]
        private ScoreKeeper _scoreKeeper;
        [SerializeField, Tooltip("Optional: the referee whose valid goals make the crowd cheer, the screens flash and the fire jets burn.")]
        private MatchController _match;

        [Header("Materials")]
        [SerializeField, Tooltip("Opaque lit material; most of the scenery uses tinted copies of it.")]
        private Material _baseMaterial;
        [SerializeField, Tooltip("Glowing material for floodlights and city windows.")]
        private Material _lampMaterial;
        [SerializeField] private Material _waterMaterial;
        [SerializeField, Tooltip("Additive particle material for the braziers and the goal flame jets.")]
        private Material _fireMaterial;

        [Header("Amounts")]
        [SerializeField, Tooltip("Same seed = same world every time.")]
        private int _seed = 2627;
        [SerializeField, Range(1, 12)] private int _standTiers = 8;
        [SerializeField, Range(0f, 1f), Tooltip("Share of seats with someone in them.")]
        private float _crowdDensity = 0.4f;
        [SerializeField, Range(0, 400)] private int _treeCount = 220;
        [SerializeField, Range(0, 80)] private int _buildingCount = 45;
        [SerializeField, Range(0, 40)] private int _mountainCount = 22;
        [SerializeField, Range(0, 40)] private int _cloudCount = 20;

        [SerializeField] private EnvironmentColours _colours = new EnvironmentColours();

        private Palette _palette;

        private void Awake()
        {
            if (_arena == null || _baseMaterial == null)
            {
                Debug.LogError($"{nameof(EnvironmentBuilder)} on '{name}' needs the arena and a base material.", this);
                enabled = false;
                return;
            }

            Transform scenery = CreateChild("Scenery");
            Transform dynamic = CreateChild("Dynamic");
            Transform crowdRoot = CreateChild("Crowd");

            BlockBuilder sceneryBlocks = new BlockBuilder(scenery);
            System.Random random = new System.Random(_seed);
            StadiumLayout layout = new StadiumLayout(_arena.Settings, _standTiers, TierDepth, TierRise);
            _palette = new Palette(_baseMaterial);

            // Explicit Unity null check: an unassigned field must reach the effects as a real null.
            IGoalSource goals = _match != null ? _match : null;

            CrowdCheer crowd = crowdRoot.gameObject.AddComponent<CrowdCheer>();
            crowd.Initialize(goals);

            StandsBuilder stands = new StandsBuilder(sceneryBlocks, new BlockBuilder(crowdRoot), crowd, _palette, layout, _colours, random, _crowdDensity);
            stands.Build();

            StadiumDecorBuilder decor = new StadiumDecorBuilder(sceneryBlocks, dynamic, _palette, layout, _colours, _lampMaterial);
            decor.BuildFloodlights();
            decor.BuildScreens(_scoreKeeper, goals);
            decor.BuildAdBoards();

            if (_fireMaterial != null)
            {
                FireEffects fire = dynamic.gameObject.AddComponent<FireEffects>();
                fire.Initialize(goals, _fireMaterial, layout, _palette.Paint(_colours.Tower));
            }

            LandscapeBuilder landscape = new LandscapeBuilder(sceneryBlocks, dynamic, _palette, layout, _colours, random, _lampMaterial, _waterMaterial);
            landscape.BuildGround();
            landscape.BuildLake();
            landscape.BuildTrees(_treeCount);
            landscape.BuildCity(_buildingCount);
            landscape.BuildMountains(_mountainCount);
            landscape.BuildClouds(_cloudCount);
            landscape.BuildBlimp();

            // Nothing under Scenery moves: merge it into a few big meshes.
            StaticBatchingUtility.Combine(scenery.gameObject);
        }

        private void OnDestroy()
        {
            _palette?.Release();
        }

        private Transform CreateChild(string childName)
        {
            GameObject child = new GameObject(childName);
            child.transform.SetParent(transform, false);
            return child.transform;
        }
    }
}
