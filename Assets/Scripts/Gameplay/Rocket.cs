using UnityEngine;
using GravityRocket.VFX;

namespace GravityRocket.Gameplay
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    public class Rocket : MonoBehaviour
    {
        [SerializeField] private RocketParticles _particles;
        [SerializeField] private TrailRenderer _trail;
        [SerializeField] private float _launchSpeed = 9f;
        [SerializeField] private float _aberrationMultiplier = 0.005f;

        private Rigidbody2D _rb;
        private CircleCollider2D _collider;
        private bool _isFlying;
        private bool _isWarping;
        private int _warpTimer;
        private float _warpX;
        private Vector2 _warpTarget;

        private const float BoundsPadding = 2f;

        private System.Action<string> _onOutOfBounds;
        private System.Action _onPlanetCrash;

        public Rigidbody2D Rb => _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _collider = GetComponent<CircleCollider2D>();

            _rb.gravityScale = 0f;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        public void ResetToPosition(Vector2 position, System.Action<string> onOutOfBounds, System.Action onPlanetCrash)
        {
            _onOutOfBounds = onOutOfBounds;
            _onPlanetCrash = onPlanetCrash;

            transform.position = new Vector3(position.x, position.y, 0f);
            transform.rotation = Quaternion.identity;

            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
            _rb.position = position;
            Physics2D.SyncTransforms();

            _isFlying = false;
            _isWarping = false;
            _warpTimer = 0;

            if (_trail != null)
            {
                _trail.Clear();
                _trail.emitting = false;
            }

            if (_particles != null)
            {
                _particles.Clear();
                _particles.SetExhaustActive(false);
            }

            ChromaticAberrationFeature.SetOffset(Vector2.zero);

            var sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = true;

            gameObject.SetActive(true);
        }

        public void Launch(Vector2 direction)
        {
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.linearVelocity = direction.normalized * _launchSpeed;
            _isFlying = true;

            if (_trail != null) _trail.emitting = true;
            if (_particles != null) _particles.SetExhaustActive(true);
        }

        public void StartWarp(Vector2 goalCenter)
        {
            _isFlying = false;
            _isWarping = true;
            _warpTimer = 0;
            _warpX = transform.position.x;
            _warpTarget = goalCenter;

            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.linearVelocity = Vector2.zero;
        }

        public void Crash()
        {
            _isFlying = false;
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.linearVelocity = Vector2.zero;

            if (_particles != null) _particles.Explode();
            if (_trail != null) _trail.emitting = false;

            var sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = false;
        }

        private void Update()
        {
            if (_isFlying)
            {
                UpdateRotation();
                UpdateAberration();
                CheckBounds();
            }
            else if (_isWarping)
            {
                UpdateWarp();
            }
        }

        private void UpdateRotation()
        {
            if (_rb.linearVelocity.sqrMagnitude > 0.01f)
            {
                float angle = Mathf.Atan2(_rb.linearVelocity.y, _rb.linearVelocity.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }
        }

        private void UpdateAberration()
        {
            var fields = FindObjectsByType<GravityField>(FindObjectsSortMode.None);
            Vector2 totalAccel = Vector2.zero;
            foreach (var field in fields)
            {
                totalAccel += field.GetGravityAcceleration();
            }

            Vector2 offset = totalAccel * _aberrationMultiplier;
            float maxOffset = 0.3f;
            offset.x = Mathf.Clamp(offset.x, -maxOffset, maxOffset);
            offset.y = Mathf.Clamp(offset.y, -maxOffset, maxOffset);
            ChromaticAberrationFeature.SetOffset(offset);
        }

        private void CheckBounds()
        {
            float worldW = Core.LevelGenerator.WorldWidth;
            float worldH = Core.LevelGenerator.WorldHeight;
            Vector2 pos = transform.position;

            if (pos.x < -BoundsPadding || pos.x > worldW + BoundsPadding ||
                pos.y < -BoundsPadding || pos.y > worldH + BoundsPadding)
            {
                _isFlying = false;
                _onOutOfBounds?.Invoke("out_of_bounds");
            }
        }

        private void UpdateWarp()
        {
            _warpTimer++;

            if (_warpTimer < 30)
            {
                Vector3 pos = transform.position;
                pos.x += (_warpX - pos.x) * 0.15f;
                pos.y += (_warpTarget.y - pos.y) * 0.15f;
                transform.position = pos;

                float currentAngle = transform.eulerAngles.z;
                float targetAngle = 90f;
                float da = Mathf.DeltaAngle(currentAngle, targetAngle);
                transform.rotation = Quaternion.Euler(0, 0, currentAngle + da * 0.15f);

                Vector2 currentOffset = Vector2.Lerp(
                    ChromaticAberrationFeature.GetOffset(),
                    Vector2.zero,
                    0.06f
                );
                ChromaticAberrationFeature.SetOffset(currentOffset);
            }
            else
            {
                Vector3 pos = transform.position;
                pos.y += (_warpTimer - 20) * 1.5f * Time.deltaTime * 60f;
                transform.position = pos;

                ChromaticAberrationFeature.SetOffset(new Vector2(0f, _warpTimer * 0.0005f));

                // Stop warp when rocket is well above screen
                if (pos.y > Core.LevelGenerator.WorldHeight + 10f)
                {
                    _isWarping = false;
                    ChromaticAberrationFeature.SetOffset(Vector2.zero);
                    gameObject.SetActive(false);
                }
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!_isFlying) return;

            if (collision.gameObject.GetComponent<GravityField>() != null)
            {
                _isFlying = false;
                _onPlanetCrash?.Invoke();
            }
        }
    }
}
