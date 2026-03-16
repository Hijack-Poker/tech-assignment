using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using System.IO;

public static class RebuildScene
{
    static readonly Color BG = H("07121E");
    static readonly Color FELT = H("175C4A");
    static readonly Color FELT_L = H("21836A");
    static readonly Color RAIL_O = H("0A111A");
    static readonly Color RAIL_I = H("29495A");
    static readonly Color PANEL = H("0A1624", 0.95f);
    static readonly Color GOLD = H("F4C86E");
    static readonly Color ORANGE = H("F0AE63");
    static readonly Color TW = H("EBF4FF");
    static readonly Color TD = H("8DA4BC");
    static readonly Color SEAT_BG = H("0B1724", 0.9f);
    static readonly Color SEAT_BORDER = H("70B6D0", 0.25f);
    static readonly Color CE = new Color(1, 1, 1, 0.06f);
    static readonly Color SHADOW = new Color(0, 0, 0, 0.56f);

    // Dealer palette
    static readonly Color SKIN = H("D4A574");
    static readonly Color SKIN_SHADE = H("C08B5C");
    static readonly Color VEST = H("1A1A2E");
    static readonly Color VEST_HIGHLIGHT = H("252545");
    static readonly Color SHIRT = H("F0F0F0");
    static readonly Color BOWTIE = H("C0392B");
    static readonly Color BOWTIE_DARK = H("962D22");
    static readonly Color HAIR = H("2C1810");
    static readonly Color HAIR_HIGHLIGHT = H("3D251A");
    static readonly Color NAMETAG_BG = H("FFD700");
    static readonly Color NAMETAG_TEXT = H("1A0A05");
    static readonly Color CHIP_GREEN = H("2E7D32");
    static readonly Color CHIP_EDGE = H("1B5E20");
    static readonly Color CHIP_STRIPE = H("FFFFFF", 0.3f);

    static Sprite _oval, _roundRect;
    static Sprite[] _chipSprites;
    static readonly string[] ChipNames = { "chipRedWhite", "chipGreenWhite", "chipBlueWhite", "chipBlackWhite" };

    [MenuItem("Tools/Rebuild Scene")]
    public static void Build()
    {
        _oval = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/OvalFelt.png");
        if (!_oval) { Debug.LogError("OvalFelt.png missing!"); return; }
        _roundRect = GenerateRoundedRect("RoundRect", 128, 128, 20);
        _chipSprites = new Sprite[ChipNames.Length];
        for (int i = 0; i < ChipNames.Length; i++)
            _chipSprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Sprites/Chips/{ChipNames[i]}.png");
        var pfb = HistoryPrefab();

        var sc = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        Camera.main.backgroundColor = BG;
        Camera.main.clearFlags = CameraClearFlags.SolidColor;

        // Canvas
        var cv = new GameObject("PokerTableCanvas");
        var canvas = cv.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = cv.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        cv.AddComponent<GraphicRaycaster>();

        Vector2 TC = new Vector2(-80, 15);

        // ══════ TABLE ══════
        OvalImg("RailOuter", cv.transform, RAIL_O, TC, 1220, 710);
        OvalImg("RailInner", cv.transform, RAIL_I, TC, 1180, 675);
        var felt = OvalImg("TableFelt", cv.transform, FELT, TC, 1120, 630);
        OvalImg("FeltHighlight", cv.transform, FELT_L, TC, 900, 470);
        OvalImg("FeltAura", cv.transform, H("33B7A1", 0.08f), TC + new Vector2(0, -20), 1040, 560);

        // ══════ DEALER ══════
        BuildDealer(cv.transform, TC + new Vector2(0, 418));

        // ══════ COMMUNITY CARDS ══════
        var ccArea = UI("CommunityCardsArea", cv.transform);
        Rect(ccArea, 0.5f, 0.5f, 0.5f, 0.5f, TC + new Vector2(0, 30), new Vector2(440, 100));
        var ccHLG = ccArea.AddComponent<HorizontalLayoutGroup>();
        ccHLG.spacing = 8; ccHLG.childAlignment = TextAnchor.MiddleCenter;
        ccHLG.childForceExpandWidth = false; ccHLG.childForceExpandHeight = false;
        var ccViews = new HijackPoker.UI.CardView[5];
        for (int i = 0; i < 5; i++) ccViews[i] = CardWithShadow($"CC{i + 1}", ccArea.transform, 70, 97);
        var ccComp = ccArea.AddComponent<HijackPoker.UI.CommunityCardsView>();
        WArr(ccComp, "_slots", ccViews);

        // Pot chip stack
        var potChipGO = UI("PotChips", cv.transform);
        var potChipImg = potChipGO.AddComponent<Image>();
        potChipImg.sprite = _chipSprites[0]; // red chip
        potChipImg.preserveAspect = true;
        Rect(potChipGO, 0.5f, 0.5f, 0.5f, 0.5f, TC + new Vector2(-80, -70), new Vector2(36, 36));
        potChipGO.SetActive(false);

        var potChip2 = UI("PotChip2", cv.transform);
        var potChip2Img = potChip2.AddComponent<Image>();
        potChip2Img.sprite = _chipSprites[1]; // green chip
        potChip2Img.preserveAspect = true;
        Rect(potChip2, 0.5f, 0.5f, 0.5f, 0.5f, TC + new Vector2(-65, -62), new Vector2(36, 36));
        potChip2.transform.SetParent(potChipGO.transform, true);

        var potChip3 = UI("PotChip3", cv.transform);
        var potChip3Img = potChip3.AddComponent<Image>();
        potChip3Img.sprite = _chipSprites[3]; // black chip
        potChip3Img.preserveAspect = true;
        Rect(potChip3, 0.5f, 0.5f, 0.5f, 0.5f, TC + new Vector2(-72, -55), new Vector2(36, 36));
        potChip3.transform.SetParent(potChipGO.transform, true);

        // Pot text
        var potGO = UI("PotText", cv.transform);
        var potTMP = potGO.AddComponent<TextMeshProUGUI>();
        potTMP.fontSize = 24; potTMP.fontStyle = FontStyles.Bold;
        potTMP.alignment = TextAlignmentOptions.Center; potTMP.color = GOLD;
        potTMP.enableAutoSizing = false;
        Rect(potGO, 0.5f, 0.5f, 0.5f, 0.5f, TC + new Vector2(0, -40), new Vector2(300, 36));

        // ══════ 6 SEATS ══════
        Vector2[] seatPos = {
            TC + new Vector2(-450, 200),
            TC + new Vector2(450, 200),
            TC + new Vector2(529, -85),
            TC + new Vector2(260, -310),
            TC + new Vector2(-260, -310),
            TC + new Vector2(-529, -85),
        };
        var seatViews = new HijackPoker.UI.SeatView[6];
        for (int i = 0; i < 6; i++)
            seatViews[i] = BuildSeat(i + 1, cv.transform, seatPos[i]);
        var tableView = felt.AddComponent<HijackPoker.UI.TableView>();

        // ══════ HUD ══════
        var hud = UI("HUDPanel", cv.transform);
        hud.AddComponent<Image>().color = PANEL;
        var hudRT = hud.GetComponent<RectTransform>();
        hudRT.anchorMin = new Vector2(0, 1); hudRT.anchorMax = new Vector2(1, 1);
        hudRT.pivot = new Vector2(0.5f, 1);
        hudRT.anchoredPosition = Vector2.zero; hudRT.sizeDelta = new Vector2(0, 50);

        var hn = UI("HandNumberText", hud.transform);
        var hnT = hn.AddComponent<TextMeshProUGUI>();
        hnT.text = "HIJACK POKER"; hnT.fontSize = 16; hnT.fontStyle = FontStyles.Bold;
        hnT.alignment = TextAlignmentOptions.Left; hnT.color = GOLD; hnT.characterSpacing = 4;
        Anch(hn, 0, 0, 0.2f, 1, new Vector2(20, 0), Vector2.zero);

        var ph = UI("PhaseLabel", hud.transform);
        var phT = ph.AddComponent<TextMeshProUGUI>();
        phT.text = "Connecting..."; phT.fontSize = 14;
        phT.alignment = TextAlignmentOptions.Center; phT.color = ORANGE;
        Anch(ph, 0.2f, 0, 0.55f, 1, Vector2.zero, Vector2.zero);

        var pd = UI("PotDisplayText", hud.transform);
        var pdT = pd.AddComponent<TextMeshProUGUI>();
        pdT.fontSize = 15; pdT.fontStyle = FontStyles.Bold;
        pdT.alignment = TextAlignmentOptions.Center; pdT.color = GOLD;
        Anch(pd, 0.55f, 0, 0.75f, 1, Vector2.zero, Vector2.zero);

        var ac = UI("ActionText", hud.transform);
        var acT = ac.AddComponent<TextMeshProUGUI>();
        acT.fontSize = 12; acT.alignment = TextAlignmentOptions.Right; acT.color = TD;
        Anch(ac, 0.75f, 0, 1, 1, Vector2.zero, new Vector2(-16, 0));

        var hudBorder = UI("HudBottomBorder", hud.transform);
        var hudBorderImg = hudBorder.AddComponent<Image>();
        hudBorderImg.color = H("78C6D6", 0.2f);
        Anch(hudBorder, 0, 0, 1, 0, Vector2.zero, new Vector2(0, 1));
        var hudView = hud.AddComponent<HijackPoker.UI.HudView>();

        // ══════ CONTROLS ══════
        var ctrl = UI("ControlsPanel", cv.transform);
        var ctrlBg = ctrl.AddComponent<Image>();
        ctrlBg.sprite = _roundRect; ctrlBg.type = Image.Type.Sliced;
        ctrlBg.color = H("0B1623", 0.97f);
        var ctrlRT = ctrl.GetComponent<RectTransform>();
        ctrlRT.anchorMin = new Vector2(0, 0); ctrlRT.anchorMax = new Vector2(1, 0);
        ctrlRT.pivot = new Vector2(0.5f, 0);
        ctrlRT.anchoredPosition = Vector2.zero; ctrlRT.sizeDelta = new Vector2(0, 86);

        // Top border line (subtle gold accent)
        var ctrlBorder = UI("CtrlBorder", ctrl.transform);
        var ctrlBorderImg = ctrlBorder.AddComponent<Image>();
        ctrlBorderImg.color = H("7AD0DD", 0.24f);
        Anch(ctrlBorder, 0, 1, 1, 1, Vector2.zero, new Vector2(0, 1));

        var ctrlHLG = ctrl.AddComponent<HorizontalLayoutGroup>();
        ctrlHLG.spacing = 16; ctrlHLG.childAlignment = TextAnchor.MiddleCenter;
        ctrlHLG.padding = new RectOffset(24, 24, 8, 8);
        ctrlHLG.childForceExpandWidth = false; ctrlHLG.childForceExpandHeight = false;

        // ── Next Step button (green gradient style) ──
        var nb = FancyBtn("NextStepButton", "NEXT STEP", ctrl.transform, 160, 70,
            H("1F8E67"), H("3FBE95"), H("176749"));

        // ── Auto Play button (blue gradient style) ──
        var ab = FancyBtn("AutoPlayButton", "AUTO PLAY", ctrl.transform, 160, 70,
            H("1D79D8"), H("53A8F8"), H("155BA6"));
        var abTxt = ab.GetComponentInChildren<TextMeshProUGUI>();

        // ── Separator ──
        var sep = UI("Separator", ctrl.transform);
        var sepImg = sep.AddComponent<Image>(); sepImg.color = H("FFFFFF", 0.08f);
        sep.AddComponent<LayoutElement>().preferredWidth = 1;
        sep.GetComponent<LayoutElement>().preferredHeight = 30;

        // ── Speed label ──
        var spdLabel = UI("SpeedLabel", ctrl.transform);
        var spdLabelTMP = spdLabel.AddComponent<TextMeshProUGUI>();
        spdLabelTMP.text = "SPEED"; spdLabelTMP.fontSize = 9; spdLabelTMP.fontStyle = FontStyles.Bold;
        spdLabelTMP.alignment = TextAlignmentOptions.Center; spdLabelTMP.color = H("8EA4BA");
        spdLabelTMP.characterSpacing = 3;
        spdLabel.AddComponent<LayoutElement>().preferredWidth = 42;

        // ── Speed pill group (connected rounded buttons) ──
        var sg = UI("SpeedGroup", ctrl.transform);
        var sgBg = sg.AddComponent<Image>();
        sgBg.sprite = _roundRect; sgBg.type = Image.Type.Sliced;
        sgBg.color = H("0F2132");
        var sgH = sg.AddComponent<HorizontalLayoutGroup>();
        sgH.spacing = 2; sgH.childAlignment = TextAnchor.MiddleCenter;
        sgH.padding = new RectOffset(3, 3, 3, 3);
        sgH.childForceExpandWidth = false; sgH.childForceExpandHeight = false;
        sg.AddComponent<LayoutElement>().preferredWidth = 250;
        sg.GetComponent<LayoutElement>().preferredHeight = 70;

        string[] spd = { "0.25s", "0.5s", "1s", "2s" };
        var sBtns = new Button[4]; var sImgs = new Image[4];
        for (int i = 0; i < 4; i++)
        {
            var b = SpeedPill($"Speed_{spd[i]}", spd[i], sg.transform, 58, 60);
            sBtns[i] = b.GetComponent<Button>(); sImgs[i] = b.GetComponent<Image>();
        }
        var ctrlView = ctrl.AddComponent<HijackPoker.UI.ControlsView>();

        // ══════ HISTORY ══════
        var hist = UI("HistoryPanel", cv.transform);
        var histBg = hist.AddComponent<Image>();
        histBg.sprite = _roundRect; histBg.type = Image.Type.Sliced;
        histBg.color = H("0A1623", 0.94f);
        var histRT = hist.GetComponent<RectTransform>();
        histRT.anchorMin = new Vector2(1, 0); histRT.anchorMax = new Vector2(1, 1);
        histRT.pivot = new Vector2(1, 0.5f);
        histRT.anchoredPosition = Vector2.zero;
        histRT.offsetMin = new Vector2(-350, 86); histRT.offsetMax = new Vector2(0, -50);
        var hVLG = hist.AddComponent<VerticalLayoutGroup>();
        hVLG.spacing = 3; hVLG.padding = new RectOffset(10, 10, 10, 10);
        hVLG.childForceExpandWidth = true; hVLG.childForceExpandHeight = false;

        var hdr = UI("Header", hist.transform);
        var hdrT = hdr.AddComponent<TextMeshProUGUI>();
        hdrT.text = "HAND HISTORY"; hdrT.fontSize = 11; hdrT.fontStyle = FontStyles.Bold;
        hdrT.alignment = TextAlignmentOptions.Center; hdrT.color = H("9BB2C8"); hdrT.characterSpacing = 4;
        hdr.AddComponent<LayoutElement>().preferredHeight = 22;

        var dv = UI("Div", hist.transform);
        dv.AddComponent<Image>().color = H("7CC9D6", 0.18f);
        dv.AddComponent<LayoutElement>().preferredHeight = 1;

        var scr = UI("Scroll", hist.transform);
        var sr = scr.AddComponent<ScrollRect>();
        sr.horizontal = false; sr.vertical = true; sr.scrollSensitivity = 20;
        scr.AddComponent<LayoutElement>().flexibleHeight = 1;
        var vp = UI("VP", scr.transform);
        vp.AddComponent<Image>().color = Color.clear;
        vp.AddComponent<Mask>().showMaskGraphic = false; Stretch(vp);
        var cnt = UI("Content", vp.transform);
        var cntVLG = cnt.AddComponent<VerticalLayoutGroup>();
        cntVLG.spacing = 2; cntVLG.padding = new RectOffset(2, 2, 2, 2);
        cntVLG.childForceExpandWidth = true; cntVLG.childForceExpandHeight = false;
        cnt.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var cntRT = cnt.GetComponent<RectTransform>();
        cntRT.anchorMin = new Vector2(0, 1); cntRT.anchorMax = new Vector2(1, 1);
        cntRT.pivot = new Vector2(0.5f, 1); cntRT.sizeDelta = Vector2.zero;
        sr.viewport = vp.GetComponent<RectTransform>(); sr.content = cntRT;
        var histView = hist.AddComponent<HijackPoker.UI.HandHistoryView>();

        // ══════ GAME MANAGER ══════
        var gmGO = new GameObject("GameManager");
        var api = gmGO.AddComponent<HijackPoker.Api.PokerApiClient>();
        var sm = gmGO.AddComponent<HijackPoker.Managers.TableStateManager>();
        var gm = gmGO.AddComponent<HijackPoker.Managers.GameManager>();

        // ══════ WIRE ══════
        W(gm, "_apiClient", api); W(gm, "_stateManager", sm);
        W(tableView, "_stateManager", sm); W(tableView, "_communityCardsView", ccComp);
        WArr(tableView, "_seatViews", seatViews);
        W(hudView, "_stateManager", sm); W(hudView, "_phaseLabel", phT);
        W(hudView, "_handNumberText", hnT); W(hudView, "_actionText", acT); W(hudView, "_potText", pdT);
        W(hudView, "_potChipImage", potChipImg);
        W(ctrlView, "_gameManager", gm);
        W(ctrlView, "_nextStepButton", nb.GetComponent<Button>());
        W(ctrlView, "_autoPlayButton", ab.GetComponent<Button>());
        W(ctrlView, "_autoPlayButtonText", abTxt);
        WArr(ctrlView, "_speedButtons", sBtns); WArr(ctrlView, "_speedButtonImages", sImgs);
        W(histView, "_stateManager", sm); W(histView, "_content", cnt.transform);
        W(histView, "_scrollRect", sr); W(histView, "_entryPrefab", pfb);

        // EventSystem
        if (!Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>())
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        EditorSceneManager.SaveScene(sc, "Assets/Scenes/PokerTable.unity");
        Debug.Log("Polished scene built!");
    }

    // ══════ DEALER CHARACTER ══════
    static void BuildDealer(Transform parent, Vector2 pos)
    {
        var root = UI("Dealer", parent);
        Rect(root, 0.5f, 0.5f, 0.5f, 0.5f, pos, new Vector2(160, 200));

        // ── Body / Vest (trapezoid via wider bottom) ──
        var body = UI("Body", root.transform);
        var bodyImg = body.AddComponent<Image>();
        bodyImg.sprite = _roundRect; bodyImg.type = Image.Type.Sliced;
        bodyImg.color = VEST;
        var bodyRT = body.GetComponent<RectTransform>();
        bodyRT.anchorMin = bodyRT.anchorMax = new Vector2(0.5f, 0.5f);
        bodyRT.anchoredPosition = new Vector2(0, -48);
        bodyRT.sizeDelta = new Vector2(130, 90);

        // Vest highlight (lapel shine)
        var lapelL = UI("LapelL", body.transform);
        var lapelLImg = lapelL.AddComponent<Image>();
        lapelLImg.sprite = _roundRect; lapelLImg.type = Image.Type.Sliced;
        lapelLImg.color = VEST_HIGHLIGHT;
        var lapelLRT = lapelL.GetComponent<RectTransform>();
        lapelLRT.anchorMin = lapelLRT.anchorMax = new Vector2(0.5f, 0.5f);
        lapelLRT.anchoredPosition = new Vector2(-30, 10);
        lapelLRT.sizeDelta = new Vector2(18, 50);
        lapelLRT.localRotation = Quaternion.Euler(0, 0, 8f);

        var lapelR = UI("LapelR", body.transform);
        var lapelRImg = lapelR.AddComponent<Image>();
        lapelRImg.sprite = _roundRect; lapelRImg.type = Image.Type.Sliced;
        lapelRImg.color = VEST_HIGHLIGHT;
        var lapelRRT = lapelR.GetComponent<RectTransform>();
        lapelRRT.anchorMin = lapelRRT.anchorMax = new Vector2(0.5f, 0.5f);
        lapelRRT.anchoredPosition = new Vector2(30, 10);
        lapelRRT.sizeDelta = new Vector2(18, 50);
        lapelRRT.localRotation = Quaternion.Euler(0, 0, -8f);

        // Shirt collar (white V behind bowtie)
        var collar = UI("Collar", root.transform);
        var collarImg = collar.AddComponent<Image>();
        collarImg.sprite = _roundRect; collarImg.type = Image.Type.Sliced;
        collarImg.color = SHIRT;
        var collarRT = collar.GetComponent<RectTransform>();
        collarRT.anchorMin = collarRT.anchorMax = new Vector2(0.5f, 0.5f);
        collarRT.anchoredPosition = new Vector2(0, -6);
        collarRT.sizeDelta = new Vector2(36, 22);

        // ── Neck ──
        var neck = UI("Neck", root.transform);
        var neckImg = neck.AddComponent<Image>();
        neckImg.sprite = _roundRect; neckImg.type = Image.Type.Sliced;
        neckImg.color = SKIN_SHADE;
        var neckRT = neck.GetComponent<RectTransform>();
        neckRT.anchorMin = neckRT.anchorMax = new Vector2(0.5f, 0.5f);
        neckRT.anchoredPosition = new Vector2(0, 4);
        neckRT.sizeDelta = new Vector2(28, 20);

        // ── Bow Tie ──
        var bowtie = UI("Bowtie", root.transform);
        var btImg = bowtie.AddComponent<Image>();
        btImg.sprite = _roundRect; btImg.type = Image.Type.Sliced;
        btImg.color = BOWTIE;
        var btRT = bowtie.GetComponent<RectTransform>();
        btRT.anchorMin = btRT.anchorMax = new Vector2(0.5f, 0.5f);
        btRT.anchoredPosition = new Vector2(0, -4);
        btRT.sizeDelta = new Vector2(30, 12);

        // Bowtie left wing
        var btwL = UI("BowL", root.transform);
        var btwLImg = btwL.AddComponent<Image>();
        btwLImg.sprite = _roundRect; btwLImg.type = Image.Type.Sliced;
        btwLImg.color = BOWTIE_DARK;
        var btwLRT = btwL.GetComponent<RectTransform>();
        btwLRT.anchorMin = btwLRT.anchorMax = new Vector2(0.5f, 0.5f);
        btwLRT.anchoredPosition = new Vector2(-14, -4);
        btwLRT.sizeDelta = new Vector2(14, 10);
        btwLRT.localRotation = Quaternion.Euler(0, 0, 15f);

        // Bowtie right wing
        var btwR = UI("BowR", root.transform);
        var btwRImg = btwR.AddComponent<Image>();
        btwRImg.sprite = _roundRect; btwRImg.type = Image.Type.Sliced;
        btwRImg.color = BOWTIE_DARK;
        var btwRRT = btwR.GetComponent<RectTransform>();
        btwRRT.anchorMin = btwRRT.anchorMax = new Vector2(0.5f, 0.5f);
        btwRRT.anchoredPosition = new Vector2(14, -4);
        btwRRT.sizeDelta = new Vector2(14, 10);
        btwRRT.localRotation = Quaternion.Euler(0, 0, -15f);

        // Bowtie center knot
        var btwC = UI("BowC", root.transform);
        var btwCImg = btwC.AddComponent<Image>();
        btwCImg.sprite = _roundRect; btwCImg.type = Image.Type.Sliced;
        btwCImg.color = BOWTIE;
        var btwCRT = btwC.GetComponent<RectTransform>();
        btwCRT.anchorMin = btwCRT.anchorMax = new Vector2(0.5f, 0.5f);
        btwCRT.anchoredPosition = new Vector2(0, -4);
        btwCRT.sizeDelta = new Vector2(8, 8);

        // ── Head ──
        var head = UI("Head", root.transform);
        var headImg = head.AddComponent<Image>();
        headImg.sprite = _roundRect; headImg.type = Image.Type.Sliced;
        headImg.color = SKIN;
        var headRT = head.GetComponent<RectTransform>();
        headRT.anchorMin = headRT.anchorMax = new Vector2(0.5f, 0.5f);
        headRT.anchoredPosition = new Vector2(0, 32);
        headRT.sizeDelta = new Vector2(64, 64);

        // ── Hair (on top of head) ──
        var hair = UI("Hair", root.transform);
        var hairImg = hair.AddComponent<Image>();
        hairImg.sprite = _roundRect; hairImg.type = Image.Type.Sliced;
        hairImg.color = HAIR;
        var hairRT = hair.GetComponent<RectTransform>();
        hairRT.anchorMin = hairRT.anchorMax = new Vector2(0.5f, 0.5f);
        hairRT.anchoredPosition = new Vector2(0, 56);
        hairRT.sizeDelta = new Vector2(68, 24);

        // Hair sides (sideburns)
        var hairL = UI("HairL", root.transform);
        var hairLImg = hairL.AddComponent<Image>();
        hairLImg.sprite = _roundRect; hairLImg.type = Image.Type.Sliced;
        hairLImg.color = HAIR;
        var hairLRT = hairL.GetComponent<RectTransform>();
        hairLRT.anchorMin = hairLRT.anchorMax = new Vector2(0.5f, 0.5f);
        hairLRT.anchoredPosition = new Vector2(-32, 42);
        hairLRT.sizeDelta = new Vector2(10, 28);

        var hairR = UI("HairR", root.transform);
        var hairRImg = hairR.AddComponent<Image>();
        hairRImg.sprite = _roundRect; hairRImg.type = Image.Type.Sliced;
        hairRImg.color = HAIR;
        var hairRRT = hairR.GetComponent<RectTransform>();
        hairRRT.anchorMin = hairRRT.anchorMax = new Vector2(0.5f, 0.5f);
        hairRRT.anchoredPosition = new Vector2(32, 42);
        hairRRT.sizeDelta = new Vector2(10, 28);

        // Hair highlight
        var hairHL = UI("HairHL", root.transform);
        var hairHLImg = hairHL.AddComponent<Image>();
        hairHLImg.sprite = _roundRect; hairHLImg.type = Image.Type.Sliced;
        hairHLImg.color = HAIR_HIGHLIGHT;
        var hairHLRT = hairHL.GetComponent<RectTransform>();
        hairHLRT.anchorMin = hairHLRT.anchorMax = new Vector2(0.5f, 0.5f);
        hairHLRT.anchoredPosition = new Vector2(-8, 60);
        hairHLRT.sizeDelta = new Vector2(28, 10);

        // ── Eyes ──
        var eyeL = UI("EyeL", root.transform);
        var eyeLImg = eyeL.AddComponent<Image>();
        eyeLImg.sprite = _roundRect; eyeLImg.type = Image.Type.Sliced;
        eyeLImg.color = Color.white;
        var eyeLRT = eyeL.GetComponent<RectTransform>();
        eyeLRT.anchorMin = eyeLRT.anchorMax = new Vector2(0.5f, 0.5f);
        eyeLRT.anchoredPosition = new Vector2(-12, 36);
        eyeLRT.sizeDelta = new Vector2(14, 10);

        var pupilL = UI("PupilL", eyeL.transform);
        var pupilLImg = pupilL.AddComponent<Image>();
        pupilLImg.sprite = _roundRect; pupilLImg.type = Image.Type.Sliced;
        pupilLImg.color = H("1A1A2E");
        var pupilLRT = pupilL.GetComponent<RectTransform>();
        pupilLRT.anchorMin = pupilLRT.anchorMax = new Vector2(0.5f, 0.5f);
        pupilLRT.anchoredPosition = new Vector2(1, -1);
        pupilLRT.sizeDelta = new Vector2(7, 7);

        var eyeR = UI("EyeR", root.transform);
        var eyeRImg = eyeR.AddComponent<Image>();
        eyeRImg.sprite = _roundRect; eyeRImg.type = Image.Type.Sliced;
        eyeRImg.color = Color.white;
        var eyeRRT = eyeR.GetComponent<RectTransform>();
        eyeRRT.anchorMin = eyeRRT.anchorMax = new Vector2(0.5f, 0.5f);
        eyeRRT.anchoredPosition = new Vector2(12, 36);
        eyeRRT.sizeDelta = new Vector2(14, 10);

        var pupilR = UI("PupilR", eyeR.transform);
        var pupilRImg = pupilR.AddComponent<Image>();
        pupilRImg.sprite = _roundRect; pupilRImg.type = Image.Type.Sliced;
        pupilRImg.color = H("1A1A2E");
        var pupilRRT = pupilR.GetComponent<RectTransform>();
        pupilRRT.anchorMin = pupilRRT.anchorMax = new Vector2(0.5f, 0.5f);
        pupilRRT.anchoredPosition = new Vector2(1, -1);
        pupilRRT.sizeDelta = new Vector2(7, 7);

        // ── Eyebrows ──
        var browL = UI("BrowL", root.transform);
        var browLImg = browL.AddComponent<Image>();
        browLImg.sprite = _roundRect; browLImg.type = Image.Type.Sliced;
        browLImg.color = HAIR;
        var browLRT = browL.GetComponent<RectTransform>();
        browLRT.anchorMin = browLRT.anchorMax = new Vector2(0.5f, 0.5f);
        browLRT.anchoredPosition = new Vector2(-12, 44);
        browLRT.sizeDelta = new Vector2(16, 4);
        browLRT.localRotation = Quaternion.Euler(0, 0, 3f);

        var browR = UI("BrowR", root.transform);
        var browRImg = browR.AddComponent<Image>();
        browRImg.sprite = _roundRect; browRImg.type = Image.Type.Sliced;
        browRImg.color = HAIR;
        var browRRT = browR.GetComponent<RectTransform>();
        browRRT.anchorMin = browRRT.anchorMax = new Vector2(0.5f, 0.5f);
        browRRT.anchoredPosition = new Vector2(12, 44);
        browRRT.sizeDelta = new Vector2(16, 4);
        browRRT.localRotation = Quaternion.Euler(0, 0, -3f);

        // ── Subtle smile ──
        var mouth = UI("Mouth", root.transform);
        var mouthImg = mouth.AddComponent<Image>();
        mouthImg.sprite = _roundRect; mouthImg.type = Image.Type.Sliced;
        mouthImg.color = H("C08060");
        var mouthRT = mouth.GetComponent<RectTransform>();
        mouthRT.anchorMin = mouthRT.anchorMax = new Vector2(0.5f, 0.5f);
        mouthRT.anchoredPosition = new Vector2(0, 20);
        mouthRT.sizeDelta = new Vector2(16, 5);

        // ── Ears ──
        var earL = UI("EarL", root.transform);
        var earLImg = earL.AddComponent<Image>();
        earLImg.sprite = _roundRect; earLImg.type = Image.Type.Sliced;
        earLImg.color = SKIN_SHADE;
        var earLRT = earL.GetComponent<RectTransform>();
        earLRT.anchorMin = earLRT.anchorMax = new Vector2(0.5f, 0.5f);
        earLRT.anchoredPosition = new Vector2(-34, 32);
        earLRT.sizeDelta = new Vector2(8, 14);

        var earR = UI("EarR", root.transform);
        var earRImg = earR.AddComponent<Image>();
        earRImg.sprite = _roundRect; earRImg.type = Image.Type.Sliced;
        earRImg.color = SKIN_SHADE;
        var earRRT = earR.GetComponent<RectTransform>();
        earRRT.anchorMin = earRRT.anchorMax = new Vector2(0.5f, 0.5f);
        earRRT.anchoredPosition = new Vector2(34, 32);
        earRRT.sizeDelta = new Vector2(8, 14);

        // ── Name tag on vest ──
        var tag = UI("NameTag", root.transform);
        var tagImg = tag.AddComponent<Image>();
        tagImg.sprite = _roundRect; tagImg.type = Image.Type.Sliced;
        tagImg.color = NAMETAG_BG;
        var tagRT = tag.GetComponent<RectTransform>();
        tagRT.anchorMin = tagRT.anchorMax = new Vector2(0.5f, 0.5f);
        tagRT.anchoredPosition = new Vector2(0, -38);
        tagRT.sizeDelta = new Vector2(50, 16);

        var tagTxt = UI("TagText", tag.transform);
        var tagTMP = tagTxt.AddComponent<TextMeshProUGUI>();
        tagTMP.text = "DEALER"; tagTMP.fontSize = 8; tagTMP.fontStyle = FontStyles.Bold;
        tagTMP.alignment = TextAlignmentOptions.Center; tagTMP.color = NAMETAG_TEXT;
        Stretch(tagTxt);

        // ── Vest buttons ──
        for (int i = 0; i < 3; i++)
        {
            var btn = UI($"VestBtn{i}", root.transform);
            var btnImg = btn.AddComponent<Image>();
            btnImg.sprite = _roundRect; btnImg.type = Image.Type.Sliced;
            btnImg.color = H("333355");
            var btnRT = btn.GetComponent<RectTransform>();
            btnRT.anchorMin = btnRT.anchorMax = new Vector2(0.5f, 0.5f);
            btnRT.anchoredPosition = new Vector2(0, -20 - i * 14);
            btnRT.sizeDelta = new Vector2(6, 6);
        }

        // ── Poker chip in hand (left side) ──
        var chip = UI("Chip", root.transform);
        var chipImg = chip.AddComponent<Image>();
        chipImg.sprite = _oval; chipImg.color = CHIP_GREEN;
        var chipRT = chip.GetComponent<RectTransform>();
        chipRT.anchorMin = chipRT.anchorMax = new Vector2(0.5f, 0.5f);
        chipRT.anchoredPosition = new Vector2(-58, -55);
        chipRT.sizeDelta = new Vector2(26, 26);
        chipRT.localRotation = Quaternion.Euler(0, 0, 15f);

        var chipEdge = UI("ChipEdge", chip.transform);
        var chipEdgeImg = chipEdge.AddComponent<Image>();
        chipEdgeImg.sprite = _oval; chipEdgeImg.color = CHIP_EDGE;
        var chipEdgeRT = chipEdge.GetComponent<RectTransform>();
        chipEdgeRT.anchorMin = chipEdgeRT.anchorMax = new Vector2(0.5f, 0.5f);
        chipEdgeRT.anchoredPosition = Vector2.zero;
        chipEdgeRT.sizeDelta = new Vector2(20, 20);

        var chipCenter = UI("ChipCenter", chip.transform);
        var chipCenterImg = chipCenter.AddComponent<Image>();
        chipCenterImg.sprite = _oval; chipCenterImg.color = CHIP_GREEN;
        var chipCenterRT = chipCenter.GetComponent<RectTransform>();
        chipCenterRT.anchorMin = chipCenterRT.anchorMax = new Vector2(0.5f, 0.5f);
        chipCenterRT.anchoredPosition = Vector2.zero;
        chipCenterRT.sizeDelta = new Vector2(14, 14);

        var chipTxt = UI("$", chipCenter.transform);
        var chipTMP = chipTxt.AddComponent<TextMeshProUGUI>();
        chipTMP.text = "$"; chipTMP.fontSize = 9; chipTMP.fontStyle = FontStyles.Bold;
        chipTMP.alignment = TextAlignmentOptions.Center; chipTMP.color = Color.white;
        Stretch(chipTxt);

        // ── Hand (holding chip) ──
        var handL = UI("HandL", root.transform);
        var handLImg = handL.AddComponent<Image>();
        handLImg.sprite = _roundRect; handLImg.type = Image.Type.Sliced;
        handLImg.color = SKIN;
        var handLRT = handL.GetComponent<RectTransform>();
        handLRT.anchorMin = handLRT.anchorMax = new Vector2(0.5f, 0.5f);
        handLRT.anchoredPosition = new Vector2(-52, -48);
        handLRT.sizeDelta = new Vector2(18, 16);
        handLRT.localRotation = Quaternion.Euler(0, 0, 10f);

        // ── Right hand (card fanning gesture) ──
        var handR = UI("HandR", root.transform);
        var handRImg = handR.AddComponent<Image>();
        handRImg.sprite = _roundRect; handRImg.type = Image.Type.Sliced;
        handRImg.color = SKIN;
        var handRRT = handR.GetComponent<RectTransform>();
        handRRT.anchorMin = handRRT.anchorMax = new Vector2(0.5f, 0.5f);
        handRRT.anchoredPosition = new Vector2(52, -48);
        handRRT.sizeDelta = new Vector2(18, 16);
        handRRT.localRotation = Quaternion.Euler(0, 0, -10f);

        // ── Arms (connecting body to hands) ──
        var armL = UI("ArmL", root.transform);
        var armLImg = armL.AddComponent<Image>();
        armLImg.sprite = _roundRect; armLImg.type = Image.Type.Sliced;
        armLImg.color = VEST;
        var armLRT = armL.GetComponent<RectTransform>();
        armLRT.anchorMin = armLRT.anchorMax = new Vector2(0.5f, 0.5f);
        armLRT.anchoredPosition = new Vector2(-48, -38);
        armLRT.sizeDelta = new Vector2(16, 30);
        armLRT.localRotation = Quaternion.Euler(0, 0, 20f);

        var armR = UI("ArmR", root.transform);
        var armRImg = armR.AddComponent<Image>();
        armRImg.sprite = _roundRect; armRImg.type = Image.Type.Sliced;
        armRImg.color = VEST;
        var armRRT = armR.GetComponent<RectTransform>();
        armRRT.anchorMin = armRRT.anchorMax = new Vector2(0.5f, 0.5f);
        armRRT.anchoredPosition = new Vector2(48, -38);
        armRRT.sizeDelta = new Vector2(16, 30);
        armRRT.localRotation = Quaternion.Euler(0, 0, -20f);
    }

    // ══════ SEAT ══════
    static HijackPoker.UI.SeatView BuildSeat(int n, Transform parent, Vector2 pos)
    {
        var root = UI($"Seat{n}", parent);
        Rect(root, 0.5f, 0.5f, 0.5f, 0.5f, pos, new Vector2(195, 160));
        var cg = root.AddComponent<CanvasGroup>();

        // Border glow
        var brd = UI("Border", root.transform);
        var brdImg = brd.AddComponent<Image>();
        brdImg.sprite = _roundRect;
        brdImg.type = Image.Type.Sliced;
        brdImg.color = Color.clear;
        Stretch(brd);

        // Rounded background
        var bg = UI("Bg", root.transform);
        var bgImg = bg.AddComponent<Image>();
        bgImg.sprite = _roundRect; bgImg.type = Image.Type.Sliced;
        bgImg.color = SEAT_BG;
        var bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;

        // Subtle border
        var borderLine = UI("BorderLine", root.transform);
        var blImg = borderLine.AddComponent<Image>();
        blImg.sprite = _roundRect; blImg.type = Image.Type.Sliced;
        blImg.color = SEAT_BORDER;
        var blRT = borderLine.GetComponent<RectTransform>();
        blRT.anchorMin = Vector2.zero; blRT.anchorMax = Vector2.one;
        blRT.offsetMin = new Vector2(-1, -1); blRT.offsetMax = new Vector2(1, 1);
        borderLine.transform.SetAsFirstSibling(); // behind bg

        // Name
        var nm = UI("Name", root.transform);
        var nmT = nm.AddComponent<TextMeshProUGUI>();
        nmT.fontSize = 14; nmT.fontStyle = FontStyles.Bold;
        nmT.alignment = TextAlignmentOptions.Center; nmT.color = TW;
        Anch(nm, 0.05f, 0.82f, 0.95f, 0.97f, Vector2.zero, Vector2.zero);

        // Stack
        var sk = UI("Stack", root.transform);
        var skT = sk.AddComponent<TextMeshProUGUI>();
        skT.fontSize = 12; skT.alignment = TextAlignmentOptions.Center;
        skT.color = H("78DFA6");
        Anch(sk, 0.05f, 0.69f, 0.95f, 0.82f, Vector2.zero, Vector2.zero);

        // Cards - overlapping with rotation
        var ca = UI("Cards", root.transform);
        Anch(ca, 0.05f, 0.18f, 0.95f, 0.69f, Vector2.zero, Vector2.zero);

        // Card shadows
        var s1 = UI("Shadow1", ca.transform);
        var s1Img = s1.AddComponent<Image>(); s1Img.color = SHADOW;
        var s1RT = s1.GetComponent<RectTransform>();
        s1RT.anchorMin = s1RT.anchorMax = new Vector2(0.5f, 0.5f);
        s1RT.anchoredPosition = new Vector2(-13, -3); s1RT.sizeDelta = new Vector2(52, 72);
        s1RT.localRotation = Quaternion.Euler(0, 0, -5f);

        var s2 = UI("Shadow2", ca.transform);
        var s2Img = s2.AddComponent<Image>(); s2Img.color = SHADOW;
        var s2RT = s2.GetComponent<RectTransform>();
        s2RT.anchorMin = s2RT.anchorMax = new Vector2(0.5f, 0.5f);
        s2RT.anchoredPosition = new Vector2(17, -1); s2RT.sizeDelta = new Vector2(52, 72);
        s2RT.localRotation = Quaternion.Euler(0, 0, 5f);

        // Cards
        var c1 = ManualCard("C1", ca.transform, 52, 72, new Vector2(-15, 0), -5f);
        var c2 = ManualCard("C2", ca.transform, 52, 72, new Vector2(15, 2), 5f);

        // Stack chip visualization (player stack amount)
        var stackChips = UI("StackChips", root.transform);
        var scRT = stackChips.GetComponent<RectTransform>();
        scRT.anchorMin = scRT.anchorMax = new Vector2(0.16f, 0.49f);
        scRT.sizeDelta = new Vector2(78, 76);
        var chipStackView = stackChips.AddComponent<HijackPoker.UI.ChipStackView>();
        var csvSo = new SerializedObject(chipStackView);
        var csvSprites = csvSo.FindProperty("_chipSprites");
        csvSprites.arraySize = _chipSprites.Length;
        for (int i = 0; i < _chipSprites.Length; i++)
            csvSprites.GetArrayElementAtIndex(i).objectReferenceValue = _chipSprites[i];
        csvSo.ApplyModifiedPropertiesWithoutUndo();

        // Bet chip
        var betChip = UI("BetChip", root.transform);
        var betChipImg = betChip.AddComponent<Image>();
        betChipImg.sprite = _chipSprites[n % _chipSprites.Length];
        betChipImg.preserveAspect = true;
        var bcRT = betChip.GetComponent<RectTransform>();
        bcRT.anchorMin = bcRT.anchorMax = new Vector2(0.15f, 0.11f);
        bcRT.sizeDelta = new Vector2(28, 28);
        betChip.SetActive(false);

        // Bet
        var bt = UI("Bet", root.transform);
        var btT = bt.AddComponent<TextMeshProUGUI>();
        btT.fontSize = 11; btT.alignment = TextAlignmentOptions.Center; btT.color = H("F8D98B");
        Anch(bt, 0, 0.04f, 1, 0.18f, new Vector2(4, 0), new Vector2(-4, 0));

        // Action
        var at = UI("Act", root.transform);
        var atT = at.AddComponent<TextMeshProUGUI>();
        atT.fontSize = 10; atT.alignment = TextAlignmentOptions.Center; atT.color = Color.white;
        Anch(at, 0, -0.06f, 0.5f, 0.06f, Vector2.zero, Vector2.zero);

        // Hand rank
        var rk = UI("Rank", root.transform);
        var rkT = rk.AddComponent<TextMeshProUGUI>();
        rkT.fontSize = 10; rkT.alignment = TextAlignmentOptions.Center; rkT.color = H("6EC6FF");
        Anch(rk, 0.5f, -0.06f, 1, 0.06f, Vector2.zero, Vector2.zero);

        // Winnings
        var wn = UI("Win", root.transform);
        var wnT = wn.AddComponent<TextMeshProUGUI>();
        wnT.fontSize = 13; wnT.fontStyle = FontStyles.Bold;
        wnT.alignment = TextAlignmentOptions.Center; wnT.color = H("74E89D");
        Anch(wn, 0, -0.18f, 1, -0.04f, Vector2.zero, Vector2.zero);

        // Badges
        var db = Badge("D", root.transform, GOLD);
        var sb = Badge("SB", root.transform, H("81D4FA"));
        var bb = Badge("BB", root.transform, H("CE93D8"));
        db.SetActive(false); sb.SetActive(false); bb.SetActive(false);

        // Wire
        var sv = root.AddComponent<HijackPoker.UI.SeatView>();
        var so = new SerializedObject(sv);
        so.FindProperty("_nameText").objectReferenceValue = nmT;
        so.FindProperty("_stackText").objectReferenceValue = skT;
        so.FindProperty("_betText").objectReferenceValue = btT;
        so.FindProperty("_actionText").objectReferenceValue = atT;
        so.FindProperty("_dealerBadge").objectReferenceValue = db;
        so.FindProperty("_sbBadge").objectReferenceValue = sb;
        so.FindProperty("_bbBadge").objectReferenceValue = bb;
        so.FindProperty("_card1").objectReferenceValue = c1;
        so.FindProperty("_card2").objectReferenceValue = c2;
        so.FindProperty("_backgroundImage").objectReferenceValue = bgImg;
        so.FindProperty("_borderImage").objectReferenceValue = brdImg;
        so.FindProperty("_canvasGroup").objectReferenceValue = cg;
        so.FindProperty("_betChipImage").objectReferenceValue = betChipImg;
        so.FindProperty("_chipStackView").objectReferenceValue = chipStackView;
        so.FindProperty("_handRankText").objectReferenceValue = rkT;
        so.FindProperty("_winningsText").objectReferenceValue = wnT;
        so.ApplyModifiedProperties();
        return sv;
    }

    // ══════ SPRITE GENERATORS ══════
    static Sprite GenerateRoundedRect(string name, int w, int h, int radius)
    {
        string dir = "Assets/Sprites";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        string path = $"{dir}/{name}.png";

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // Distance to nearest corner
                float dx = Mathf.Max(0, Mathf.Max(radius - x, x - (w - 1 - radius)));
                float dy = Mathf.Max(0, Mathf.Max(radius - y, y - (h - 1 - radius)));
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = 1f - Mathf.Clamp01((d - radius + 1.5f) / 1.5f);
                tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
            }
        }
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        // 9-slice borders for proper scaling
        importer.spriteBorder = new Vector4(radius + 2, radius + 2, radius + 2, radius + 2);
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // ══════ HELPERS ══════
    static GameObject OvalImg(string name, Transform parent, Color color, Vector2 pos, float w, float h)
    {
        var go = UI(name, parent);
        var img = go.AddComponent<Image>(); img.sprite = _oval; img.color = color;
        Rect(go, 0.5f, 0.5f, 0.5f, 0.5f, pos, new Vector2(w, h));
        return go;
    }

    static HijackPoker.UI.CardView CardWithShadow(string name, Transform parent, float w, float h)
    {
        // Wrapper for layout
        var wrapper = UI(name + "Wrap", parent);
        wrapper.AddComponent<LayoutElement>().preferredWidth = w;
        wrapper.GetComponent<LayoutElement>().preferredHeight = h;

        // Shadow
        var shadow = UI("Shadow", wrapper.transform);
        var shImg = shadow.AddComponent<Image>(); shImg.color = SHADOW;
        var shRT = shadow.GetComponent<RectTransform>();
        shRT.anchorMin = Vector2.zero; shRT.anchorMax = Vector2.one;
        shRT.offsetMin = new Vector2(-2, -4); shRT.offsetMax = new Vector2(2, 0);

        // Card
        var card = UI(name, wrapper.transform);
        var img = card.AddComponent<Image>(); img.color = CE; img.preserveAspect = true;
        Stretch(card);
        var cv = card.AddComponent<HijackPoker.UI.CardView>();
        W(cv, "_cardImage", img);
        return cv;
    }

    static HijackPoker.UI.CardView ManualCard(string name, Transform parent, float w, float h, Vector2 offset, float angle)
    {
        var go = UI(name, parent);
        var img = go.AddComponent<Image>(); img.color = CE; img.preserveAspect = true;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = offset; rt.sizeDelta = new Vector2(w, h);
        rt.localRotation = Quaternion.Euler(0, 0, angle);
        var cv = go.AddComponent<HijackPoker.UI.CardView>();
        W(cv, "_cardImage", img);
        return cv;
    }

    static GameObject Badge(string text, Transform parent, Color color)
    {
        var go = UI(text + "Badge", parent);
        var img = go.AddComponent<Image>();
        img.sprite = _roundRect; img.type = Image.Type.Sliced;
        img.color = H("0A0A1A", 0.85f);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.93f, 0.93f);
        rt.sizeDelta = new Vector2(32, 20);
        var tx = UI("T", go.transform);
        var tmp = tx.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = 11; tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center; tmp.color = color;
        Stretch(tx);
        return go;
    }

    static GameObject FancyBtn(string name, string label, Transform parent, float w, float h,
        Color bgMain, Color bgHighlight, Color bgDark)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        // Main background
        var img = go.AddComponent<Image>();
        img.sprite = _roundRect; img.type = Image.Type.Sliced; img.color = bgMain;
        var button = go.AddComponent<Button>();
        var le = go.AddComponent<LayoutElement>(); le.preferredWidth = w; le.preferredHeight = h;

        // Top highlight (simulates gradient)
        var shine = UI("Shine", go.transform);
        var shineImg = shine.AddComponent<Image>();
        shineImg.sprite = _roundRect; shineImg.type = Image.Type.Sliced;
        shineImg.color = bgHighlight;
        shineImg.raycastTarget = false;
        var shineRT = shine.GetComponent<RectTransform>();
        shineRT.anchorMin = new Vector2(0, 0.5f); shineRT.anchorMax = Vector2.one;
        shineRT.offsetMin = new Vector2(2, 0); shineRT.offsetMax = new Vector2(-2, -2);

        // Bottom shadow edge
        var edge = UI("Edge", go.transform);
        var edgeImg = edge.AddComponent<Image>();
        edgeImg.sprite = _roundRect; edgeImg.type = Image.Type.Sliced;
        edgeImg.color = bgDark;
        edgeImg.raycastTarget = false;
        var edgeRT = edge.GetComponent<RectTransform>();
        edgeRT.anchorMin = Vector2.zero; edgeRT.anchorMax = new Vector2(1, 0.15f);
        edgeRT.offsetMin = new Vector2(2, 2); edgeRT.offsetMax = new Vector2(-2, 0);

        // Inner glow (subtle white at top)
        var glow = UI("Glow", go.transform);
        var glowImg = glow.AddComponent<Image>();
        glowImg.sprite = _roundRect; glowImg.type = Image.Type.Sliced;
        glowImg.color = new Color(1, 1, 1, 0.12f);
        glowImg.raycastTarget = false;
        var glowRT = glow.GetComponent<RectTransform>();
        glowRT.anchorMin = new Vector2(0.1f, 0.7f); glowRT.anchorMax = new Vector2(0.9f, 0.95f);
        glowRT.offsetMin = glowRT.offsetMax = Vector2.zero;

        var stroke = UI("Stroke", go.transform);
        var strokeImg = stroke.AddComponent<Image>();
        strokeImg.sprite = _roundRect; strokeImg.type = Image.Type.Sliced;
        strokeImg.color = H("B8E5FF", 0.22f);
        strokeImg.raycastTarget = false;
        var strokeRT = stroke.GetComponent<RectTransform>();
        strokeRT.anchorMin = Vector2.zero; strokeRT.anchorMax = Vector2.one;
        strokeRT.offsetMin = new Vector2(-1, -1); strokeRT.offsetMax = new Vector2(1, 1);

        // Text
        var txt = new GameObject("T", typeof(RectTransform));
        txt.transform.SetParent(go.transform, false);
        var tmp = txt.AddComponent<TextMeshProUGUI>();
        tmp.text = label; tmp.fontSize = 14; tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center; tmp.color = H("F3FBFF");
        tmp.enableAutoSizing = false;
        tmp.characterSpacing = 2.5f;
        Stretch(txt);
        var txtRT = txt.GetComponent<RectTransform>();
        txtRT.offsetMin = new Vector2(4, 0); txtRT.offsetMax = new Vector2(-4, 0);

        var textShadow = txt.AddComponent<Shadow>();
        textShadow.effectColor = new Color(0f, 0f, 0f, 0.28f);
        textShadow.effectDistance = new Vector2(0f, -1f);
        textShadow.useGraphicAlpha = true;

        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.95f, 1f, 0.97f, 1f);
        colors.pressedColor = new Color(0.85f, 0.94f, 0.89f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.64f, 0.72f, 0.8f, 0.88f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.transition = Selectable.Transition.ColorTint;

        return go;
    }

    static GameObject SpeedPill(string name, string label, Transform parent, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = _roundRect; img.type = Image.Type.Sliced;
        img.color = H("223247"); // inactive state
        var button = go.AddComponent<Button>();
        var le = go.AddComponent<LayoutElement>(); le.preferredWidth = w; le.preferredHeight = h;

        // Hover highlight layer
        var hl = UI("HL", go.transform);
        var hlImg = hl.AddComponent<Image>();
        hlImg.sprite = _roundRect; hlImg.type = Image.Type.Sliced;
        hlImg.color = new Color(1, 1, 1, 0.06f);
        hlImg.raycastTarget = false;
        var hlRT = hl.GetComponent<RectTransform>();
        hlRT.anchorMin = Vector2.zero; hlRT.anchorMax = Vector2.one;
        hlRT.offsetMin = new Vector2(1, 1); hlRT.offsetMax = new Vector2(-1, -1);

        // Text
        var txt = new GameObject("T", typeof(RectTransform));
        txt.transform.SetParent(go.transform, false);
        var tmp = txt.AddComponent<TextMeshProUGUI>();
        tmp.text = label; tmp.fontSize = 12; tmp.fontStyle = FontStyles.Normal;
        tmp.alignment = TextAlignmentOptions.Center; tmp.color = H("BFD2E3");
        Stretch(txt);

        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.92f, 0.97f, 1f, 1f);
        colors.pressedColor = new Color(0.82f, 0.9f, 0.98f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.62f, 0.68f, 0.78f, 0.85f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.transition = Selectable.Transition.ColorTint;

        return go;
    }

    // Keep legacy RoundBtn for any other usage
    static GameObject RoundBtn(string name, string label, Transform parent, float w, float h, Color bg, int fs)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = _roundRect; img.type = Image.Type.Sliced; img.color = bg;
        go.AddComponent<Button>();
        var le = go.AddComponent<LayoutElement>(); le.preferredWidth = w; le.preferredHeight = h;
        var txt = new GameObject("T", typeof(RectTransform));
        txt.transform.SetParent(go.transform, false);
        var tmp = txt.AddComponent<TextMeshProUGUI>();
        tmp.text = label; tmp.fontSize = fs; tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center; tmp.color = Color.white;
        Stretch(txt);
        return go;
    }

    static GameObject HistoryPrefab()
    {
        if (!Directory.Exists("Assets/Resources")) Directory.CreateDirectory("Assets/Resources");
        var go = new GameObject("HistoryEntry", typeof(RectTransform));
        go.AddComponent<LayoutElement>().minHeight = 20;
        var t = new GameObject("T", typeof(RectTransform));
        t.transform.SetParent(go.transform, false);
        var tmp = t.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 11; tmp.color = H("9EB3C8");
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.enableWordWrapping = true; tmp.richText = true;
        Stretch(t);
        var p = PrefabUtility.SaveAsPrefabAsset(go, "Assets/Resources/HistoryEntryPrefab.prefab");
        Object.DestroyImmediate(go);
        return p;
    }

    static Color H(string hex, float a = 1f) { ColorUtility.TryParseHtmlString("#" + hex, out var c); c.a = a; return c; }
    static GameObject UI(string n, Transform p) { var g = new GameObject(n, typeof(RectTransform)); g.transform.SetParent(p, false); return g; }
    static void Rect(GameObject g, float a0, float a1, float a2, float a3, Vector2 p, Vector2 s)
    { var r = g.GetComponent<RectTransform>(); r.anchorMin = new Vector2(a0, a1); r.anchorMax = new Vector2(a2, a3); r.anchoredPosition = p; r.sizeDelta = s; }
    static void Stretch(GameObject g)
    { var r = g.GetComponent<RectTransform>(); r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
    static void Anch(GameObject g, float a0, float a1, float a2, float a3, Vector2 oMin, Vector2 oMax)
    { var r = g.GetComponent<RectTransform>(); r.anchorMin = new Vector2(a0, a1); r.anchorMax = new Vector2(a2, a3); r.offsetMin = oMin; r.offsetMax = oMax; }
    static void W(Component c, string p, Object v) { var so = new SerializedObject(c); so.FindProperty(p).objectReferenceValue = v; so.ApplyModifiedProperties(); }
    static void WArr<T>(Component c, string p, T[] v) where T : Object
    { var so = new SerializedObject(c); var a = so.FindProperty(p); a.arraySize = v.Length; for (int i = 0; i < v.Length; i++) a.GetArrayElementAtIndex(i).objectReferenceValue = v[i]; so.ApplyModifiedProperties(); }
}
