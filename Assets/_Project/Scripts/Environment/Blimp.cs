using UnityEngine;

namespace RockyLeague.Environment
{
    /// <summary>A blimp that circles slowly around the stadium, always facing where it goes.</summary>
    public sealed class Blimp : MonoBehaviour
    {
        private Vector3 _center;
        private float _radius;
        private float _height;
        private float _degreesPerSecond;
        private float _angle;

        public void Initialize(Vector3 center, float radius, float height, float degreesPerSecond, float startAngle)
        {
            _center = center;
            _radius = radius;
            _height = height;
            _degreesPerSecond = degreesPerSecond;
            _angle = startAngle;
            Move(0f);
        }

        private void Update()
        {
            Move(Time.deltaTime);
        }

        private void Move(float deltaTime)
        {
            _angle += _degreesPerSecond * deltaTime;
            float radians = _angle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians)) * _radius;
            // Going anticlockwise, the direction of travel is the tangent of the circle.
            Vector3 heading = new Vector3(-Mathf.Sin(radians), 0f, Mathf.Cos(radians));

            transform.position = _center + offset + Vector3.up * _height;
            transform.rotation = Quaternion.LookRotation(heading, Vector3.up);
        }
    }
}
