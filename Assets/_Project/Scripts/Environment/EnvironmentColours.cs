using System;
using RockyLeague.Arena;
using UnityEngine;

namespace RockyLeague.Environment
{
    /// <summary>All the colours of the scenery, editable in the Inspector of the EnvironmentBuilder.</summary>
    [Serializable]
    public sealed class EnvironmentColours
    {
        [SerializeField] private Color _ground = new Color(0.33f, 0.52f, 0.25f);
        [SerializeField] private Color _concrete = new Color(0.62f, 0.63f, 0.66f);
        [SerializeField] private Color _road = new Color(0.22f, 0.23f, 0.25f);
        [SerializeField] private Color _roof = new Color(0.85f, 0.87f, 0.9f);
        [SerializeField] private Color _blueSeats = new Color(0.16f, 0.36f, 0.85f);
        [SerializeField] private Color _orangeSeats = new Color(0.95f, 0.5f, 0.1f);
        [SerializeField] private Color _tower = new Color(0.2f, 0.22f, 0.26f);
        [SerializeField] private Color _screenFrame = new Color(0.08f, 0.09f, 0.11f);
        [SerializeField] private Color _trunk = new Color(0.4f, 0.27f, 0.16f);
        [SerializeField] private Color[] _leaves =
        {
            new Color(0.2f, 0.45f, 0.2f), new Color(0.27f, 0.55f, 0.22f), new Color(0.16f, 0.38f, 0.24f)
        };
        [SerializeField] private Color _pine = new Color(0.12f, 0.33f, 0.22f);
        [SerializeField] private Color _mountain = new Color(0.45f, 0.48f, 0.5f);
        [SerializeField] private Color _building = new Color(0.32f, 0.36f, 0.44f);
        [SerializeField] private Color _cloud = new Color(0.98f, 0.95f, 0.92f);
        [SerializeField] private Color _blimp = new Color(0.95f, 0.95f, 0.97f);
        [SerializeField] private Color[] _crowd =
        {
            new Color(0.9f, 0.2f, 0.2f), new Color(0.95f, 0.85f, 0.2f), new Color(0.2f, 0.7f, 0.3f),
            new Color(0.95f, 0.95f, 0.95f), new Color(0.15f, 0.15f, 0.18f)
        };

        public Color Ground => _ground;
        public Color Concrete => _concrete;
        public Color Road => _road;
        public Color Roof => _roof;
        public Color Tower => _tower;
        public Color ScreenFrame => _screenFrame;
        public Color Trunk => _trunk;
        public Color[] Leaves => _leaves;
        public Color Pine => _pine;
        public Color Mountain => _mountain;
        public Color Building => _building;
        public Color Cloud => _cloud;
        public Color Blimp => _blimp;
        public Color[] CrowdColours => _crowd;

        public Color SeatColour(Team team)
        {
            return team == Team.Blue ? _blueSeats : _orangeSeats;
        }
    }
}
