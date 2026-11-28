using System;
using System.Collections.Generic;
using RockyLeague.AI;
using RockyLeague.Arena;
using RockyLeague.Cameras;
using RockyLeague.Car;
using RockyLeague.Car.Inputs;
using RockyLeague.Core;
using RockyLeague.Core.Game;
using UnityEngine;

namespace RockyLeague.Match
{
    /// <summary>
    /// Builds a match from a <see cref="MatchConfig"/> (Factory pattern): it creates the
    /// human player's car and fills every other place with a bot, gives each bot what it
    /// needs (team, difficulty, ball, arena) and points the camera at the player and the ball.
    ///
    /// Nobody else creates cars, so the rules for "who plays where" live in a single place:
    /// the player is always Home slot 0; Home's other slots and every Away slot are bots.
    /// 1v1 and 2v2 use exactly the same code, only CarsPerTeam changes.
    ///
    /// It is also where dependencies are injected by hand (DIP): the bots never search the
    /// scene for the ball or the arena, the factory hands them over. Every car's input is
    /// wrapped in a <see cref="GatedCarInput"/>, so the match decides when cars may move.
    /// </summary>
    public sealed class MatchFactory : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField, Tooltip("Car_Player prefab.")]
        private PlayerCarInput _playerCarPrefab;
        [SerializeField, Tooltip("Car_AI prefab.")]
        private AIDriverInput _botCarPrefab;

        [Header("Bot difficulty")]
        [SerializeField] private AIDifficulty _easy;
        [SerializeField] private AIDifficulty _normal;
        [SerializeField] private AIDifficulty _hard;

        [Header("Scene")]
        [SerializeField] private ArenaBuilder _arena;
        [SerializeField] private Ball _ball;
        [SerializeField, Tooltip("Camera that follows the player's car.")]
        private CameraRig _camera;

        private readonly List<MatchCar> _cars = new List<MatchCar>();

        /// <summary>Every car of the match, available after <see cref="Build"/>.</summary>
        public IReadOnlyList<MatchCar> Cars => _cars;

        /// <summary>
        /// Creates every car of the match. Returns what puts them on their kickoff spots,
        /// so the match flow can reset them together with the ball.
        /// </summary>
        /// <param name="canDrive">Asked every physics step: while false, no car receives input.</param>
        public IResettable Build(MatchConfig config, Func<bool> canDrive)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (canDrive == null) throw new ArgumentNullException(nameof(canDrive));
            if (_cars.Count > 0) throw new InvalidOperationException("The match has already been built.");
            CheckReferences();
            if (config.CarsPerTeam > _arena.KickoffSpotCount)
            {
                throw new InvalidOperationException($"{config.CarsPerTeam} cars per team need {config.CarsPerTeam} kickoff spots, but the arena settings only have {_arena.KickoffSpotCount}.");
            }

            AIDifficulty botDifficulty = ResolveDifficulty(config.Difficulty);

            foreach (Team team in new[] { Team.Home, Team.Away })
            {
                for (int slot = 0; slot < config.CarsPerTeam; slot++)
                {
                    bool isPlayer = team == Team.Home && slot == 0;
                    Pose pose = _arena.GetKickoffPose(team, slot);
                    CarController car = isPlayer
                        ? SpawnPlayer(pose, canDrive)
                        : SpawnBot(team, slot, botDifficulty, pose, canDrive);

                    _cars.Add(new MatchCar(car, team, slot, isPlayer));
                }
            }

            return new KickoffArranger(_arena, _cars, config.CarsPerTeam, new System.Random());
        }

        private CarController SpawnPlayer(Pose pose, Func<bool> canDrive)
        {
            PlayerCarInput player = Instantiate(_playerCarPrefab, pose.position, pose.rotation, transform);
            player.name = $"{Team.Home}_Player";

            if (_camera != null)
            {
                _camera.SetTargets(player.transform, _ball);
            }

            return WithGatedInput(player.GetComponent<CarController>(), player, canDrive);
        }

        // Initialize runs right after Instantiate, before the bot's Start, as AIDriverInput requires.
        private CarController SpawnBot(Team team, int slot, AIDifficulty difficulty, Pose pose, Func<bool> canDrive)
        {
            AIDriverInput bot = Instantiate(_botCarPrefab, pose.position, pose.rotation, transform);
            bot.name = $"{team}_Bot_{slot}";
            bot.Initialize(team, difficulty, _ball, _arena, canDrive);
            return WithGatedInput(bot.GetComponent<CarController>(), bot, canDrive);
        }

        // The car found its driver on its own GameObject in Awake; replace it with the same
        // driver behind a gate (Decorator), so the car itself never needs to know about the match.
        private static CarController WithGatedInput(CarController car, ICarInput driver, Func<bool> canDrive)
        {
            car.SetInput(new GatedCarInput(driver, canDrive));
            return car;
        }

        // The menu speaks in levels (plain C#); the bots need the matching asset.
        private AIDifficulty ResolveDifficulty(DifficultyLevel level)
        {
            return level switch
            {
                DifficultyLevel.Easy => _easy,
                DifficultyLevel.Normal => _normal,
                DifficultyLevel.Hard => _hard,
                _ => throw new ArgumentOutOfRangeException(nameof(level), level, "Unknown difficulty level.")
            };
        }

        private void CheckReferences()
        {
            if (_playerCarPrefab == null || _botCarPrefab == null || _arena == null || _ball == null
                || _easy == null || _normal == null || _hard == null)
            {
                throw new InvalidOperationException($"{nameof(MatchFactory)} on '{name}' is missing a prefab, a difficulty, the arena or the ball.");
            }
        }
    }
}
