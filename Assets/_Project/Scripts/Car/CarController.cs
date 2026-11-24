using RockyLeague.Car.Inputs;
using RockyLeague.Car.States;
using RockyLeague.Core.StateMachines;
using UnityEngine;

namespace RockyLeague.Car
{
    /// <summary>
    /// Unity entry point of a car. It wires the car's parts together (composition)
    /// and drives them from FixedUpdate:
    /// input source -> state machine (Grounded / Airborne) -> motor -> Rigidbody.
    ///
    /// It does not know who drives it: the input source is injected through
    /// <see cref="SetInput"/> (DIP). If none is injected, it looks for an
    /// <see cref="ICarInput"/> on the same GameObject, and otherwise receives no input.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
    public sealed class CarController : MonoBehaviour
    {
        [SerializeField] private CarSettings _settings;

        private Rigidbody _body;
        private GroundSensor _groundSensor;
        private CarMotor _motor;
        private CarBoost _boost;
        private CarJump _jump;
        private StateMachine _stateMachine;
        private ICarInput _input = NullCarInput.Instance;
        private CarInputData _currentInput;

        public bool IsGrounded => _groundSensor != null && _groundSensor.IsGrounded;
        public float ForwardSpeed => _motor?.ForwardSpeed ?? 0f;

        /// <summary>Boost left from 0 to 1, for the UI.</summary>
        public float NormalizedBoost => _boost?.NormalizedAmount ?? 0f;
        public bool IsBoosting => _boost != null && _boost.IsBoosting;

        /// <summary>Exposes the current state for debugging and UI; not meant to be changed from outside.</summary>
        public IState CurrentState => _stateMachine?.CurrentState;

        public void SetInput(ICarInput input) => _input = input ?? NullCarInput.Instance;

        private void Awake()
        {
            if (_settings == null)
            {
                Debug.LogError($"{nameof(CarController)} on '{name}' has no {nameof(CarSettings)} assigned.", this);
                enabled = false;
                return;
            }

            _body = GetComponent<Rigidbody>();
            ConfigureRigidbody(GetComponent<BoxCollider>());

            if (TryGetComponent(out ICarInput localInput))
            {
                SetInput(localInput);
            }

            _groundSensor = GroundSensor.FromBoxCollider(GetComponent<BoxCollider>(), _settings);
            _motor = new CarMotor(_body, _settings, _groundSensor);
            _boost = new CarBoost(_body, _settings, _groundSensor);
            _jump = new CarJump(_body, _settings, _groundSensor);
            _stateMachine = BuildStateMachine();
        }

        private void FixedUpdate()
        {
            _currentInput = _input.ReadInput();
            _groundSensor.Refresh();
            _stateMachine.Tick(Time.fixedDeltaTime);
        }

        private StateMachine BuildStateMachine()
        {
            StateMachine machine = new StateMachine();
            GroundedCarState grounded = new GroundedCarState(_motor, _boost, _jump, () => _currentInput);
            AirborneCarState airborne = new AirborneCarState(_boost, _jump, () => _currentInput);

            machine.AddTransition(grounded, airborne, () => !_groundSensor.IsGrounded);
            machine.AddTransition(airborne, grounded, () => _groundSensor.IsGrounded);

            machine.ChangeState(airborne);
            return machine;
        }

        private void ConfigureRigidbody(BoxCollider box)
        {
            _body.mass = _settings.Mass;
            _body.centerOfMass = box.center + _settings.CenterOfMassOffset;
            _body.maxAngularVelocity = _settings.MaxAngularSpeed;
            _body.interpolation = RigidbodyInterpolation.Interpolate;
            _body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        private void OnDrawGizmosSelected() => _groundSensor?.DrawGizmos();
    }
}
