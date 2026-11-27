using System.Collections;
using UnityEngine;

namespace RockyLeague.UI
{
    /// <summary>
    /// Shows and hides a menu panel with a short fade, through its <see cref="CanvasGroup"/>
    /// (transparency, clicks and keyboard focus at once). Reused by every overlay of the match
    /// (results, pause...) by composition: each screen has a UIPanel instead of inheriting one.
    ///
    /// The fade uses unscaled time, so it also works while the game is paused (timeScale = 0).
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class UIPanel : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _fadeSeconds = 0.2f;
        [SerializeField] private bool _visibleOnStart;

        private CanvasGroup _group;
        private Coroutine _fade;

        public bool IsVisible { get; private set; }

        public void Show() => FadeTo(true);

        public void Hide() => FadeTo(false);

        /// <summary>Lets the buttons be clicked or not, e.g. to avoid a double click while a scene loads.</summary>
        public void SetInteractable(bool interactable)
        {
            _group.interactable = interactable;
        }

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            Apply(_visibleOnStart, _visibleOnStart ? 1f : 0f);
        }

        private void FadeTo(bool visible)
        {
            if (_fade != null)
            {
                StopCoroutine(_fade);
            }

            _fade = StartCoroutine(Fade(visible));
        }

        private IEnumerator Fade(bool visible)
        {
            // Clicks are blocked as soon as hiding starts, and allowed only once fully shown.
            Apply(visible, _group.alpha);
            _group.interactable = false;

            float target = visible ? 1f : 0f;
            while (!Mathf.Approximately(_group.alpha, target))
            {
                float step = _fadeSeconds > 0f ? Time.unscaledDeltaTime / _fadeSeconds : 1f;
                _group.alpha = Mathf.MoveTowards(_group.alpha, target, step);
                yield return null;
            }

            Apply(visible, target);
            _fade = null;
        }

        private void Apply(bool visible, float alpha)
        {
            IsVisible = visible;
            _group.alpha = alpha;
            _group.interactable = visible;
            _group.blocksRaycasts = visible;
        }
    }
}
