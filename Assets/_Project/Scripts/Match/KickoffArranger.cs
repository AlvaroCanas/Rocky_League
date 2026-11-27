using System;
using System.Collections.Generic;
using System.Linq;
using RockyLeague.Arena;
using RockyLeague.Core;

namespace RockyLeague.Match
{
    /// <summary>
    /// Puts every car on a kickoff spot before each kickoff. The spots are drawn at random
    /// every time, and both teams get the same ones: car number i of each team goes to spot
    /// number i of the draw, turned 180 degrees for the Away team. So kickoffs change but
    /// are always fair.
    ///
    /// To the match flow it is just one more <see cref="IResettable"/> (like the ball): the
    /// MatchController resets "everything" without knowing that cars need a random spot (OCP).
    /// It is plain C# and receives its random generator, so a test can pass a fixed seed.
    /// </summary>
    public sealed class KickoffArranger : IResettable
    {
        private readonly ArenaBuilder _arena;
        private readonly IReadOnlyList<MatchCar> _cars;
        private readonly int _carsPerTeam;
        private readonly Random _random;

        public KickoffArranger(ArenaBuilder arena, IReadOnlyList<MatchCar> cars, int carsPerTeam, Random random)
        {
            _arena = arena != null ? arena : throw new ArgumentNullException(nameof(arena));
            _cars = cars ?? throw new ArgumentNullException(nameof(cars));
            _random = random ?? throw new ArgumentNullException(nameof(random));

            if (carsPerTeam < 1 || carsPerTeam > arena.KickoffSpotCount)
            {
                throw new ArgumentOutOfRangeException(nameof(carsPerTeam), $"There are {arena.KickoffSpotCount} kickoff spots, so a team can have between 1 and {arena.KickoffSpotCount} cars.");
            }

            _carsPerTeam = carsPerTeam;
        }

        public void ResetToKickoff()
        {
            int[] spots = DrawSpots();

            foreach (MatchCar matchCar in _cars)
            {
                matchCar.Car.SetKickoffPose(_arena.GetKickoffPose(matchCar.Team, spots[matchCar.Slot]));
                matchCar.Car.ResetToKickoff();
            }
        }

        // Shuffle the spot numbers and keep one per car of a team: no two team-mates share a spot.
        // LINQ is fine here: it runs once per kickoff, not every frame.
        private int[] DrawSpots()
        {
            return Enumerable.Range(0, _arena.KickoffSpotCount)
                .OrderBy(_ => _random.Next())
                .Take(_carsPerTeam)
                .ToArray();
        }
    }
}
