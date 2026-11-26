using UnityEngine;

namespace RockyLeague.Car
{
    /// <summary>
    /// Tuning data of a car, kept apart from the logic that uses it.
    /// Being a ScriptableObject, several cars (player and bots) can share one asset
    /// and values can be tweaked in the editor without touching code.
    /// Default values are loosely based on Rocket League, in meters (1 uu = 1 cm).
    /// </summary>
    [CreateAssetMenu(fileName = "CarSettings", menuName = "Rocky League/Car Settings")]
    public sealed class CarSettings : ScriptableObject
    {
        [Header("Rigidbody")]
        [SerializeField, Min(0.1f)] private float _mass = 180f;
        [SerializeField, Tooltip("Offset from the collider centre. Lower = harder to flip over.")]
        private Vector3 _centerOfMassOffset = new Vector3(0f, -0.15f, 0f);
        [SerializeField, Min(0.1f), Tooltip("Max angular speed in rad/s.")]
        private float _maxAngularSpeed = 5.5f;

        [Header("Driving (m/s, m/s²)")]
        [SerializeField, Min(0.1f)] private float _maxDriveSpeed = 14f;
        [SerializeField, Min(0f)] private float _throttleAcceleration = 16f;
        [SerializeField, Range(0f, 1f), Tooltip("Fraction of the acceleration left when reaching max speed.")]
        private float _accelerationAtMaxSpeed = 0.1f;
        [SerializeField, Min(0f), Tooltip("Deceleration when throttle opposes the movement.")]
        private float _brakeDeceleration = 35f;
        [SerializeField, Min(0f), Tooltip("Deceleration with no throttle.")]
        private float _coastDeceleration = 5f;

        [Header("Steering")]
        [SerializeField, Min(0f), Tooltip("Yaw rate in rad/s at full steer.")]
        private float _maxYawRate = 2.6f;
        [SerializeField, Tooltip("Steering factor (Y) by normalized forward speed (X, 0..1).")]
        private AnimationCurve _steerBySpeed = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.1f, 1f),
            new Keyframe(1f, 0.65f));

        [Header("Grip")]
        [SerializeField, Min(0f), Tooltip("How fast sideways velocity is removed (1/s).")]
        private float _lateralGrip = 15f;
        [SerializeField, Min(0f), Tooltip("Lateral grip while the handbrake is held (drift).")]
        private float _handbrakeLateralGrip = 2f;
        [SerializeField, Min(0f), Tooltip("Acceleration pushing the car against the surface it drives on.")]
        private float _stickyAcceleration = 5f;

        [Header("Boost")]
        [SerializeField, Min(1f), Tooltip("Boost the tank holds when full.")]
        private float _maxBoost = 100f;
        [SerializeField, Range(0f, 1f), Tooltip("Fraction of the tank the car starts with (and gets back on kickoff).")]
        private float _startingBoostFraction = 0.33f;
        [SerializeField, Min(0f), Tooltip("Boost spent per second while boosting.")]
        private float _boostConsumption = 33f;
        [SerializeField, Min(0f), Tooltip("Boost recovered per second while not boosting.")]
        private float _boostRecharge = 10f;
        [SerializeField, Min(0f), Tooltip("Extra forward acceleration while boosting (m/s²).")]
        private float _boostAcceleration = 12f;
        [SerializeField, Min(0.1f), Tooltip("Speed cap while boosting (m/s). Must be above Max Drive Speed.")]
        private float _maxBoostSpeed = 23f;

        [Header("Jump")]
        [SerializeField, Min(0f), Tooltip("Speed added along the surface normal when jumping (m/s).")]
        private float _jumpSpeed = 5f;
        [SerializeField, Min(0f), Tooltip("Seconds before the car can jump again.")]
        private float _jumpCooldown = 1f;

        [Header("Air control")]
        [SerializeField, Min(0f), Tooltip("Max pitch speed in rad/s (nose up / down).")]
        private float _airPitchSpeed = 3.6f;
        [SerializeField, Min(0f), Tooltip("Max yaw speed in rad/s (nose left / right).")]
        private float _airYawSpeed = 3.2f;
        [SerializeField, Min(0f), Tooltip("Max roll speed in rad/s (lean left / right).")]
        private float _airRollSpeed = 4.5f;
        [SerializeField, Min(0f), Tooltip("How fast the spin changes towards the input, in rad/s per second.")]
        private float _airControlAcceleration = 16f;
        [SerializeField, Min(0f), Tooltip("How fast an axis slows down when there is no input on it, in rad/s per second.")]
        private float _airControlDamping = 4f;

        [Header("Flip recovery")]
        [SerializeField, Min(0.5f), Tooltip("Speed of the hop (m/s) when flipping back onto the wheels. The spin is worked out from it, so the car lands about upright.")]
        private float _recoveryHopSpeed = 3.5f;
        [SerializeField, Min(0.01f), Tooltip("Radius (m) of the probe on each top corner that detects the car lying on a surface.")]
        private float _flippedProbeRadius = 0.15f;
        [SerializeField, Min(0f), Tooltip("Seconds the same face must rest on a surface before the car counts as flipped.")]
        private float _flippedDelay = 0.2f;

        [Header("Ground detection")]
        [SerializeField, Min(0.01f), Tooltip("Ray length below the collider's bottom face.")]
        private float _groundCheckDistance = 0.12f;
        [SerializeField, Range(1, 4), Tooltip("Wheels touching a surface needed to be grounded.")]
        private int _minGroundedWheels = 3;
        [SerializeField] private LayerMask _groundLayers = ~0;

        public float Mass => _mass;
        public Vector3 CenterOfMassOffset => _centerOfMassOffset;
        public float MaxAngularSpeed => _maxAngularSpeed;

        public float MaxDriveSpeed => _maxDriveSpeed;
        public float ThrottleAcceleration => _throttleAcceleration;
        public float AccelerationAtMaxSpeed => _accelerationAtMaxSpeed;
        public float BrakeDeceleration => _brakeDeceleration;
        public float CoastDeceleration => _coastDeceleration;

        public float MaxYawRate => _maxYawRate;
        public float LateralGrip => _lateralGrip;
        public float HandbrakeLateralGrip => _handbrakeLateralGrip;
        public float StickyAcceleration => _stickyAcceleration;

        public float MaxBoost => _maxBoost;
        public float StartingBoost => _maxBoost * _startingBoostFraction;
        public float BoostConsumption => _boostConsumption;
        public float BoostRecharge => _boostRecharge;
        public float BoostAcceleration => _boostAcceleration;
        public float MaxBoostSpeed => _maxBoostSpeed;

        public float JumpSpeed => _jumpSpeed;
        public float JumpCooldown => _jumpCooldown;

        public float AirPitchSpeed => _airPitchSpeed;
        public float AirYawSpeed => _airYawSpeed;
        public float AirRollSpeed => _airRollSpeed;
        public float AirControlAcceleration => _airControlAcceleration;
        public float AirControlDamping => _airControlDamping;

        public float RecoveryHopSpeed => _recoveryHopSpeed;
        public float FlippedProbeRadius => _flippedProbeRadius;
        public float FlippedDelay => _flippedDelay;

        public float GroundCheckDistance => _groundCheckDistance;
        public int MinGroundedWheels => _minGroundedWheels;
        public LayerMask GroundLayers => _groundLayers;

        public float EvaluateSteerFactor(float normalizedSpeed) => _steerBySpeed.Evaluate(Mathf.Clamp01(normalizedSpeed));
    }
}
