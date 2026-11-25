using System;
using UnityEngine;

namespace RockyLeague.Car
{
    /// <summary>
    /// Jump of one car: an instant impulse away from the surface, with a cooldown.
    ///
    /// The input only says whether the button is HELD, so this class detects the
    /// moment it is PRESSED by remembering the previous step. That is why it must be
    /// ticked every step, also when jumping is not allowed (airborne): otherwise a
    /// button held in the air would count as a new press when landing.
    ///
    /// For now every car can jump; it will become the Jumper car's special ability.
    /// All methods are meant to be called from FixedUpdate.
    /// </summary>
    public sealed class CarJump
    {
        private readonly Rigidbody _body;
        private readonly CarSettings _settings;
        private readonly GroundSensor _groundSensor;

        private bool _wasJumpHeld;
        private float _cooldownLeft;

        public CarJump(Rigidbody body, CarSettings settings, GroundSensor groundSensor)
        {
            if (body == null)
            {
                throw new ArgumentNullException(nameof(body));
            }
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }
            if (groundSensor == null)
            {
                throw new ArgumentNullException(nameof(groundSensor));
            }

            _body = body;
            _settings = settings;
            _groundSensor = groundSensor;
        }

        /// <summary>True when the cooldown has finished.</summary>
        public bool IsReady => _cooldownLeft <= 0f;

        /// <summary>
        /// Updates the cooldown and the button tracking, and jumps if the button
        /// has just been pressed, jumping is allowed and the cooldown is over.
        /// </summary>
        public void Tick(bool jumpHeld, bool canJump, float deltaTime)
        {
            _cooldownLeft = Mathf.Max(0f, _cooldownLeft - deltaTime);

            bool justPressed = jumpHeld && !_wasJumpHeld;
            _wasJumpHeld = jumpHeld;

            if (justPressed && canJump && IsReady)
            {
                Jump();
            }
        }

        /// <summary>Makes the jump available again (used when the match restarts).</summary>
        public void ResetCooldown()
        {
            _cooldownLeft = 0f;
        }

        private void Jump()
        {
            // VelocityChange: changes the speed directly, ignoring the mass.
            _body.AddForce(_groundSensor.GroundNormal * _settings.JumpSpeed, ForceMode.VelocityChange);
            _cooldownLeft = _settings.JumpCooldown;
        }
    }
}
