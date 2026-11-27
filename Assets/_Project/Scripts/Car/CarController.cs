using RockyLeague.Car.Inputs;
using RockyLeague.Car.States;
using RockyLeague.Core;
using RockyLeague.Core.StateMachines;
using UnityEngine;

namespace RockyLeague.Car
{
    /// <summary>
    /// Unity entry point of a car. It wires the car's parts together (composition)
    /// and drives them from FixedUpdate:
    /// input source -> state machine (Grounded / Airborne / Flipped) -> motor -> Rigidbody.
    ///
    /// It does not know who drives it: the input source is injected through
    /// <see cref="SetInput"/> (DIP). If none is injected, it looks for an
    /// <see cref="ICarInput"/> on the same GameObject, and otherwise receives no input.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
    public sealed class CarController : MonoBehaviour, IResettable
    {
        [SerializeField] private CarSettings _settings;

        private Rigidbody _body;
        private GroundSensor _groundSensor;
        private CarMotor _motor;
        private CarBoost _boost;
        private CarJump _jump;
        private CarAirControl _airControl;
        private FlipSensor _flipSensor;
        private CarRecovery _recovery;
        private StateMachine _stateMachine;
        private ICarInput _input = NullCarInput.Instance;
        private CarInputData _currentInput;
        private Vector3 _kickoffPosition;
        private Quaternion _kickoffRotation;

        public bool IsGrounded => _groundSensor != null && _groundSensor.IsGrounded;
        public float ForwardSpeed => _motor?.ForwardSpeed ?? 0f;

        /// <summary>Boost left from 0 to 1, for the UI.</summary>
        public float NormalizedBoost => _boost?.NormalizedAmount ?? 0f;
        public bool IsBoosting => _boost != null && _boost.IsBoosting;

        /// <summary>Exposes the current state for debugging and UI; not meant to be changed from outside.</summary>
        public IState CurrentState => _stateMachine?.CurrentState;

        public void SetInput(ICarInput input) => _input = input ?? NullCarInput.Instance;

        /// <summary>
        /// Stops the car where it is and keeps it there (no physics acts on it any more).
        /// Used at the final whistle so nothing moves behind the results screen.
        /// </summary>
        public void Freeze()
        {
            if (_body == null || _body.isKinematic)
            {
                return;
            }

            _body.linearVelocity = Vector3.zero;
            _body.angularVelocity = Vector3.zero;
            _body.isKinematic = true;
        }

        /// <summary>
        /// Changes where the car goes on the next <see cref="ResetToKickoff"/>. The match moves
        /// the cars to a different kickoff spot every time, so the spot is not fixed at Awake.
        /// </summary>
        public void SetKickoffPose(Pose pose)
        {
            _kickoffPosition = pose.position;
            _kickoffRotation = pose.rotation;
        }

        /// <summary>
        /// Puts the car back on its kickoff spot: stopped, with full boost and the jump ready.
        /// Called by the match flow before every kickoff.
        /// </summary>
        public void ResetToKickoff()
        {
            // Nothing to reset if the car never finished setting itself up (no settings assigned).
            if (_body == null)
            {
                return;
            }

            _body.isKinematic = false;
            _body.linearVelocity = Vector3.zero;
            _body.angularVelocity = Vector3.zero;
            _body.position = _kickoffPosition;
            _body.rotation = _kickoffRotation;
            transform.SetPositionAndRotation(_kickoffPosition, _kickoffRotation);

            _boost.Refill();
            _jump.ResetCooldown();
        }

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

            // Until the match says otherwise, wherever the car is placed is its kickoff spot.
            _kickoffPosition = transform.position;
            _kickoffRotation = transform.rotation;

            if (TryGetComponent(out ICarInput localInput))
            {
                SetInput(localInput);
            }

            _groundSensor = GroundSensor.FromBoxCollider(GetComponent<BoxCollider>(), _settings);
            _motor = new CarMotor(_body, _settings, _groundSensor);
            _boost = new CarBoost(_body, _settings, _groundSensor);
            _jump = new CarJump(_body, _settings, _groundSensor);
            _airControl = new CarAirControl(_body, _settings);
            _flipSensor = FlipSensor.FromBoxCollider(GetComponent<BoxCollider>(), _settings);
            _recovery = new CarRecovery(_body, _settings, _flipSensor);
            _stateMachine = BuildStateMachine();
        }

        private void FixedUpdate()
        {
            _currentInput = _input.ReadInput();
            _groundSensor.Refresh();
            _flipSensor.Refresh(Time.fixedDeltaTime);
            _stateMachine.Tick(Time.fixedDeltaTime);
        }

        private StateMachine BuildStateMachine()
        {
            StateMachine machine = new StateMachine();
            GroundedCarState grounded = new GroundedCarState(_motor, _boost, _jump, () => _currentInput);
            AirborneCarState airborne = new AirborneCarState(_boost, _jump, _airControl, () => _currentInput);
            FlippedCarState flipped = new FlippedCarState(_boost, _jump, _recovery, () => _currentInput);

            // For states with several exits the first transition that holds wins, so touching
            // the ground is always checked before anything else.
            machine.AddTransition(grounded, airborne, () => !_groundSensor.IsGrounded);

            machine.AddTransition(airborne, grounded, () => _groundSensor.IsGrounded);
            machine.AddTransition(airborne, flipped, () => _flipSensor.IsFlipped);

            machine.AddTransition(flipped, grounded, () => _groundSensor.IsGrounded);
            machine.AddTransition(flipped, airborne, () => _flipSensor.TouchingFace == CarFace.None);

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

        private void OnDrawGizmosSelected()
        {
            _groundSensor?.DrawGizmos();
            _flipSensor?.DrawGizmos();
        }
    }
}
