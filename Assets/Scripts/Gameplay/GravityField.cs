using UnityEngine;
using GravityRocket.VFX;

namespace GravityRocket.Gameplay
{
    [RequireComponent(typeof(CircleCollider2D))]
    public class GravityField : MonoBehaviour
    {
        [SerializeField] private float _mass = 100f;
        [SerializeField] private float _gravityConstant = 15f;

        private Rigidbody2D _rocketRb;
        private Transform _rocketTransform;
        private bool _hasRocket;

        public void Configure(float mass, float radius)
        {
            _mass = mass;

            var collider = GetComponent<CircleCollider2D>();
            if (collider != null)
            {
                collider.radius = radius;
            }

            transform.localScale = Vector3.one * (radius * 2f);

            var waveRing = GetComponentInChildren<GravityWaveRing>();
            if (waveRing != null)
            {
                waveRing.Configure(mass, radius);
            }
        }

        public void SetRocket(Rigidbody2D rocketRb)
        {
            _rocketRb = rocketRb;
            _rocketTransform = rocketRb != null ? rocketRb.transform : null;
            _hasRocket = rocketRb != null;
        }

        public float Mass => _mass;

        private void FixedUpdate()
        {
            if (!_hasRocket || _rocketRb == null) return;

            Vector2 direction = (Vector2)transform.position - _rocketRb.position;
            float distSq = direction.sqrMagnitude;

            if (distSq < 0.01f) return;

            float forceMag = _gravityConstant * _mass / distSq;

            Vector2 force = direction.normalized * forceMag;
            _rocketRb.AddForce(force);
        }

        public Vector2 GetGravityAcceleration()
        {
            if (!_hasRocket || _rocketRb == null) return Vector2.zero;

            Vector2 direction = (Vector2)transform.position - _rocketRb.position;
            float distSq = direction.sqrMagnitude;
            if (distSq < 0.01f) return Vector2.zero;

            float forceMag = _gravityConstant * _mass / distSq;
            return direction.normalized * forceMag;
        }
    }
}
