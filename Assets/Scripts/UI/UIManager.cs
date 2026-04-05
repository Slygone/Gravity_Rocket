using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages all UI screens: TopNav, Map, Sector, Hangar, BattlePass, Shop,
/// Loadout, SectorSummary, in-game HUD, GameOver, Success, and overlays.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    public enum View { Map, Sector, BattlePass, Hangar, Shop }

    // Root canvas
    private Canvas rootCanvas;
    private CanvasScaler canvasScaler;

    // View state
    private View currentView = View.Map;
    private int activeSectorU, activeSectorS;
    private bool hasSectorSelected = false;

    // Loadout state
    private bool loadoutOpen = false;
    private int loadoutU, loadoutS, loadoutL;
    private bool loadoutRequiresAd;

    // Overlay state
    private bool adOverlayActive = false;
    private bool purchaseOverlayActive = false;
    private int purchaseAmount;

    // Sector summary state
    private bool sectorSummaryActive = false;
    private int summaryU, summaryS;

    // In-game fleet panel
    private bool fleetPanelOpen = false;

    // Font
    private Font monoFont;

    // Panel references (we rebuild on show for simplicity and data-driven approach)
    private GameObject topNavPanel;
    private GameObject contentPanel;
    private GameObject loadoutPanel;
    private GameObject overlayPanel;
    private GameObject gameHudPanel;
    private GameObject gameOverPanel;
    private GameObject successPanel;
    private GameObject fleetPanel;
    private GameObject sectorSummaryPanel;

    // Game HUD elements
    private Text hudLocationText;
    private Text hudShieldText;
    private Text hudFleetText;
    private Text hudMessageText;

    // Success panel elements
    private Text successStarsText;

    // Scroll content for battle pass
    private RectTransform bpScrollContent;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        monoFont = Font.CreateDynamicFontFromOSFont("Courier New", 72);
    }

    public void Initialize()
    {
        CreateRootCanvas();
        CreateTopNav();
        CreateContentArea();
        CreateLoadoutPanel();
        CreateOverlayPanel();
        CreateGameHUD();
        CreateGameOverPanel();
        CreateSuccessPanel();
        CreateFleetPanel();
        CreateSectorSummaryPanel();

        ShowMenuUI();
    }

    // ==================== CANVAS ====================
    void CreateRootCanvas()
    {
        GameObject canvasObj = new GameObject("UICanvas");
        canvasObj.transform.SetParent(transform);
        rootCanvas = canvasObj.AddComponent<Canvas>();
        rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        rootCanvas.sortingOrder = 100;

        canvasScaler = canvasObj.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1080, 1920);
        canvasScaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();
    }

    // ==================== PUBLIC API ====================
    public void ShowMenuUI()
    {
        HideAllGameUI();
        topNavPanel.SetActive(true);
        contentPanel.SetActive(true);
        loadoutPanel.SetActive(false);
        overlayPanel.SetActive(false);
        sectorSummaryPanel.SetActive(false);

        currentView = View.Map;
        hasSectorSelected = false;
        RefreshTopNav();
        RefreshContent();
    }

    public void ShowGameUI(int universe, int sector, int level, string activeShipId)
    {
        topNavPanel.SetActive(false);
        contentPanel.SetActive(false);
        loadoutPanel.SetActive(false);
        overlayPanel.SetActive(false);
        sectorSummaryPanel.SetActive(false);

        gameHudPanel.SetActive(true);
        gameOverPanel.SetActive(false);
        successPanel.SetActive(false);
        fleetPanel.SetActive(false);

        UpdateGameHUD(universe, sector, level, activeShipId);
        ShowHudMessage("DRAG TO PLOT TRAJECTORY");
    }

    public void HideAllGameUI()
    {
        gameHudPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        successPanel.SetActive(false);
        fleetPanel.SetActive(false);
    }

    public void ShowGameOver(bool hasShields)
    {
        gameOverPanel.SetActive(true);
        RebuildGameOverPanel(hasShields);
    }

    public void ShowSuccess(int starsEarned)
    {
        successPanel.SetActive(true);
        RebuildSuccessPanel(starsEarned);
    }

    public void HideHudMessage()
    {
        if (hudMessageText != null) hudMessageText.gameObject.SetActive(false);
    }

    public void ShowHudMessage(string msg)
    {
        if (hudMessageText != null)
        {
            hudMessageText.text = msg;
            hudMessageText.gameObject.SetActive(true);
        }
    }

    public void UpdateGameHUD(int universe, int sector, int level, string activeShipId)
    {
        var ps = PlayerState.Instance;
        if (hudLocationText != null)
            hudLocationText.text = $"U{universe} - SEC {sector} - LVL {level}";
        if (hudShieldText != null)
        {
            string shields = "";
            for (int i = 0; i < ps.MaxShields; i++)
                shields += (i < ps.GlobalShields) ? "\u2588" : "\u2592";
            hudShieldText.text = "SHIELD: " + shields;
            hudShieldText.color = ps.GlobalShields > 0 ? Color.white : new Color(1f, 0.3f, 0.3f);
        }
        if (hudFleetText != null && GameData.SHIPS.ContainsKey(activeShipId))
        {
            var ship = GameData.SHIPS[activeShipId];
            hudFleetText.text = "FLEET: " + ship.name;
            hudFleetText.color = ship.color;
        }
    }

    public void ShowFleetPanel(List<string> shipIds, string activeShipId, System.Action<string> onSelect)
    {
        fleetPanelOpen = true;
        fleetPanel.SetActive(true);
        RebuildFleetPanel(shipIds, activeShipId, onSelect);
    }

    public void HideFleetPanel()
    {
        fleetPanelOpen = false;
        fleetPanel.SetActive(false);
    }

    public bool IsFleetPanelOpen => fleetPanelOpen;

    public void ShowSectorSummary(int u, int s)
    {
        summaryU = u;
        summaryS = s;
        sectorSummaryActive = true;
        sectorSummaryPanel.SetActive(true);
        StartCoroutine(SectorSummarySequence(u, s));
    }

    // ==================== TOP NAV ====================
    void CreateTopNav()
    {
        topNavPanel = CreateFullPanel(rootCanvas.transform, "TopNav");
        RectTransform rt = topNavPanel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.sizeDelta = new Vector2(0, 160);
        rt.anchoredPosition = Vector2.zero;

        Image bg = topNavPanel.GetComponent<Image>();
        bg.color = Color.black;

        Outline outline = topNavPanel.AddComponent<Outline>();
        outline.effectColor = Color.white;
        outline.effectDistance = new Vector2(0, -2);
    }

    void RefreshTopNav()
    {
        // Clear children
        ClearChildren(topNavPanel.transform);
        var ps = PlayerState.Instance;

        // Left side: Stars + Level + Credits
        GameObject leftGroup = CreateHLayout(topNavPanel.transform, "LeftGroup", new Vector2(30, 0), TextAnchor.MiddleLeft);
        RectTransform lgRt = leftGroup.GetComponent<RectTransform>();
        lgRt.anchorMin = new Vector2(0, 0);
        lgRt.anchorMax = new Vector2(0.5f, 1);
        lgRt.offsetMin = new Vector2(30, 10);
        lgRt.offsetMax = new Vector2(0, -10);

        AddNavLabel(leftGroup.transform, "\u2605 " + ps.TotalStars, Color.white);
        AddNavLabel(leftGroup.transform, "LVL " + ps.AccountLevel, Color.white);
        AddNavLabel(leftGroup.transform, "\u25C9 " + ps.Credits + " +", new Color(1f, 0.84f, 0f));

        // Right side: Nav buttons
        GameObject rightGroup = CreateHLayout(topNavPanel.transform, "RightGroup", Vector2.zero, TextAnchor.MiddleRight);
        RectTransform rgRt = rightGroup.GetComponent<RectTransform>();
        rgRt.anchorMin = new Vector2(0.5f, 0);
        rgRt.anchorMax = new Vector2(1, 1);
        rgRt.offsetMin = new Vector2(0, 15);
        rgRt.offsetMax = new Vector2(-30, -15);

        AddNavButton(rightGroup.transform, "MAP", View.Map);
        AddNavButton(rightGroup.transform, "PASS" + (ps.HasUnclaimedRewards ? " \u25CF" : ""), View.BattlePass);
        AddNavButton(rightGroup.transform, "HANGAR", View.Hangar);
        AddNavButton(rightGroup.transform, "SHOP", View.Shop);
    }

    void AddNavLabel(Transform parent, string text, Color color)
    {
        Text t = CreateTextElement(parent, "lbl", text, 36, color, TextAnchor.MiddleLeft);
        t.fontStyle = FontStyle.Bold;
        LayoutElement le = t.gameObject.AddComponent<LayoutElement>();
        le.minWidth = 120;
        le.preferredWidth = 180;
    }

    void AddNavButton(Transform parent, string label, View targetView)
    {
        bool isActive = (currentView == targetView && !hasSectorSelected);

        GameObject btnObj = new GameObject(label + "Btn", typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);

        Image img = btnObj.GetComponent<Image>();
        img.color = isActive ? Color.white : Color.black;

        Outline ol = btnObj.AddComponent<Outline>();
        ol.effectColor = Color.white;
        ol.effectDistance = new Vector2(2, 2);

        LayoutElement le = btnObj.AddComponent<LayoutElement>();
        le.minWidth = 140;
        le.preferredWidth = 160;
        le.minHeight = 80;

        Text txt = CreateTextElement(btnObj.transform, "Label", label, 24, isActive ? Color.black : Color.white, TextAnchor.MiddleCenter);
        txt.fontStyle = FontStyle.Bold;
        txt.raycastTarget = false;

        Button btn = btnObj.GetComponent<Button>();
        btn.onClick.AddListener(() =>
        {
            currentView = targetView;
            hasSectorSelected = false;
            RefreshTopNav();
            RefreshContent();
        });
    }

    // ==================== CONTENT AREA ====================
    void CreateContentArea()
    {
        contentPanel = CreateFullPanel(rootCanvas.transform, "Content");
        RectTransform rt = contentPanel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 1);
        rt.offsetMin = new Vector2(0, 0);
        rt.offsetMax = new Vector2(0, -160); // Below topnav

        Image bg = contentPanel.GetComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.95f);
    }

    void RefreshContent()
    {
        ClearChildren(contentPanel.transform);

        // Create scrollable area
        GameObject scrollObj = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect));
        scrollObj.transform.SetParent(contentPanel.transform, false);
        RectTransform scrollRt = scrollObj.GetComponent<RectTransform>();
        scrollRt.anchorMin = Vector2.zero;
        scrollRt.anchorMax = Vector2.one;
        scrollRt.offsetMin = Vector2.zero;
        scrollRt.offsetMax = Vector2.zero;

        // Viewport
        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(scrollObj.transform, false);
        RectTransform vpRt = viewport.GetComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero;
        vpRt.anchorMax = Vector2.one;
        vpRt.offsetMin = Vector2.zero;
        vpRt.offsetMax = Vector2.zero;
        viewport.GetComponent<Image>().color = new Color(0, 0, 0, 0.01f);
        viewport.GetComponent<Mask>().showMaskGraphic = false;

        // Content container
        GameObject content = new GameObject("ScrollContent", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        RectTransform cRt = content.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0, 1);
        cRt.anchorMax = new Vector2(1, 1);
        cRt.pivot = new Vector2(0.5f, 1);
        cRt.offsetMin = new Vector2(40, 0);
        cRt.offsetMax = new Vector2(-40, 0);

        VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 20;
        vlg.padding = new RectOffset(0, 0, 30, 100);
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;

        ContentSizeFitter csf = content.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect sr = scrollObj.GetComponent<ScrollRect>();
        sr.viewport = vpRt;
        sr.content = cRt;
        sr.horizontal = false;
        sr.vertical = true;
        sr.scrollSensitivity = 40;

        switch (currentView)
        {
            case View.Map:
                if (hasSectorSelected) BuildSectorView(content.transform);
                else BuildMapView(content.transform);
                break;
            case View.BattlePass: BuildBattlePassView(content.transform); break;
            case View.Hangar: BuildHangarView(content.transform); break;
            case View.Shop: BuildShopView(content.transform); break;
        }
    }

    // ==================== MAP VIEW ====================
    void BuildMapView(Transform parent)
    {
        var ps = PlayerState.Instance;

        // Header
        GameObject header = CreateHLayout(parent, "Header", Vector2.zero, TextAnchor.MiddleLeft);
        SetHeight(header, 80);
        Text title = CreateTextElement(header.transform, "Title", "UNIVERSE " + ps.CurrentUniverse, 60, Color.white, TextAnchor.MiddleLeft);
        title.fontStyle = FontStyle.Bold;

        // Universe buttons
        GameObject uBtns = CreateHLayout(parent, "UBtns", Vector2.zero, TextAnchor.MiddleLeft);
        SetHeight(uBtns, 80);
        AddSmallButton(uBtns.transform, "U1", ps.CurrentUniverse == 1, () => { ps.CurrentUniverse = 1; RefreshTopNav(); RefreshContent(); });
        AddSmallButton(uBtns.transform, "U2" + (!ps.CanEnterUniverse2 ? " \u26BF" : ""), ps.CurrentUniverse == 2, () =>
        {
            if (ps.CanEnterUniverse2) { ps.CurrentUniverse = 2; RefreshTopNav(); RefreshContent(); }
        });

        // Unlock hint
        if (!ps.CanEnterUniverse2 && ps.CurrentUniverse == 1)
        {
            GameObject hint = CreateLayoutItem(parent, "Hint", 80);
            AddOutline(hint);
            Text hintTxt = CreateTextElement(hint.transform, "HintTxt",
                $"\u26BF Clear all 10 Sectors and earn 100 Stars to unlock Universe 2. (Current: {ps.TotalStars}/100 Stars)",
                24, Color.white, TextAnchor.MiddleLeft);
            RectTransform hrt = hintTxt.GetComponent<RectTransform>();
            hrt.offsetMin = new Vector2(20, 0);
        }

        // Sectors grid (2 columns)
        for (int row = 0; row < 5; row++)
        {
            GameObject rowObj = CreateHLayout(parent, "Row" + row, Vector2.zero, TextAnchor.MiddleLeft);
            SetHeight(rowObj, 340);
            rowObj.GetComponent<HorizontalLayoutGroup>().spacing = 20;

            for (int col = 0; col < 2; col++)
            {
                int sectorNum = row * 2 + col + 1;
                BuildSectorCard(rowObj.transform, sectorNum);
            }
        }
    }

    void BuildSectorCard(Transform parent, int sectorNum)
    {
        var ps = PlayerState.Instance;
        bool isUnlocked = ps.IsSectorUnlocked(ps.CurrentUniverse, sectorNum);
        string[] mechIds = GameData.GetSectorMechanics(sectorNum);
        int sectorStars = ps.GetSectorStars(ps.CurrentUniverse, sectorNum);
        int levelsCleared = ps.GetSectorLevelsCleared(ps.CurrentUniverse, sectorNum);

        GameObject card = new GameObject("Sector" + sectorNum, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        card.transform.SetParent(parent, false);

        Image img = card.GetComponent<Image>();
        img.color = Color.black;

        Outline ol = card.AddComponent<Outline>();
        ol.effectColor = isUnlocked ? Color.white : new Color(1, 1, 1, 0.3f);
        ol.effectDistance = new Vector2(2, 2);

        LayoutElement le = card.GetComponent<LayoutElement>();
        le.flexibleWidth = 1;
        le.preferredHeight = 320;

        if (!isUnlocked)
        {
            CanvasGroup cg = card.AddComponent<CanvasGroup>();
            cg.alpha = 0.5f;
        }

        // Content
        VerticalLayoutGroup vlg = card.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(20, 20, 15, 15);
        vlg.spacing = 8;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Title row
        Text titleTxt = CreateTextElement(card.transform, "Title", "SECTOR " + sectorNum, 36, Color.white, TextAnchor.UpperLeft);
        titleTxt.fontStyle = FontStyle.Bold;
        SetHeight(titleTxt.gameObject, 44);

        // Mechanics tags
        GameObject tagsRow = CreateHLayout(card.transform, "Tags", Vector2.zero, TextAnchor.MiddleLeft);
        SetHeight(tagsRow, 40);
        tagsRow.GetComponent<HorizontalLayoutGroup>().spacing = 8;

        foreach (string mechId in mechIds)
        {
            if (!GameData.SECTOR_MECHANICS.ContainsKey(mechId)) continue;
            var mech = GameData.SECTOR_MECHANICS[mechId];
            GameObject tag = new GameObject("Tag", typeof(RectTransform), typeof(Image));
            tag.transform.SetParent(tagsRow.transform, false);
            tag.GetComponent<Image>().color = Color.black;
            Outline tagOl = tag.AddComponent<Outline>();
            tagOl.effectColor = isUnlocked ? mech.color : new Color(1, 1, 1, 0.3f);
            tagOl.effectDistance = new Vector2(1, 1);
            LayoutElement tagLe = tag.AddComponent<LayoutElement>();
            tagLe.preferredWidth = 180;
            tagLe.preferredHeight = 36;
            Text tagTxt = CreateTextElement(tag.transform, "Lbl", mech.name.ToUpper(), 18, isUnlocked ? mech.color : new Color(1, 1, 1, 0.3f), TextAnchor.MiddleCenter);
            tagTxt.fontStyle = FontStyle.Bold;
        }

        // Spacer
        GameObject spacer = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
        spacer.transform.SetParent(card.transform, false);
        spacer.GetComponent<LayoutElement>().flexibleHeight = 1;

        // Stats
        Text lvlTxt = CreateTextElement(card.transform, "Lvl", $"LVL: {levelsCleared}/5", 24, Color.white, TextAnchor.LowerLeft);
        lvlTxt.fontStyle = FontStyle.Bold;
        SetHeight(lvlTxt.gameObject, 30);

        Text starsTxt = CreateTextElement(card.transform, "Stars", $"{sectorStars}/15 \u2605", 28, Color.white, TextAnchor.LowerLeft);
        starsTxt.fontStyle = FontStyle.Bold;
        SetHeight(starsTxt.gameObject, 36);

        // Button
        Button btn = card.GetComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.black;
        cb.highlightedColor = isUnlocked ? Color.white : Color.black;
        cb.pressedColor = isUnlocked ? new Color(0.9f, 0.9f, 0.9f) : Color.black;
        btn.colors = cb;

        int capturedSector = sectorNum;
        btn.onClick.AddListener(() =>
        {
            if (!isUnlocked) return;
            activeSectorU = ps.CurrentUniverse;
            activeSectorS = capturedSector;
            hasSectorSelected = true;
            RefreshContent();
        });
    }

    // ==================== SECTOR VIEW ====================
    void BuildSectorView(Transform parent)
    {
        var ps = PlayerState.Instance;
        int u = activeSectorU, s = activeSectorS;
        string[] mechIds = GameData.GetSectorMechanics(s);
        bool isSectorBeaten = ps.IsSectorBeaten(u, s);

        // Back button
        GameObject backRow = CreateLayoutItem(parent, "BackRow", 60);
        AddActionButton(backRow.transform, "\u25C0 BACK", 200, () =>
        {
            hasSectorSelected = false;
            RefreshContent();
        });

        // Header
        Text title = CreateTextElement(parent, "Title", $"UNIVERSE {u} - SECTOR {s}", 52, Color.white, TextAnchor.MiddleLeft);
        title.fontStyle = FontStyle.Bold;
        SetHeight(title.gameObject, 70);

        // Next sector button
        if (isSectorBeaten && s < 10)
        {
            GameObject nextRow = CreateLayoutItem(parent, "NextRow", 70);
            int nextS = s + 1;
            AddActionButton(nextRow.transform, "NEXT SECTOR \u25B6", 340, () =>
            {
                activeSectorS = nextS;
                RefreshContent();
            });
        }

        // Hazard warnings
        foreach (string mechId in mechIds)
        {
            if (!GameData.SECTOR_MECHANICS.ContainsKey(mechId)) continue;
            var mech = GameData.SECTOR_MECHANICS[mechId];
            bool isMet = string.IsNullOrEmpty(mech.reqShip) || ps.ActiveShips.Contains(mech.reqShip);

            GameObject hazard = CreateLayoutItem(parent, "Hazard_" + mechId, 120);
            AddOutline(hazard, isMet ? mech.color : Color.red);

            Text hazardTitle = CreateTextElement(hazard.transform, "HTitle", "\u26A0 HAZARD: " + mech.name.ToUpper(), 28, isMet ? mech.color : Color.red, TextAnchor.UpperLeft);
            hazardTitle.fontStyle = FontStyle.Bold;
            PositionText(hazardTitle, new Vector2(20, -10), new Vector2(-40, -40));

            Text hazardDesc = CreateTextElement(hazard.transform, "HDesc", mech.warning, 22, isMet ? mech.color : Color.red, TextAnchor.MiddleLeft);
            PositionText(hazardDesc, new Vector2(20, -50), new Vector2(-40, -30));

            if (!isMet)
            {
                Text warnTxt = CreateTextElement(hazard.transform, "Warn", "WARNING: Correct ship not equipped in Fleet!", 20, new Color(1, 0.2f, 0.2f), TextAnchor.LowerLeft);
                warnTxt.fontStyle = FontStyle.Bold;
                PositionText(warnTxt, new Vector2(20, -85), new Vector2(-40, -20));
            }
        }

        // Level list
        for (int i = 1; i <= 5; i++)
        {
            int l = i;
            int stars = ps.GetLevelScore(u, s, l);
            bool isCleared = stars > 0;
            bool canPlay = (l == 1) || isCleared;
            bool requiresAd = (l != 1) && isCleared;

            GameObject levelRow = CreateLayoutItem(parent, "Level" + l, 120);
            AddOutline(levelRow, canPlay ? Color.white : new Color(1, 1, 1, 0.3f));

            if (!canPlay)
            {
                CanvasGroup cg = levelRow.AddComponent<CanvasGroup>();
                cg.alpha = 0.5f;
            }

            // Level number box
            GameObject numBox = new GameObject("Num", typeof(RectTransform), typeof(Image));
            numBox.transform.SetParent(levelRow.transform, false);
            numBox.GetComponent<Image>().color = Color.black;
            Outline numOl = numBox.AddComponent<Outline>();
            numOl.effectColor = canPlay ? Color.white : new Color(1, 1, 1, 0.3f);
            numOl.effectDistance = new Vector2(1, 1);
            RectTransform numRt = numBox.GetComponent<RectTransform>();
            numRt.anchorMin = new Vector2(0, 0.5f);
            numRt.anchorMax = new Vector2(0, 0.5f);
            numRt.pivot = new Vector2(0, 0.5f);
            numRt.sizeDelta = new Vector2(80, 80);
            numRt.anchoredPosition = new Vector2(15, 0);
            Text numTxt = CreateTextElement(numBox.transform, "N", l.ToString(), 36, Color.white, TextAnchor.MiddleCenter);
            numTxt.fontStyle = FontStyle.Bold;
            FillRect(numTxt);

            // Level info
            string levelLabel = "LEVEL " + l + (isCleared ? "  [CLEARED]" : "");
            Text lvlTitle = CreateTextElement(levelRow.transform, "LTitle", levelLabel, 28, Color.white, TextAnchor.MiddleLeft);
            lvlTitle.fontStyle = FontStyle.Bold;
            RectTransform ltRt = lvlTitle.GetComponent<RectTransform>();
            ltRt.anchorMin = new Vector2(0, 0.5f);
            ltRt.anchorMax = new Vector2(0.6f, 1);
            ltRt.offsetMin = new Vector2(110, 5);
            ltRt.offsetMax = new Vector2(0, -5);

            // Stars display
            string starStr = "";
            for (int si = 1; si <= 3; si++) starStr += (si <= stars) ? "\u2605" : "\u2606";
            Text starTxt = CreateTextElement(levelRow.transform, "Stars", starStr, 32, Color.white, TextAnchor.MiddleLeft);
            RectTransform stRt = starTxt.GetComponent<RectTransform>();
            stRt.anchorMin = new Vector2(0, 0);
            stRt.anchorMax = new Vector2(0.6f, 0.5f);
            stRt.offsetMin = new Vector2(110, 5);
            stRt.offsetMax = new Vector2(0, -5);

            // Play button or lock
            if (canPlay)
            {
                GameObject playBtn = new GameObject("Play", typeof(RectTransform), typeof(Image), typeof(Button));
                playBtn.transform.SetParent(levelRow.transform, false);
                Image pbImg = playBtn.GetComponent<Image>();
                pbImg.color = requiresAd ? Color.black : Color.white;
                Outline pbOl = playBtn.AddComponent<Outline>();
                pbOl.effectColor = Color.white;
                pbOl.effectDistance = new Vector2(2, 2);
                RectTransform pbRt = playBtn.GetComponent<RectTransform>();
                pbRt.anchorMin = new Vector2(1, 0.5f);
                pbRt.anchorMax = new Vector2(1, 0.5f);
                pbRt.pivot = new Vector2(1, 0.5f);
                pbRt.sizeDelta = new Vector2(280, 80);
                pbRt.anchoredPosition = new Vector2(-15, 0);

                Text pbTxt = CreateTextElement(playBtn.transform, "Lbl", "\u25B6 PRE-FLIGHT", 22,
                    requiresAd ? Color.white : Color.black, TextAnchor.MiddleCenter);
                pbTxt.fontStyle = FontStyle.Bold;
                pbTxt.raycastTarget = false;
                FillRect(pbTxt);

                int capturedL = l;
                bool capturedAd = requiresAd;
                playBtn.GetComponent<Button>().onClick.AddListener(() =>
                {
                    ShowLoadout(u, s, capturedL, capturedAd);
                });
            }
            else
            {
                Text lockTxt = CreateTextElement(levelRow.transform, "Lock", "\u26BF REACH VIA LVL " + (l - 1), 20,
                    new Color(1, 1, 1, 0.5f), TextAnchor.MiddleRight);
                RectTransform lkRt = lockTxt.GetComponent<RectTransform>();
                lkRt.anchorMin = new Vector2(0.6f, 0);
                lkRt.anchorMax = new Vector2(1, 1);
                lkRt.offsetMin = new Vector2(0, 0);
                lkRt.offsetMax = new Vector2(-15, 0);
            }
        }
    }

    // ==================== HANGAR VIEW ====================
    void BuildHangarView(Transform parent)
    {
        var ps = PlayerState.Instance;

        // Title
        Text title = CreateTextElement(parent, "Title", "HANGAR TERMINAL", 60, Color.white, TextAnchor.MiddleLeft);
        title.fontStyle = FontStyle.Bold;
        SetHeight(title.gameObject, 80);

        AddSeparator(parent);

        // Ships section
        Text shipHeader = CreateTextElement(parent, "ShipHeader", "FLEET / SHIPS (MULTI-SELECT)", 36, new Color(1, 1, 1, 0.7f), TextAnchor.MiddleLeft);
        shipHeader.fontStyle = FontStyle.Bold;
        SetHeight(shipHeader.gameObject, 50);

        foreach (var kvp in GameData.SHIPS)
        {
            var ship = kvp.Value;
            bool isUnlocked = ps.UnlockedShips.Contains(ship.id);
            bool isEquipped = ps.ActiveShips.Contains(ship.id);

            GameObject card = CreateLayoutItem(parent, "Ship_" + ship.id, 200);
            Image cardImg = card.GetComponent<Image>();
            cardImg.color = isEquipped ? Color.white : Color.black;
            AddOutline(card, isEquipped ? Color.white : (isUnlocked ? new Color(1, 1, 1, 0.5f) : new Color(1, 1, 1, 0.2f)));

            if (!isUnlocked)
            {
                CanvasGroup cg = card.AddComponent<CanvasGroup>();
                cg.alpha = 0.4f;
            }

            Color textColor = isEquipped ? Color.black : Color.white;

            Text nameText = CreateTextElement(card.transform, "Name", ship.name.ToUpper(), 36, textColor, TextAnchor.UpperLeft);
            nameText.fontStyle = FontStyle.Bold;
            PositionText(nameText, new Vector2(20, -15), new Vector2(-20, -40));

            Text perkText = CreateTextElement(card.transform, "Perk", ship.perk, 24, isEquipped ? new Color(0, 0, 0, 0.7f) : new Color(1, 1, 1, 0.5f), TextAnchor.MiddleLeft);
            PositionText(perkText, new Vector2(20, -60), new Vector2(-20, -40));

            if (isEquipped)
            {
                Text activeTxt = CreateTextElement(card.transform, "Active", "ACTIVE", 20, Color.white, TextAnchor.UpperRight);
                activeTxt.fontStyle = FontStyle.Bold;
                RectTransform atRt = activeTxt.GetComponent<RectTransform>();
                atRt.anchorMin = new Vector2(1, 1);
                atRt.anchorMax = new Vector2(1, 1);
                atRt.pivot = new Vector2(1, 1);
                atRt.sizeDelta = new Vector2(120, 36);
                atRt.anchoredPosition = new Vector2(-15, -15);

                // Add bg for "ACTIVE" label
                Image atBg = activeTxt.gameObject.AddComponent<Image>();
                atBg.color = Color.black;
                atBg.raycastTarget = false;
            }

            // Action button
            string btnLabel = isUnlocked ? (isEquipped ? "DETACH" : "ASSIGN TO FLEET") : "LOCKED";
            GameObject actionBtn = new GameObject("Action", typeof(RectTransform), typeof(Image), typeof(Button));
            actionBtn.transform.SetParent(card.transform, false);
            Image abImg = actionBtn.GetComponent<Image>();
            abImg.color = isEquipped ? Color.black : Color.black;
            Outline abOl = actionBtn.AddComponent<Outline>();
            abOl.effectColor = isEquipped ? (isEquipped ? Color.black : Color.white) : new Color(1, 1, 1, 0.2f);
            abOl.effectDistance = new Vector2(1, 1);
            RectTransform abRt = actionBtn.GetComponent<RectTransform>();
            abRt.anchorMin = new Vector2(0, 0);
            abRt.anchorMax = new Vector2(1, 0);
            abRt.pivot = new Vector2(0.5f, 0);
            abRt.sizeDelta = new Vector2(-30, 60);
            abRt.anchoredPosition = new Vector2(0, 15);

            Text abTxt = CreateTextElement(actionBtn.transform, "Lbl", btnLabel, 22,
                isEquipped ? Color.white : (isUnlocked ? Color.white : new Color(1, 1, 1, 0.3f)), TextAnchor.MiddleCenter);
            abTxt.fontStyle = FontStyle.Bold;
            abTxt.raycastTarget = false;
            FillRect(abTxt);

            if (isUnlocked)
            {
                string capturedId = ship.id;
                actionBtn.GetComponent<Button>().onClick.AddListener(() =>
                {
                    ps.ToggleShipEquip(capturedId);
                    RefreshContent();
                });
            }
        }

        AddSpacer(parent, 40);

        // Trails section
        Text trailHeader = CreateTextElement(parent, "TrailHeader", "EXHAUST / TRAILS", 36, new Color(1, 1, 1, 0.7f), TextAnchor.MiddleLeft);
        trailHeader.fontStyle = FontStyle.Bold;
        SetHeight(trailHeader.gameObject, 50);

        foreach (var kvp in GameData.TRAILS)
        {
            var trail = kvp.Value;
            bool isUnlocked = ps.UnlockedTrails.Contains(trail.id);
            bool isEquipped = ps.ActiveTrail == trail.id;

            GameObject card = CreateLayoutItem(parent, "Trail_" + trail.id, 200);
            card.GetComponent<Image>().color = isEquipped ? Color.white : Color.black;
            AddOutline(card, isEquipped ? Color.white : (isUnlocked ? new Color(1, 1, 1, 0.5f) : new Color(1, 1, 1, 0.2f)));

            if (!isUnlocked)
            {
                CanvasGroup cg = card.AddComponent<CanvasGroup>();
                cg.alpha = 0.4f;
            }

            Color textColor = isEquipped ? Color.black : Color.white;

            Text nameText = CreateTextElement(card.transform, "Name", trail.name.ToUpper(), 36, textColor, TextAnchor.UpperLeft);
            nameText.fontStyle = FontStyle.Bold;
            PositionText(nameText, new Vector2(20, -15), new Vector2(-20, -40));

            Text perkText = CreateTextElement(card.transform, "Perk", trail.perk, 24, isEquipped ? new Color(0, 0, 0, 0.7f) : new Color(1, 1, 1, 0.5f), TextAnchor.MiddleLeft);
            PositionText(perkText, new Vector2(20, -60), new Vector2(-20, -40));

            if (isEquipped)
            {
                Text activeTxt = CreateTextElement(card.transform, "Active", "EQUIPPED", 20, Color.white, TextAnchor.UpperRight);
                activeTxt.fontStyle = FontStyle.Bold;
                RectTransform atRt = activeTxt.GetComponent<RectTransform>();
                atRt.anchorMin = new Vector2(1, 1);
                atRt.anchorMax = new Vector2(1, 1);
                atRt.pivot = new Vector2(1, 1);
                atRt.sizeDelta = new Vector2(140, 36);
                atRt.anchoredPosition = new Vector2(-15, -15);
                Image atBg = activeTxt.gameObject.AddComponent<Image>();
                atBg.color = Color.black;
                atBg.raycastTarget = false;
            }

            string btnLabel = isUnlocked ? (isEquipped ? "EQUIPPED" : "INITIALIZE") : "LOCKED";
            GameObject actionBtn = new GameObject("Action", typeof(RectTransform), typeof(Image), typeof(Button));
            actionBtn.transform.SetParent(card.transform, false);
            actionBtn.GetComponent<Image>().color = Color.black;
            Outline abOl = actionBtn.AddComponent<Outline>();
            abOl.effectColor = isEquipped ? Color.black : (isUnlocked ? Color.white : new Color(1, 1, 1, 0.2f));
            abOl.effectDistance = new Vector2(1, 1);
            RectTransform abRt = actionBtn.GetComponent<RectTransform>();
            abRt.anchorMin = new Vector2(0, 0);
            abRt.anchorMax = new Vector2(1, 0);
            abRt.pivot = new Vector2(0.5f, 0);
            abRt.sizeDelta = new Vector2(-30, 60);
            abRt.anchoredPosition = new Vector2(0, 15);

            Text abTxt = CreateTextElement(actionBtn.transform, "Lbl", btnLabel, 22,
                isEquipped ? Color.white : (isUnlocked ? Color.white : new Color(1, 1, 1, 0.3f)), TextAnchor.MiddleCenter);
            abTxt.fontStyle = FontStyle.Bold;
            abTxt.raycastTarget = false;
            FillRect(abTxt);

            if (isUnlocked && !isEquipped)
            {
                string capturedId = trail.id;
                actionBtn.GetComponent<Button>().onClick.AddListener(() =>
                {
                    ps.ActiveTrail = capturedId;
                    RefreshContent();
                });
            }
        }
    }

    // ==================== BATTLE PASS VIEW ====================
    void BuildBattlePassView(Transform parent)
    {
        var ps = PlayerState.Instance;

        // Header
        Text title = CreateTextElement(parent, "Title", "PROGRESSION", 60, Color.white, TextAnchor.MiddleLeft);
        title.fontStyle = FontStyle.Bold;
        SetHeight(title.gameObject, 70);

        Text subtitle = CreateTextElement(parent, "Sub", $"Earn stars to unlock. Current Level: [{ps.AccountLevel}]", 24, new Color(1, 1, 1, 0.7f), TextAnchor.MiddleLeft);
        SetHeight(subtitle.gameObject, 35);

        // Premium status
        GameObject premRow = CreateLayoutItem(parent, "PremRow", 80);
        AddOutline(premRow);
        if (ps.IsPremium)
        {
            Text premTxt = CreateTextElement(premRow.transform, "Prem", "\u265B PREMIUM ACTIVE", 28, Color.white, TextAnchor.MiddleCenter);
            premTxt.fontStyle = FontStyle.Bold;
            FillRect(premTxt);
        }
        else
        {
            GameObject premBtn = new GameObject("PremBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            premBtn.transform.SetParent(premRow.transform, false);
            FillRect(premBtn);
            premBtn.GetComponent<Image>().color = Color.black;
            Text premBtnTxt = CreateTextElement(premBtn.transform, "Lbl", "\u265B UPGRADE PASS", 28, Color.white, TextAnchor.MiddleCenter);
            premBtnTxt.fontStyle = FontStyle.Bold;
            premBtnTxt.raycastTarget = false;
            FillRect(premBtnTxt);
            premBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                ps.IsPremium = true;
                RefreshContent();
            });
        }

        AddSeparator(parent);

        // Tiers (vertical list for mobile)
        foreach (var tier in GameData.BATTLE_PASS_TIERS)
        {
            bool isUnlocked = ps.AccountLevel >= tier.level;
            bool freeClaimed = ps.ClaimedRewards.Contains(tier.level + "-free");
            bool premiumClaimed = ps.ClaimedRewards.Contains(tier.level + "-premium");

            GameObject tierCard = CreateLayoutItem(parent, "Tier" + tier.level, 360);
            tierCard.GetComponent<Image>().color = Color.black;
            AddOutline(tierCard, isUnlocked ? Color.white : new Color(1, 1, 1, 0.3f));

            if (!isUnlocked)
            {
                CanvasGroup cg = tierCard.AddComponent<CanvasGroup>();
                cg.alpha = 0.7f;
            }

            // Tier header
            GameObject tierHeader = new GameObject("TierHeader", typeof(RectTransform), typeof(Image));
            tierHeader.transform.SetParent(tierCard.transform, false);
            tierHeader.GetComponent<Image>().color = isUnlocked ? Color.white : new Color(0.2f, 0.2f, 0.2f);
            RectTransform thRt = tierHeader.GetComponent<RectTransform>();
            thRt.anchorMin = new Vector2(0, 1);
            thRt.anchorMax = new Vector2(1, 1);
            thRt.pivot = new Vector2(0.5f, 1);
            thRt.sizeDelta = new Vector2(0, 60);
            thRt.anchoredPosition = Vector2.zero;

            Text tierTitle = CreateTextElement(tierHeader.transform, "TTitle", $"TIER {tier.level}", 28,
                isUnlocked ? Color.black : new Color(1, 1, 1, 0.7f), TextAnchor.MiddleLeft);
            tierTitle.fontStyle = FontStyle.Bold;
            PositionText(tierTitle, new Vector2(15, 0), new Vector2(-150, 0));

            Text starsReq = CreateTextElement(tierHeader.transform, "StarsReq", $"{tier.starsReq} \u2605", 22,
                isUnlocked ? Color.black : new Color(1, 1, 1, 0.7f), TextAnchor.MiddleRight);
            starsReq.fontStyle = FontStyle.Bold;
            RectTransform srRt = starsReq.GetComponent<RectTransform>();
            srRt.anchorMin = new Vector2(1, 0);
            srRt.anchorMax = new Vector2(1, 1);
            srRt.pivot = new Vector2(1, 0.5f);
            srRt.sizeDelta = new Vector2(130, 0);
            srRt.anchoredPosition = new Vector2(-10, 0);

            // Free reward section
            float yOff = -70;
            Text freeLabel = CreateTextElement(tierCard.transform, "FreeLabel", "FREE", 20, new Color(1, 1, 1, 0.5f), TextAnchor.UpperLeft);
            freeLabel.fontStyle = FontStyle.Bold;
            PositionTextAbs(freeLabel, new Vector2(15, yOff), new Vector2(200, 25));

            string freeNameStr = (tier.free.type == "ship" ? "\u2708 " : "") + tier.free.name;
            Text freeName = CreateTextElement(tierCard.transform, "FreeName", freeNameStr, 28, Color.white, TextAnchor.UpperLeft);
            freeName.fontStyle = FontStyle.Bold;
            PositionTextAbs(freeName, new Vector2(15, yOff - 30), new Vector2(500, 35));

            if (tier.free.credits > 0)
            {
                Text credTxt = CreateTextElement(tierCard.transform, "FreeCred", $"\u25C9 +{tier.free.credits} CREDITS", 20, new Color(1f, 0.84f, 0f), TextAnchor.UpperLeft);
                credTxt.fontStyle = FontStyle.Bold;
                PositionTextAbs(credTxt, new Vector2(15, yOff - 65), new Vector2(300, 25));
            }

            // Free claim button
            float btnY = yOff - 95;
            if (isUnlocked)
            {
                if (freeClaimed)
                {
                    Text claimedTxt = CreateTextElement(tierCard.transform, "Claimed", "\u2713 CLAIMED", 22, Color.white, TextAnchor.MiddleLeft);
                    claimedTxt.fontStyle = FontStyle.Bold;
                    PositionTextAbs(claimedTxt, new Vector2(15, btnY), new Vector2(200, 40));
                }
                else
                {
                    int capturedTier = tier.level;
                    AddClaimButton(tierCard.transform, "CLAIM FREE", new Vector2(15, btnY), false, () =>
                    {
                        ps.ClaimReward(capturedTier, false);
                        RefreshTopNav();
                        RefreshContent();
                    });
                }
            }
            else
            {
                Text lockTxt = CreateTextElement(tierCard.transform, "Lock", "\u26BF", 32, new Color(1, 1, 1, 0.3f), TextAnchor.MiddleLeft);
                PositionTextAbs(lockTxt, new Vector2(15, btnY), new Vector2(50, 40));
            }

            // Premium section
            float premY = -225;
            GameObject premSection = new GameObject("PremSection", typeof(RectTransform), typeof(Image));
            premSection.transform.SetParent(tierCard.transform, false);
            premSection.GetComponent<Image>().color = ps.IsPremium ? Color.white : new Color(0.1f, 0.1f, 0.1f);
            RectTransform psRt = premSection.GetComponent<RectTransform>();
            psRt.anchorMin = new Vector2(0, 0);
            psRt.anchorMax = new Vector2(1, 0);
            psRt.pivot = new Vector2(0.5f, 0);
            psRt.sizeDelta = new Vector2(0, 130);
            psRt.anchoredPosition = Vector2.zero;

            Color premTextColor = ps.IsPremium ? Color.black : Color.white;

            Text premLabel = CreateTextElement(premSection.transform, "PremLabel", "\u265B PREMIUM", 20, premTextColor * 0.7f, TextAnchor.UpperLeft);
            premLabel.fontStyle = FontStyle.Bold;
            PositionText(premLabel, new Vector2(15, -10), new Vector2(-30, -20));

            Text premName = CreateTextElement(premSection.transform, "PremName", tier.premium.name, 28, premTextColor, TextAnchor.UpperLeft);
            premName.fontStyle = FontStyle.Bold;
            PositionText(premName, new Vector2(15, -35), new Vector2(-30, -30));

            if (tier.premium.credits > 0)
            {
                Color credColor = ps.IsPremium ? new Color(0, 0, 0, 0.7f) : new Color(1f, 0.84f, 0f);
                Text premCred = CreateTextElement(premSection.transform, "PremCred", $"\u25C9 +{tier.premium.credits} CREDITS", 20, credColor, TextAnchor.UpperLeft);
                premCred.fontStyle = FontStyle.Bold;
                PositionText(premCred, new Vector2(15, -65), new Vector2(-30, -20));
            }

            if (!ps.IsPremium)
            {
                // Lock overlay
                GameObject lockOverlay = new GameObject("LockOvr", typeof(RectTransform), typeof(Image));
                lockOverlay.transform.SetParent(premSection.transform, false);
                lockOverlay.GetComponent<Image>().color = new Color(0, 0, 0, 0.8f);
                FillRect(lockOverlay);
                Text lockIcon = CreateTextElement(lockOverlay.transform, "LockIcon", "\u26BF", 48, new Color(1, 1, 1, 0.5f), TextAnchor.MiddleCenter);
                FillRect(lockIcon);
            }
            else if (isUnlocked)
            {
                if (premiumClaimed)
                {
                    Text pClaimedTxt = CreateTextElement(premSection.transform, "PClaimed", "\u2713 CLAIMED", 22, Color.black, TextAnchor.LowerLeft);
                    pClaimedTxt.fontStyle = FontStyle.Bold;
                    PositionText(pClaimedTxt, new Vector2(15, 10), new Vector2(-30, -30));
                }
                else
                {
                    int capturedTier = tier.level;
                    GameObject claimBtn = new GameObject("ClaimPrem", typeof(RectTransform), typeof(Image), typeof(Button));
                    claimBtn.transform.SetParent(premSection.transform, false);
                    claimBtn.GetComponent<Image>().color = Color.black;
                    RectTransform cbRt = claimBtn.GetComponent<RectTransform>();
                    cbRt.anchorMin = new Vector2(0, 0);
                    cbRt.anchorMax = new Vector2(1, 0);
                    cbRt.pivot = new Vector2(0.5f, 0);
                    cbRt.sizeDelta = new Vector2(-30, 40);
                    cbRt.anchoredPosition = new Vector2(0, 10);
                    Text cbTxt = CreateTextElement(claimBtn.transform, "Lbl", "CLAIM PREM", 22, Color.white, TextAnchor.MiddleCenter);
                    cbTxt.fontStyle = FontStyle.Bold;
                    cbTxt.raycastTarget = false;
                    FillRect(cbTxt);
                    claimBtn.GetComponent<Button>().onClick.AddListener(() =>
                    {
                        ps.ClaimReward(capturedTier, true);
                        RefreshTopNav();
                        RefreshContent();
                    });
                }
            }
        }
    }

    // ==================== SHOP VIEW ====================
    void BuildShopView(Transform parent)
    {
        var ps = PlayerState.Instance;

        Text title = CreateTextElement(parent, "Title", "CURRENCY TERMINAL", 60, Color.white, TextAnchor.MiddleLeft);
        title.fontStyle = FontStyle.Bold;
        SetHeight(title.gameObject, 80);

        AddSeparator(parent);

        // Credit packs
        foreach (var pack in GameData.CREDIT_PACKS)
        {
            GameObject card = CreateLayoutItem(parent, "Pack_" + pack.amount, 180);
            AddOutline(card);

            Text amountTxt = CreateTextElement(card.transform, "Amount", $"\u25C9 {pack.amount} CREDITS +", 40, new Color(1f, 0.84f, 0f), TextAnchor.MiddleCenter);
            amountTxt.fontStyle = FontStyle.Bold;
            PositionText(amountTxt, new Vector2(0, 20), new Vector2(-20, -60));

            int capturedAmount = pack.amount;
            GameObject buyBtn = new GameObject("Buy", typeof(RectTransform), typeof(Image), typeof(Button));
            buyBtn.transform.SetParent(card.transform, false);
            buyBtn.GetComponent<Image>().color = Color.white;
            RectTransform bbRt = buyBtn.GetComponent<RectTransform>();
            bbRt.anchorMin = new Vector2(0.1f, 0);
            bbRt.anchorMax = new Vector2(0.9f, 0);
            bbRt.pivot = new Vector2(0.5f, 0);
            bbRt.sizeDelta = new Vector2(0, 60);
            bbRt.anchoredPosition = new Vector2(0, 15);

            Text buyTxt = CreateTextElement(buyBtn.transform, "Lbl", "ACQUIRE " + pack.price, 24, Color.black, TextAnchor.MiddleCenter);
            buyTxt.fontStyle = FontStyle.Bold;
            buyTxt.raycastTarget = false;
            FillRect(buyTxt);

            buyBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                SimulatePurchase(capturedAmount);
            });
        }

        AddSpacer(parent, 30);

        Text puHeader = CreateTextElement(parent, "PUHeader", "\u26A1 POWER UPS", 36, new Color(1, 1, 1, 0.7f), TextAnchor.MiddleLeft);
        puHeader.fontStyle = FontStyle.Bold;
        SetHeight(puHeader.gameObject, 50);

        AddSeparator(parent);

        // Power up items
        foreach (var item in GameData.SHOP_ITEMS)
        {
            GameObject card = CreateLayoutItem(parent, "Item_" + item.name, 180);
            AddOutline(card, new Color(1, 1, 1, 0.5f));

            Text nameText = CreateTextElement(card.transform, "Name", $"{item.name.ToUpper()} x{item.qty}", 28, Color.white, TextAnchor.UpperLeft);
            nameText.fontStyle = FontStyle.Bold;
            PositionText(nameText, new Vector2(20, -15), new Vector2(-20, -35));

            Text descText = CreateTextElement(card.transform, "Desc", item.desc.ToUpper(), 20, new Color(1, 1, 1, 0.5f), TextAnchor.MiddleLeft);
            PositionText(descText, new Vector2(20, -50), new Vector2(-20, -30));

            // Price
            string priceStr = item.originalCost > 0 ? $"\u25C9 {item.cost} CREDITS" : $"\u25C9 {item.cost} CREDITS";
            Text priceTxt = CreateTextElement(card.transform, "Price", priceStr, 24, new Color(1f, 0.84f, 0f), TextAnchor.LowerLeft);
            priceTxt.fontStyle = FontStyle.Bold;
            PositionText(priceTxt, new Vector2(20, 15), new Vector2(-200, -30));

            if (item.originalCost > 0)
            {
                Text origTxt = CreateTextElement(card.transform, "Orig", $"{item.originalCost} CREDITS", 18, new Color(1, 0.3f, 0.3f), TextAnchor.LowerLeft);
                origTxt.fontStyle = FontStyle.Italic;
                PositionText(origTxt, new Vector2(20, 40), new Vector2(-200, -20));
            }

            // Buy button
            GameObject buyBtn = new GameObject("Buy", typeof(RectTransform), typeof(Image), typeof(Button));
            buyBtn.transform.SetParent(card.transform, false);
            buyBtn.GetComponent<Image>().color = Color.white;
            RectTransform bbRt = buyBtn.GetComponent<RectTransform>();
            bbRt.anchorMin = new Vector2(1, 0);
            bbRt.anchorMax = new Vector2(1, 0);
            bbRt.pivot = new Vector2(1, 0);
            bbRt.sizeDelta = new Vector2(150, 50);
            bbRt.anchoredPosition = new Vector2(-15, 15);

            Text buyTxt = CreateTextElement(buyBtn.transform, "Lbl", "BUY", 22, Color.black, TextAnchor.MiddleCenter);
            buyTxt.fontStyle = FontStyle.Bold;
            buyTxt.raycastTarget = false;
            FillRect(buyTxt);
        }
    }

    // ==================== LOADOUT PANEL ====================
    void CreateLoadoutPanel()
    {
        loadoutPanel = CreateFullScreenPanel(rootCanvas.transform, "LoadoutPanel", new Color(0, 0, 0, 0.9f));
        loadoutPanel.SetActive(false);
    }

    void ShowLoadout(int u, int s, int l, bool requiresAd)
    {
        loadoutU = u; loadoutS = s; loadoutL = l; loadoutRequiresAd = requiresAd;
        loadoutOpen = true;
        loadoutPanel.SetActive(true);
        RebuildLoadout();
    }

    void RebuildLoadout()
    {
        ClearChildren(loadoutPanel.transform);
        var ps = PlayerState.Instance;
        int u = loadoutU, s = loadoutS, l = loadoutL;
        string[] mechIds = GameData.GetSectorMechanics(s);

        // Container
        GameObject container = new GameObject("Container", typeof(RectTransform), typeof(Image));
        container.transform.SetParent(loadoutPanel.transform, false);
        container.GetComponent<Image>().color = Color.black;
        AddOutline(container);
        RectTransform cRt = container.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0.05f, 0.1f);
        cRt.anchorMax = new Vector2(0.95f, 0.9f);
        cRt.offsetMin = Vector2.zero;
        cRt.offsetMax = Vector2.zero;

        // Add VLG for content
        VerticalLayoutGroup vlg = container.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(30, 30, 20, 20);
        vlg.spacing = 15;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Header
        Text headerLeft = CreateTextElement(container.transform, "SecureLabel", "TRANSMISSION SECURE", 20, new Color(1, 1, 1, 0.5f), TextAnchor.MiddleLeft);
        SetHeight(headerLeft.gameObject, 25);

        Text headerTitle = CreateTextElement(container.transform, "Title", "PRE-FLIGHT LOADOUT", 48, Color.white, TextAnchor.MiddleLeft);
        headerTitle.fontStyle = FontStyle.Bold;
        SetHeight(headerTitle.gameObject, 55);

        Text coordsTxt = CreateTextElement(container.transform, "Coords", $"TARGET COORDS: U{u} - S{s} - L{l}", 28, Color.white, TextAnchor.MiddleRight);
        coordsTxt.fontStyle = FontStyle.Bold;
        SetHeight(coordsTxt.gameObject, 40);

        AddSeparatorIn(container.transform);

        // Squadron Details
        Text squadTitle = CreateTextElement(container.transform, "SquadTitle", "SQUADRON DETAILS (FLEET)", 26, new Color(1, 1, 1, 0.7f), TextAnchor.MiddleLeft);
        squadTitle.fontStyle = FontStyle.Bold;
        SetHeight(squadTitle.gameObject, 35);

        foreach (var kvp in GameData.SHIPS)
        {
            var ship = kvp.Value;
            if (!ps.UnlockedShips.Contains(ship.id)) continue;
            bool isEquipped = ps.ActiveShips.Contains(ship.id);

            GameObject shipRow = CreateLayoutItem(container.transform, "Ship_" + ship.id, 60);
            shipRow.GetComponent<Image>().color = isEquipped ? Color.white : Color.black;
            AddOutline(shipRow, isEquipped ? Color.white : new Color(1, 1, 1, 0.3f));

            Text shipTxt = CreateTextElement(shipRow.transform, "Name",
                (isEquipped ? "\u2713 " : "") + ship.name.ToUpper(), 24,
                isEquipped ? Color.black : new Color(1, 1, 1, 0.5f), TextAnchor.MiddleLeft);
            shipTxt.fontStyle = FontStyle.Bold;
            PositionText(shipTxt, new Vector2(15, 0), new Vector2(-30, 0));

            string capturedId = ship.id;
            Button shipBtn = shipRow.AddComponent<Button>();
            shipBtn.onClick.AddListener(() =>
            {
                ps.ToggleShipEquip(capturedId);
                RebuildLoadout();
            });
        }

        // Hazard Analysis
        Text hazardTitle = CreateTextElement(container.transform, "HazardTitle", "HAZARD ANALYSIS", 26, new Color(1, 1, 1, 0.7f), TextAnchor.MiddleLeft);
        hazardTitle.fontStyle = FontStyle.Bold;
        SetHeight(hazardTitle.gameObject, 35);

        foreach (string mechId in mechIds)
        {
            if (!GameData.SECTOR_MECHANICS.ContainsKey(mechId)) continue;
            var mech = GameData.SECTOR_MECHANICS[mechId];
            bool isMet = string.IsNullOrEmpty(mech.reqShip) || ps.ActiveShips.Contains(mech.reqShip);

            GameObject hazRow = CreateLayoutItem(container.transform, "Haz_" + mechId, 60);
            hazRow.GetComponent<Image>().color = Color.black;
            AddOutline(hazRow, isMet ? mech.color : Color.red);

            string status = isMet ? "Addressed by Fleet" : ("Missing: " + (GameData.SHIPS.ContainsKey(mech.reqShip) ? GameData.SHIPS[mech.reqShip].name : "Unknown"));
            Text hazTxt = CreateTextElement(hazRow.transform, "Haz", $"\u26A0 {mech.name.ToUpper()} - {status}", 22,
                isMet ? mech.color : Color.red, TextAnchor.MiddleLeft);
            hazTxt.fontStyle = FontStyle.Bold;
            PositionText(hazTxt, new Vector2(15, 0), new Vector2(-30, 0));
        }

        // Spacer
        AddSpacer(container.transform, 10);

        // Launch button
        GameObject launchBtn = new GameObject("Launch", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        launchBtn.transform.SetParent(container.transform, false);
        launchBtn.GetComponent<Image>().color = Color.white;
        launchBtn.GetComponent<LayoutElement>().preferredHeight = 80;

        string launchLabel = loadoutRequiresAd ? "\u25B6 WATCH AD & LAUNCH" : "\u25B6 LAUNCH MISSION";
        Text launchTxt = CreateTextElement(launchBtn.transform, "Lbl", launchLabel, 30, Color.black, TextAnchor.MiddleCenter);
        launchTxt.fontStyle = FontStyle.Bold;
        launchTxt.raycastTarget = false;
        FillRect(launchTxt);

        launchBtn.GetComponent<Button>().onClick.AddListener(() =>
        {
            loadoutPanel.SetActive(false);
            if (loadoutRequiresAd)
            {
                ShowAdOverlay(() =>
                {
                    GameManager.Instance.LaunchLevel(loadoutU, loadoutS, loadoutL, true);
                });
            }
            else
            {
                GameManager.Instance.LaunchLevel(loadoutU, loadoutS, loadoutL, true);
            }
        });

        // Abort button
        GameObject abortBtn = new GameObject("Abort", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        abortBtn.transform.SetParent(container.transform, false);
        abortBtn.GetComponent<Image>().color = Color.black;
        AddOutline(abortBtn, new Color(1, 1, 1, 0.3f));
        abortBtn.GetComponent<LayoutElement>().preferredHeight = 60;

        Text abortTxt = CreateTextElement(abortBtn.transform, "Lbl", "ABORT", 24, new Color(1, 1, 1, 0.7f), TextAnchor.MiddleCenter);
        abortTxt.fontStyle = FontStyle.Bold;
        abortTxt.raycastTarget = false;
        FillRect(abortTxt);

        abortBtn.GetComponent<Button>().onClick.AddListener(() =>
        {
            loadoutPanel.SetActive(false);
        });
    }

    // ==================== OVERLAY PANEL ====================
    void CreateOverlayPanel()
    {
        overlayPanel = CreateFullScreenPanel(rootCanvas.transform, "OverlayPanel", new Color(0, 0, 0, 0.95f));
        overlayPanel.SetActive(false);
    }

    void ShowAdOverlay(System.Action onComplete)
    {
        overlayPanel.SetActive(true);
        ClearChildren(overlayPanel.transform);

        Text adText = CreateTextElement(overlayPanel.transform, "AdText", "ESTABLISHING WARP LINK...", 32, Color.white, TextAnchor.MiddleCenter);
        FillRect(adText);

        StartCoroutine(AdOverlayRoutine(onComplete));
    }

    IEnumerator AdOverlayRoutine(System.Action onComplete)
    {
        yield return new WaitForSeconds(1.5f);
        overlayPanel.SetActive(false);
        onComplete?.Invoke();
    }

    void SimulatePurchase(int amount)
    {
        overlayPanel.SetActive(true);
        ClearChildren(overlayPanel.transform);

        Text procText = CreateTextElement(overlayPanel.transform, "Proc", "SECURE LINK ESTABLISHED\n\nProcessing Transaction...\nAcquiring: " + amount + " Galactic Credits\n\nContacting Bank Server...", 28, Color.white, TextAnchor.MiddleCenter);
        FillRect(procText);

        StartCoroutine(PurchaseRoutine(amount));
    }

    IEnumerator PurchaseRoutine(int amount)
    {
        yield return new WaitForSeconds(2f);
        PlayerState.Instance.Credits += amount;
        overlayPanel.SetActive(false);
        RefreshTopNav();
        RefreshContent();
    }

    // ==================== GAME HUD ====================
    void CreateGameHUD()
    {
        gameHudPanel = new GameObject("GameHUD", typeof(RectTransform));
        gameHudPanel.transform.SetParent(rootCanvas.transform, false);
        FillRect(gameHudPanel);
        // Make it non-blocking for raycasts
        gameHudPanel.SetActive(false);

        // Location text (top-left)
        hudLocationText = CreateTextElement(gameHudPanel.transform, "Location", "", 32, Color.white, TextAnchor.UpperLeft);
        hudLocationText.fontStyle = FontStyle.Bold;
        RectTransform locRt = hudLocationText.GetComponent<RectTransform>();
        locRt.anchorMin = new Vector2(0, 1);
        locRt.anchorMax = new Vector2(0.6f, 1);
        locRt.pivot = new Vector2(0, 1);
        locRt.sizeDelta = new Vector2(0, 40);
        locRt.anchoredPosition = new Vector2(40, -40);

        // Shield text
        hudShieldText = CreateTextElement(gameHudPanel.transform, "Shield", "", 26, Color.white, TextAnchor.UpperLeft);
        RectTransform shRt = hudShieldText.GetComponent<RectTransform>();
        shRt.anchorMin = new Vector2(0, 1);
        shRt.anchorMax = new Vector2(0.6f, 1);
        shRt.pivot = new Vector2(0, 1);
        shRt.sizeDelta = new Vector2(0, 30);
        shRt.anchoredPosition = new Vector2(40, -85);

        // Fleet text
        hudFleetText = CreateTextElement(gameHudPanel.transform, "Fleet", "", 22, Color.white, TextAnchor.UpperLeft);
        hudFleetText.fontStyle = FontStyle.Bold;
        RectTransform flRt = hudFleetText.GetComponent<RectTransform>();
        flRt.anchorMin = new Vector2(0, 1);
        flRt.anchorMax = new Vector2(0.6f, 1);
        flRt.pivot = new Vector2(0, 1);
        flRt.sizeDelta = new Vector2(0, 25);
        flRt.anchoredPosition = new Vector2(40, -120);

        // HUD message (bottom center)
        hudMessageText = CreateTextElement(gameHudPanel.transform, "Message", "", 26, new Color(1, 1, 1, 0.5f), TextAnchor.MiddleCenter);
        RectTransform msgRt = hudMessageText.GetComponent<RectTransform>();
        msgRt.anchorMin = new Vector2(0, 0);
        msgRt.anchorMax = new Vector2(1, 0);
        msgRt.pivot = new Vector2(0.5f, 0);
        msgRt.sizeDelta = new Vector2(0, 60);
        msgRt.anchoredPosition = new Vector2(0, 240);

        // Fleet button (bottom-left)
        GameObject fleetBtn = new GameObject("FleetBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        fleetBtn.transform.SetParent(gameHudPanel.transform, false);
        fleetBtn.GetComponent<Image>().color = Color.black;
        AddOutline(fleetBtn);
        RectTransform fbRt = fleetBtn.GetComponent<RectTransform>();
        fbRt.anchorMin = new Vector2(0, 0);
        fbRt.anchorMax = new Vector2(0, 0);
        fbRt.pivot = new Vector2(0, 0);
        fbRt.sizeDelta = new Vector2(220, 70);
        fbRt.anchoredPosition = new Vector2(40, 80);

        Text fbTxt = CreateTextElement(fleetBtn.transform, "Lbl", "\u2708 FLEET", 24, Color.white, TextAnchor.MiddleCenter);
        fbTxt.fontStyle = FontStyle.Bold;
        fbTxt.raycastTarget = false;
        FillRect(fbTxt);

        fleetBtn.GetComponent<Button>().onClick.AddListener(() =>
        {
            if (GameManager.Instance != null && GameManager.Instance.State == GameManager.GameState.AIMING)
            {
                var ps = PlayerState.Instance;
                ShowFleetPanel(ps.ActiveShips, GameManager.Instance.GetActiveShipId(), (selectedId) =>
                {
                    GameManager.Instance.SetActiveShipId(selectedId);
                    HideFleetPanel();
                    UpdateGameHUD(GameManager.Instance.CurrentU, GameManager.Instance.CurrentS, GameManager.Instance.CurrentL, selectedId);
                });
            }
        });
    }

    // ==================== GAME OVER PANEL ====================
    void CreateGameOverPanel()
    {
        gameOverPanel = CreateFullScreenPanel(rootCanvas.transform, "GameOverPanel", new Color(0, 0, 0, 0.8f));
        gameOverPanel.SetActive(false);
    }

    void RebuildGameOverPanel(bool hasShields)
    {
        ClearChildren(gameOverPanel.transform);
        var ps = PlayerState.Instance;

        GameObject box = new GameObject("Box", typeof(RectTransform), typeof(Image));
        box.transform.SetParent(gameOverPanel.transform, false);
        box.GetComponent<Image>().color = Color.black;
        AddOutline(box);
        RectTransform bRt = box.GetComponent<RectTransform>();
        bRt.anchorMin = new Vector2(0.1f, 0.3f);
        bRt.anchorMax = new Vector2(0.9f, 0.7f);
        bRt.offsetMin = Vector2.zero;
        bRt.offsetMax = Vector2.zero;

        VerticalLayoutGroup vlg = box.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(40, 40, 40, 40);
        vlg.spacing = 20;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childAlignment = TextAnchor.MiddleCenter;

        Text goTitle = CreateTextElement(box.transform, "Title", "SIGNAL LOST", 52, Color.white, TextAnchor.MiddleCenter);
        goTitle.fontStyle = FontStyle.Bold;
        SetHeight(goTitle.gameObject, 60);

        Text goDesc = CreateTextElement(box.transform, "Desc", "Trajectory compromised.", 32, new Color(0.8f, 0.8f, 0.8f), TextAnchor.MiddleCenter);
        SetHeight(goDesc.gameObject, 45);

        if (hasShields)
        {
            AddFullWidthButton(box.transform, "RECALCULATE", 80, () =>
            {
                gameOverPanel.SetActive(false);
                GameManager.Instance.RetryLevel();
            });
        }
        else
        {
            Text depletedTxt = CreateTextElement(box.transform, "Depleted", "SHIELDS DEPLETED", 24, new Color(1, 0.3f, 0.3f), TextAnchor.MiddleCenter);
            depletedTxt.fontStyle = FontStyle.Bold;
            SetHeight(depletedTxt.gameObject, 35);

            AddFullWidthButton(box.transform, "\u25B6 REFILL SHIELDS (AD)", 70, () =>
            {
                gameOverPanel.SetActive(false);
                ps.GlobalShields = ps.MaxShields;
                GameManager.Instance.RetryLevel();
            });

            AddFullWidthButton(box.transform, "RETREAT TO SECTOR", 60, () =>
            {
                gameOverPanel.SetActive(false);
                GameManager.Instance.ExitToMenu();
            });
        }
    }

    // ==================== SUCCESS PANEL ====================
    void CreateSuccessPanel()
    {
        successPanel = CreateFullScreenPanel(rootCanvas.transform, "SuccessPanel", new Color(0, 0, 0, 0.8f));
        successPanel.SetActive(false);
    }

    void RebuildSuccessPanel(int starsEarned)
    {
        ClearChildren(successPanel.transform);

        GameObject box = new GameObject("Box", typeof(RectTransform), typeof(Image));
        box.transform.SetParent(successPanel.transform, false);
        box.GetComponent<Image>().color = Color.black;
        AddOutline(box);
        RectTransform bRt = box.GetComponent<RectTransform>();
        bRt.anchorMin = new Vector2(0.1f, 0.3f);
        bRt.anchorMax = new Vector2(0.9f, 0.65f);
        bRt.offsetMin = Vector2.zero;
        bRt.offsetMax = Vector2.zero;

        VerticalLayoutGroup vlg = box.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(40, 40, 40, 40);
        vlg.spacing = 20;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childAlignment = TextAnchor.MiddleCenter;

        Text title = CreateTextElement(box.transform, "Title", "DOCKING SUCCESSFUL", 48, Color.white, TextAnchor.MiddleCenter);
        title.fontStyle = FontStyle.Bold;
        SetHeight(title.gameObject, 55);

        string starStr = "";
        for (int i = 1; i <= 3; i++) starStr += (i <= starsEarned ? "\u2605 " : "\u2606 ");
        Text starsTxt = CreateTextElement(box.transform, "Stars", starStr.Trim(), 72, Color.white, TextAnchor.MiddleCenter);
        SetHeight(starsTxt.gameObject, 80);

        AddFullWidthButton(box.transform, "CONTINUE \u25B6", 80, () =>
        {
            successPanel.SetActive(false);
            GameManager.Instance.OnContinueAfterSuccess();
        });
    }

    // ==================== FLEET PANEL (IN-GAME) ====================
    void CreateFleetPanel()
    {
        fleetPanel = CreateFullScreenPanel(rootCanvas.transform, "FleetPanel", new Color(0, 0, 0, 0.8f));
        fleetPanel.SetActive(false);
    }

    void RebuildFleetPanel(List<string> shipIds, string activeShipId, System.Action<string> onSelect)
    {
        ClearChildren(fleetPanel.transform);

        GameObject box = new GameObject("Box", typeof(RectTransform), typeof(Image));
        box.transform.SetParent(fleetPanel.transform, false);
        box.GetComponent<Image>().color = Color.black;
        AddOutline(box);
        RectTransform bRt = box.GetComponent<RectTransform>();
        bRt.anchorMin = new Vector2(0.1f, 0.25f);
        bRt.anchorMax = new Vector2(0.9f, 0.75f);
        bRt.offsetMin = Vector2.zero;
        bRt.offsetMax = Vector2.zero;

        VerticalLayoutGroup vlg = box.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(30, 30, 30, 30);
        vlg.spacing = 15;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        Text title = CreateTextElement(box.transform, "Title", "SELECT VESSEL", 32, Color.white, TextAnchor.MiddleLeft);
        title.fontStyle = FontStyle.Bold;
        SetHeight(title.gameObject, 40);
        AddSeparatorIn(box.transform);

        foreach (string id in shipIds)
        {
            if (!GameData.SHIPS.ContainsKey(id)) continue;
            var ship = GameData.SHIPS[id];
            bool isActive = (id == activeShipId);

            GameObject shipBtn = new GameObject("Ship_" + id, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            shipBtn.transform.SetParent(box.transform, false);
            shipBtn.GetComponent<Image>().color = isActive ? Color.white : Color.black;
            AddOutline(shipBtn, isActive ? Color.white : new Color(1, 1, 1, 0.3f));
            shipBtn.GetComponent<LayoutElement>().preferredHeight = 70;

            Text shipTxt = CreateTextElement(shipBtn.transform, "Name",
                (isActive ? "\u2713 " : "") + ship.name.Split(' ')[0].ToUpper(), 24,
                isActive ? Color.black : Color.white, TextAnchor.MiddleLeft);
            shipTxt.fontStyle = FontStyle.Bold;
            shipTxt.raycastTarget = false;
            PositionText(shipTxt, new Vector2(20, 0), new Vector2(-40, 0));

            string capturedId = id;
            shipBtn.GetComponent<Button>().onClick.AddListener(() => onSelect(capturedId));
        }

        // Close button
        AddFullWidthButton(box.transform, "CLOSE", 60, () => HideFleetPanel());
    }

    // ==================== SECTOR SUMMARY ====================
    void CreateSectorSummaryPanel()
    {
        sectorSummaryPanel = CreateFullScreenPanel(rootCanvas.transform, "SectorSummary", new Color(0, 0, 0, 0.9f));
        sectorSummaryPanel.SetActive(false);
    }

    IEnumerator SectorSummarySequence(int u, int s)
    {
        ClearChildren(sectorSummaryPanel.transform);
        var ps = PlayerState.Instance;

        GameObject box = new GameObject("Box", typeof(RectTransform), typeof(Image));
        box.transform.SetParent(sectorSummaryPanel.transform, false);
        box.GetComponent<Image>().color = Color.black;
        AddOutline(box);
        RectTransform bRt = box.GetComponent<RectTransform>();
        bRt.anchorMin = new Vector2(0.08f, 0.2f);
        bRt.anchorMax = new Vector2(0.92f, 0.8f);
        bRt.offsetMin = Vector2.zero;
        bRt.offsetMax = Vector2.zero;

        VerticalLayoutGroup vlg = box.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(30, 30, 30, 30);
        vlg.spacing = 15;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        Text header = CreateTextElement(box.transform, "Header", $"\u25B6 SECTOR {s} REPORT", 36, Color.white, TextAnchor.MiddleLeft);
        header.fontStyle = FontStyle.Bold;
        SetHeight(header.gameObject, 50);
        AddSeparatorIn(box.transform);

        // Type out each level result
        int totalStars = 0;
        for (int l = 1; l <= 5; l++)
        {
            int st = ps.GetLevelScore(u, s, l);
            totalStars += st;

            GameObject row = CreateLayoutItem(box.transform, "Row" + l, 40);
            Text leftTxt = CreateTextElement(row.transform, "Left", "", 28, new Color(1, 1, 1, 0.7f), TextAnchor.MiddleLeft);
            PositionText(leftTxt, new Vector2(0, 0), new Vector2(-100, 0));

            Text rightTxt = CreateTextElement(row.transform, "Right", "", 28, Color.white, TextAnchor.MiddleRight);
            RectTransform rrRt = rightTxt.GetComponent<RectTransform>();
            rrRt.anchorMin = new Vector2(0.5f, 0);
            rrRt.anchorMax = new Vector2(1, 1);
            rrRt.offsetMin = Vector2.zero;
            rrRt.offsetMax = Vector2.zero;

            // Typewriter effect
            string leftStr = $"LEVEL {l}";
            string rightStr = $"{st} STAR{(st != 1 ? "S" : "")}";

            yield return TypewriterEffect(leftTxt, leftStr, 0.04f);
            yield return TypewriterEffect(rightTxt, rightStr, 0.04f);
            yield return new WaitForSeconds(0.2f);
        }

        AddSeparatorIn(box.transform);

        // Total
        GameObject totalRow = CreateLayoutItem(box.transform, "Total", 50);
        Text totalLeftTxt = CreateTextElement(totalRow.transform, "TLeft", "", 32, Color.white, TextAnchor.MiddleLeft);
        totalLeftTxt.fontStyle = FontStyle.Bold;
        PositionText(totalLeftTxt, new Vector2(0, 0), new Vector2(-100, 0));

        Text totalRightTxt = CreateTextElement(totalRow.transform, "TRight", "", 32, Color.white, TextAnchor.MiddleRight);
        totalRightTxt.fontStyle = FontStyle.Bold;
        RectTransform trRt = totalRightTxt.GetComponent<RectTransform>();
        trRt.anchorMin = new Vector2(0.5f, 0);
        trRt.anchorMax = new Vector2(1, 1);
        trRt.offsetMin = Vector2.zero;
        trRt.offsetMax = Vector2.zero;

        yield return TypewriterEffect(totalLeftTxt, "TOTAL STARS", 0.04f);
        yield return TypewriterEffect(totalRightTxt, $"{totalStars}/15", 0.04f);
        yield return new WaitForSeconds(0.3f);

        // Complete button
        AddFullWidthButton(box.transform, "SECTOR COMPLETED!", 80, () =>
        {
            sectorSummaryPanel.SetActive(false);
            sectorSummaryActive = false;
            GameManager.Instance.ExitToMenu();
        });
    }

    IEnumerator TypewriterEffect(Text textComponent, string fullText, float charDelay)
    {
        textComponent.text = "";
        for (int i = 0; i <= fullText.Length; i++)
        {
            textComponent.text = fullText.Substring(0, i);
            yield return new WaitForSeconds(charDelay);
        }
    }

    // ==================== UTILITY METHODS ====================
    Text CreateTextElement(Transform parent, string name, string content, int fontSize, Color color, TextAnchor alignment)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        Text txt = obj.AddComponent<Text>();
        txt.text = content;
        txt.font = monoFont;
        txt.fontSize = fontSize;
        txt.alignment = alignment;
        txt.color = color;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        return txt;
    }

    GameObject CreateFullPanel(Transform parent, string name)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        return panel;
    }

    GameObject CreateFullScreenPanel(Transform parent, string name, Color bgColor)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        panel.GetComponent<Image>().color = bgColor;
        FillRect(panel);
        return panel;
    }

    GameObject CreateHLayout(Transform parent, string name, Vector2 pos, TextAnchor alignment)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
        obj.transform.SetParent(parent, false);
        HorizontalLayoutGroup hlg = obj.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childAlignment = alignment;
        return obj;
    }

    GameObject CreateLayoutItem(Transform parent, string name, float height)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        obj.transform.SetParent(parent, false);
        obj.GetComponent<Image>().color = Color.black;
        obj.GetComponent<LayoutElement>().preferredHeight = height;
        return obj;
    }

    void SetHeight(GameObject obj, float height)
    {
        LayoutElement le = obj.GetComponent<LayoutElement>();
        if (le == null) le = obj.AddComponent<LayoutElement>();
        le.preferredHeight = height;
    }

    void FillRect(GameObject obj)
    {
        RectTransform rt = obj.GetComponent<RectTransform>();
        if (rt == null) return;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    void FillRect(Text txt)
    {
        FillRect(txt.gameObject);
    }

    void AddOutline(GameObject obj, Color color = default)
    {
        if (color == default) color = Color.white;
        Outline ol = obj.GetComponent<Outline>();
        if (ol == null) ol = obj.AddComponent<Outline>();
        ol.effectColor = color;
        ol.effectDistance = new Vector2(2, 2);
    }

    void PositionText(Text txt, Vector2 offset, Vector2 sizeOffset)
    {
        RectTransform rt = txt.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(Mathf.Max(0, offset.x), Mathf.Max(0, -offset.y - Mathf.Abs(sizeOffset.y)));
        rt.offsetMax = new Vector2(sizeOffset.x < 0 ? sizeOffset.x : 0, offset.y);
    }

    void PositionTextAbs(Text txt, Vector2 anchoredPos, Vector2 size)
    {
        RectTransform rt = txt.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;
    }

    void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }

    void AddSeparator(Transform parent)
    {
        GameObject sep = new GameObject("Sep", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        sep.transform.SetParent(parent, false);
        sep.GetComponent<Image>().color = Color.white;
        sep.GetComponent<LayoutElement>().preferredHeight = 2;
    }

    void AddSeparatorIn(Transform parent)
    {
        GameObject sep = new GameObject("Sep", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        sep.transform.SetParent(parent, false);
        sep.GetComponent<Image>().color = new Color(1, 1, 1, 0.3f);
        sep.GetComponent<LayoutElement>().preferredHeight = 1;
    }

    void AddSpacer(Transform parent, float height)
    {
        GameObject spacer = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
        spacer.transform.SetParent(parent, false);
        spacer.GetComponent<LayoutElement>().preferredHeight = height;
    }

    void AddSmallButton(Transform parent, string label, bool isActive, System.Action onClick)
    {
        GameObject btn = new GameObject(label + "Btn", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        btn.transform.SetParent(parent, false);
        btn.GetComponent<Image>().color = isActive ? Color.white : Color.black;
        AddOutline(btn);
        btn.GetComponent<LayoutElement>().preferredWidth = 130;
        btn.GetComponent<LayoutElement>().preferredHeight = 60;

        Text txt = CreateTextElement(btn.transform, "Lbl", label, 24, isActive ? Color.black : Color.white, TextAnchor.MiddleCenter);
        txt.fontStyle = FontStyle.Bold;
        txt.raycastTarget = false;
        FillRect(txt);

        btn.GetComponent<Button>().onClick.AddListener(() => onClick());
    }

    void AddActionButton(Transform parent, string label, float width, System.Action onClick)
    {
        GameObject btn = new GameObject("Btn", typeof(RectTransform), typeof(Image), typeof(Button));
        btn.transform.SetParent(parent, false);
        btn.GetComponent<Image>().color = Color.black;
        AddOutline(btn);
        RectTransform rt = btn.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0.5f);
        rt.anchorMax = new Vector2(0, 0.5f);
        rt.pivot = new Vector2(0, 0.5f);
        rt.sizeDelta = new Vector2(width, 50);
        rt.anchoredPosition = Vector2.zero;

        Text txt = CreateTextElement(btn.transform, "Lbl", label, 22, Color.white, TextAnchor.MiddleCenter);
        txt.fontStyle = FontStyle.Bold;
        txt.raycastTarget = false;
        FillRect(txt);

        btn.GetComponent<Button>().onClick.AddListener(() => onClick());
    }

    void AddFullWidthButton(Transform parent, string label, float height, System.Action onClick)
    {
        GameObject btn = new GameObject("Btn_" + label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        btn.transform.SetParent(parent, false);
        btn.GetComponent<Image>().color = Color.white;
        btn.GetComponent<LayoutElement>().preferredHeight = height;

        Text txt = CreateTextElement(btn.transform, "Lbl", label, 28, Color.black, TextAnchor.MiddleCenter);
        txt.fontStyle = FontStyle.Bold;
        txt.raycastTarget = false;
        FillRect(txt);

        btn.GetComponent<Button>().onClick.AddListener(() => onClick());
    }

    void AddClaimButton(Transform parent, string label, Vector2 pos, bool isPremium, System.Action onClick)
    {
        GameObject btn = new GameObject("Claim", typeof(RectTransform), typeof(Image), typeof(Button));
        btn.transform.SetParent(parent, false);
        btn.GetComponent<Image>().color = Color.black;
        AddOutline(btn);
        RectTransform rt = btn.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0, 1);
        rt.sizeDelta = new Vector2(-30, 40);
        rt.anchoredPosition = pos;

        Text txt = CreateTextElement(btn.transform, "Lbl", label, 22, Color.white, TextAnchor.MiddleCenter);
        txt.fontStyle = FontStyle.Bold;
        txt.raycastTarget = false;
        FillRect(txt);

        btn.GetComponent<Button>().onClick.AddListener(() => onClick());
    }
}
