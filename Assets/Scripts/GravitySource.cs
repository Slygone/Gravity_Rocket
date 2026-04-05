using UnityEngine;
using UnityEngine.Rendering;

public class GravitySource : MonoBehaviour
{
    [Header("Planet Properties")]
    public float mass = 80f;
    public float radius = 20f;

    private static Material glMaterial;

    public Vector2 GetPosition()
    {
        return new Vector2(transform.position.x, transform.position.y);
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

        Vector2 pos = GetPosition();
        int segments = 48;

        // Gravity wave ring (animated, expanding and fading)
        float waveScale = (Time.time % 1f);
        float waveRadius = radius + (mass * 0.2f * waveScale);
        float waveAlpha = 1f - waveScale;

        GL.Begin(GL.LINES);
        GL.Color(new Color(1, 1, 1, waveAlpha));
        for (int i = 0; i < segments; i++)
        {
            float a1 = (float)i / segments * Mathf.PI * 2f;
            float a2 = (float)(i + 1) / segments * Mathf.PI * 2f;
            GL.Vertex3(pos.x + Mathf.Cos(a1) * waveRadius, pos.y + Mathf.Sin(a1) * waveRadius, 0);
            GL.Vertex3(pos.x + Mathf.Cos(a2) * waveRadius, pos.y + Mathf.Sin(a2) * waveRadius, 0);
        }
        GL.End();

        // Planet body (filled black circle)
        GL.Begin(GL.TRIANGLES);
        GL.Color(Color.black);
        for (int i = 0; i < segments; i++)
        {
            float a1 = (float)i / segments * Mathf.PI * 2f;
            float a2 = (float)(i + 1) / segments * Mathf.PI * 2f;
            GL.Vertex3(pos.x, pos.y, 0);
            GL.Vertex3(pos.x + Mathf.Cos(a1) * radius, pos.y + Mathf.Sin(a1) * radius, 0);
            GL.Vertex3(pos.x + Mathf.Cos(a2) * radius, pos.y + Mathf.Sin(a2) * radius, 0);
        }
        GL.End();

        // Planet outline (white circle)
        GL.Begin(GL.LINES);
        GL.Color(Color.white);
        for (int i = 0; i < segments; i++)
        {
            float a1 = (float)i / segments * Mathf.PI * 2f;
            float a2 = (float)(i + 1) / segments * Mathf.PI * 2f;
            GL.Vertex3(pos.x + Mathf.Cos(a1) * radius, pos.y + Mathf.Sin(a1) * radius, 0);
            GL.Vertex3(pos.x + Mathf.Cos(a2) * radius, pos.y + Mathf.Sin(a2) * radius, 0);
        }
        GL.End();

        // Crosshair lines
        float ch = radius * 0.5f;
        GL.Begin(GL.LINES);
        GL.Color(new Color(1, 1, 1, 0.5f));
        GL.Vertex3(pos.x - ch, pos.y, 0);
        GL.Vertex3(pos.x + ch, pos.y, 0);
        GL.Vertex3(pos.x, pos.y - ch, 0);
        GL.Vertex3(pos.x, pos.y + ch, 0);
        GL.End();

        GL.PopMatrix();
    }
}
