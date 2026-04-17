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
    private int loadoutU, loadoutS, loadoutL;
    private bool loadoutRequiresAd;

    // Sector summary state
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
        rt.sizeDelta = new Vector2(0, 120);
        rt.anchoredPosition = Vector2.zero;

        Image bg = topNavPanel.GetComponent<Image>();
        bg.color = Color.black;

        Outline outline = topNavPanel.AddComponent<Outline>();
        outline.effectColor = Color.white;
        outline.effectDistance = new Vector2(0, -1);
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
        lgRt.offsetMin = new Vector2(34, 8);
        lgRt.offsetMax = new Vector2(0, -8);

        AddNavLabel(leftGroup.transform, "\u2605 " + ps.TotalStars, Color.white);
        AddNavLabel(leftGroup.transform, "LVL " + ps.AccountLevel, Color.white);
        AddNavLabel(leftGroup.transform, "\u25C9 " + ps.Credits + " +", new Color(1f, 0.84f, 0f));

        // Right side: Nav buttons
        GameObject rightGroup = CreateHLayout(topNavPanel.transform, "RightGroup", Vector2.zero, TextAnchor.MiddleRight);
        RectTransform rgRt = rightGroup.GetComponent<RectTransform>();
        rgRt.anchorMin = new Vector2(0.5f, 0);
        rgRt.anchorMax = new Vector2(1, 1);
        rgRt.offsetMin = new Vector2(0, 10);
        rgRt.offsetMax = new Vector2(-20, -10);

        AddNavButton(rightGroup.transform, "MAP", View.Map);
        AddNavButton(rightGroup.transform, "PASS" + (ps.HasUnclaimedRewards ? " \u25CF" : ""), View.BattlePass);
        AddNavButton(rightGroup.transform, "HANGAR", View.Hangar);
        AddNavButton(rightGroup.transform, "SHOP", View.Shop);
    }

    void AddNavLabel(Transform parent, string text, Color color)
    {
        Text t = CreateTextElement(parent, "lbl", text, 28, color, TextAnchor.MiddleLeft);
        t.fontStyle = FontStyle.Bold;
        t.raycastTarget = false;
        LayoutElement le = t.gameObject.AddComponent<LayoutElement>();
        le.minWidth = 80;
        le.preferredWidth = 140;
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
        ol.effectDistance = new Vector2(1, -1);

        LayoutElement le = btnObj.AddComponent<LayoutElement>();
        le.minWidth = 100;
        le.preferredWidth = 120;
        le.minHeight = 55;

        Text txt = CreateTextElement(btnObj.transform, "Label", label, 22, isActive ? Color.black : Color.white, TextAnchor.MiddleCenter);
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

        // RGB glitch on PASS button when there are unclaimed rewards
        if (targetView == View.BattlePass && !isActive && PlayerState.Instance.HasUnclaimedRewards)
        {
            btnObj.AddComponent<RGBGlitchEffect>();
        }
    }

    // ==================== CONTENT AREA ====================
    void CreateContentArea()
    {
        contentPanel = CreateFullPanel(rootCanvas.transform, "Content");
        RectTransform rt = contentPanel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 1);
        rt.offsetMin = new Vector2(0, 0);
        rt.offsetMax = new Vector2(0, -120); // Below topnav

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
        cRt.offsetMin = new Vector2(60, 0);
        cRt.offsetMax = new Vector2(-60, 0);

        VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 24;
        vlg.padding = new RectOffset(0, 0, 34, 80);
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;

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
        SetHeight(header, 70);
        Text title = CreateTextElement(header.transform, "Title", "UNIVERSE " + ps.CurrentUniverse, 64, Color.white, TextAnchor.MiddleLeft);
        title.fontStyle = FontStyle.Bold;

        // Universe buttons
        GameObject uBtns = CreateHLayout(parent, "UBtns", Vector2.zero, TextAnchor.MiddleLeft);
        SetHeight(uBtns, 55);
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

        // Sectors grid (5 columns x 2 rows)
        for (int row = 0; row < 2; row++)
        {
            GameObject rowObj = CreateHLayout(parent, "Row" + row, Vector2.zero, TextAnchor.MiddleLeft);
            SetHeight(rowObj, 280);
            rowObj.GetComponent<HorizontalLayoutGroup>().spacing = 24;

            for (int col = 0; col < 5; col++)
            {
                int sectorNum = row * 5 + col + 1;
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
        ol.effectDistance = new Vector2(1, -1);

        LayoutElement le = card.GetComponent<LayoutElement>();
        le.flexibleWidth = 1;
        le.preferredHeight = 260;

        if (!isUnlocked)
        {
            CanvasGroup cg = card.AddComponent<CanvasGroup>();
            cg.alpha = 0.5f;
        }

        // Content
        VerticalLayoutGroup vlg = card.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.spacing = 4;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;

        // Title row
        Text titleTxt = CreateTextElement(card.transform, "Title", "SEC " + sectorNum, 26, Color.white, TextAnchor.UpperLeft);
        titleTxt.fontStyle = FontStyle.Bold;
        titleTxt.raycastTarget = false;
        SetHeight(titleTxt.gameObject, 32);

        // Mechanics tags
        GameObject tagsRow = CreateHLayout(card.transform, "Tags", Vector2.zero, TextAnchor.MiddleLeft);
        SetHeight(tagsRow, 26);
        HorizontalLayoutGroup tagHlg = tagsRow.GetComponent<HorizontalLayoutGroup>();
        tagHlg.spacing = 4;
        tagHlg.childForceExpandHeight = true;

        foreach (string mechId in mechIds)
        {
            if (!GameData.SECTOR_MECHANICS.ContainsKey(mechId)) continue;
            var mech = GameData.SECTOR_MECHANICS[mechId];
            GameObject tag = new GameObject("Tag", typeof(RectTransform), typeof(Image));
            tag.transform.SetParent(tagsRow.transform, false);
            tag.GetComponent<Image>().color = Color.black;
            Outline tagOl = tag.AddComponent<Outline>();
            tagOl.effectColor = isUnlocked ? mech.color : new Color(1, 1, 1, 0.3f);
            tagOl.effectDistance = new Vector2(1, -1);
            LayoutElement tagLe = tag.AddComponent<LayoutElement>();
            tagLe.preferredWidth = 70;
            tagLe.preferredHeight = 24;
            string shortName = mech.name.ToUpper();
            if (shortName.Length > 7) shortName = shortName.Substring(0, 7);
            Text tagTxt = CreateTextElement(tag.transform, "Lbl", shortName, 13, isUnlocked ? mech.color : new Color(1, 1, 1, 0.3f), TextAnchor.MiddleCenter);
            tagTxt.fontStyle = FontStyle.Bold;
            tagTxt.raycastTarget = false;
            FillRect(tagTxt);
        }

        // Spacer
        GameObject spacer = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
        spacer.transform.SetParent(card.transform, false);
        spacer.GetComponent<LayoutElement>().flexibleHeight = 1;

        // Stats
        Text lvlTxt = CreateTextElement(card.transform, "Lvl", $"LVL {levelsCleared}/5", 18, Color.white, TextAnchor.LowerLeft);
        lvlTxt.fontStyle = FontStyle.Bold;
        lvlTxt.raycastTarget = false;
        SetHeight(lvlTxt.gameObject, 22);

        Text starsTxt = CreateTextElement(card.transform, "Stars", $"{sectorStars}/15 \u2605", 22, Color.white, TextAnchor.LowerLeft);
        starsTxt.fontStyle = FontStyle.Bold;
        starsTxt.raycastTarget = false;
        SetHeight(starsTxt.gameObject, 26);

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
        Text title = CreateTextElement(parent, "Title", $"UNIVERSE {u} - SECTOR {s}", 64, Color.white, TextAnchor.MiddleLeft);
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

            GameObject hazard = new GameObject("Hazard_" + mechId, typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(VerticalLayoutGroup));
            hazard.transform.SetParent(parent, false);
            hazard.GetComponent<Image>().color = Color.black;
            hazard.GetComponent<LayoutElement>().preferredHeight = isMet ? 95 : 130;
            AddOutline(hazard, isMet ? mech.color : Color.red);
            VerticalLayoutGroup hzVlg = hazard.GetComponent<VerticalLayoutGroup>();
            hzVlg.padding = new RectOffset(20, 20, 10, 10);
            hzVlg.spacing = 4;
            hzVlg.childForceExpandWidth = true;
            hzVlg.childForceExpandHeight = false;

            Text hazardTitle = CreateTextElement(hazard.transform, "HTitle", "\u26A0 HAZARD: " + mech.name.ToUpper(), 28, isMet ? mech.color : Color.red, TextAnchor.MiddleLeft);
            hazardTitle.fontStyle = FontStyle.Bold;
            SetHeight(hazardTitle.gameObject, 32);

            Text hazardDesc = CreateTextElement(hazard.transform, "HDesc", mech.warning, 24, isMet ? mech.color : Color.red, TextAnchor.MiddleLeft);
            hazardDesc.horizontalOverflow = HorizontalWrapMode.Wrap;
            SetHeight(hazardDesc.gameObject, 28);

            if (!isMet)
            {
                Text warnTxt = CreateTextElement(hazard.transform, "Warn", "WARNING: Correct ship not equipped in Fleet!", 20, new Color(1, 0.2f, 0.2f), TextAnchor.MiddleLeft);
                warnTxt.fontStyle = FontStyle.Bold;
                SetHeight(warnTxt.gameObject, 24);
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

            GameObject levelRow = CreateLayoutItem(parent, "Level" + l, 100);
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
            numOl.effectDistance = new Vector2(1, -1);
            RectTransform numRt = numBox.GetComponent<RectTransform>();
            numRt.anchorMin = new Vector2(0, 0.5f);
            numRt.anchorMax = new Vector2(0, 0.5f);
            numRt.pivot = new Vector2(0, 0.5f);
            numRt.sizeDelta = new Vector2(70, 70);
            numRt.anchoredPosition = new Vector2(15, 0);
            Text numTxt = CreateTextElement(numBox.transform, "N", l.ToString(), 30, Color.white, TextAnchor.MiddleCenter);
            numTxt.fontStyle = FontStyle.Bold;
            FillRect(numTxt);

            // Level info
            string levelLabel = "LEVEL " + l + (isCleared ? "  [CLEARED]" : "");
            Text lvlTitle = CreateTextElement(levelRow.transform, "LTitle", levelLabel, 30, Color.white, TextAnchor.MiddleLeft);
            lvlTitle.fontStyle = FontStyle.Bold;
            RectTransform ltRt = lvlTitle.GetComponent<RectTransform>();
            ltRt.anchorMin = new Vector2(0, 0.5f);
            ltRt.anchorMax = new Vector2(0.6f, 1);
            ltRt.offsetMin = new Vector2(110, 5);
            ltRt.offsetMax = new Vector2(0, -5);

            // Stars display
            string starStr = "";
            for (int si = 1; si <= 3; si++) starStr += (si <= stars) ? "\u2605" : "\u2606";
            Text starTxt = CreateTextElement(levelRow.transform, "Stars", starStr, 28, Color.white, TextAnchor.MiddleLeft);
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
                pbOl.effectDistance = new Vector2(1, -1);
                RectTransform pbRt = playBtn.GetComponent<RectTransform>();
                pbRt.anchorMin = new Vector2(1, 0.5f);
                pbRt.anchorMax = new Vector2(1, 0.5f);
                pbRt.pivot = new Vector2(1, 0.5f);
                pbRt.sizeDelta = new Vector2(240, 60);
                pbRt.anchoredPosition = new Vector2(-15, 0);

                Text pbTxt = CreateTextElement(playBtn.transform, "Lbl", "\u25B6 PRE-FLIGHT", 20,
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

        Text title = CreateTextElement(parent, "Title", "HANGAR TERMINAL", 64, Color.white, TextAnchor.MiddleLeft);
        title.fontStyle = FontStyle.Bold;
        SetHeight(title.gameObject, 80);
        AddSeparator(parent);

        Text shipHeader = CreateTextElement(parent, "ShipHeader", "FLEET / SHIPS (MULTI-SELECT)", 42, new Color(1, 1, 1, 0.7f), TextAnchor.MiddleLeft);
        shipHeader.fontStyle = FontStyle.Bold;
        SetHeight(shipHeader.gameObject, 50);

        // Ship cards in rows of 3
        {
            var shipList = new List<GameData.ShipDef>();
            foreach (var kvp in GameData.SHIPS) shipList.Add(kvp.Value);
            for (int i = 0; i < shipList.Count; i += 3)
            {
                GameObject row = CreateHLayout(parent, "ShipRow" + i, Vector2.zero, TextAnchor.MiddleLeft);
                SetHeight(row, 260);
                row.GetComponent<HorizontalLayoutGroup>().spacing = 24;

                for (int j = i; j < Mathf.Min(i + 3, shipList.Count); j++)
                {
                    BuildShipCard(row.transform, shipList[j], ps);
                }
            }
        }

        AddSpacer(parent, 40);

        Text trailHeader = CreateTextElement(parent, "TrailHeader", "EXHAUST / TRAILS", 42, new Color(1, 1, 1, 0.7f), TextAnchor.MiddleLeft);
        trailHeader.fontStyle = FontStyle.Bold;
        SetHeight(trailHeader.gameObject, 50);

        // Trail cards in rows of 3
        {
            var trailList = new List<GameData.TrailDef>();
            foreach (var kvp in GameData.TRAILS) trailList.Add(kvp.Value);
            for (int i = 0; i < trailList.Count; i += 3)
            {
                GameObject row = CreateHLayout(parent, "TrailRow" + i, Vector2.zero, TextAnchor.MiddleLeft);
                SetHeight(row, 260);
                row.GetComponent<HorizontalLayoutGroup>().spacing = 24;

                for (int j = i; j < Mathf.Min(i + 3, trailList.Count); j++)
                {
                    BuildTrailCard(row.transform, trailList[j], ps);
                }
            }
        }
    }

    void BuildShipCard(Transform parent, GameData.ShipDef ship, PlayerState ps)
    {
        bool isUnlocked = ps.UnlockedShips.Contains(ship.id);
        bool isEquipped = ps.ActiveShips.Contains(ship.id);
        Color textColor = isEquipped ? Color.black : Color.white;

        GameObject card = new GameObject("Ship_" + ship.id, typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(VerticalLayoutGroup));
        card.transform.SetParent(parent, false);
        card.GetComponent<Image>().color = isEquipped ? Color.white : Color.black;
        card.GetComponent<LayoutElement>().flexibleWidth = 1;
        AddOutline(card, isEquipped ? Color.white : (isUnlocked ? new Color(1, 1, 1, 0.5f) : new Color(1, 1, 1, 0.2f)));
        VerticalLayoutGroup vlg = card.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(12, 12, 10, 10);
        vlg.spacing = 3;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        if (!isUnlocked) card.AddComponent<CanvasGroup>().alpha = 0.4f;

        Text nameText = CreateTextElement(card.transform, "Name", ship.name.ToUpper(), 24, textColor, TextAnchor.MiddleLeft);
        nameText.fontStyle = FontStyle.Bold;
        nameText.raycastTarget = false;
        SetHeight(nameText.gameObject, 32);

        Text perkText = CreateTextElement(card.transform, "Perk", ship.perk, 18,
            isEquipped ? new Color(0, 0, 0, 0.7f) : new Color(1, 1, 1, 0.5f), TextAnchor.MiddleLeft);
        perkText.horizontalOverflow = HorizontalWrapMode.Wrap;
        perkText.raycastTarget = false;
        SetHeight(perkText.gameObject, 44);

        AddFlexSpacer(card.transform);

        string btnLabel = isUnlocked ? (isEquipped ? "DETACH" : "ASSIGN") : "LOCKED";
        GameObject actionBtn = new GameObject("Action", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        actionBtn.transform.SetParent(card.transform, false);
        actionBtn.GetComponent<Image>().color = Color.black;
        actionBtn.GetComponent<LayoutElement>().preferredHeight = 48;
        AddOutline(actionBtn, isUnlocked ? Color.white : new Color(1, 1, 1, 0.2f));
        Text abTxt = CreateTextElement(actionBtn.transform, "Lbl", btnLabel, 18,
            isUnlocked ? Color.white : new Color(1, 1, 1, 0.3f), TextAnchor.MiddleCenter);
        abTxt.fontStyle = FontStyle.Bold;
        abTxt.raycastTarget = false;
        FillRect(abTxt);
        if (isUnlocked)
        {
            string capturedId = ship.id;
            actionBtn.GetComponent<Button>().onClick.AddListener(() => { ps.ToggleShipEquip(capturedId); RefreshContent(); });
        }

        if (isEquipped)
        {
            GameObject badge = new GameObject("ActiveBadge", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            badge.transform.SetParent(card.transform, false);
            badge.GetComponent<Image>().color = Color.black;
            badge.GetComponent<LayoutElement>().ignoreLayout = true;
            AddOutline(badge);
            RectTransform atRt = badge.GetComponent<RectTransform>();
            atRt.anchorMin = new Vector2(1, 1); atRt.anchorMax = new Vector2(1, 1);
            atRt.pivot = new Vector2(1, 1);
            atRt.sizeDelta = new Vector2(70, 22);
            atRt.anchoredPosition = new Vector2(-8, -8);
            Text activeTxt = CreateTextElement(badge.transform, "Lbl", "ACTIVE", 14, Color.white, TextAnchor.MiddleCenter);
            activeTxt.fontStyle = FontStyle.Bold; activeTxt.raycastTarget = false; FillRect(activeTxt);
        }
    }

    void BuildTrailCard(Transform parent, GameData.TrailDef trail, PlayerState ps)
    {
        bool isUnlocked = ps.UnlockedTrails.Contains(trail.id);
        bool isEquipped = ps.ActiveTrail == trail.id;
        Color textColor = isEquipped ? Color.black : Color.white;

        GameObject card = new GameObject("Trail_" + trail.id, typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(VerticalLayoutGroup));
        card.transform.SetParent(parent, false);
        card.GetComponent<Image>().color = isEquipped ? Color.white : Color.black;
        card.GetComponent<LayoutElement>().flexibleWidth = 1;
        AddOutline(card, isEquipped ? Color.white : (isUnlocked ? new Color(1, 1, 1, 0.5f) : new Color(1, 1, 1, 0.2f)));
        VerticalLayoutGroup vlg = card.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(12, 12, 10, 10);
        vlg.spacing = 3;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        if (!isUnlocked) card.AddComponent<CanvasGroup>().alpha = 0.4f;

        Text nameText = CreateTextElement(card.transform, "Name", trail.name.ToUpper(), 24, textColor, TextAnchor.MiddleLeft);
        nameText.fontStyle = FontStyle.Bold;
        nameText.raycastTarget = false;
        SetHeight(nameText.gameObject, 32);

        Text perkText = CreateTextElement(card.transform, "Perk", trail.perk, 18,
            isEquipped ? new Color(0, 0, 0, 0.7f) : new Color(1, 1, 1, 0.5f), TextAnchor.MiddleLeft);
        perkText.horizontalOverflow = HorizontalWrapMode.Wrap;
        perkText.raycastTarget = false;
        SetHeight(perkText.gameObject, 44);

        AddFlexSpacer(card.transform);

        string btnLabel = isUnlocked ? (isEquipped ? "EQUIPPED" : "EQUIP") : "LOCKED";
        GameObject actionBtn = new GameObject("Action", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        actionBtn.transform.SetParent(card.transform, false);
        actionBtn.GetComponent<Image>().color = Color.black;
        actionBtn.GetComponent<LayoutElement>().preferredHeight = 48;
        AddOutline(actionBtn, isUnlocked ? Color.white : new Color(1, 1, 1, 0.2f));
        Text abTxt = CreateTextElement(actionBtn.transform, "Lbl", btnLabel, 18,
            isUnlocked ? Color.white : new Color(1, 1, 1, 0.3f), TextAnchor.MiddleCenter);
        abTxt.fontStyle = FontStyle.Bold; abTxt.raycastTarget = false; FillRect(abTxt);
        if (isUnlocked && !isEquipped)
        {
            string capturedId = trail.id;
            actionBtn.GetComponent<Button>().onClick.AddListener(() => { ps.ActiveTrail = capturedId; RefreshContent(); });
        }

        if (isEquipped)
        {
            GameObject badge = new GameObject("EquipBadge", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            badge.transform.SetParent(card.transform, false);
            badge.GetComponent<Image>().color = Color.black;
            badge.GetComponent<LayoutElement>().ignoreLayout = true;
            AddOutline(badge);
            RectTransform atRt = badge.GetComponent<RectTransform>();
            atRt.anchorMin = new Vector2(1, 1); atRt.anchorMax = new Vector2(1, 1);
            atRt.pivot = new Vector2(1, 1);
            atRt.sizeDelta = new Vector2(86, 22);
            atRt.anchoredPosition = new Vector2(-8, -8);
            Text activeTxt = CreateTextElement(badge.transform, "Lbl", "EQUIPPED", 14, Color.white, TextAnchor.MiddleCenter);
            activeTxt.fontStyle = FontStyle.Bold; activeTxt.raycastTarget = false; FillRect(activeTxt);
        }
    }

    // ==================== BATTLE PASS VIEW ====================
    void BuildBattlePassView(Transform parent)
    {
        var ps = PlayerState.Instance;

        // Header row: title on left, premium box on right
        GameObject headerRow = CreateHLayout(parent, "PassHeader", Vector2.zero, TextAnchor.MiddleLeft);
        SetHeight(headerRow, 140);
        HorizontalLayoutGroup hrHlg = headerRow.GetComponent<HorizontalLayoutGroup>();
        hrHlg.spacing = 20;
        hrHlg.childForceExpandWidth = false;
        hrHlg.childForceExpandHeight = true;

        // Left: Title group
        GameObject titleGroup = new GameObject("TitleGroup", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        titleGroup.transform.SetParent(headerRow.transform, false);
        titleGroup.GetComponent<LayoutElement>().flexibleWidth = 1;
        VerticalLayoutGroup tgVlg = titleGroup.GetComponent<VerticalLayoutGroup>();
        tgVlg.childForceExpandWidth = true;
        tgVlg.childForceExpandHeight = false;
        tgVlg.childControlWidth = true;
        tgVlg.childControlHeight = true;
        tgVlg.spacing = 4;

        Text title = CreateTextElement(titleGroup.transform, "Title", "PROGRESSION", 64, Color.white, TextAnchor.MiddleLeft);
        title.fontStyle = FontStyle.Bold;
        SetHeight(title.gameObject, 70);

        Text subtitle = CreateTextElement(titleGroup.transform, "Sub", $"Earn stars to unlock. Current Level: [{ps.AccountLevel}]", 26, new Color(1, 1, 1, 0.7f), TextAnchor.MiddleLeft);
        SetHeight(subtitle.gameObject, 35);

        // Right: Premium status box
        GameObject premBox = new GameObject("PremBox", typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(VerticalLayoutGroup));
        premBox.transform.SetParent(headerRow.transform, false);
        premBox.GetComponent<Image>().color = Color.black;
        premBox.GetComponent<LayoutElement>().preferredWidth = 280;
        AddOutline(premBox);
        VerticalLayoutGroup pbVlg = premBox.GetComponent<VerticalLayoutGroup>();
        pbVlg.padding = new RectOffset(16, 16, 12, 12);
        pbVlg.spacing = 8;
        pbVlg.childForceExpandWidth = true;
        pbVlg.childForceExpandHeight = false;
        pbVlg.childControlWidth = true;
        pbVlg.childControlHeight = true;
        pbVlg.childAlignment = TextAnchor.MiddleCenter;

        Text passStatus = CreateTextElement(premBox.transform, "Status", "PASS STATUS", 20, new Color(1, 1, 1, 0.7f), TextAnchor.MiddleCenter);
        passStatus.fontStyle = FontStyle.Bold;
        SetHeight(passStatus.gameObject, 28);

        if (ps.IsPremium)
        {
            Text premTxt = CreateTextElement(premBox.transform, "Prem", "\u265B PREMIUM ACTIVE", 24, Color.white, TextAnchor.MiddleCenter);
            premTxt.fontStyle = FontStyle.Bold;
            SetHeight(premTxt.gameObject, 40);
        }
        else
        {
            GameObject premBtn = new GameObject("PremBtn", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            premBtn.transform.SetParent(premBox.transform, false);
            premBtn.GetComponent<Image>().color = Color.black;
            premBtn.GetComponent<LayoutElement>().preferredHeight = 40;
            AddOutline(premBtn);
            Text premBtnTxt = CreateTextElement(premBtn.transform, "Lbl", "\u265B UPGRADE PASS", 24, Color.white, TextAnchor.MiddleCenter);
            premBtnTxt.fontStyle = FontStyle.Bold; premBtnTxt.raycastTarget = false; FillRect(premBtnTxt);
            premBtn.GetComponent<Button>().onClick.AddListener(() => { ps.IsPremium = true; RefreshContent(); });
        }

        AddSeparator(parent);

        // Horizontal scroll for tier cards
        GameObject hScrollObj = new GameObject("TierScroll", typeof(RectTransform), typeof(ScrollRect), typeof(LayoutElement));
        hScrollObj.transform.SetParent(parent, false);
        hScrollObj.GetComponent<LayoutElement>().preferredHeight = 480;

        GameObject hViewport = new GameObject("HViewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        hViewport.transform.SetParent(hScrollObj.transform, false);
        FillRect(hViewport);
        hViewport.GetComponent<Image>().color = new Color(0, 0, 0, 0.01f);
        hViewport.GetComponent<Mask>().showMaskGraphic = false;

        GameObject hContent = new GameObject("HContent", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        hContent.transform.SetParent(hViewport.transform, false);
        RectTransform hcRt = hContent.GetComponent<RectTransform>();
        hcRt.anchorMin = new Vector2(0, 0);
        hcRt.anchorMax = new Vector2(0, 1);
        hcRt.pivot = new Vector2(0, 0.5f);
        hcRt.offsetMin = Vector2.zero;
        hcRt.offsetMax = Vector2.zero;

        HorizontalLayoutGroup hlg = hContent.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 16;
        hlg.padding = new RectOffset(0, 40, 0, 0);
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;

        ContentSizeFitter hcsf = hContent.GetComponent<ContentSizeFitter>();
        hcsf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect hsr = hScrollObj.GetComponent<ScrollRect>();
        hsr.viewport = hViewport.GetComponent<RectTransform>();
        hsr.content = hcRt;
        hsr.horizontal = true;
        hsr.vertical = false;
        hsr.scrollSensitivity = 40;

        // Tier cards (horizontal)
        foreach (var tier in GameData.BATTLE_PASS_TIERS)
        {
            bool isUnlocked = ps.AccountLevel >= tier.level;
            bool freeClaimed = ps.ClaimedRewards.Contains(tier.level + "-free");
            bool premiumClaimed = ps.ClaimedRewards.Contains(tier.level + "-premium");

            GameObject tierCard = new GameObject("Tier" + tier.level, typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(VerticalLayoutGroup));
            tierCard.transform.SetParent(hContent.transform, false);
            tierCard.GetComponent<Image>().color = Color.black;
            LayoutElement tcLe = tierCard.GetComponent<LayoutElement>();
            tcLe.preferredWidth = 350;
            AddOutline(tierCard, isUnlocked ? Color.white : new Color(1, 1, 1, 0.3f));
            VerticalLayoutGroup vlg = tierCard.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(0, 0, 0, 0);
            vlg.spacing = 0;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            if (!isUnlocked) tierCard.AddComponent<CanvasGroup>().alpha = 0.7f;

            // Tier header bar
            GameObject tierHeader = new GameObject("TierHeader", typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(HorizontalLayoutGroup));
            tierHeader.transform.SetParent(tierCard.transform, false);
            tierHeader.GetComponent<Image>().color = isUnlocked ? Color.white : new Color(0.2f, 0.2f, 0.2f);
            tierHeader.GetComponent<LayoutElement>().preferredHeight = 55;
            HorizontalLayoutGroup thHlg = tierHeader.GetComponent<HorizontalLayoutGroup>();
            thHlg.padding = new RectOffset(15, 15, 0, 0);
            thHlg.childForceExpandHeight = true;
            thHlg.childForceExpandWidth = false;
            thHlg.childControlWidth = true;
            thHlg.childControlHeight = true;

            Color headerColor = isUnlocked ? Color.black : new Color(1, 1, 1, 0.7f);
            Text tierTitle = CreateTextElement(tierHeader.transform, "TTitle", $"TIER {tier.level}", 28, headerColor, TextAnchor.MiddleLeft);
            tierTitle.fontStyle = FontStyle.Bold;
            tierTitle.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            Text starsReq = CreateTextElement(tierHeader.transform, "Stars", $"{tier.starsReq} \u2605", 22, headerColor, TextAnchor.MiddleRight);
            starsReq.fontStyle = FontStyle.Bold;
            starsReq.gameObject.AddComponent<LayoutElement>().preferredWidth = 100;

            // Free section - VLG
            GameObject freeSection = new GameObject("FreeSection", typeof(RectTransform), typeof(LayoutElement), typeof(VerticalLayoutGroup));
            freeSection.transform.SetParent(tierCard.transform, false);
            freeSection.GetComponent<LayoutElement>().preferredHeight = 165;
            VerticalLayoutGroup fsVlg = freeSection.GetComponent<VerticalLayoutGroup>();
            fsVlg.padding = new RectOffset(15, 15, 10, 10);
            fsVlg.spacing = 4;
            fsVlg.childForceExpandWidth = true;
            fsVlg.childForceExpandHeight = false;
            fsVlg.childControlWidth = true;
            fsVlg.childControlHeight = true;

            Text freeLabel = CreateTextElement(freeSection.transform, "FreeLabel", "FREE", 20, new Color(1, 1, 1, 0.5f), TextAnchor.MiddleLeft);
            freeLabel.fontStyle = FontStyle.Bold;
            SetHeight(freeLabel.gameObject, 24);

            string freeNameStr = (tier.free.type == "ship" ? "\u2708 " : "") + tier.free.name;
            Text freeName = CreateTextElement(freeSection.transform, "FreeName", freeNameStr, 28, Color.white, TextAnchor.MiddleLeft);
            freeName.fontStyle = FontStyle.Bold;
            SetHeight(freeName.gameObject, 34);

            if (tier.free.credits > 0)
            {
                Text credTxt = CreateTextElement(freeSection.transform, "FreeCred", $"\u25C9 +{tier.free.credits} CREDITS", 20, new Color(1f, 0.84f, 0f), TextAnchor.MiddleLeft);
                credTxt.fontStyle = FontStyle.Bold;
                SetHeight(credTxt.gameObject, 24);
            }

            AddFlexSpacer(freeSection.transform);

            if (isUnlocked)
            {
                if (freeClaimed)
                {
                    Text claimedTxt = CreateTextElement(freeSection.transform, "Claimed", "\u2713 CLAIMED", 22, Color.white, TextAnchor.MiddleLeft);
                    claimedTxt.fontStyle = FontStyle.Bold;
                    SetHeight(claimedTxt.gameObject, 30);
                }
                else
                {
                    int ct = tier.level;
                    AddFullWidthButton(freeSection.transform, "CLAIM FREE", 40, () => { ps.ClaimReward(ct, false); RefreshTopNav(); RefreshContent(); });
                }
            }
            else
            {
                Text lockTxt = CreateTextElement(freeSection.transform, "Lock", "\u26BF LOCKED", 22, new Color(1, 1, 1, 0.3f), TextAnchor.MiddleLeft);
                SetHeight(lockTxt.gameObject, 30);
            }

            // Premium section - VLG with bg color
            GameObject premSection = new GameObject("PremSection", typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(VerticalLayoutGroup));
            premSection.transform.SetParent(tierCard.transform, false);
            premSection.GetComponent<Image>().color = ps.IsPremium ? Color.white : new Color(0.1f, 0.1f, 0.1f);
            premSection.GetComponent<LayoutElement>().preferredHeight = 165;
            premSection.GetComponent<LayoutElement>().flexibleHeight = 1;
            VerticalLayoutGroup psVlg = premSection.GetComponent<VerticalLayoutGroup>();
            psVlg.padding = new RectOffset(15, 15, 10, 10);
            psVlg.spacing = 4;
            psVlg.childForceExpandWidth = true;
            psVlg.childForceExpandHeight = false;
            psVlg.childControlWidth = true;
            psVlg.childControlHeight = true;

            Color premTextColor = ps.IsPremium ? Color.black : Color.white;

            Text premLabel = CreateTextElement(premSection.transform, "PremLabel", "\u265B PREMIUM", 20, premTextColor * 0.7f, TextAnchor.MiddleLeft);
            premLabel.fontStyle = FontStyle.Bold;
            SetHeight(premLabel.gameObject, 24);

            Text premName = CreateTextElement(premSection.transform, "PremName", tier.premium.name, 28, premTextColor, TextAnchor.MiddleLeft);
            premName.fontStyle = FontStyle.Bold;
            SetHeight(premName.gameObject, 34);

            if (tier.premium.credits > 0)
            {
                Color credColor = ps.IsPremium ? new Color(0, 0, 0, 0.7f) : new Color(1f, 0.84f, 0f);
                Text premCred = CreateTextElement(premSection.transform, "PremCred", $"\u25C9 +{tier.premium.credits} CREDITS", 20, credColor, TextAnchor.MiddleLeft);
                premCred.fontStyle = FontStyle.Bold;
                SetHeight(premCred.gameObject, 24);
            }

            AddFlexSpacer(premSection.transform);

            if (!ps.IsPremium)
            {
                GameObject lockOverlay = new GameObject("LockOvr", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                lockOverlay.transform.SetParent(premSection.transform, false);
                lockOverlay.GetComponent<Image>().color = new Color(0, 0, 0, 0.8f);
                lockOverlay.GetComponent<LayoutElement>().ignoreLayout = true;
                FillRect(lockOverlay);
                Text lockIcon = CreateTextElement(lockOverlay.transform, "LockIcon", "\u26BF", 48, new Color(1, 1, 1, 0.5f), TextAnchor.MiddleCenter);
                FillRect(lockIcon);
            }
            else if (isUnlocked)
            {
                if (premiumClaimed)
                {
                    Text pClaimedTxt = CreateTextElement(premSection.transform, "PClaimed", "\u2713 CLAIMED", 22, Color.black, TextAnchor.MiddleLeft);
                    pClaimedTxt.fontStyle = FontStyle.Bold;
                    SetHeight(pClaimedTxt.gameObject, 30);
                }
                else
                {
                    int ct = tier.level;
                    GameObject claimBtn = new GameObject("ClaimPrem", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                    claimBtn.transform.SetParent(premSection.transform, false);
                    claimBtn.GetComponent<Image>().color = Color.black;
                    claimBtn.GetComponent<LayoutElement>().preferredHeight = 40;
                    Text cbTxt = CreateTextElement(claimBtn.transform, "Lbl", "CLAIM PREM", 22, Color.white, TextAnchor.MiddleCenter);
                    cbTxt.fontStyle = FontStyle.Bold; cbTxt.raycastTarget = false; FillRect(cbTxt);
                    claimBtn.GetComponent<Button>().onClick.AddListener(() => { ps.ClaimReward(ct, true); RefreshTopNav(); RefreshContent(); });
                }
            }
        }
    }

    // ==================== SHOP VIEW ====================
    void BuildShopView(Transform parent)
    {
        var ps = PlayerState.Instance;

        Text title = CreateTextElement(parent, "Title", "CURRENCY TERMINAL", 64, Color.white, TextAnchor.MiddleLeft);
        title.fontStyle = FontStyle.Bold;
        SetHeight(title.gameObject, 80);
        AddSeparator(parent);

        // Credit packs in a single row of 3
        {
            GameObject packRow = CreateHLayout(parent, "PackRow", Vector2.zero, TextAnchor.MiddleLeft);
            SetHeight(packRow, 200);
            packRow.GetComponent<HorizontalLayoutGroup>().spacing = 24;

            foreach (var pack in GameData.CREDIT_PACKS)
            {
                GameObject card = new GameObject("Pack_" + pack.amount, typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(VerticalLayoutGroup));
                card.transform.SetParent(packRow.transform, false);
                card.GetComponent<Image>().color = Color.black;
                card.GetComponent<LayoutElement>().flexibleWidth = 1;
                AddOutline(card);
                VerticalLayoutGroup vlg = card.GetComponent<VerticalLayoutGroup>();
                vlg.padding = new RectOffset(20, 20, 20, 20);
                vlg.spacing = 10;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;
                vlg.childControlWidth = true;
                vlg.childControlHeight = true;
                vlg.childAlignment = TextAnchor.MiddleCenter;

                Text amountTxt = CreateTextElement(card.transform, "Amount", $"\u25C9 {pack.amount} +", 26, new Color(1f, 0.84f, 0f), TextAnchor.MiddleCenter);
                amountTxt.fontStyle = FontStyle.Bold;
                amountTxt.raycastTarget = false;
                SetHeight(amountTxt.gameObject, 36);

                Text credLbl = CreateTextElement(card.transform, "CredLbl", "CREDITS", 16, new Color(1, 1, 1, 0.6f), TextAnchor.MiddleCenter);
                credLbl.fontStyle = FontStyle.Bold;
                credLbl.raycastTarget = false;
                SetHeight(credLbl.gameObject, 20);

                AddFlexSpacer(card.transform);

                int capturedAmount = pack.amount;
                GameObject buyBtn = new GameObject("Buy", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                buyBtn.transform.SetParent(card.transform, false);
                buyBtn.GetComponent<Image>().color = Color.white;
                buyBtn.GetComponent<LayoutElement>().preferredHeight = 48;
                Text buyTxt = CreateTextElement(buyBtn.transform, "Lbl", pack.price, 18, Color.black, TextAnchor.MiddleCenter);
                buyTxt.fontStyle = FontStyle.Bold; buyTxt.raycastTarget = false; FillRect(buyTxt);
                buyBtn.GetComponent<Button>().onClick.AddListener(() => { SimulatePurchase(capturedAmount); });
            }
        }

        AddSpacer(parent, 30);

        Text puHeader = CreateTextElement(parent, "PUHeader", "\u26A1 POWER UPS", 42, new Color(1, 1, 1, 0.7f), TextAnchor.MiddleLeft);
        puHeader.fontStyle = FontStyle.Bold;
        SetHeight(puHeader.gameObject, 50);
        AddSeparator(parent);

        // Power up items in rows of 3
        {
            var items = GameData.SHOP_ITEMS;
            for (int i = 0; i < items.Length; i += 3)
            {
                GameObject row = CreateHLayout(parent, "ItemRow" + i, Vector2.zero, TextAnchor.MiddleLeft);
                SetHeight(row, 180);
                row.GetComponent<HorizontalLayoutGroup>().spacing = 24;

                for (int j = i; j < Mathf.Min(i + 3, items.Length); j++)
                {
                    var item = items[j];
                    GameObject card = new GameObject("Item_" + item.name, typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(VerticalLayoutGroup));
                    card.transform.SetParent(row.transform, false);
                    card.GetComponent<Image>().color = Color.black;
                    card.GetComponent<LayoutElement>().flexibleWidth = 1;
                    AddOutline(card, new Color(1, 1, 1, 0.5f));
                    VerticalLayoutGroup vlg = card.GetComponent<VerticalLayoutGroup>();
                    vlg.padding = new RectOffset(20, 20, 15, 15);
                    vlg.spacing = 4;
                    vlg.childForceExpandWidth = true;
                    vlg.childForceExpandHeight = false;
                    vlg.childControlWidth = true;
                    vlg.childControlHeight = true;

                    Text nameText = CreateTextElement(card.transform, "Name", $"{item.name.ToUpper()} X{item.qty}", 20, Color.white, TextAnchor.MiddleLeft);
                    nameText.fontStyle = FontStyle.Bold;
                    nameText.raycastTarget = false;
                    SetHeight(nameText.gameObject, 26);

                    Text descText = CreateTextElement(card.transform, "Desc", item.desc.ToUpper(), 14, new Color(1, 1, 1, 0.5f), TextAnchor.UpperLeft);
                    descText.horizontalOverflow = HorizontalWrapMode.Wrap;
                    descText.raycastTarget = false;
                    SetHeight(descText.gameObject, 40);

                    AddFlexSpacer(card.transform);

                    // Price + Buy row
                    GameObject priceRow = CreateHLayout(card.transform, "PriceRow", Vector2.zero, TextAnchor.MiddleLeft);
                    SetHeight(priceRow, 44);
                    priceRow.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;

                    string priceStr = $"\u25C9 {item.cost}";
                    if (item.originalCost > 0) priceStr = $"\u25C9 {item.cost} (was {item.originalCost})";
                    Text priceTxt = CreateTextElement(priceRow.transform, "Price", priceStr, 16, new Color(1f, 0.84f, 0f), TextAnchor.MiddleLeft);
                    priceTxt.fontStyle = FontStyle.Bold;
                    priceTxt.raycastTarget = false;
                    LayoutElement plLe = priceTxt.gameObject.AddComponent<LayoutElement>();
                    plLe.flexibleWidth = 1;

                    GameObject buyBtn = new GameObject("Buy", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                    buyBtn.transform.SetParent(priceRow.transform, false);
                    buyBtn.GetComponent<Image>().color = Color.white;
                    buyBtn.GetComponent<LayoutElement>().preferredWidth = 80;
                    buyBtn.GetComponent<LayoutElement>().preferredHeight = 38;
                    Text buyTxt = CreateTextElement(buyBtn.transform, "Lbl", "BUY", 16, Color.black, TextAnchor.MiddleCenter);
                    buyTxt.fontStyle = FontStyle.Bold; buyTxt.raycastTarget = false; FillRect(buyTxt);
                }
            }
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
        vlg.spacing = 12;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;

        // Header
        Text headerLeft = CreateTextElement(container.transform, "SecureLabel", "TRANSMISSION SECURE", 20, new Color(1, 1, 1, 0.5f), TextAnchor.MiddleLeft);
        headerLeft.raycastTarget = false;
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

            GameObject shipRow = new GameObject("Ship_" + ship.id, typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(HorizontalLayoutGroup));
            shipRow.transform.SetParent(container.transform, false);
            shipRow.GetComponent<Image>().color = isEquipped ? Color.white : Color.black;
            shipRow.GetComponent<LayoutElement>().preferredHeight = 60;
            AddOutline(shipRow, isEquipped ? Color.white : new Color(1, 1, 1, 0.3f));
            HorizontalLayoutGroup srHlg = shipRow.GetComponent<HorizontalLayoutGroup>();
            srHlg.padding = new RectOffset(15, 15, 0, 0);
            srHlg.childForceExpandHeight = true;
            srHlg.childForceExpandWidth = true;
            srHlg.childControlWidth = true;
            srHlg.childControlHeight = true;

            Text shipTxt = CreateTextElement(shipRow.transform, "Name",
                (isEquipped ? "\u2713 " : "") + ship.name.ToUpper(), 24,
                isEquipped ? Color.black : new Color(1, 1, 1, 0.5f), TextAnchor.MiddleLeft);
            shipTxt.fontStyle = FontStyle.Bold;
            shipTxt.raycastTarget = false;

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

            GameObject hazRow = new GameObject("Haz_" + mechId, typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(HorizontalLayoutGroup));
            hazRow.transform.SetParent(container.transform, false);
            hazRow.GetComponent<Image>().color = Color.black;
            hazRow.GetComponent<LayoutElement>().preferredHeight = 60;
            AddOutline(hazRow, isMet ? mech.color : Color.red);
            HorizontalLayoutGroup hzHlg = hazRow.GetComponent<HorizontalLayoutGroup>();
            hzHlg.padding = new RectOffset(15, 15, 0, 0);
            hzHlg.childForceExpandHeight = true;
            hzHlg.childForceExpandWidth = true;
            hzHlg.childControlWidth = true;
            hzHlg.childControlHeight = true;

            string status = isMet ? "Addressed by Fleet" : ("Missing: " + (GameData.SHIPS.ContainsKey(mech.reqShip) ? GameData.SHIPS[mech.reqShip].name : "Unknown"));
            Text hazTxt = CreateTextElement(hazRow.transform, "Haz", $"\u26A0 {mech.name.ToUpper()} - {status}", 22,
                isMet ? mech.color : Color.red, TextAnchor.MiddleLeft);
            hazTxt.fontStyle = FontStyle.Bold;
            hazTxt.raycastTarget = false;
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
        hudLocationText = CreateTextElement(gameHudPanel.transform, "Location", "", 30, Color.white, TextAnchor.UpperLeft);
        hudLocationText.fontStyle = FontStyle.Bold;
        RectTransform locRt = hudLocationText.GetComponent<RectTransform>();
        locRt.anchorMin = new Vector2(0, 1);
        locRt.anchorMax = new Vector2(0.6f, 1);
        locRt.pivot = new Vector2(0, 1);
        locRt.sizeDelta = new Vector2(0, 40);
        locRt.anchoredPosition = new Vector2(40, -40);

        // Shield text
        hudShieldText = CreateTextElement(gameHudPanel.transform, "Shield", "", 24, Color.white, TextAnchor.UpperLeft);
        RectTransform shRt = hudShieldText.GetComponent<RectTransform>();
        shRt.anchorMin = new Vector2(0, 1);
        shRt.anchorMax = new Vector2(0.6f, 1);
        shRt.pivot = new Vector2(0, 1);
        shRt.sizeDelta = new Vector2(0, 30);
        shRt.anchoredPosition = new Vector2(40, -85);

        // Fleet text
        hudFleetText = CreateTextElement(gameHudPanel.transform, "Fleet", "", 22, Color.white, TextAnchor.UpperLeft);
        // text-[10px] scaled
        hudFleetText.fontStyle = FontStyle.Bold;
        RectTransform flRt = hudFleetText.GetComponent<RectTransform>();
        flRt.anchorMin = new Vector2(0, 1);
        flRt.anchorMax = new Vector2(0.6f, 1);
        flRt.pivot = new Vector2(0, 1);
        flRt.sizeDelta = new Vector2(0, 25);
        flRt.anchoredPosition = new Vector2(40, -120);

        // HUD message (bottom center)
        hudMessageText = CreateTextElement(gameHudPanel.transform, "Message", "", 24, new Color(1, 1, 1, 0.5f), TextAnchor.MiddleCenter);
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
        fbRt.sizeDelta = new Vector2(180, 60);
        fbRt.anchoredPosition = new Vector2(40, 80);

        Text fbTxt = CreateTextElement(fleetBtn.transform, "Lbl", "\u2708 FLEET", 22, Color.white, TextAnchor.MiddleCenter);
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
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childAlignment = TextAnchor.MiddleCenter;

        Text goTitle = CreateTextElement(box.transform, "Title", "SIGNAL LOST", 42, Color.white, TextAnchor.MiddleCenter);
        goTitle.fontStyle = FontStyle.Bold;
        SetHeight(goTitle.gameObject, 60);

        Text goDesc = CreateTextElement(box.transform, "Desc", "Trajectory compromised.", 30, new Color(0.8f, 0.8f, 0.8f), TextAnchor.MiddleCenter);
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
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
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
        vlg.spacing = 12;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;

        Text title = CreateTextElement(box.transform, "Title", "SELECT VESSEL", 32, Color.white, TextAnchor.MiddleLeft);
        title.fontStyle = FontStyle.Bold;
        SetHeight(title.gameObject, 40);
        AddSeparatorIn(box.transform);

        foreach (string id in shipIds)
        {
            if (!GameData.SHIPS.ContainsKey(id)) continue;
            var ship = GameData.SHIPS[id];
            bool isActive = (id == activeShipId);

            GameObject shipBtn = new GameObject("Ship_" + id, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement), typeof(HorizontalLayoutGroup));
            shipBtn.transform.SetParent(box.transform, false);
            shipBtn.GetComponent<Image>().color = isActive ? Color.white : Color.black;
            AddOutline(shipBtn, isActive ? Color.white : new Color(1, 1, 1, 0.3f));
            shipBtn.GetComponent<LayoutElement>().preferredHeight = 70;
            HorizontalLayoutGroup sbHlg = shipBtn.GetComponent<HorizontalLayoutGroup>();
            sbHlg.padding = new RectOffset(20, 20, 0, 0);
            sbHlg.childForceExpandHeight = true;
            sbHlg.childForceExpandWidth = true;
            sbHlg.childControlWidth = true;
            sbHlg.childControlHeight = true;

            Text shipTxt = CreateTextElement(shipBtn.transform, "Name",
                (isActive ? "\u2713 " : "") + ship.name.Split(' ')[0].ToUpper(), 24,
                isActive ? Color.black : Color.white, TextAnchor.MiddleLeft);
            shipTxt.fontStyle = FontStyle.Bold;
            shipTxt.raycastTarget = false;

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
        vlg.spacing = 12;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;

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

            GameObject row = new GameObject("Row" + l, typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(box.transform, false);
            row.GetComponent<Image>().color = Color.black;
            row.GetComponent<LayoutElement>().preferredHeight = 40;
            HorizontalLayoutGroup rowHlg = row.GetComponent<HorizontalLayoutGroup>();
            rowHlg.childForceExpandHeight = true;
            rowHlg.childForceExpandWidth = false;
            rowHlg.childControlWidth = true;
            rowHlg.childControlHeight = true;

            Text leftTxt = CreateTextElement(row.transform, "Left", "", 28, new Color(1, 1, 1, 0.7f), TextAnchor.MiddleLeft);
            leftTxt.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            Text rightTxt = CreateTextElement(row.transform, "Right", "", 28, Color.white, TextAnchor.MiddleRight);
            rightTxt.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            // Typewriter effect
            string leftStr = $"LEVEL {l}";
            string rightStr = $"{st} STAR{(st != 1 ? "S" : "")}";

            yield return TypewriterEffect(leftTxt, leftStr, 0.04f);
            yield return TypewriterEffect(rightTxt, rightStr, 0.04f);
            yield return new WaitForSeconds(0.2f);
        }

        AddSeparatorIn(box.transform);

        // Total
        GameObject totalRow = new GameObject("Total", typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(HorizontalLayoutGroup));
        totalRow.transform.SetParent(box.transform, false);
        totalRow.GetComponent<Image>().color = Color.black;
        totalRow.GetComponent<LayoutElement>().preferredHeight = 50;
        HorizontalLayoutGroup tRowHlg = totalRow.GetComponent<HorizontalLayoutGroup>();
        tRowHlg.childForceExpandHeight = true;
        tRowHlg.childForceExpandWidth = false;
        tRowHlg.childControlWidth = true;
        tRowHlg.childControlHeight = true;

        Text totalLeftTxt = CreateTextElement(totalRow.transform, "TLeft", "", 32, Color.white, TextAnchor.MiddleLeft);
        totalLeftTxt.fontStyle = FontStyle.Bold;
        totalLeftTxt.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

        Text totalRightTxt = CreateTextElement(totalRow.transform, "TRight", "", 32, Color.white, TextAnchor.MiddleRight);
        totalRightTxt.fontStyle = FontStyle.Bold;
        totalRightTxt.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

        yield return TypewriterEffect(totalLeftTxt, "TOTAL STARS", 0.04f);
        yield return TypewriterEffect(totalRightTxt, $"{totalStars}/15", 0.04f);
        yield return new WaitForSeconds(0.3f);

        // Complete button
        AddFullWidthButton(box.transform, "SECTOR COMPLETED!", 80, () =>
        {
            sectorSummaryPanel.SetActive(false);
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
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
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
        ol.effectDistance = new Vector2(1, -1);
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

    void AddFlexSpacer(Transform parent)
    {
        GameObject spacer = new GameObject("FlexSpacer", typeof(RectTransform), typeof(LayoutElement));
        spacer.transform.SetParent(parent, false);
        spacer.GetComponent<LayoutElement>().flexibleHeight = 1;
    }

    void AddSmallButton(Transform parent, string label, bool isActive, System.Action onClick)
    {
        GameObject btn = new GameObject(label + "Btn", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        btn.transform.SetParent(parent, false);
        btn.GetComponent<Image>().color = isActive ? Color.white : Color.black;
        AddOutline(btn);
        btn.GetComponent<LayoutElement>().preferredWidth = 110;
        btn.GetComponent<LayoutElement>().preferredHeight = 50;

        Text txt = CreateTextElement(btn.transform, "Lbl", label, 26, isActive ? Color.black : Color.white, TextAnchor.MiddleCenter);
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
