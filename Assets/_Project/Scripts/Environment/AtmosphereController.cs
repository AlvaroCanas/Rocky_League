using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RockyLeague.Environment
{
    /// <summary>
    /// Light and mood of the match: the sky, a low golden sun, the fog that blends the
    /// horizon and the camera effects (glow on bright lights, film-like colours, darker
    /// corners). Kept apart from the builders: it changes how things look, not what exists.
    /// </summary>
    public sealed class AtmosphereController : MonoBehaviour
    {
        [Header("Sky and sun")]
        [SerializeField] private Material _skybox;
        [SerializeField] private Light _sun;
        [SerializeField, Range(0f, 90f), Tooltip("Height of the sun over the horizon, in degrees.")]
        private float _sunElevation = 22f;
        [SerializeField, Range(-180f, 180f)] private float _sunAzimuth = -40f;
        [SerializeField] private Color _sunColour = new Color(1f, 0.84f, 0.66f);
        [SerializeField, Min(0f)] private float _sunIntensity = 1.5f;

        [Header("Fog")]
        [SerializeField] private Color _fogColour = new Color(0.86f, 0.78f, 0.72f);
        [SerializeField, Min(0f)] private float _fogStart = 200f;
        [SerializeField, Min(1f)] private float _fogEnd = 1000f;

        [Header("Camera effects")]
        [SerializeField, Min(0f)] private float _bloomIntensity = 0.9f;
        [SerializeField, Min(0f), Tooltip("Only things brighter than this glow (the lamps).")]
        private float _bloomThreshold = 1.1f;
        [SerializeField, Range(-100f, 100f)] private float _saturation = 15f;
        [SerializeField, Range(0f, 1f)] private float _vignette = 0.22f;

        private VolumeProfile _profile;

        private void Awake()
        {
            ApplySkyAndSun();
            ApplyFog();
            ApplyCameraEffects();
        }

        private void OnDestroy()
        {
            if (_profile != null)
            {
                Destroy(_profile);
            }
        }

        private void ApplySkyAndSun()
        {
            if (_sun != null)
            {
                _sun.transform.rotation = Quaternion.Euler(_sunElevation, _sunAzimuth, 0f);
                _sun.color = _sunColour;
                _sun.intensity = _sunIntensity;
                RenderSettings.sun = _sun;
            }

            if (_skybox != null)
            {
                RenderSettings.skybox = _skybox;
            }

            // Ambient light comes from the sky: refresh it after changing sky and sun.
            DynamicGI.UpdateEnvironment();
        }

        private void ApplyFog()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = _fogColour;
            RenderSettings.fogStartDistance = _fogStart;
            RenderSettings.fogEndDistance = _fogEnd;
        }

        /// <summary>A global post-processing volume created by code, above any other in the scene.</summary>
        private void ApplyCameraEffects()
        {
            _profile = ScriptableObject.CreateInstance<VolumeProfile>();

            Bloom bloom = _profile.Add<Bloom>(true);
            bloom.intensity.Override(_bloomIntensity);
            bloom.threshold.Override(_bloomThreshold);
            bloom.scatter.Override(0.7f);

            Tonemapping tonemapping = _profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.ACES);

            ColorAdjustments colours = _profile.Add<ColorAdjustments>(true);
            colours.saturation.Override(_saturation);
            colours.postExposure.Override(0.15f);

            Vignette vignette = _profile.Add<Vignette>(true);
            vignette.intensity.Override(_vignette);

            Volume volume = gameObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.sharedProfile = _profile;

            Camera camera = Camera.main;
            if (camera != null)
            {
                camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            }
        }
    }
}
