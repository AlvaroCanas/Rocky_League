using UnityEngine;

namespace RockyLeague.Cameras
{
    /// <summary>
    /// Where the camera wants to be and where it wants to look, for one frame.
    /// Camera modes only work out poses; the <see cref="CameraRig"/> is the only one that moves
    /// the camera, so blending two modes is just blending two poses.
    /// A struct because it is small, created every frame and has no identity.
    /// </summary>
    public readonly struct CameraPose
    {
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }

        public CameraPose(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }

        /// <summary>
        /// Pose at <paramref name="position"/> looking along <paramref name="direction"/>, with the
        /// world up as up (the camera never rolls with the car).
        /// </summary>
        public static CameraPose LookingAlong(Vector3 position, Vector3 direction)
        {
            // A zero direction has no rotation; it can only happen with a badly tuned mode.
            Quaternion rotation = direction.sqrMagnitude > 1e-6f
                ? Quaternion.LookRotation(direction, Vector3.up)
                : Quaternion.identity;

            return new CameraPose(position, rotation);
        }

        /// <summary>Pose between <paramref name="from"/> (t = 0) and <paramref name="to"/> (t = 1).</summary>
        public static CameraPose Lerp(in CameraPose from, in CameraPose to, float t)
        {
            return new CameraPose(
                Vector3.Lerp(from.Position, to.Position, t),
                Quaternion.Slerp(from.Rotation, to.Rotation, t));
        }
    }
}
