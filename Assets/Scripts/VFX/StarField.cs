using UnityEngine;

namespace GravityRocket.VFX
{
    public class StarField : MonoBehaviour
    {
        [SerializeField] private int _starCount = 150;
        [SerializeField] private float _minSize = 0.02f;
        [SerializeField] private float _maxSize = 0.06f;
        [SerializeField] private float _minAlpha = 0.2f;
        [SerializeField] private float _maxAlpha = 0.8f;

        private void Start()
        {
            GenerateStars();
        }

        private void GenerateStars()
        {
            float worldWidth = Core.LevelGenerator.WorldWidth;
            float worldHeight = Core.LevelGenerator.WorldHeight;

            for (int i = 0; i < _starCount; i++)
            {
                var star = new GameObject($"Star_{i}");
                star.transform.SetParent(transform);

                float x = Random.Range(0f, worldWidth);
                float y = Random.Range(0f, worldHeight);
                star.transform.position = new Vector3(x, y, 1f);

                var sr = star.AddComponent<SpriteRenderer>();
                sr.sprite = CreatePixelSprite();
                sr.sortingLayerName = "Default";
                sr.sortingOrder = -100;

                float size = Random.Range(_minSize, _maxSize);
                star.transform.localScale = new Vector3(size, size, 1f);

                float alpha = Random.Range(_minAlpha, _maxAlpha);
                sr.color = new Color(1f, 1f, 1f, alpha);
            }
        }

        private Sprite _cachedPixelSprite;

        private Sprite CreatePixelSprite()
        {
            if (_cachedPixelSprite != null) return _cachedPixelSprite;

            var tex = new Texture2D(4, 4);
            var pixels = new Color[16];
            for (int i = 0; i < 16; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;

            _cachedPixelSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            return _cachedPixelSprite;
        }
    }
}
