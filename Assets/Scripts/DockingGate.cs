using UnityEngine;
using UnityEngine.Rendering;

public class DockingGate : MonoBehaviour
{
    [Header("Gate Properties")]
    public float gateWidth = 160f;
    public float gateHeight = 30f;

    private static Material glMaterial;

    public float GetLeft()
    {
        return transform.position.x - gateWidth * 0.5f;
    }

    public float GetBottom()
    {
        return transform.position.y - gateHeight * 0.5f;
    }

    void OnEnable()
    {
        RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
    }

    void OnDisable()
    {
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
    }

    static void EnsureGLMaterial()
    {
        if (glMaterial != null) return;
        Shader shader = Shader.Find("Hidden/Internal-Colored");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        glMaterial = new Material(shader);
        glMaterial.hideFlags = HideFlags.HideAndDontSave;
        glMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        glMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        glMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        glMaterial.SetInt("_ZWrite", 0);
    }

    void OnEndCameraRendering(ScriptableRenderContext context, Camera cam)
    {
        if (cam != Camera.main) return;

        EnsureGLMaterial();
        glMaterial.SetPass(0);

        GL.PushMatrix();
        GL.LoadProjectionMatrix(cam.projectionMatrix);
        GL.modelview = cam.worldToCameraMatrix;

        float gX = GetLeft();
        float gY = GetBottom();

        // Zone widths (each 20% of total)
        float zoneW = gateWidth * 0.2f;

        // Outer zones [1] — dashed
        DrawDashedRect(gX, gY, zoneW, gateHeight, 4f, 4f, new Color(1, 1, 1, 0.6f));
        DrawDashedRect(gX + gateWidth * 0.8f, gY, zoneW, gateHeight, 4f, 4f, new Color(1, 1, 1, 0.6f));

        // Middle zones [2] — shorter dash
        DrawDashedRect(gX + gateWidth * 0.2f, gY, zoneW, gateHeight, 2f, 2f, new Color(1, 1, 1, 0.7f));
        DrawDashedRect(gX + gateWidth * 0.6f, gY, zoneW, gateHeight, 2f, 2f, new Color(1, 1, 1, 0.7f));

        // Center zone [3] — solid
        DrawRect(gX + gateWidth * 0.4f, gY, zoneW, gateHeight, Color.white);

        // Pulsing center fill
        float pulse = (Mathf.Sin(Time.time * 5f) + 1f) * 0.15f;
        DrawFilledRect(gX + gateWidth * 0.4f, gY, zoneW, gateHeight, new Color(1, 1, 1, pulse));

        // Bracket corners
        float bSize = 8f;
        DrawBracketCorner(gX, gY + gateHeight, bSize, true, true);                   // Top-left
        DrawBracketCorner(gX + gateWidth, gY + gateHeight, bSize, false, true);       // Top-right
        DrawBracketCorner(gX, gY, bSize, true, false);                                // Bottom-left
        DrawBracketCorner(gX + gateWidth, gY, bSize, false, false);                   // Bottom-right

        GL.PopMatrix();
    }

    void DrawRect(float x, float y, float w, float h, Color color)
    {
        GL.Begin(GL.LINES);
        GL.Color(color);
        // Bottom
        GL.Vertex3(x, y, 0); GL.Vertex3(x + w, y, 0);
        // Top
        GL.Vertex3(x, y + h, 0); GL.Vertex3(x + w, y + h, 0);
        // Left
        GL.Vertex3(x, y, 0); GL.Vertex3(x, y + h, 0);
        // Right
        GL.Vertex3(x + w, y, 0); GL.Vertex3(x + w, y + h, 0);
        GL.End();
    }

    void DrawFilledRect(float x, float y, float w, float h, Color color)
    {
        GL.Begin(GL.QUADS);
        GL.Color(color);
        GL.Vertex3(x, y, 0);
        GL.Vertex3(x + w, y, 0);
        GL.Vertex3(x + w, y + h, 0);
        GL.Vertex3(x, y + h, 0);
        GL.End();
    }

    void DrawDashedRect(float x, float y, float w, float h, float dashLen, float gapLen, Color color)
    {
        GL.Begin(GL.LINES);
        GL.Color(color);

        // Animate dash offset
        float offset = Time.time * 10f;

        // Draw each side with dashes
        DrawDashedLine(x, y, x + w, y, dashLen, gapLen, offset);
        DrawDashedLine(x, y + h, x + w, y + h, dashLen, gapLen, offset);
        DrawDashedLine(x, y, x, y + h, dashLen, gapLen, offset);
        DrawDashedLine(x + w, y, x + w, y + h, dashLen, gapLen, offset);

        GL.End();
    }

    void DrawDashedLine(float x1, float y1, float x2, float y2, float dashLen, float gapLen, float offset)
    {
        float dx = x2 - x1;
        float dy = y2 - y1;
        float length = Mathf.Sqrt(dx * dx + dy * dy);
        if (length < 0.01f) return;

        float nx = dx / length;
        float ny = dy / length;
        float total = dashLen + gapLen;
        float pos = offset % total;

        while (pos < length)
        {
            float start = pos;
            float end = Mathf.Min(pos + dashLen, length);
            if (start < length)
            {
                GL.Vertex3(x1 + nx * start, y1 + ny * start, 0);
                GL.Vertex3(x1 + nx * end, y1 + ny * end, 0);
            }
            pos += total;
        }
    }

    void DrawBracketCorner(float x, float y, float size, bool left, bool top)
    {
        GL.Begin(GL.LINES);
        GL.Color(Color.white);

        float hDir = left ? 1f : -1f;
        float vDir = top ? -1f : 1f;

        // Horizontal arm
        GL.Vertex3(x, y, 0);
        GL.Vertex3(x + hDir * size, y, 0);

        // Vertical arm
        GL.Vertex3(x, y, 0);
        GL.Vertex3(x, y + vDir * size, 0);

        GL.End();
    }
}
