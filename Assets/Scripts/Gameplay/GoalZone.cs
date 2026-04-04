using UnityEngine;

namespace GravityRocket.Gameplay
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class GoalZone : MonoBehaviour
    {
        private float _goalWidth;
        private float _goalHeight = 0.75f;
        private System.Action<float> _onRocketArrived;

        public void Configure(Vector2 position, float width, System.Action<float> onRocketArrived)
        {
            _goalWidth = width;
            _onRocketArrived = onRocketArrived;

            transform.position = new Vector3(position.x + width / 2f, position.y + _goalHeight / 2f, 0f);

            var collider = GetComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(width, _goalHeight);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Rocket")) return;

            float localX = other.transform.position.x - (transform.position.x - _goalWidth / 2f);
            float hitRatio = Mathf.Clamp01(localX / _goalWidth);

            _onRocketArrived?.Invoke(hitRatio);
        }
    }
}
