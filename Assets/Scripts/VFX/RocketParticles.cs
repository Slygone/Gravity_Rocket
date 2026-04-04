using UnityEngine;

namespace GravityRocket.VFX
{
    public class RocketParticles : MonoBehaviour
    {
        [SerializeField] private ParticleSystem _exhaustSystem;

        public void SetExhaustActive(bool active)
        {
            if (_exhaustSystem == null) return;

            var emission = _exhaustSystem.emission;
            emission.enabled = active;

            if (active && !_exhaustSystem.isPlaying)
                _exhaustSystem.Play();
        }

        public void Explode()
        {
            if (_exhaustSystem == null) return;

            var emission = _exhaustSystem.emission;
            emission.enabled = false;

            var burstParams = new ParticleSystem.EmitParams();
            for (int i = 0; i < 40; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float speed = Random.Range(2f, 7f);
                burstParams.velocity = new Vector3(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed, 0f);
                burstParams.startLifetime = Random.Range(0.5f, 1.0f);
                burstParams.position = transform.position;
                _exhaustSystem.Emit(burstParams, 1);
            }
        }

        public void Clear()
        {
            if (_exhaustSystem == null) return;
            _exhaustSystem.Stop();
            _exhaustSystem.Clear();
        }
    }
}
