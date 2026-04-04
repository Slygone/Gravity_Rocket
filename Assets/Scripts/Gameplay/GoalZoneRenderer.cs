using UnityEngine;

namespace GravityRocket.Gameplay
{
    public class GoalZoneRenderer : MonoBehaviour
    {
        [SerializeField] private LineRenderer _outerBorder;
        [SerializeField] private LineRenderer _centerZone;
        [SerializeField] private SpriteRenderer _centerFill;

        private float _goalWidth;
        private float _goalHeight = 0.75f;
        private Vector2 _goalPosition;

        public void Configure(Vector2 position, float width)
        {
            _goalPosition = position;
            _goalWidth = width;

            SetupOuterBorder();
            SetupCenterZone();
            SetupCenterFill();
        }

        private void SetupOuterBorder()
        {
            if (_outerBorder == null) return;

            _outerBorder.positionCount = 5;
            _outerBorder.loop = false;
            _outerBorder.startWidth = 0.03f;
            _outerBorder.endWidth = 0.03f;
            _outerBorder.startColor = Color.white;
            _outerBorder.endColor = Color.white;
            _outerBorder.useWorldSpace = true;

            float x = _goalPosition.x;
            float y = _goalPosition.y;
            _outerBorder.SetPositions(new Vector3[]
            {
                new(x, y, 0),
                new(x + _goalWidth, y, 0),
                new(x + _goalWidth, y + _goalHeight, 0),
                new(x, y + _goalHeight, 0),
                new(x, y, 0)
            });
        }

        private void SetupCenterZone()
        {
            if (_centerZone == null) return;

            float centerX = _goalPosition.x + _goalWidth * 0.4f;
            float centerW = _goalWidth * 0.2f;
            float y = _goalPosition.y;

            _centerZone.positionCount = 5;
            _centerZone.loop = false;
            _centerZone.startWidth = 0.05f;
            _centerZone.endWidth = 0.05f;
            _centerZone.startColor = Color.white;
            _centerZone.endColor = Color.white;
            _centerZone.useWorldSpace = true;

            _centerZone.SetPositions(new Vector3[]
            {
                new(centerX, y, 0),
                new(centerX + centerW, y, 0),
                new(centerX + centerW, y + _goalHeight, 0),
                new(centerX, y + _goalHeight, 0),
                new(centerX, y, 0)
            });
        }

        private void SetupCenterFill()
        {
            if (_centerFill == null) return;

            float centerX = _goalPosition.x + _goalWidth * 0.5f;
            float centerY = _goalPosition.y + _goalHeight / 2f;
            _centerFill.transform.position = new Vector3(centerX, centerY, 0);
            _centerFill.transform.localScale = new Vector3(_goalWidth * 0.2f, _goalHeight, 1f);
        }

        private void Update()
        {
            if (_centerFill != null)
            {
                float pulse = (Mathf.Sin(Time.time * 5f) + 1f) * 0.15f;
                _centerFill.color = new Color(1f, 1f, 1f, pulse);
            }
        }
    }
}
