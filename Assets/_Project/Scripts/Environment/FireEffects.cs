using System.Collections.Generic;
using RockyLeague.Arena;
using UnityEngine;

namespace RockyLeague.Environment
{
    /// <summary>
    /// Fire on top of the end stands: braziers that always burn and flame jets that fire
    /// for a few seconds when a goal is scored, on the stand of the team that scored.
    /// Like the crowd and the screens, it is a subscriber of the referee's GoalScored event
    /// (Observer): only goals that count fire the jets, and the referee does not know it exists.
    ///
    /// The flames are particle systems set up by code. The soft round shape of each
    /// particle is a small texture drawn by code too, so no image file is needed.
    /// </summary>
    public sealed class FireEffects : MonoBehaviour
    {
        private const float JetSeconds = 3f;
        private const float JetRate = 120f;
        private const int SpriteSize = 64;

        private readonly List<ParticleSystem> _homeJets = new List<ParticleSystem>();
        private readonly List<ParticleSystem> _awayJets = new List<ParticleSystem>();
        private IGoalSource _goals;
        private Material _material;
        private Texture2D _sprite;
        private Team _firingTeam;
        private float _jetTimeLeft;

        /// <param name="fireMaterial">Transparent particle material; a copy gets the round sprite.</param>
        public void Initialize(IGoalSource goals, Material fireMaterial, StadiumLayout layout, Material bowlMaterial)
        {
            _goals = goals;
            _sprite = CreateSoftDot(SpriteSize);
            _material = new Material(fireMaterial);
            _material.SetTexture("_BaseMap", _sprite);

            float wallTop = layout.BackWallHeight;
            float halfBackWall = layout.EndStandLength * 0.5f + layout.StandDepth;

            foreach (Team team in new[] { Team.Home, Team.Away })
            {
                float z = (team == Team.Home ? -1f : 1f) * (layout.EndStandStart + layout.StandDepth + 0.5f);
                List<ParticleSystem> jets = team == Team.Home ? _homeJets : _awayJets;

                // Braziers at both ends of the wall and beside the screen.
                foreach (float x in new[] { -(halfBackWall - 2f), -16f, 16f, halfBackWall - 2f })
                {
                    Vector3 spot = new Vector3(x, wallTop, z);
                    CreateBowl(spot, bowlMaterial);
                    CreateFlame("Brazier_Fire", spot + Vector3.up * 0.9f, rate: 70f, speed: 5f, size: 3.5f, radius: 1f).Play();
                }

                // Jets in between, quiet until a goal.
                foreach (float x in new[] { -26f, -21f, 21f, 26f })
                {
                    if (Mathf.Abs(x) > halfBackWall - 3f)
                    {
                        continue;
                    }

                    ParticleSystem jet = CreateFlame("Goal_Jet", new Vector3(x, wallTop + 0.3f, z), rate: 0f, speed: 22f, size: 4f, radius: 0.5f);
                    jet.Play();
                    jets.Add(jet);
                }
            }
        }

        private void Start()
        {
            if (_goals == null)
            {
                enabled = false;
                return;
            }

            _goals.GoalScored += OnGoalScored;
        }

        private void OnDestroy()
        {
            if (_goals != null)
            {
                _goals.GoalScored -= OnGoalScored;
            }

            // Made by code, so not freed with the scene.
            Destroy(_material);
            Destroy(_sprite);
        }

        private void OnGoalScored(object sender, GoalScoredEventArgs e)
        {
            _firingTeam = e.ScoringTeam;
            _jetTimeLeft = JetSeconds;
            SetJetRate(_firingTeam, JetRate);
        }

        private void Update()
        {
            if (_jetTimeLeft <= 0f)
            {
                return;
            }

            _jetTimeLeft -= Time.deltaTime;
            if (_jetTimeLeft <= 0f)
            {
                SetJetRate(_firingTeam, 0f);
            }
        }

        private void SetJetRate(Team team, float rate)
        {
            foreach (ParticleSystem jet in team == Team.Home ? _homeJets : _awayJets)
            {
                ParticleSystem.EmissionModule emission = jet.emission;
                emission.rateOverTime = rate;
            }
        }

        private void CreateBowl(Vector3 spot, Material bowlMaterial)
        {
            GameObject bowl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bowl.name = "Brazier_Bowl";
            bowl.transform.SetParent(transform, false);
            bowl.transform.localPosition = spot + Vector3.up * 0.3f;
            bowl.transform.localScale = new Vector3(3f, 0.4f, 3f);
            Destroy(bowl.GetComponent<Collider>());
            if (bowlMaterial != null)
            {
                bowl.GetComponent<MeshRenderer>().sharedMaterial = bowlMaterial;
            }
        }

        /// <summary>A cone of glowing particles pointing up: yellow at the base, red and fading at the top.</summary>
        private ParticleSystem CreateFlame(string flameName, Vector3 spot, float rate, float speed, float size, float radius)
        {
            GameObject holder = new GameObject(flameName);
            holder.transform.SetParent(transform, false);
            holder.transform.localPosition = spot;
            // The cone shape emits along the object's forward axis: point it up.
            holder.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

            ParticleSystem flame = holder.AddComponent<ParticleSystem>();
            // A new system starts playing at once; settings like the duration need it stopped.
            flame.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = flame.main;
            main.duration = 1f;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.7f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.45f), new Color(1f, 0.5f, 0.12f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = -0.15f;
            main.maxParticles = 400;

            ParticleSystem.EmissionModule emission = flame.emission;
            emission.rateOverTime = rate;

            ParticleSystem.ShapeModule shape = flame.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 10f;
            shape.radius = radius;

            ParticleSystem.ColorOverLifetimeModule colour = flame.colorOverLifetime;
            colour.enabled = true;
            Gradient fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(new Color(1f, 0.9f, 0.5f), 0f), new GradientColorKey(new Color(1f, 0.45f, 0.1f), 0.45f), new GradientColorKey(new Color(0.6f, 0.1f, 0.05f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(0.8f, 0.6f), new GradientAlphaKey(0f, 1f) });
            colour.color = fade;

            ParticleSystem.SizeOverLifetimeModule shrink = flame.sizeOverLifetime;
            shrink.enabled = true;
            shrink.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.25f));

            ParticleSystemRenderer flameRenderer = holder.GetComponent<ParticleSystemRenderer>();
            flameRenderer.sharedMaterial = _material;
            flameRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            return flame;
        }

        /// <summary>A white dot that fades to transparent at the edge: the shape of every flame particle.</summary>
        private static Texture2D CreateSoftDot(int size)
        {
            Texture2D dot = new Texture2D(size, size, TextureFormat.RGBA32, false);
            dot.wrapMode = TextureWrapMode.Clamp;
            float centre = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(centre, centre)) / centre;
                    float alpha = Mathf.Clamp01(1f - distance);
                    dot.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha));
                }
            }

            dot.Apply();
            return dot;
        }
    }
}
