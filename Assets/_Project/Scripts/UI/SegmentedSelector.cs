using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RockyLeague.UI
{
    /// <summary>
    /// A row of buttons where exactly one is selected (like "1 vs 1 | 2 vs 2").
    /// It only knows WHICH position is selected, not what it means: the screen that owns it
    /// translates the index into a game option. So the same component is reused for the
    /// mode, the difficulty, or any future option (SRP, DRY).
    ///
    /// The selected option is shown by tinting the button's target graphic (a frame or a fill),
    /// optionally making it a little bigger and changing the colour of its label. The look
    /// itself (sprites, sizes) is left to the scene.
    /// </summary>
    public sealed class SegmentedSelector : MonoBehaviour
    {
        [SerializeField, Tooltip("Option buttons, in the order the owning screen expects.")]
        private Button[] _options = new Button[0];
        [SerializeField, Min(0), Tooltip("Option selected when the screen opens.")]
        private int _defaultIndex;
        [SerializeField] private Color _selectedColor = new Color(1f, 0.6f, 0.15f);
        [SerializeField] private Color _unselectedColor = Color.white;
        [SerializeField, Min(1f), Tooltip("Size of the selected option compared with the others.")]
        private float _selectedScale = 1f;
        [SerializeField] private Color _selectedTextColor = Color.white;
        [SerializeField] private Color _unselectedTextColor = Color.white;

        private UnityAction[] _listeners;
        private TMP_Text[] _labels;

        public int OptionCount => _options.Length;

        public int SelectedIndex { get; private set; }

        /// <summary>Selects an option by its position. Out-of-range indices are ignored.</summary>
        public void Select(int index)
        {
            if (index < 0 || index >= _options.Length)
            {
                return;
            }

            SelectedIndex = index;
            RefreshHighlight();
        }

        private void Awake()
        {
            SelectedIndex = Mathf.Clamp(_defaultIndex, 0, Mathf.Max(0, _options.Length - 1));

            // One listener per button. Each lambda captures its own copy of the index, so the
            // button knows which position it is without a separate component per option.
            // They are stored to be able to remove exactly the same delegates in OnDisable.
            _listeners = new UnityAction[_options.Length];
            _labels = new TMP_Text[_options.Length];
            for (int i = 0; i < _options.Length; i++)
            {
                int index = i;
                _listeners[i] = () => Select(index);
                _labels[i] = _options[i] != null ? _options[i].GetComponentInChildren<TMP_Text>() : null;
            }
        }

        private void OnEnable()
        {
            for (int i = 0; i < _options.Length; i++)
            {
                if (_options[i] == null)
                {
                    Debug.LogError($"{nameof(SegmentedSelector)} on '{name}' has an empty option slot ({i}).", this);
                    continue;
                }

                _options[i].onClick.AddListener(_listeners[i]);
            }

            RefreshHighlight();
        }

        private void OnDisable()
        {
            for (int i = 0; i < _options.Length; i++)
            {
                if (_options[i] != null)
                {
                    _options[i].onClick.RemoveListener(_listeners[i]);
                }
            }
        }

        private void RefreshHighlight()
        {
            for (int i = 0; i < _options.Length; i++)
            {
                if (_options[i] == null)
                {
                    continue;
                }

                bool selected = i == SelectedIndex;
                if (_options[i].targetGraphic != null)
                {
                    _options[i].targetGraphic.color = selected ? _selectedColor : _unselectedColor;
                }
                if (_labels[i] != null)
                {
                    _labels[i].color = selected ? _selectedTextColor : _unselectedTextColor;
                }

                _options[i].transform.localScale = Vector3.one * (selected ? _selectedScale : 1f);
            }
        }
    }
}
