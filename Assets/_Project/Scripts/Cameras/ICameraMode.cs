namespace RockyLeague.Cameras
{
    /// <summary>
    /// A way of placing the camera (Strategy): behind the car, looking at the ball...
    /// Each mode keeps its own smoothing, so the <see cref="CameraRig"/> can keep every mode up to
    /// date and switch or blend between them at any moment without a jump. A new camera (goal
    /// replay, spectator for the training scene) is a new mode, not a change to the rig (OCP).
    /// </summary>
    public interface ICameraMode
    {
        /// <summary>Works out this frame's pose. Called every frame, also while the mode is not shown.</summary>
        CameraPose Evaluate(in CameraContext context, float deltaTime);
    }
}
