using UnityEngine;

namespace RockyLeague.Cameras
{
    /// <summary>
    /// What a camera mode needs to know about the scene in one frame: the car it follows, the
    /// ball and the lens. Plain data handed over by the <see cref="CameraRig"/>, so the modes never
    /// search the scene or touch Unity objects, and could be tested without a scene.
    /// </summary>
    public readonly struct CameraContext
    {
        public Vector3 CarPosition { get; }
        public Vector3 CarForward { get; }
        public Vector3 CarUp { get; }

        /// <summary>True while the car rests on a surface (floor, wall, ramp).</summary>
        public bool CarIsGrounded { get; }

        public Vector3 BallPosition { get; }

        /// <summary>Vertical field of view of the camera, in degrees.</summary>
        public float VerticalFieldOfView { get; }

        public CameraContext(Vector3 carPosition, Vector3 carForward, Vector3 carUp, bool carIsGrounded,
            Vector3 ballPosition, float verticalFieldOfView)
        {
            CarPosition = carPosition;
            CarForward = carForward;
            CarUp = carUp;
            CarIsGrounded = carIsGrounded;
            BallPosition = ballPosition;
            VerticalFieldOfView = verticalFieldOfView;
        }
    }
}
