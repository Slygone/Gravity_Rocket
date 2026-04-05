using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public enum GameState { MENU, AIMING, FLYING, GAMEOVER, LEVEL_COMPLETE }

    public static GameManager Instance { get; private set; }

    [Header("References (auto-created)")]
    public RocketController rocketController;
    public GravitySource[] gravitySources;
    public DockingGate dockingGate;

    public GameState State { get; private set; } = GameState.MENU;

    // Current session data
    private int currentU, currentS, currentL;
    private int currentGlobalIndex;
    private int starsEarned;
    private string activeShipId = "shipA";
    private GameData.LevelData[] allLevels;

    // Nebula state
    private float hullIntegrity = 100f;
    private bool isNebulaLevel = false;

    // Public accessors for UI
    public int CurrentU => currentU;
    public int CurrentS => currentS;
    public int CurrentL => currentL;

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

        allLevels = GameData.GenerateAllLevels();
        PlayerState.Instance.Load();
        SetupScene();
    }

    void Start()
    {
        // Create UIManager
        GameObject uiObj = new GameObject("UIManager");
        uiObj.transform.SetParent(transform);
        uiObj.AddComponent<UIManager>();
        UIManager.Instance.Initialize();

        ShowMenu();
    }

    void SetupScene()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(200f, 400f, -10f);
            cam.orthographic = true;
            cam.orthographicSize = 400f;
            cam.backgroundColor = Color.black;
            cam.clearFlags = CameraClearFlags.SolidColor;
        }

        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<InputSystemUIInputModule>();
        }

        if (FindFirstObjectByType<StarField>() == null)
        {
            GameObject sfObj = new GameObject("StarField");
            sfObj.AddComponent<StarField>();
        }

        if (rocketController == null)
        {
            GameObject rocketObj = new GameObject("Rocket");
            rocketController = rocketObj.AddComponent<RocketController>();
        }

        // Planets and gate will be created per-level
        gravitySources = new GravitySource[0];

        if (dockingGate == null)
        {
            GameObject gateObj = new GameObject("DockingGate");
            dockingGate = gateObj.AddComponent<DockingGate>();
        }

        GameObject globalLight = GameObject.Find("Global Light 2D");
        if (globalLight != null)
            globalLight.SetActive(false);
    }

    // ==================== PUBLIC API ====================
    public string GetActiveShipId() => activeShipId;

    public void SetActiveShipId(string id)
    {
        activeShipId = id;
        if (rocketController != null)
            rocketController.SetGravityConstant(GameData.GetGravityForShip(id));
    }

    public float GetHullIntegrity() => hullIntegrity;
    public bool IsNebulaLevel() => isNebulaLevel;

    public void DamageHull(float amount)
    {
        if (activeShipId == "shipC") return; // Nebula Piercer immune
        hullIntegrity -= amount;
        if (hullIntegrity <= 0)
        {
            hullIntegrity = 0;
            OnGameOver();
        }
    }

    public void ShowMenu()
    {
        CancelInvoke();
        State = GameState.MENU;
        SetWorldObjectsVisible(false);
        if (rocketController != null) rocketController.gameObject.SetActive(false);
        if (UIManager.Instance != null) UIManager.Instance.ShowMenuUI();
        PlayerState.Instance.Save();
    }

    public void LaunchLevel(int u, int s, int l, bool isMenuLaunch)
    {
        currentU = u;
        currentS = s;
        currentL = l;
        currentGlobalIndex = GameData.ToGlobalIndex(u, s, l);

        if (isMenuLaunch)
            PlayerState.Instance.GlobalShields = PlayerState.Instance.MaxShields;

        activeShipId = PlayerState.Instance.ActiveShips[0];

        // Determine sector mechanics
        string[] mechanics = GameData.GetSectorMechanics(s);
        isNebulaLevel = System.Array.IndexOf(mechanics, "nebula") >= 0;
        hullIntegrity = 100f;

        SetupLevelGeometry();
        StartLevel();
    }

    void SetupLevelGeometry()
    {
        if (currentGlobalIndex >= allLevels.Length) return;
        GameData.LevelData lvl = allLevels[currentGlobalIndex];

        // Destroy old planets
        if (gravitySources != null)
        {
            foreach (var gs in gravitySources)
                if (gs != null) Destroy(gs.gameObject);
        }

        // Create new planets
        gravitySources = new GravitySource[lvl.planets.Length];
        for (int i = 0; i < lvl.planets.Length; i++)
        {
            var pd = lvl.planets[i];
            GameObject pObj = new GameObject("Planet" + i);
            pObj.transform.position = new Vector3(pd.x, pd.y, 0f);
            GravitySource gs = pObj.AddComponent<GravitySource>();
            gs.mass = pd.mass;
            gs.radius = pd.radius;
            gravitySources[i] = gs;
        }

        // Setup docking gate
        if (dockingGate != null)
        {
            float gateCenterX = lvl.goalX + GameData.GoalWidth * 0.5f;
            float gateCenterY = lvl.goalY + GameData.GOAL_HEIGHT * 0.5f;
            dockingGate.transform.position = new Vector3(gateCenterX, gateCenterY, 0f);
            dockingGate.gateWidth = GameData.GoalWidth;
            dockingGate.gateHeight = GameData.GOAL_HEIGHT;
        }
    }

    void StartLevel()
    {
        CancelInvoke();
        State = GameState.AIMING;
        starsEarned = 0;

        SetWorldObjectsVisible(true);
        rocketController.gameObject.SetActive(true);

        GameData.LevelData lvl = allLevels[currentGlobalIndex];
        rocketController.ResetRocket(new Vector2(lvl.startX, lvl.startY));
        rocketController.SetGravityConstant(GameData.GetGravityForShip(activeShipId));
        rocketController.SetTrailColors(PlayerState.Instance.ActiveTrail, activeShipId);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowGameUI(currentU, currentS, currentL, activeShipId);
        }
    }

    public void OnRocketLaunched()
    {
        State = GameState.FLYING;
        if (UIManager.Instance != null) UIManager.Instance.HideHudMessage();
    }

    public void OnGameOver()
    {
        if (State == GameState.GAMEOVER) return;
        State = GameState.GAMEOVER;

        var ps = PlayerState.Instance;
        ps.GlobalShields = Mathf.Max(0, ps.GlobalShields - 1);

        if (rocketController != null)
        {
            MeshRenderer mr = rocketController.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = false;
        }

        Invoke(nameof(ShowGameOverUI), 0.8f);
    }

    void ShowGameOverUI()
    {
        SetWorldObjectsVisible(false);
        if (UIManager.Instance != null)
            UIManager.Instance.ShowGameOver(PlayerState.Instance.GlobalShields > 0);
    }

    public void OnLevelComplete(float hitRatio)
    {
        State = GameState.LEVEL_COMPLETE;

        if (hitRatio > 0.4f && hitRatio < 0.6f) starsEarned = 3;
        else if (hitRatio > 0.2f && hitRatio < 0.8f) starsEarned = 2;
        else starsEarned = 1;

        PlayerState.Instance.SetLevelScore(currentU, currentS, currentL, starsEarned);

        // Regain a shield on success
        var ps = PlayerState.Instance;
        ps.GlobalShields = Mathf.Min(ps.MaxShields, ps.GlobalShields + 1);

        Invoke(nameof(ShowSuccessUI), 1.5f);
    }

    void ShowSuccessUI()
    {
        SetWorldObjectsVisible(false);
        if (UIManager.Instance != null)
            UIManager.Instance.ShowSuccess(starsEarned);
    }

    public void OnContinueAfterSuccess()
    {
        if (currentL < 5)
        {
            // Auto-advance to next level in sector
            LaunchLevel(currentU, currentS, currentL + 1, false);
        }
        else
        {
            // Sector complete — show summary
            if (UIManager.Instance != null)
                UIManager.Instance.ShowSectorSummary(currentU, currentS);
        }
    }

    public void RetryLevel()
    {
        StartLevel();
    }

    public void ExitToMenu()
    {
        ShowMenu();
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
}
