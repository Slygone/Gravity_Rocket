using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public class GameManager : MonoBehaviour
{
    public enum GameState { MENU, AIMING, FLYING, GAMEOVER, LEVEL_COMPLETE }

    public static GameManager Instance { get; private set; }

    [Header("References (auto-created if null)")]
    public RocketController rocketController;
    public GravitySource[] gravitySources;
    public DockingGate dockingGate;

    [Header("Level Data")]
    public Vector2 rocketStartPos = new Vector2(200f, 60f);

    public GameState State { get; private set; } = GameState.MENU;

    // UI References (created at runtime)
    private Canvas canvas;
    private GameObject menuPanel;
    private GameObject gameOverPanel;
    private GameObject levelCompletePanel;
    private Text starsDisplayText;
    private Text hudText;
    private Text instructionText;

    private int starsEarned = 0;
    private int totalStars = 0;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoBootstrap()
    {
        if (FindFirstObjectByType<GameManager>() == null)
        {
            GameObject go = new GameObject("GameManager");
            go.AddComponent<GameManager>();
        }
    }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        SetupScene();
    }

    void Start()
    {
        CreateUI();
        ShowMenu();
    }

    void SetupScene()
    {
        // Configure camera
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(200f, 400f, -10f);
            cam.orthographic = true;
            cam.orthographicSize = 400f;
            cam.backgroundColor = Color.black;
            cam.clearFlags = CameraClearFlags.SolidColor;
        }

        // Ensure EventSystem exists
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<InputSystemUIInputModule>();
        }

        // Create StarField
        if (FindFirstObjectByType<StarField>() == null)
        {
            GameObject sfObj = new GameObject("StarField");
            sfObj.AddComponent<StarField>();
        }

        // Create Rocket
        if (rocketController == null)
        {
            GameObject rocketObj = new GameObject("Rocket");
            rocketController = rocketObj.AddComponent<RocketController>();
        }

        // Create Planets (Level 1 tutorial layout)
        if (gravitySources == null || gravitySources.Length == 0)
        {
            gravitySources = new GravitySource[2];

            GameObject p1 = new GameObject("Planet1");
            p1.transform.position = new Vector3(80f, 400f, 0f);
            GravitySource gs1 = p1.AddComponent<GravitySource>();
            gs1.mass = 100f;
            gs1.radius = 30f;
            gravitySources[0] = gs1;

            GameObject p2 = new GameObject("Planet2");
            p2.transform.position = new Vector3(320f, 400f, 0f);
            GravitySource gs2 = p2.AddComponent<GravitySource>();
            gs2.mass = 100f;
            gs2.radius = 30f;
            gravitySources[1] = gs2;
        }

        // Create Docking Gate
        if (dockingGate == null)
        {
            GameObject gateObj = new GameObject("DockingGate");
            gateObj.transform.position = new Vector3(200f, 745f, 0f);
            dockingGate = gateObj.AddComponent<DockingGate>();
            dockingGate.gateWidth = 160f;
            dockingGate.gateHeight = 30f;
        }

        // Disable the 2D global light if present (it's a URP Light2D, find by name)
        GameObject globalLight = GameObject.Find("Global Light 2D");
        if (globalLight != null)
            globalLight.SetActive(false);
    }

    public void SetState(GameState newState)
    {
        State = newState;
    }

    public void ShowMenu()
    {
        CancelInvoke();
        State = GameState.MENU;
        HideAllPanels();
        menuPanel.SetActive(true);
        if (instructionText != null) instructionText.gameObject.SetActive(false);
        if (rocketController != null) rocketController.gameObject.SetActive(false);
        SetWorldObjectsVisible(false);
    }

    public void StartGame()
    {
        totalStars = 0;
        LoadLevel();
    }

    public void LoadLevel()
    {
        CancelInvoke();
        HideAllPanels();
        State = GameState.AIMING;
        starsEarned = 0;

        SetWorldObjectsVisible(true);
        rocketController.gameObject.SetActive(true);
        rocketController.ResetRocket(rocketStartPos);

        if (instructionText != null)
        {
            instructionText.gameObject.SetActive(true);
            instructionText.text = "DRAG TO PLOT TRAJECTORY";
        }

        UpdateHUD();
    }

    public void OnRocketLaunched()
    {
        State = GameState.FLYING;
        if (instructionText != null) instructionText.gameObject.SetActive(false);
    }

    public void OnGameOver()
    {
        State = GameState.GAMEOVER;
        if (instructionText != null) instructionText.gameObject.SetActive(false);

        // Hide rocket mesh after explosion (particles still update)
        if (rocketController != null)
        {
            MeshRenderer mr = rocketController.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = false;
        }

        Invoke(nameof(ShowGameOverPanel), 0.8f);
    }

    public void OnLevelComplete(float hitRatio)
    {
        State = GameState.LEVEL_COMPLETE;
        if (instructionText != null) instructionText.gameObject.SetActive(false);

        if (hitRatio > 0.4f && hitRatio < 0.6f) starsEarned = 3;
        else if (hitRatio > 0.2f && hitRatio < 0.8f) starsEarned = 2;
        else starsEarned = 1;

        totalStars += starsEarned;

        Invoke(nameof(ShowLevelCompletePanel), 1.5f);
    }

    void ShowGameOverPanel()
    {
        HideAllPanels();
        SetWorldObjectsVisible(false);
        gameOverPanel.SetActive(true);
    }

    void ShowLevelCompletePanel()
    {
        HideAllPanels();
        SetWorldObjectsVisible(false);
        levelCompletePanel.SetActive(true);

        string starText = "";
        for (int i = 0; i < 3; i++)
            starText += (i < starsEarned) ? "\u2605" : "\u2606";
        if (starsDisplayText != null)
            starsDisplayText.text = starText;

        UpdateHUD();
    }

    void HideAllPanels()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (levelCompletePanel != null) levelCompletePanel.SetActive(false);
    }

    void UpdateHUD()
    {
        if (hudText != null)
            hudText.text = "U1 - SEC 1 - LVL 1\n\u2605 " + totalStars;
    }

    void SetWorldObjectsVisible(bool visible)
    {
        if (gravitySources != null)
        {
            foreach (var gs in gravitySources)
                if (gs != null) gs.gameObject.SetActive(visible);
        }
        if (dockingGate != null) dockingGate.gameObject.SetActive(visible);
    }

    // ==================== UI CREATION ====================
    void CreateUI()
    {
        // Canvas — use 1080x1920 reference for crisp Full HD text
        GameObject canvasObj = new GameObject("UICanvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        // HUD
        hudText = CreateText(canvasObj.transform, "HUD", "U1 - SEC 1 - LVL 1\n\u2605 0",
            TextAnchor.UpperLeft, new Vector2(50, -50), new Vector2(800, 160), 42);
        hudText.rectTransform.anchorMin = new Vector2(0, 1);
        hudText.rectTransform.anchorMax = new Vector2(0, 1);
        hudText.rectTransform.pivot = new Vector2(0, 1);

        // Instruction
        instructionText = CreateText(canvasObj.transform, "Instruction", "DRAG TO PLOT TRAJECTORY",
            TextAnchor.MiddleCenter, new Vector2(0, -800), new Vector2(1000, 100), 36);
        instructionText.color = new Color(1, 1, 1, 0.5f);
        instructionText.gameObject.SetActive(false);

        // Menu Panel
        menuPanel = CreatePanel(canvasObj.transform, "MenuPanel");
        CreateText(menuPanel.transform, "Title", "GRAVITY\nROCKET",
            TextAnchor.MiddleCenter, new Vector2(0, 160), new Vector2(750, 260), 84);
        CreateText(menuPanel.transform, "Subtitle", "Monochrome Edition\nHit dead center for 3 stars.",
            TextAnchor.MiddleCenter, new Vector2(0, -20), new Vector2(750, 130), 36).color = new Color(0.8f, 0.8f, 0.8f);
        CreateButton(menuPanel.transform, "StartBtn", "INITIATE", new Vector2(0, -190), () => StartGame());

        // Game Over Panel
        gameOverPanel = CreatePanel(canvasObj.transform, "GameOverPanel");
        CreateText(gameOverPanel.transform, "GOTitle", "SIGNAL LOST",
            TextAnchor.MiddleCenter, new Vector2(0, 110), new Vector2(750, 110), 58);
        CreateText(gameOverPanel.transform, "GODesc", "Trajectory compromised.",
            TextAnchor.MiddleCenter, new Vector2(0, 10), new Vector2(750, 80), 36).color = new Color(0.8f, 0.8f, 0.8f);
        CreateButton(gameOverPanel.transform, "RetryBtn", "RECALCULATE", new Vector2(0, -130), () => LoadLevel());
        gameOverPanel.SetActive(false);

        // Level Complete Panel
        levelCompletePanel = CreatePanel(canvasObj.transform, "LevelCompletePanel");
        CreateText(levelCompletePanel.transform, "LCTitle", "DOCKING SUCCESSFUL",
            TextAnchor.MiddleCenter, new Vector2(0, 160), new Vector2(750, 110), 58);
        starsDisplayText = CreateText(levelCompletePanel.transform, "Stars", "\u2606\u2606\u2606",
            TextAnchor.MiddleCenter, new Vector2(0, 30), new Vector2(750, 150), 108);
        CreateButton(levelCompletePanel.transform, "ReplayBtn", "REPLAY LEVEL", new Vector2(0, -140), () => LoadLevel());
        levelCompletePanel.SetActive(false);
    }

    GameObject CreatePanel(Transform parent, string name)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(800, 650);
        rt.anchoredPosition = Vector2.zero;

        Image img = panel.GetComponent<Image>();
        img.color = new Color(0, 0, 0, 0.9f);

        // Border via Outline
        Outline outline = panel.AddComponent<Outline>();
        outline.effectColor = Color.white;
        outline.effectDistance = new Vector2(2, 2);

        return panel;
    }

    Text CreateText(Transform parent, string name, string content, TextAnchor alignment, Vector2 pos, Vector2 size, int fontSize)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        Text txt = obj.AddComponent<Text>();
        txt.text = content;
        txt.font = Font.CreateDynamicFontFromOSFont("Courier New", fontSize * 2);
        txt.fontSize = fontSize;
        txt.alignment = alignment;
        txt.color = Color.white;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;

        return txt;
    }

    void CreateButton(Transform parent, string name, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(580, 120);
        rt.anchoredPosition = pos;

        Image img = btnObj.GetComponent<Image>();
        img.color = new Color(0, 0, 0, 0.8f);

        Outline outline = btnObj.AddComponent<Outline>();
        outline.effectColor = Color.white;
        outline.effectDistance = new Vector2(2, 2);

        Button btn = btnObj.GetComponent<Button>();
        btn.onClick.AddListener(onClick);

        ColorBlock cb = btn.colors;
        cb.normalColor = new Color(0, 0, 0, 0.8f);
        cb.highlightedColor = new Color(1, 1, 1, 1f);
        cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        btn.colors = cb;

        Text txt = CreateText(btnObj.transform, "Label", label,
            TextAnchor.MiddleCenter, Vector2.zero, new Vector2(540, 100), 42);
        txt.raycastTarget = false;
    }
}
