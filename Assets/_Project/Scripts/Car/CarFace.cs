namespace RockyLeague.Car
{
    /// <summary>
    /// A face of the car's box, seen from the car itself: "Left" is always the car's left,
    /// whatever the way the car is lying. Used to say which face rests on a surface when
    /// the car is not on its wheels.
    /// </summary>
    public enum CarFace
    {
        /// <summary>No face rests on a surface.</summary>
        None,

        /// <summary>The roof: the car is upside down.</summary>
        Top,

        Left,
        Right,

        /// <summary>The nose.</summary>
        Front,

        /// <summary>The tail.</summary>
        Back
    }
}
