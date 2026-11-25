namespace RockyLeague.Core
{
    /// <summary>
    /// Something that goes back to its kickoff state: the ball to the centre spot, a car to
    /// its starting position with full boost. The match flow resets "everything that can be
    /// reset" by walking a list of these, without knowing what each one is (ISP, OCP):
    /// a new resettable thing (boost pads, power-ups...) only has to implement this interface.
    /// </summary>
    public interface IResettable
    {
        void ResetToKickoff();
    }
}
