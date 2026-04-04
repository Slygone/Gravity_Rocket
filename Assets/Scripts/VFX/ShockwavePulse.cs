using System.Collections;
using UnityEngine;

namespace GravityRocket.VFX
{
    public class ShockwavePulse : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _ringRenderer;
        [SerializeField] private float _startScale = 0.5f;
        [SerializeField] private float _endScale = 5f;
        [SerializeField] private float _duration = 1f;

        public void TriggerPulse(Vector2 position)
        {
            StartCoroutine(PulseSequence(position));
        }

        private IEnumerator PulseSequence(Vector2 position)
        {
            StartCoroutine(AnimatePulse(position));
            yield return new WaitForSeconds(0.15f);
            StartCoroutine(AnimatePulse(position));
        }

        private IEnumerator AnimatePulse(Vector2 position)
        {
            if (_ringRenderer == null) yield break;

            var pulseObj = new GameObject("Pulse");
            pulseObj.transform.position = new Vector3(position.x, position.y, 0f);

            var sr = pulseObj.AddComponent<SpriteRenderer>();
            sr.sprite = _ringRenderer.sprite;
            sr.sortingOrder = _ringRenderer.sortingOrder;

            float elapsed = 0f;
            while (elapsed < _duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _duration;

                float scale = Mathf.Lerp(_startScale, _endScale, t);
                pulseObj.transform.localScale = new Vector3(scale, scale, 1f);

                float alpha = 1f - t;
                sr.color = new Color(1f, 1f, 1f, alpha);

                yield return null;
            }

            Destroy(pulseObj);
        }
    }
}
