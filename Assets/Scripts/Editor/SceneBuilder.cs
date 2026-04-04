using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

namespace GravityRocket.Editor
{
    public static class SceneBuilder
    {
        [MenuItem("Gravity Rocket/Build UI")]
        public static void BuildUI()
        {
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            // Create Canvas
            var canvasGO = new GameObject("Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1.0f;

            canvasGO.AddComponent<GraphicRaycaster>();

            // Safe Area container
            var safeAreaGO = MakeUIObj("SafeArea", canvasGO.transform);
            var safeRect = safeAreaGO.GetComponent<RectTransform>();
            SetAnchors(safeRect, 0, 0, 1, 1);

            // HUD Panel
            var hudGO = MakeUIObj("HUD", safeAreaGO.transform);
            SetAnchors(hudGO.GetComponent<RectTransform>(), 0, 0.9f, 1, 1);
            hudGO.GetComponent<RectTransform>().offsetMin = new Vector2(20, 0);
            hudGO.GetComponent<RectTransform>().offsetMax = new Vector2(-20, -10);

            var sectorText = MakeText("SectorText", hudGO.transform, "Sector 1/50", 28, TextAlignmentOptions.TopLeft);
            SetAnchors(sectorText.GetComponent<RectTransform>(), 0, 0.5f, 0.5f, 1);

            var starsText = MakeText("StarsText", hudGO.transform, "\u2605 0", 28, TextAlignmentOptions.TopRight);
            SetAnchors(starsText.GetComponent<RectTransform>(), 0.5f, 0.5f, 1, 1);

            var shieldText = MakeText("ShieldText", hudGO.transform, "SHIELD: \u2588\u2588\u2588\u2588\u2588", 24, TextAlignmentOptions.BottomLeft);
            SetAnchors(shieldText.GetComponent<RectTransform>(), 0, 0, 1, 0.5f);

            // Instruction text
            var instrGO = MakeText("InstructionText", safeAreaGO.transform, "DRAG TO PLOT TRAJECTORY", 32, TextAlignmentOptions.Center);
            SetAnchors(instrGO.GetComponent<RectTransform>(), 0.1f, 0.35f, 0.9f, 0.45f);

            // ===== MENU PANEL =====
            var menuPanel = MakePanel("MenuPanel", canvasGO.transform);
            var menuTitle = MakeText("Title", menuPanel.transform, "GRAVITY\nROCKET", 72, TextAlignmentOptions.Center);
            SetAnchors(menuTitle.GetComponent<RectTransform>(), 0.1f, 0.5f, 0.9f, 0.8f);
            var startBtn = MakeButton("StartBtn", menuPanel.transform, "INITIATE", 0.25f, 0.3f, 0.75f, 0.38f);
            var muteBtn = MakeButton("MuteToggleBtn", menuPanel.transform, "SOUND: ON", 0.25f, 0.18f, 0.75f, 0.24f);

            // Volume slider
            var sliderGO = MakeUIObj("VolumeSlider", menuPanel.transform);
            SetAnchors(sliderGO.GetComponent<RectTransform>(), 0.2f, 0.1f, 0.8f, 0.15f);
            var slider = sliderGO.AddComponent<Slider>();
            var sliderBG = MakeUIObj("Background", sliderGO.transform);
            sliderBG.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f, 1f);
            SetAnchors(sliderBG.GetComponent<RectTransform>(), 0, 0, 1, 1);
            var fillArea = MakeUIObj("Fill Area", sliderGO.transform);
            SetAnchors(fillArea.GetComponent<RectTransform>(), 0, 0, 1, 1);
            var fill = MakeUIObj("Fill", fillArea.transform);
            fill.AddComponent<Image>().color = Color.white;
            SetAnchors(fill.GetComponent<RectTransform>(), 0, 0, 1, 1);
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.targetGraphic = sliderBG.GetComponent<Image>();

            // ===== GAME OVER PANEL =====
            var goPanel = MakePanel("GameOverPanel", canvasGO.transform);
            goPanel.SetActive(false);
            MakeText("Title", goPanel.transform, "TRAJECTORY\nLOST", 56, TextAlignmentOptions.Center);
            var retryBtn = MakeButton("RetryBtn", goPanel.transform, "RECALCULATE", 0.2f, 0.35f, 0.8f, 0.43f);

            // ===== SHIELD DEPLETED PANEL =====
            var sdPanel = MakePanel("ShieldDepletedPanel", canvasGO.transform);
            sdPanel.SetActive(false);
            MakeText("Title", sdPanel.transform, "SHIELDS\nCRITICAL", 56, TextAlignmentOptions.Center);
            var refillBtn = MakeButton("RefillBtn", sdPanel.transform, "REFILL SHIELDS", 0.15f, 0.38f, 0.85f, 0.46f);
            var retreatBtn = MakeButton("RetreatBtn", sdPanel.transform, "RETREAT", 0.25f, 0.28f, 0.75f, 0.36f);

            // ===== LEVEL COMPLETE PANEL =====
            var lcPanel = MakePanel("LevelCompletePanel", canvasGO.transform);
            lcPanel.SetActive(false);
            MakeText("Title", lcPanel.transform, "DOCKED", 56, TextAlignmentOptions.Center);
            var levelStarsDisp = MakeText("LevelStarsDisplay", lcPanel.transform, "\u2605\u2605\u2605", 72, TextAlignmentOptions.Center);
            SetAnchors(levelStarsDisp.GetComponent<RectTransform>(), 0.1f, 0.5f, 0.9f, 0.6f);
            var totalStarsDisp = MakeText("TotalStarsDisplay", lcPanel.transform, "Total Stars: 0 / 150", 28, TextAlignmentOptions.Center);
            SetAnchors(totalStarsDisp.GetComponent<RectTransform>(), 0.1f, 0.43f, 0.9f, 0.5f);
            var replayBtn = MakeButton("ReplayBtn", lcPanel.transform, "REPLAY", 0.1f, 0.3f, 0.48f, 0.38f);
            var nextBtn = MakeButton("NextBtn", lcPanel.transform, "NEXT SECTOR", 0.52f, 0.3f, 0.9f, 0.38f);

            // ===== VICTORY PANEL =====
            var victPanel = MakePanel("VictoryPanel", canvasGO.transform);
            victPanel.SetActive(false);
            MakeText("Title", victPanel.transform, "MISSION\nCOMPLETE", 64, TextAlignmentOptions.Center);
            var finalStarsDisp = MakeText("FinalStarsDisplay", victPanel.transform, "Total Stars: 0 / 150", 32, TextAlignmentOptions.Center);
            SetAnchors(finalStarsDisp.GetComponent<RectTransform>(), 0.1f, 0.45f, 0.9f, 0.53f);
            var restartBtn = MakeButton("RestartBtn", victPanel.transform, "NEW MISSION", 0.2f, 0.32f, 0.8f, 0.4f);

            // ==================== WIRE UIMANAGER ====================
            var uiManager = canvasGO.AddComponent<GravityRocket.UI.UIManager>();
            var uiType = typeof(GravityRocket.UI.UIManager);

            uiType.GetField("_menuPanel", flags).SetValue(uiManager, menuPanel);
            uiType.GetField("_gameOverPanel", flags).SetValue(uiManager, goPanel);
            uiType.GetField("_shieldDepletedPanel", flags).SetValue(uiManager, sdPanel);
            uiType.GetField("_levelCompletePanel", flags).SetValue(uiManager, lcPanel);
            uiType.GetField("_victoryPanel", flags).SetValue(uiManager, victPanel);
            uiType.GetField("_hudPanel", flags).SetValue(uiManager, hudGO);

            uiType.GetField("_sectorText", flags).SetValue(uiManager, sectorText.GetComponent<TMP_Text>());
            uiType.GetField("_starsText", flags).SetValue(uiManager, starsText.GetComponent<TMP_Text>());
            uiType.GetField("_shieldText", flags).SetValue(uiManager, shieldText.GetComponent<TMP_Text>());
            uiType.GetField("_instructionText", flags).SetValue(uiManager, instrGO.GetComponent<TMP_Text>());
            uiType.GetField("_levelStarsDisplay", flags).SetValue(uiManager, levelStarsDisp.GetComponent<TMP_Text>());
            uiType.GetField("_totalStarsDisplay", flags).SetValue(uiManager, totalStarsDisp.GetComponent<TMP_Text>());
            uiType.GetField("_finalStarsDisplay", flags).SetValue(uiManager, finalStarsDisp.GetComponent<TMP_Text>());

            uiType.GetField("_startBtn", flags).SetValue(uiManager, startBtn.GetComponent<Button>());
            uiType.GetField("_retryBtn", flags).SetValue(uiManager, retryBtn.GetComponent<Button>());
            uiType.GetField("_refillBtn", flags).SetValue(uiManager, refillBtn.GetComponent<Button>());
            uiType.GetField("_retreatBtn", flags).SetValue(uiManager, retreatBtn.GetComponent<Button>());
            uiType.GetField("_replayBtn", flags).SetValue(uiManager, replayBtn.GetComponent<Button>());
            uiType.GetField("_nextBtn", flags).SetValue(uiManager, nextBtn.GetComponent<Button>());
            uiType.GetField("_restartBtn", flags).SetValue(uiManager, restartBtn.GetComponent<Button>());
            uiType.GetField("_muteToggleBtn", flags).SetValue(uiManager, muteBtn.GetComponent<Button>());
            uiType.GetField("_volumeSlider", flags).SetValue(uiManager, slider);
            uiType.GetField("_muteToggleText", flags).SetValue(uiManager, muteBtn.GetComponentInChildren<TMP_Text>());
            uiType.GetField("_safeAreaRect", flags).SetValue(uiManager, safeRect);

            // Wire UIManager to GameManager
            var gmGO = GameObject.Find("GameManager");
            if (gmGO != null)
            {
                var gm = gmGO.GetComponent<GravityRocket.Core.GameManager>();
                typeof(GravityRocket.Core.GameManager).GetField("_uiManager", flags).SetValue(gm, uiManager);
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log("UI Canvas built successfully!");
        }

        static GameObject MakeUIObj(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return go;
        }

        static void SetAnchors(RectTransform rt, float xMin, float yMin, float xMax, float yMax)
        {
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static GameObject MakeText(string name, Transform parent, string text, float fontSize, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            SetAnchors(rt, 0.1f, 0.55f, 0.9f, 0.75f);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            tmp.font = TMP_Settings.defaultFontAsset;

            return go;
        }

        static GameObject MakePanel(string name, Transform parent)
        {
            var go = MakeUIObj(name, parent);
            var img = go.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0.85f);
            return go;
        }

        static GameObject MakeButton(string name, Transform parent, string label, float xMin, float yMin, float xMax, float yMax)
        {
            var btnGO = new GameObject(name, typeof(RectTransform));
            btnGO.transform.SetParent(parent, false);
            SetAnchors(btnGO.GetComponent<RectTransform>(), xMin, yMin, xMax, yMax);

            var img = btnGO.AddComponent<Image>();
            img.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            var btn = btnGO.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = new Color(0.15f, 0.15f, 0.15f, 1f);
            colors.highlightedColor = new Color(0.25f, 0.25f, 0.25f, 1f);
            colors.pressedColor = new Color(0.4f, 0.4f, 0.4f, 1f);
            btn.colors = colors;

            // Border outline
            var outline = btnGO.AddComponent<Outline>();
            outline.effectColor = Color.white;
            outline.effectDistance = new Vector2(2, 2);

            var textGO = new GameObject("Text", typeof(RectTransform));
            textGO.transform.SetParent(btnGO.transform, false);
            SetAnchors(textGO.GetComponent<RectTransform>(), 0, 0, 1, 1);

            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 32;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.font = TMP_Settings.defaultFontAsset;

            return btnGO;
        }
    }
}
