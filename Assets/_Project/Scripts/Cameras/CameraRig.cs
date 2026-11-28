using RockyLeague.Arena;
using RockyLeague.Car;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RockyLeague.Cameras
{
    /// <summary>
    /// The player's camera. It does not decide where to look by itself: it asks its camera modes
    /// (Strategy, <see cref="ICameraMode"/>) for a pose and moves the camera there.
    ///   - <see cref="CarChaseMode"/>: behind the car.
    ///   - <see cref="BallChaseMode"/>: ball cam, ball in the middle and the car always visible.
    ///
    /// The ball cam is switched on and off with its own input action (Tab by default, editable in
    /// the inspector). It is NOT part of <see cref="Car.Inputs.CarInputData"/>: the camera belongs
    /// to the player, not to the car, and bots have no camera (SRP, ISP).
    ///
    /// Both modes are evaluated every frame, so each one keeps its smoothing up to date and
    /// switching is a short blend between their two poses, never a jump. While there is no ball to
    /// look at (none in the scene, or hidden after a goal) the chase camera is shown, and the ball
    /// cam comes back by itself when the ball does.
    ///
    /// The targets are injected with <see cref="SetTargets"/> by whoever builds the match (DIP).
    /// It runs in LateUpdate, after the cars and the ball have moved.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CameraRig : MonoBehaviour
    {
        // Unity's default, used only if the rig is not on a Camera.
        private const float DefaultFieldOfView = 60f;

        [Header("Targets")]
        [SerializeField, Tooltip("Car to follow. In a match the MatchFactory sets it.")]
        private Transform _target;
        [SerializeField, Tooltip("Ball for the ball cam. Optional: without it only the chase camera is used.")]
        private Ball _ball;

        [Header("Modes")]
        [SerializeField] private CarChaseMode _carChase = new CarChaseMode();
        [SerializeField] private BallChaseMode _ballChase = new BallChaseMode();

        [Header("Ball cam")]
        [SerializeField] private bool _ballCamOnStart = true;
        [SerializeField, Min(0.01f), Tooltip("Seconds the camera takes to blend from one mode to the other.")]
        private float _blendTime = 0.25f;
        [SerializeField, Tooltip("Switches the ball cam on and off.")]
        private InputAction _toggleBallCam = new InputAction("ToggleBallCam", InputActionType.Button, "<Keyboard>/tab");

        private Camera _camera;
        private CarController _targetCar;
        private bool _ballCamOn;
        private bool _hasPose;

        // 0 = chase camera, 1 = ball cam; in between while blending.
        private float _blend;

        /// <summary>Whether the player has the ball cam on (it is only shown while there is a ball).</summary>
        public bool IsBallCamOn => _ballCamOn;

        private bool HasBall => _ball != null && _ball.IsPresent;

        public void SetTargets(Transform car, Ball ball)
        {
            _target = car;
            _ball = ball;
            _targetCar = FindCar(car);
        }

        public void ToggleBallCam() => _ballCamOn = !_ballCamOn;

        private void Awake()
        {
            TryGetComponent(out _camera);
            _targetCar = FindCar(_target);
            _ballCamOn = _ballCamOnStart;
        }

        // The target is a Transform so any object can be followed; if it is a car, the modes also
        // get to know whether it is on the ground.
        private static CarController FindCar(Transform target)
        {
            return target != null && target.TryGetComponent(out CarController car) ? car : null;
        }

        private void OnEnable()
        {
            _toggleBallCam.performed += OnToggleBallCam;
            _toggleBallCam.Enable();
        }

        private void OnDisable()
        {
            _toggleBallCam.performed -= OnToggleBallCam;
            _toggleBallCam.Disable();
        }

        private void OnToggleBallCam(InputAction.CallbackContext context) => ToggleBallCam();

        private void LateUpdate()
        {
            if (_target == null)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            CameraContext context = new CameraContext(
                _target.position,
                _target.forward,
                _target.up,
                _targetCar == null || _targetCar.IsGrounded,
                _ball != null ? _ball.transform.position : _target.position,
                _camera != null ? _camera.fieldOfView : DefaultFieldOfView);

            CameraPose chase = _carChase.Evaluate(context, deltaTime);
            CameraPose ballCam = _ballChase.Evaluate(context, deltaTime);

            // The first frame starts in the right mode instead of blending into it.
            float wanted = _ballCamOn && HasBall ? 1f : 0f;
            _blend = _hasPose ? Mathf.MoveTowards(_blend, wanted, deltaTime / _blendTime) : wanted;
            _hasPose = true;

            CameraPose pose = CameraPose.Lerp(chase, ballCam, Mathf.SmoothStep(0f, 1f, _blend));
            transform.SetPositionAndRotation(pose.Position, pose.Rotation);
        }
    }
}
