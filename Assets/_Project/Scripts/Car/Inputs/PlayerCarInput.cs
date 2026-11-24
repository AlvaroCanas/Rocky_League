using UnityEngine;
using UnityEngine.InputSystem;

namespace RockyLeague.Car.Inputs
{
    /// <summary>
    /// Human input source: translates the "Car" action map of the Input System
    /// into <see cref="CarInputData"/>. It knows nothing about physics (SRP).
    /// </summary>
    public sealed class PlayerCarInput : MonoBehaviour, ICarInput
    {
        private const string ActionMapName = "Car";

        [SerializeField, Tooltip("Asset with a 'Car' action map (Assets/_Project/Input/CarControls).")]
        private InputActionAsset _actions;

        private InputActionMap _map;
        private InputAction _throttle;
        private InputAction _steer;
        private InputAction _pitch;
        private InputAction _roll;
        private InputAction _jump;
        private InputAction _boost;
        private InputAction _handbrake;

        private void Awake()
        {
            if (_actions == null)
            {
                Debug.LogError($"{nameof(PlayerCarInput)} on '{name}' has no Input Action Asset assigned.", this);
                enabled = false;
                return;
            }

            _map = _actions.FindActionMap(ActionMapName, throwIfNotFound: true);
            _throttle = _map.FindAction("Throttle", throwIfNotFound: true);
            _steer = _map.FindAction("Steer", throwIfNotFound: true);
            _pitch = _map.FindAction("Pitch", throwIfNotFound: true);
            _roll = _map.FindAction("Roll", throwIfNotFound: true);
            _jump = _map.FindAction("Jump", throwIfNotFound: true);
            _boost = _map.FindAction("Boost", throwIfNotFound: true);
            _handbrake = _map.FindAction("Handbrake", throwIfNotFound: true);
        }

        private void OnEnable() => _map?.Enable();

        private void OnDisable() => _map?.Disable();

        public CarInputData ReadInput()
        {
            if (!isActiveAndEnabled || _map == null)
            {
                return CarInputData.None;
            }

            return new CarInputData(
                throttle: _throttle.ReadValue<float>(),
                steer: _steer.ReadValue<float>(),
                pitch: _pitch.ReadValue<float>(),
                roll: _roll.ReadValue<float>(),
                jump: _jump.IsPressed(),
                boost: _boost.IsPressed(),
                handbrake: _handbrake.IsPressed());
        }
    }
}
