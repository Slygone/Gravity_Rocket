using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Applies an RGB chromatic aberration glitch effect to UI elements,
/// mimicking the CSS rgb-glitch keyframe animation.
/// Attach to a UI GameObject; it will add Shadow components to all
/// child Graphics (Image, Text) and cycle through glitch frames.
/// </summary>
public class RGBGlitchEffect : MonoBehaviour
{
    private Shadow[] shadowsA;
    private Shadow[] shadowsB;
    private float timer;
    private const float CYCLE = 2f;

    void Start()
    {
        Graphic[] graphics = GetComponentsInChildren<Graphic>();
        shadowsA = new Shadow[graphics.Length];
        shadowsB = new Shadow[graphics.Length];

        for (int i = 0; i < graphics.Length; i++)
        {
            shadowsA[i] = graphics[i].gameObject.AddComponent<Shadow>();
            shadowsA[i].effectColor = Color.clear;
            shadowsA[i].effectDistance = Vector2.zero;

            shadowsB[i] = graphics[i].gameObject.AddComponent<Shadow>();
            shadowsB[i].effectColor = Color.clear;
            shadowsB[i].effectDistance = Vector2.zero;
        }
    }

    void Update()
    {
        timer += Time.unscaledDeltaTime;
        float t = (timer % CYCLE) / CYCLE;

        Color cA = Color.clear, cB = Color.clear;
        Vector2 oA = Vector2.zero, oB = Vector2.zero;

        // Matches CSS keyframes: 88%, 91%, 94%, 97%
        if (t >= 0.88f && t < 0.91f)
        {
            // red + cyan
            cA = new Color(1, 0, 0, 0.8f); oA = new Vector2(-3, 0);
            cB = new Color(0, 1, 1, 0.8f); oB = new Vector2(3, 0);
        }
        else if (t >= 0.91f && t < 0.94f)
        {
            // red + cyan reversed
            cA = new Color(1, 0, 0, 0.8f); oA = new Vector2(3, 0);
            cB = new Color(0, 1, 1, 0.8f); oB = new Vector2(-3, 0);
        }
        else if (t >= 0.94f && t < 0.97f)
        {
            // green + magenta
            cA = new Color(0, 1, 0, 0.8f); oA = new Vector2(-3, 0);
            cB = new Color(1, 0, 1, 0.8f); oB = new Vector2(3, 0);
        }
        else if (t >= 0.97f)
        {
            // blue + yellow
            cA = new Color(0, 0, 1, 0.8f); oA = new Vector2(3, 0);
            cB = new Color(1, 1, 0, 0.8f); oB = new Vector2(-3, 0);
        }

        for (int i = 0; i < shadowsA.Length; i++)
        {
            if (shadowsA[i] != null) { shadowsA[i].effectColor = cA; shadowsA[i].effectDistance = oA; }
            if (shadowsB[i] != null) { shadowsB[i].effectColor = cB; shadowsB[i].effectDistance = oB; }
        }
    }
}
