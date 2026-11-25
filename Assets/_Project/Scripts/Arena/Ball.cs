using RockyLeague.Core;
using UnityEngine;

namespace RockyLeague.Arena
{
    /// <summary>
    /// The match ball: a big, light and bouncy physics sphere.
    /// It configures its own Rigidbody, collider and physics material from the
    /// values below, so a plain Unity sphere becomes a ball just by adding this component.
    ///
    /// There is a single ball, so its tuning lives here instead of in a shared
    /// ScriptableObject (unlike cars, there is nothing to share: KISS / YAGNI).
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public sealed class Ball : MonoBehaviour, IResettable
    {
        // Radius of Unity's built-in sphere mesh at scale 1.
        private const float UnitSphereRadius = 0.5f;

        [Header("Shape")]
        [SerializeField, Min(0.1f), Tooltip("Ball radius in meters. The car is about 1.2 m long.")]
        private float _radius = 1f;

        [Header("Physics")]
        [SerializeField, Min(0.1f), Tooltip("Lighter than a car (180 kg) so cars can push it easily.")]
        private float _mass = 30f;
        [SerializeField, Min(1f), Tooltip("Speed cap in m/s, so hits never send it through walls.")]
        private float _maxSpeed = 60f;
        [SerializeField, Range(0f, 1f)] private float _bounciness = 0.6f;
        [SerializeField, Range(0f, 1f)] private float _friction = 0.3f;
        [SerializeField, Min(0f), Tooltip("Air resistance: how fast it slows down on its own.")]
        private float _linearDamping = 0.05f;
        [SerializeField, Min(0f)] private float _angularDamping = 0.1f;

        private Rigidbody _body;
        private SphereCollider _collider;
        private Renderer[] _renderers;
        private Vector3 _kickoffPosition;

        public float Radius => _radius;
        public Vector3 Position => _body.position;
        public Vector3 Velocity => _body.linearVelocity;

        /// <summary>
        /// Makes the ball disappear (after a goal): invisible, without collisions and frozen,
        /// so nothing can touch it while the goal is celebrated.
        /// </summary>
        public void Hide()
        {
            _body.linearVelocity = Vector3.zero;
            _body.angularVelocity = Vector3.zero;
            _body.isKinematic = true;
            SetPresent(false);
        }

        /// <summary>
        /// Puts the ball back at the centre spot, completely stopped and visible again.
        /// Called by the match flow after every goal.
        /// </summary>
        public void ResetToKickoff()
        {
            // Velocity cannot be set on a kinematic body; a hidden ball is already stopped.
            if (!_body.isKinematic)
            {
                _body.linearVelocity = Vector3.zero;
                _body.angularVelocity = Vector3.zero;
            }

            _body.position = _kickoffPosition;
            transform.position = _kickoffPosition;
            _body.isKinematic = false;
            SetPresent(true);
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _collider = GetComponent<SphereCollider>();
            _renderers = GetComponentsInChildren<Renderer>();
            ConfigureShape(_collider);
            ConfigureRigidbody();

            // Wherever the ball is placed in the scene is its kickoff spot.
            _kickoffPosition = transform.position;
        }

        private void FixedUpdate()
        {
            Vector3 velocity = _body.linearVelocity;
            if (velocity.sqrMagnitude > _maxSpeed * _maxSpeed)
            {
                _body.linearVelocity = velocity.normalized * _maxSpeed;
            }
        }

        private void SetPresent(bool present)
        {
            _collider.enabled = present;
            foreach (Renderer meshRenderer in _renderers)
            {
                meshRenderer.enabled = present;
            }
        }

        private void ConfigureShape(SphereCollider sphere)
        {
            // The visible mesh and the collider must have the same size.
            transform.localScale = Vector3.one * (_radius / UnitSphereRadius);
            sphere.radius = UnitSphereRadius;
            sphere.center = Vector3.zero;

            PhysicsMaterial material = new PhysicsMaterial("BallMaterial");
            material.bounciness = _bounciness;
            material.bounceCombine = PhysicsMaterialCombine.Maximum;
            material.dynamicFriction = _friction;
            material.staticFriction = _friction;
            material.frictionCombine = PhysicsMaterialCombine.Average;
            sphere.sharedMaterial = material;
        }

        private void ConfigureRigidbody()
        {
            _body.mass = _mass;
            _body.linearDamping = _linearDamping;
            _body.angularDamping = _angularDamping;
            _body.useGravity = true;
            _body.interpolation = RigidbodyInterpolation.Interpolate;
            _body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
    }
}
