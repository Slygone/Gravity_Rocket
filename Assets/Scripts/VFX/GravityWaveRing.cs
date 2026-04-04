using UnityEngine;

namespace GravityRocket.VFX
{
    public class GravityWaveRing : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _ringRenderer;
        [SerializeField] private float _waveSpeed = 1f;
        [SerializeField] private float _maxScaleMultiplier = 0.2f;

        private float _baseMass;
        private float _baseScale;

        public void Configure(float planetMass, float planetRadius)
        {
            _baseMass = planetMass;
            _baseScale = planetRadius * 2f;
        }

        private void Update()
        {
            if (_ringRenderer == null) return;

            float wave = (Time.time * _waveSpeed) % 1f;
            float scale = _baseScale + (_baseMass * _maxScaleMultiplier * wave);
            transform.localScale = new Vector3(scale, scale, 1f);

            float alpha = 1f - wave;
            _ringRenderer.color = new Color(1f, 1f, 1f, alpha);
        }
    }
}
