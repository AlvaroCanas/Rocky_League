using System;
using RockyLeague.Arena;
using RockyLeague.Car;

namespace RockyLeague.Match
{
    /// <summary>
    /// A car taking part in a match: the car itself, the team it plays for and its
    /// position inside that team (slot 0, 1...). Built by the <see cref="MatchFactory"/>
    /// so the rest of the match knows who is who without searching the scene.
    /// </summary>
    public sealed class MatchCar
    {
        public CarController Car { get; }
        public Team Team { get; }
        public int Slot { get; }
        public bool IsPlayer { get; }

        public MatchCar(CarController car, Team team, int slot, bool isPlayer)
        {
            if (slot < 0) throw new ArgumentOutOfRangeException(nameof(slot), "A slot cannot be negative.");

            Car = car != null ? car : throw new ArgumentNullException(nameof(car));
            Team = team;
            Slot = slot;
            IsPlayer = isPlayer;
        }
    }
}
