using UnityEngine;
using UnityEngine.Rendering;

public class StarField : MonoBehaviour
{
    private const int STAR_COUNT = 150;
    private const float GAME_WIDTH = 400f;
    private const float GAME_HEIGHT = 800f;

    private Vector2[] starPositions;
    private float[] starSizes;
    private float[] starAlphas;

    private static Material glMaterial;

    void Awake()
    {
        GenerateStars();
    }

    void OnEnable()
    {
        RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
    }

    void OnDisable()
    {
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
    }

    void GenerateStars()
    {
        starPositions = new Vector2[STAR_COUNT];
        starSizes = new float[STAR_COUNT];
        starAlphas = new float[STAR_COUNT];

        for (int i = 0; i < STAR_COUNT; i++)
        {
            starPositions[i] = new Vector2(Random.value * GAME_WIDTH, Random.value * GAME_HEIGHT);
            starSizes[i] = Random.value * 1.5f;
            starAlphas[i] = Random.value * 0.8f + 0.2f;
        }
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
        if (starPositions == null) return;
        if (cam != Camera.main) return;

        EnsureGLMaterial();
        glMaterial.SetPass(0);

        GL.PushMatrix();
        GL.LoadProjectionMatrix(cam.projectionMatrix);
        GL.modelview = cam.worldToCameraMatrix;

        GL.Begin(GL.QUADS);
        for (int i = 0; i < STAR_COUNT; i++)
        {
            GL.Color(new Color(1, 1, 1, starAlphas[i]));
            float x = starPositions[i].x;
            float y = starPositions[i].y;
            float s = starSizes[i];
            GL.Vertex3(x, y, 0);
            GL.Vertex3(x + s, y, 0);
            GL.Vertex3(x + s, y + s, 0);
            GL.Vertex3(x, y + s, 0);
        }
        GL.End();

        GL.PopMatrix();
    }
}
