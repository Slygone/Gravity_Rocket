using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class RocketController : MonoBehaviour
{
    // Constants matching original game
    private float G = 50f;
    private const float LAUNCH_SPEED = 9f;
    private const float ROCKET_RADIUS = 8f;
    private const float GAME_WIDTH = 400f;
    private const float GAME_HEIGHT = 800f;
    private const float BOUNDS_MARGIN = 20f;

    // Rocket state
    private Vector2 velocity;
    private Vector2 acceleration;
    private float angle;
    private bool isDragging = false;
    private Vector2 dragCurrent;
    private float aimAngle;

    // Trail
    private List<TrailPoint> trail = new List<TrailPoint>();
    private LineRenderer trailLine0;
    private LineRenderer trailLine1;
    private LineRenderer trailLine2;
    private const float ABERRATION_MULT = 12f;
    private const int MAX_TRAIL = 100;

    // Configurable trail colors
    private Color[] trailColors = new Color[] { Color.red, Color.green, Color.blue };
    private float[] trailMults = new float[] { ABERRATION_MULT, 0f, -ABERRATION_MULT };
    private Color shipHexColor = Color.white;

    // Aim line
    private LineRenderer aimLine;

    // Particles
    private List<Particle> particles = new List<Particle>();
    private List<Pulse> pulses = new List<Pulse>();

    // Rocket mesh
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    // Warp animation
    private float warpTimer;
    private float warpX;

    // Camera reference for screen-to-world
    private Camera mainCam;

    struct TrailPoint
    {
        public Vector2 pos;
        public Vector2 accel;
    }

    struct Particle
    {
        public Vector2 pos;
        public Vector2 vel;
        public Vector2 accel;
        public float life;
    }

    struct Pulse
    {
        public Vector2 pos;
        public float radius;
        public float life;
    }

    void Awake()
    {
        mainCam = Camera.main;
        CreateRocketMesh();
        CreateTrailRenderers();
        CreateAimLine();
    }

    public void SetGravityConstant(float g) { G = g; }

    public void SetTrailColors(string trailId, string shipId)
    {
        if (GameData.TRAILS.ContainsKey(trailId))
        {
            var trailDef = GameData.TRAILS[trailId];
            if (GameData.SHIPS.ContainsKey(shipId))
                shipHexColor = GameData.SHIPS[shipId].hex;

            if (trailDef.isChroma)
            {
                trailColors = new Color[] { trailDef.hex[0], trailDef.hex[1], trailDef.hex[2] };
                trailMults = new float[] { ABERRATION_MULT, 0f, -ABERRATION_MULT };
            }
            else
            {
                trailColors = new Color[] { shipHexColor, shipHexColor, shipHexColor };
                trailMults = new float[] { 3f, 0f, -3f };
            }

            UpdateTrailRendererColors();

            // Update rocket mesh color to match ship
            if (meshRenderer != null && meshRenderer.material != null)
                meshRenderer.material.color = shipHexColor;
        }
    }

    void UpdateTrailRendererColors()
    {
        if (trailLine0 != null) { trailLine0.startColor = new Color(trailColors[0].r, trailColors[0].g, trailColors[0].b, 0f); trailLine0.endColor = trailColors[0]; }
        if (trailLine1 != null) { trailLine1.startColor = new Color(trailColors[1].r, trailColors[1].g, trailColors[1].b, 0f); trailLine1.endColor = trailColors[1]; }
        if (trailLine2 != null) { trailLine2.startColor = new Color(trailColors[2].r, trailColors[2].g, trailColors[2].b, 0f); trailLine2.endColor = trailColors[2]; }
    }

    void OnEnable()
    {
        RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
    }

    void OnDisable()
    {
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
    }

    public void ResetRocket(Vector2 startPos)
    {
        transform.position = new Vector3(startPos.x, startPos.y, 0);
        velocity = Vector2.zero;
        acceleration = Vector2.zero;
        angle = 90f * Mathf.Deg2Rad; // Pointing up in Unity coords
        aimAngle = angle;
        isDragging = false;
        warpTimer = 0;

        trail.Clear();
        particles.Clear();
        pulses.Clear();

        ClearTrailRenderers();
        if (aimLine != null) aimLine.positionCount = 0;

        // Re-enable mesh renderer (may have been hidden on game over)
        if (meshRenderer != null) meshRenderer.enabled = true;

        UpdateRocketRotation();
    }

    void Update()
    {
        if (GameManager.Instance == null) return;
        var state = GameManager.Instance.State;

        if (state == GameManager.GameState.AIMING)
        {
            HandleAimInput();
        }
        else if (state == GameManager.GameState.FLYING)
        {
            UpdatePhysics();
            CheckCollisions();
            SpawnExhaustParticle();

            // Nebula hull damage
            if (GameManager.Instance.IsNebulaLevel())
            {
                GameManager.Instance.DamageHull(0.5f * Time.deltaTime * 60f);
            }
        }
        else if (state == GameManager.GameState.LEVEL_COMPLETE)
        {
            UpdateWarpAnimation();
        }

        UpdateParticles();
        UpdatePulses();
    }

    void LateUpdate()
    {
        DrawTrail();
    }

    // ==================== INPUT ====================
    void HandleAimInput()
    {
        // Block input if fleet panel is open
        if (UIManager.Instance != null && UIManager.Instance.IsFleetPanelOpen) return;

        // Touch input (mobile)
        if (UnityEngine.InputSystem.Touchscreen.current != null)
        {
            var touch = UnityEngine.InputSystem.Touchscreen.current;
            if (touch.primaryTouch.press.wasPressedThisFrame)
            {
                Vector2 worldPos = ScreenToWorld(touch.primaryTouch.position.ReadValue());
                if (UnityEngine.EventSystems.EventSystem.current == null || !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    isDragging = true;
                    UpdateAim(worldPos);
                }
            }
            if (touch.primaryTouch.press.isPressed && isDragging)
            {
                UpdateAim(ScreenToWorld(touch.primaryTouch.position.ReadValue()));
            }
            if (touch.primaryTouch.press.wasReleasedThisFrame && isDragging)
            {
                isDragging = false;
                LaunchRocket();
            }
        }

        // Mouse input (editor/desktop)
        var mouse = Mouse.current;
        if (mouse != null)
        {
            if (mouse.leftButton.wasPressedThisFrame)
            {
                Vector2 worldPos = GetMouseWorldPos();
                if (UnityEngine.EventSystems.EventSystem.current == null || !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    isDragging = true;
                    UpdateAim(worldPos);
                }
            }

            if (mouse.leftButton.isPressed && isDragging)
            {
                Vector2 worldPos = GetMouseWorldPos();
                UpdateAim(worldPos);
            }

            if (mouse.leftButton.wasReleasedThisFrame && isDragging)
            {
                isDragging = false;
                LaunchRocket();
            }
        }

        // Draw aim line
        if (isDragging && aimLine != null)
        {
            Vector3 rocketPos = transform.position;
            aimLine.positionCount = 2;
            aimLine.SetPosition(0, rocketPos);
            aimLine.SetPosition(1, new Vector3(dragCurrent.x, dragCurrent.y, 0));
        }
        else if (aimLine != null)
        {
            aimLine.positionCount = 0;
        }
    }

    Vector2 ScreenToWorld(Vector2 screenPos)
    {
        Vector3 mousePos = new Vector3(screenPos.x, screenPos.y, Mathf.Abs(mainCam.transform.position.z));
        Vector3 worldPos = mainCam.ScreenToWorldPoint(mousePos);
        return new Vector2(worldPos.x, worldPos.y);
    }

    Vector2 GetMouseWorldPos()
    {
        var mouse = Mouse.current;
        if (mouse == null) return Vector2.zero;
        return ScreenToWorld(mouse.position.ReadValue());
    }

    void UpdateAim(Vector2 worldPos)
    {
        dragCurrent = worldPos;
        Vector2 rocketPos = transform.position;
        Vector2 dir = worldPos - rocketPos;
        aimAngle = Mathf.Atan2(dir.y, dir.x);
        angle = aimAngle;
        UpdateRocketRotation();
    }

    void LaunchRocket()
    {
        velocity = new Vector2(Mathf.Cos(aimAngle), Mathf.Sin(aimAngle)) * LAUNCH_SPEED;
        if (aimLine != null) aimLine.positionCount = 0;
        GameManager.Instance.OnRocketLaunched();
    }

    // ==================== PHYSICS ====================
    void UpdatePhysics()
    {
        float dt = Time.deltaTime * 60f; // Normalize to 60fps

        float ax = 0, ay = 0;
        GravitySource[] sources = GameManager.Instance.gravitySources;

        for (int i = 0; i < sources.Length; i++)
        {
            GravitySource p = sources[i];
            Vector2 planetPos = p.GetPosition();
            float dx = planetPos.x - transform.position.x;
            float dy = planetPos.y - transform.position.y;
            float distSq = dx * dx + dy * dy;
            float dist = Mathf.Sqrt(distSq);

            // Planet collision
            if (dist < p.radius + ROCKET_RADIUS)
            {
                CreateExplosion(transform.position);
                GameManager.Instance.OnGameOver();
                return;
            }

            float force = (G * p.mass) / distSq;
            ax += force * (dx / dist);
            ay += force * (dy / dist);
        }

        acceleration = new Vector2(ax, ay);

        velocity += acceleration * dt;
        Vector3 pos = transform.position;
        pos.x += velocity.x * dt;
        pos.y += velocity.y * dt;
        transform.position = pos;

        angle = Mathf.Atan2(velocity.y, velocity.x);
        UpdateRocketRotation();

        // Trail
        if (trail.Count == 0 || Vector2.Distance(trail[trail.Count - 1].pos, (Vector2)transform.position) > 4f)
        {
            trail.Add(new TrailPoint { pos = transform.position, accel = acceleration });
            if (trail.Count > MAX_TRAIL) trail.RemoveAt(0);
        }
    }

    void CheckCollisions()
    {
        Vector2 rocketPos = transform.position;

        // Out of bounds
        if (rocketPos.x < -BOUNDS_MARGIN || rocketPos.x > GAME_WIDTH + BOUNDS_MARGIN ||
            rocketPos.y < -BOUNDS_MARGIN || rocketPos.y > GAME_HEIGHT + BOUNDS_MARGIN)
        {
            GameManager.Instance.OnGameOver();
            return;
        }

        // Goal collision (Circle vs AABB)
        DockingGate gate = GameManager.Instance.dockingGate;
        if (gate != null)
        {
            float gateLeft = gate.GetLeft();
            float gateBottom = gate.GetBottom();
            float gateRight = gateLeft + gate.gateWidth;
            float gateTop = gateBottom + gate.gateHeight;

            float closestX = Mathf.Clamp(rocketPos.x, gateLeft, gateRight);
            float closestY = Mathf.Clamp(rocketPos.y, gateBottom, gateTop);

            float dxGoal = rocketPos.x - closestX;
            float dyGoal = rocketPos.y - closestY;

            if (dxGoal * dxGoal + dyGoal * dyGoal < ROCKET_RADIUS * ROCKET_RADIUS)
            {
                float hitRatio = (closestX - gateLeft) / gate.gateWidth;
                warpX = rocketPos.x;
                warpTimer = 0;
                GameManager.Instance.OnLevelComplete(hitRatio);

                // Create success pulse
                Vector2 gateCenter = new Vector2(gateLeft + gate.gateWidth * 0.5f, gateBottom + gate.gateHeight * 0.5f);
                pulses.Add(new Pulse { pos = new Vector2(warpX, gateCenter.y), radius = 5, life = 1f });
                return;
            }
        }
    }

    // ==================== WARP ANIMATION ====================
    void UpdateWarpAnimation()
    {
        float dt = Time.deltaTime * 60f;
        warpTimer += dt;

        DockingGate gate = GameManager.Instance.dockingGate;
        if (gate == null) return;

        float gateBottom = gate.GetBottom();
        float gateCenterY = gateBottom + gate.gateHeight * 0.5f;

        if (warpTimer < 30f)
        {
            Vector3 pos = transform.position;
            float targetX = warpX;
            float targetY = gateCenterY;
            pos.x += (targetX - pos.x) * 0.15f * (dt / 1f);
            pos.y += (targetY - pos.y) * 0.15f * (dt / 1f);
            transform.position = pos;

            float targetAngle = Mathf.PI / 2f; // Point up
            float da = targetAngle - angle;
            while (da > Mathf.PI) da -= Mathf.PI * 2f;
            while (da < -Mathf.PI) da += Mathf.PI * 2f;
            angle += da * 0.15f * (dt / 1f);
            UpdateRocketRotation();

            acceleration *= 0.6f;
        }
        else
        {
            Vector3 pos = transform.position;
            pos.y += (warpTimer - 20f) * 1.5f * (dt / 1f);
            transform.position = pos;

            // Warp exhaust particles
            if (Random.value > 0.2f)
            {
                particles.Add(new Particle
                {
                    pos = (Vector2)transform.position + new Vector2((Random.value - 0.5f) * 8f, -10f),
                    vel = new Vector2(0, -(Random.value * 3f + 2f)),
                    accel = Vector2.zero,
                    life = 0.8f
                });
            }
        }
    }

    // ==================== PARTICLES ====================
    void SpawnExhaustParticle()
    {
        if (Random.value > 0.4f)
        {
            Vector2 rocketPos = transform.position;
            particles.Add(new Particle
            {
                pos = rocketPos - new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 10f,
                vel = -velocity * 0.15f + new Vector2((Random.value - 0.5f), (Random.value - 0.5f)),
                accel = acceleration,
                life = 1f
            });
        }
    }

    void UpdateParticles()
    {
        float dt = Time.deltaTime * 60f;
        for (int i = particles.Count - 1; i >= 0; i--)
        {
            Particle p = particles[i];
            p.pos += p.vel * dt;
            p.life -= 0.025f * dt;
            particles[i] = p;
            if (p.life <= 0) particles.RemoveAt(i);
        }
    }

    void UpdatePulses()
    {
        float dt = Time.deltaTime * 60f;
        for (int i = pulses.Count - 1; i >= 0; i--)
        {
            Pulse p = pulses[i];
            p.radius += 6f * dt;
            p.life -= 0.03f * dt;
            pulses[i] = p;
            if (p.life <= 0) pulses.RemoveAt(i);
        }
    }

    public void CreateExplosion(Vector2 pos)
    {
        for (int i = 0; i < 40; i++)
        {
            float a = Random.value * Mathf.PI * 2f;
            float speed = Random.value * 5f + 1f;
            particles.Add(new Particle
            {
                pos = pos,
                vel = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * speed,
                accel = new Vector2(Mathf.Cos(a), Mathf.Sin(a)),
                life = 1f
            });
        }
    }

    // ==================== RENDERING ====================
    void CreateRocketMesh()
    {
        meshFilter = gameObject.AddComponent<MeshFilter>();
        meshRenderer = gameObject.AddComponent<MeshRenderer>();

        Mesh mesh = new Mesh();
        // Triangle rocket shape (pointing right, rotated by transform)
        // Original: tip at (12,0), left-back at (-8,6), notch at (-4,0), right-back at (-8,-6)
        Vector3[] verts = new Vector3[]
        {
            new Vector3(12, 0, 0),
            new Vector3(-8, 6, 0),
            new Vector3(-4, 0, 0),
            new Vector3(-8, -6, 0)
        };
        int[] tris = new int[] { 0, 1, 2, 0, 2, 3 };
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;

        // White unlit material
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = Color.white;
        meshRenderer.material = mat;
        meshRenderer.sortingOrder = 10;
    }

    void UpdateRocketRotation()
    {
        float deg = angle * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, deg);
    }

    void CreateTrailRenderers()
    {
        trailLine0 = CreateTrailLine("Trail0", trailColors[0]);
        trailLine1 = CreateTrailLine("Trail1", trailColors[1]);
        trailLine2 = CreateTrailLine("Trail2", trailColors[2]);
    }

    LineRenderer CreateTrailLine(string name, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(transform.parent);
        LineRenderer lr = obj.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = new Color(color.r, color.g, color.b, 0f);
        lr.endColor = color;
        lr.startWidth = 1.5f;
        lr.endWidth = 1.5f;
        lr.positionCount = 0;
        lr.sortingOrder = 5;
        lr.useWorldSpace = true;
        return lr;
    }

    void ClearTrailRenderers()
    {
        if (trailLine0 != null) trailLine0.positionCount = 0;
        if (trailLine1 != null) trailLine1.positionCount = 0;
        if (trailLine2 != null) trailLine2.positionCount = 0;
    }

    void DrawTrail()
    {
        if (trail.Count < 2) return;

        DrawAberratedTrail(trailLine0, trailMults[0]);
        DrawAberratedTrail(trailLine1, trailMults[1]);
        DrawAberratedTrail(trailLine2, trailMults[2]);
    }

    void DrawAberratedTrail(LineRenderer lr, float mult)
    {
        if (lr == null) return;
        lr.positionCount = trail.Count;
        for (int i = 0; i < trail.Count; i++)
        {
            TrailPoint pt = trail[i];
            lr.SetPosition(i, new Vector3(pt.pos.x + pt.accel.x * mult, pt.pos.y + pt.accel.y * mult, 0));
        }
    }

    void CreateAimLine()
    {
        GameObject obj = new GameObject("AimLine");
        obj.transform.SetParent(transform.parent);
        aimLine = obj.AddComponent<LineRenderer>();
        aimLine.material = new Material(Shader.Find("Sprites/Default"));
        aimLine.startColor = new Color(1, 1, 1, 0.4f);
        aimLine.endColor = new Color(1, 1, 1, 0.4f);
        aimLine.startWidth = 1f;
        aimLine.endWidth = 1f;
        aimLine.positionCount = 0;
        aimLine.sortingOrder = 8;
        aimLine.useWorldSpace = true;
    }

    // GL Drawing for particles, pulses, aim circle, and flame
    static Material glMaterial;

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

    void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (camera != mainCam) return;

        EnsureGLMaterial();
        glMaterial.SetPass(0);

        var state = GameManager.Instance != null ? GameManager.Instance.State : GameManager.GameState.MENU;

        // Draw particles with trail colors
        GL.PushMatrix();
        GL.LoadProjectionMatrix(mainCam.projectionMatrix);
        GL.modelview = mainCam.worldToCameraMatrix;

        GL.Begin(GL.QUADS);
        foreach (var p in particles)
        {
            float alpha = Mathf.Max(0, p.life);

            for (int ch = 0; ch < 3; ch++)
            {
                GL.Color(new Color(trailColors[ch].r, trailColors[ch].g, trailColors[ch].b, alpha));
                DrawGLQuad(p.pos + p.accel * trailMults[ch] * p.life, 2f);
            }
        }
        GL.End();

        // Draw pulses
        foreach (var p in pulses)
        {
            GL.Begin(GL.LINES);
            GL.Color(new Color(1, 1, 1, p.life));
            int segments = 32;
            for (int i = 0; i < segments; i++)
            {
                float a1 = (float)i / segments * Mathf.PI * 2f;
                float a2 = (float)(i + 1) / segments * Mathf.PI * 2f;
                GL.Vertex3(p.pos.x + Mathf.Cos(a1) * p.radius, p.pos.y + Mathf.Sin(a1) * p.radius, 0);
                GL.Vertex3(p.pos.x + Mathf.Cos(a2) * p.radius, p.pos.y + Mathf.Sin(a2) * p.radius, 0);
            }
            GL.End();
        }

        // Draw aim circle
        if (state == GameManager.GameState.AIMING)
        {
            GL.Begin(GL.LINES);
            GL.Color(new Color(1, 1, 1, 0.3f));
            Vector2 rPos = transform.position;
            int segs = 24;
            for (int i = 0; i < segs; i += 2) // Dashed circle
            {
                float a1 = (float)i / segs * Mathf.PI * 2f;
                float a2 = (float)(i + 1) / segs * Mathf.PI * 2f;
                GL.Vertex3(rPos.x + Mathf.Cos(a1) * 20f, rPos.y + Mathf.Sin(a1) * 20f, 0);
                GL.Vertex3(rPos.x + Mathf.Cos(a2) * 20f, rPos.y + Mathf.Sin(a2) * 20f, 0);
            }
            GL.End();
        }

        // Draw flame with trail colors (chromatic channels)
        if (state == GameManager.GameState.FLYING ||
            (state == GameManager.GameState.LEVEL_COMPLETE && warpTimer > 25f))
        {
            float flameLen = (state == GameManager.GameState.LEVEL_COMPLETE) ?
                25f + Random.value * 15f : 15f + Random.value * 5f;
            Vector2 rPos = transform.position;
            Vector2 back = new Vector2(-Mathf.Cos(angle), -Mathf.Sin(angle));

            for (int ch = 0; ch < 3; ch++)
            {
                Vector2 offset = acceleration * trailMults[ch];
                Vector2 flameStart = rPos + offset + back * 4f;
                Vector2 flameEnd = rPos + offset + back * flameLen;

                GL.Begin(GL.LINES);
                GL.Color(trailColors[ch]);
                GL.Vertex3(flameStart.x, flameStart.y, 0);
                GL.Vertex3(flameEnd.x, flameEnd.y, 0);
                GL.End();
            }
        }

        // Draw aim line crosshair at drag point
        if (state == GameManager.GameState.AIMING && isDragging)
        {
            GL.Begin(GL.LINES);
            GL.Color(new Color(1, 1, 1, 0.4f));
            int segs2 = 16;
            for (int i = 0; i < segs2; i++)
            {
                float a1 = (float)i / segs2 * Mathf.PI * 2f;
                float a2 = (float)(i + 1) / segs2 * Mathf.PI * 2f;
                GL.Vertex3(dragCurrent.x + Mathf.Cos(a1) * 6f, dragCurrent.y + Mathf.Sin(a1) * 6f, 0);
                GL.Vertex3(dragCurrent.x + Mathf.Cos(a2) * 6f, dragCurrent.y + Mathf.Sin(a2) * 6f, 0);
            }
            GL.End();
        }

        // Draw nebula overlay
        if (GameManager.Instance != null && GameManager.Instance.IsNebulaLevel())
        {
            float nebulaAlpha = 0.1f + Mathf.Sin(Time.time) * 0.05f;
            GL.Begin(GL.QUADS);
            GL.Color(new Color(0.39f, 0f, 0.59f, nebulaAlpha));
            GL.Vertex3(0, 0, 0);
            GL.Vertex3(GAME_WIDTH, 0, 0);
            GL.Vertex3(GAME_WIDTH, GAME_HEIGHT, 0);
            GL.Vertex3(0, GAME_HEIGHT, 0);
            GL.End();

            // Hull damage red overlay
            float hull = GameManager.Instance.GetHullIntegrity();
            if (hull < 100f)
            {
                float redAlpha = (100f - hull) / 100f * 0.5f;
                GL.Begin(GL.QUADS);
                GL.Color(new Color(1f, 0f, 0f, redAlpha));
                GL.Vertex3(0, 0, 0);
                GL.Vertex3(GAME_WIDTH, 0, 0);
                GL.Vertex3(GAME_WIDTH, GAME_HEIGHT, 0);
                GL.Vertex3(0, GAME_HEIGHT, 0);
                GL.End();
            }
        }

        GL.PopMatrix();
    }

    void DrawGLQuad(Vector2 center, float size)
    {
        float half = size * 0.5f;
        GL.Vertex3(center.x - half, center.y - half, 0);
        GL.Vertex3(center.x + half, center.y - half, 0);
        GL.Vertex3(center.x + half, center.y + half, 0);
        GL.Vertex3(center.x - half, center.y + half, 0);
    }
}
