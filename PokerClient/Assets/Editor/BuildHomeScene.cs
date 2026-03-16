using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using System.Collections.Generic;

public static class BuildHomeScene
{
    static readonly Color BG = H("08111F");
    static readonly Color BG_MID = H("10253A", 0.36f);
    static readonly Color PANEL = H("0D1B2B", 0.92f);
    static readonly Color PANEL_BORDER = H("89C8D3", 0.28f);
    static readonly Color PANEL_SHEEN = H("8FDDE2", 0.07f);
    static readonly Color CYAN = H("63DEDB");
    static readonly Color TEXT_PRIMARY = H("F3F8FF");
    static readonly Color TEXT_SECONDARY = H("A8BBD0");
    static readonly Color G_BTN = H("18A36E");
    static readonly Color INPUT_BG = H("0A1320", 0.96f);
    static readonly Color INPUT_BORDER = H("66B3BF", 0.34f);
    static readonly Color TD = H("76889D");
    static readonly Color SHADOW = new Color(0f, 0f, 0f, 0.38f);

    static Sprite _oval, _roundRect;

    [MenuItem("Tools/Build Home Scene")]
    public static void Build()
    {
        _oval = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/OvalFelt.png");
        _roundRect = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/RoundRect.png");
        if (!_roundRect)
        {
            // Generate if missing (same as RebuildScene)
            _roundRect = GenerateRoundedRect("RoundRect", 128, 128, 20);
        }

        var sc = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        if (Camera.main != null)
        {
            Camera.main.backgroundColor = BG;
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
        }

        // Canvas
        var cv = new GameObject("HomeCanvas");
        var canvas = cv.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = cv.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        cv.AddComponent<GraphicRaycaster>();

        // ══════ BACKGROUND ══════
        var bgImg = UI("Background", cv.transform);
        var bgImage = bgImg.AddComponent<Image>();
        bgImage.color = BG;
        bgImage.raycastTarget = false;
        Stretch(bgImg);

        // Atmospheric glows
        if (_oval)
        {
            var topGlow = UI("TopGlow", cv.transform);
            var topGlowImg = topGlow.AddComponent<Image>();
            topGlowImg.sprite = _oval;
            topGlowImg.color = H("2C6BA6", 0.3f);
            topGlowImg.raycastTarget = false;
            Rect(topGlow, 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(0, 390), new Vector2(2100, 1200));

            var centerGlow = UI("CenterGlow", cv.transform);
            var centerGlowImg = centerGlow.AddComponent<Image>();
            centerGlowImg.sprite = _oval;
            centerGlowImg.color = H("2DC6A0", 0.14f);
            centerGlowImg.raycastTarget = false;
            Rect(centerGlow, 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(0, -20), new Vector2(1400, 860));

            var bottomGlow = UI("BottomGlow", cv.transform);
            var bottomGlowImg = bottomGlow.AddComponent<Image>();
            bottomGlowImg.sprite = _oval;
            bottomGlowImg.color = H("1D3F62", 0.24f);
            bottomGlowImg.raycastTarget = false;
            Rect(bottomGlow, 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(0, -510), new Vector2(2200, 1100));
        }

        var tintOverlay = UI("TintOverlay", cv.transform);
        var tintImg = tintOverlay.AddComponent<Image>();
        tintImg.color = BG_MID;
        tintImg.raycastTarget = false;
        tintImg.material = null;
        Stretch(tintOverlay);

        var vignette = UI("Vignette", cv.transform);
        var vignetteImg = vignette.AddComponent<Image>();
        vignetteImg.color = new Color(0f, 0f, 0f, 0.3f);
        vignetteImg.raycastTarget = false;
        Stretch(vignette);

        // ══════ SCATTERED DECORATIONS ══════
        var scattered = UI("Scattered", cv.transform);
        Stretch(scattered);
        var scatteredCG = scattered.AddComponent<CanvasGroup>();

        // Card backs
        string[] cardBacks = { "cardBack_red1", "cardBack_red2", "cardBack_red3", "cardBack_red4", "cardBack_red5", "cardBack_blue3" };
        Vector2[] cardPositions = {
            new Vector2(-770, 360), new Vector2(760, -320), new Vector2(-660, -360),
            new Vector2(780, 220), new Vector2(-340, -470), new Vector2(690, 430)
        };
        float[] cardRotations = { -25f, 15f, 40f, -10f, 30f, -35f };

        for (int i = 0; i < cardBacks.Length; i++)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Resources/Cards/{cardBacks[i]}.png");
            if (!sprite) continue;
            var card = UI($"ScatteredCard{i}", scattered.transform);
            var cImg = card.AddComponent<Image>();
            cImg.sprite = sprite;
            cImg.color = new Color(1f, 1f, 1f, 0.1f);
            cImg.preserveAspect = true;
            cImg.raycastTarget = false;
            var cRT = card.GetComponent<RectTransform>();
            cRT.anchorMin = cRT.anchorMax = new Vector2(0.5f, 0.5f);
            cRT.anchoredPosition = cardPositions[i];
            cRT.sizeDelta = new Vector2(80, 112);
            cRT.localRotation = Quaternion.Euler(0, 0, cardRotations[i]);
        }

        // Chips
        string[] chipNames = { "chipRedWhite", "chipBlackWhite", "chipGreenWhite", "chipBlueWhite" };
        Vector2[] chipPositions = {
            new Vector2(-870, -40),
            new Vector2(870, 80),
            new Vector2(-460, 480),
            new Vector2(520, -500)
        };
        float[] chipScales = { 0.6f, 0.5f, 0.55f, 0.5f };

        for (int i = 0; i < chipNames.Length; i++)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Sprites/Chips/{chipNames[i]}.png");
            if (!sprite) continue;
            var chip = UI($"ScatteredChip{i}", scattered.transform);
            var chImg = chip.AddComponent<Image>();
            chImg.sprite = sprite;
            chImg.color = new Color(1f, 1f, 1f, 0.12f);
            chImg.preserveAspect = true;
            chImg.raycastTarget = false;
            var chRT = chip.GetComponent<RectTransform>();
            chRT.anchorMin = chRT.anchorMax = new Vector2(0.5f, 0.5f);
            chRT.anchoredPosition = chipPositions[i];
            chRT.sizeDelta = new Vector2(80, 80);
            chRT.localScale = Vector3.one * chipScales[i];
        }

        // ══════ GLASS PANEL ══════
        var panelRoot = UI("MainPanelRoot", cv.transform);
        Rect(panelRoot, 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(0, 20), new Vector2(840, 660));

        var panelShadow = UI("PanelShadow", panelRoot.transform);
        var panelShadowImg = panelShadow.AddComponent<Image>();
        panelShadowImg.sprite = _roundRect;
        panelShadowImg.type = Image.Type.Sliced;
        panelShadowImg.color = SHADOW;
        Stretch(panelShadow);
        var panelShadowRT = panelShadow.GetComponent<RectTransform>();
        panelShadowRT.offsetMin = new Vector2(12, -12);
        panelShadowRT.offsetMax = new Vector2(12, -12);

        var panel = UI("Panel", panelRoot.transform);
        var panelImg = panel.AddComponent<Image>();
        panelImg.sprite = _roundRect;
        panelImg.type = Image.Type.Sliced;
        panelImg.color = PANEL;
        Stretch(panel);

        var panelSheen = UI("PanelSheen", panel.transform);
        var panelSheenImg = panelSheen.AddComponent<Image>();
        panelSheenImg.sprite = _roundRect;
        panelSheenImg.type = Image.Type.Sliced;
        panelSheenImg.color = PANEL_SHEEN;
        panelSheenImg.raycastTarget = false;
        Stretch(panelSheen);

        var panelBorder = UI("PanelBorder", panel.transform);
        var panelBorderImg = panelBorder.AddComponent<Image>();
        panelBorderImg.sprite = _roundRect;
        panelBorderImg.type = Image.Type.Sliced;
        panelBorderImg.color = PANEL_BORDER;
        panelBorderImg.raycastTarget = false;
        Stretch(panelBorder);
        var panelBorderRT = panelBorder.GetComponent<RectTransform>();
        panelBorderRT.offsetMin = new Vector2(-1, -1);
        panelBorderRT.offsetMax = new Vector2(1, 1);

        var content = UI("Content", panel.transform);
        Stretch(content);

        var topAccent = UI("TopAccent", content.transform);
        var topAccentImg = topAccent.AddComponent<Image>();
        topAccentImg.color = H("67E3DF", 0.92f);
        topAccentImg.raycastTarget = false;
        Rect(topAccent, 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(0, 284), new Vector2(210, 3));

        var brandPill = UI("BrandPill", content.transform);
        var brandPillImg = brandPill.AddComponent<Image>();
        brandPillImg.sprite = _roundRect;
        brandPillImg.type = Image.Type.Sliced;
        brandPillImg.color = H("112941", 0.97f);
        Rect(brandPill, 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(0, 246), new Vector2(360, 34));

        var brandText = UI("BrandText", brandPill.transform);
        var brandTMP = brandText.AddComponent<TextMeshProUGUI>();
        brandTMP.text = "COMPETITIVE TABLE VIEWER";
        brandTMP.fontSize = 15;
        brandTMP.fontStyle = FontStyles.Bold;
        brandTMP.alignment = TextAlignmentOptions.Center;
        brandTMP.color = CYAN;
        brandTMP.characterSpacing = 4;
        var brandShadow = brandText.AddComponent<Shadow>();
        brandShadow.effectColor = new Color(0f, 0f, 0f, 0.25f);
        brandShadow.effectDistance = new Vector2(0f, -1f);
        brandShadow.useGraphicAlpha = true;
        Stretch(brandText);

        // ══════ TITLE ══════
        var titleGO = UI("Title", content.transform);
        var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text = "HIJACK POKER";
        titleTMP.fontSize = 82;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.color = TEXT_PRIMARY;
        titleTMP.characterSpacing = 10;
        titleTMP.enableAutoSizing = false;
        titleTMP.enableWordWrapping = false;
        Rect(titleGO, 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(0, 178), new Vector2(760, 110));
        var titleShadow = titleGO.AddComponent<Shadow>();
        titleShadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
        titleShadow.effectDistance = new Vector2(0f, -4f);
        titleShadow.useGraphicAlpha = true;

        // ══════ SUBTITLE ══════
        var subGO = UI("Subtitle", content.transform);
        var subTMP = subGO.AddComponent<TextMeshProUGUI>();
        subTMP.text = "Review every hand. Control every replay.";
        subTMP.fontSize = 28;
        subTMP.alignment = TextAlignmentOptions.Center;
        subTMP.color = TEXT_SECONDARY;
        subTMP.fontStyle = FontStyles.Normal;
        subTMP.characterSpacing = 0.7f;
        Rect(subGO, 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(0, 122), new Vector2(680, 40));
        var subShadow = subGO.AddComponent<Shadow>();
        subShadow.effectColor = new Color(0f, 0f, 0f, 0.22f);
        subShadow.effectDistance = new Vector2(0f, -1f);
        subShadow.useGraphicAlpha = true;

        var divider = UI("Divider", content.transform);
        var dividerImg = divider.AddComponent<Image>();
        dividerImg.color = H("79C9D4", 0.28f);
        dividerImg.raycastTarget = false;
        Rect(divider, 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(0, 96), new Vector2(420, 1));

        // ══════ HERO CARDS ══════
        var heroArea = UI("HeroCards", content.transform);
        Rect(heroArea, 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(0, 8), new Vector2(320, 220));

        if (_oval)
        {
            var heroGlow = UI("HeroGlow", heroArea.transform);
            var heroGlowImg = heroGlow.AddComponent<Image>();
            heroGlowImg.sprite = _oval;
            heroGlowImg.color = H("3ED5CC", 0.2f);
            heroGlowImg.raycastTarget = false;
            Rect(heroGlow, 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(0, -8), new Vector2(360, 230));
        }

        var spadesA = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Cards/cardSpadesA.png");
        var heartsA = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Cards/cardHeartsA.png");

        var hero1Shadow = UI("AceSpadesShadow", heroArea.transform);
        var h1ShadowImg = hero1Shadow.AddComponent<Image>();
        h1ShadowImg.sprite = _roundRect;
        h1ShadowImg.type = Image.Type.Sliced;
        h1ShadowImg.color = new Color(0f, 0f, 0f, 0.3f);
        var h1ShadowRT = hero1Shadow.GetComponent<RectTransform>();
        h1ShadowRT.anchorMin = h1ShadowRT.anchorMax = new Vector2(0.5f, 0.5f);
        h1ShadowRT.anchoredPosition = new Vector2(-48f, -10f);
        h1ShadowRT.sizeDelta = new Vector2(124f, 172f);
        h1ShadowRT.localRotation = Quaternion.Euler(0f, 0f, -10f);

        var hero1 = UI("AceSpades", heroArea.transform);
        var h1Img = hero1.AddComponent<Image>();
        h1Img.sprite = spadesA;
        h1Img.preserveAspect = true;
        h1Img.color = new Color(1f, 1f, 1f, 0.99f);
        var h1RT = hero1.GetComponent<RectTransform>();
        h1RT.anchorMin = h1RT.anchorMax = new Vector2(0.5f, 0.5f);
        h1RT.anchoredPosition = new Vector2(-55f, -2f);
        h1RT.sizeDelta = new Vector2(128f, 180f);
        h1RT.localRotation = Quaternion.Euler(0f, 0f, -11f);

        var hero2Shadow = UI("AceHeartsShadow", heroArea.transform);
        var h2ShadowImg = hero2Shadow.AddComponent<Image>();
        h2ShadowImg.sprite = _roundRect;
        h2ShadowImg.type = Image.Type.Sliced;
        h2ShadowImg.color = new Color(0f, 0f, 0f, 0.3f);
        var h2ShadowRT = hero2Shadow.GetComponent<RectTransform>();
        h2ShadowRT.anchorMin = h2ShadowRT.anchorMax = new Vector2(0.5f, 0.5f);
        h2ShadowRT.anchoredPosition = new Vector2(55f, -10f);
        h2ShadowRT.sizeDelta = new Vector2(124f, 172f);
        h2ShadowRT.localRotation = Quaternion.Euler(0f, 0f, 10f);

        var hero2 = UI("AceHearts", heroArea.transform);
        var h2Img = hero2.AddComponent<Image>();
        h2Img.sprite = heartsA;
        h2Img.preserveAspect = true;
        h2Img.color = new Color(1f, 1f, 1f, 0.99f);
        var h2RT = hero2.GetComponent<RectTransform>();
        h2RT.anchorMin = h2RT.anchorMax = new Vector2(0.5f, 0.5f);
        h2RT.anchoredPosition = new Vector2(55f, -2f);
        h2RT.sizeDelta = new Vector2(128f, 180f);
        h2RT.localRotation = Quaternion.Euler(0f, 0f, 11f);

        var nameLabel = UI("NameLabel", content.transform);
        var nameLabelTMP = nameLabel.AddComponent<TextMeshProUGUI>();
        nameLabelTMP.text = "PLAYER NAME";
        nameLabelTMP.fontSize = 15;
        nameLabelTMP.fontStyle = FontStyles.Bold;
        nameLabelTMP.alignment = TextAlignmentOptions.Center;
        nameLabelTMP.color = H("8DAAC4");
        nameLabelTMP.characterSpacing = 3;
        Rect(nameLabel, 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(0, -88), new Vector2(420, 26));

        // ══════ NAME INPUT ══════
        var nameInputGO = UI("NameInput", content.transform);
        var nameInputCG = nameInputGO.AddComponent<CanvasGroup>();
        Rect(nameInputGO, 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(0, -132), new Vector2(470, 62));

        // Input background
        var nameInputBg = nameInputGO.AddComponent<Image>();
        nameInputBg.sprite = _roundRect;
        nameInputBg.type = Image.Type.Sliced;
        nameInputBg.color = INPUT_BG;
        var inputOutline = nameInputGO.AddComponent<Outline>();
        inputOutline.effectColor = INPUT_BORDER;
        inputOutline.effectDistance = new Vector2(1f, -1f);
        inputOutline.useGraphicAlpha = true;
        var inputShadow = nameInputGO.AddComponent<Shadow>();
        inputShadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
        inputShadow.effectDistance = new Vector2(0f, -2f);
        inputShadow.useGraphicAlpha = true;

        // Text area
        var textArea = UI("TextArea", nameInputGO.transform);
        Stretch(textArea);
        var textAreaRT = textArea.GetComponent<RectTransform>();
        textAreaRT.offsetMin = new Vector2(20, 8);
        textAreaRT.offsetMax = new Vector2(-20, -8);
        textArea.AddComponent<RectMask2D>();

        // Placeholder
        var placeholder = UI("Placeholder", textArea.transform);
        var phTMP = placeholder.AddComponent<TextMeshProUGUI>();
        phTMP.text = "Type your display name";
        phTMP.fontSize = 22;
        phTMP.fontStyle = FontStyles.Normal;
        phTMP.color = H("6F8298");
        phTMP.alignment = TextAlignmentOptions.Left;
        phTMP.verticalAlignment = VerticalAlignmentOptions.Middle;
        Stretch(placeholder);

        // Input text
        var inputText = UI("Text", textArea.transform);
        var inputTMP = inputText.AddComponent<TextMeshProUGUI>();
        inputTMP.fontSize = 22;
        inputTMP.fontStyle = FontStyles.Bold;
        inputTMP.color = TEXT_PRIMARY;
        inputTMP.alignment = TextAlignmentOptions.Left;
        inputTMP.verticalAlignment = VerticalAlignmentOptions.Middle;
        Stretch(inputText);

        var inputField = nameInputGO.AddComponent<TMP_InputField>();
        inputField.textViewport = textArea.GetComponent<RectTransform>();
        inputField.textComponent = inputTMP;
        inputField.placeholder = phTMP;
        inputField.characterLimit = 20;
        inputField.contentType = TMP_InputField.ContentType.Name;
        inputField.customCaretColor = true;
        inputField.caretWidth = 3;
        inputField.pointSize = 22;
        inputField.richText = false;
        inputField.caretColor = CYAN;
        inputField.selectionColor = H("4BD5D1", 0.22f);

        // ══════ PLAY BUTTON ══════
        var btnShadow = UI("PlayButtonShadow", content.transform);
        var btnShadowImg = btnShadow.AddComponent<Image>();
        btnShadowImg.sprite = _roundRect;
        btnShadowImg.type = Image.Type.Sliced;
        btnShadowImg.color = SHADOW;
        Rect(btnShadow, 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(0, -228), new Vector2(342, 72));

        var btnGO = new GameObject("PlayButton", typeof(RectTransform));
        btnGO.transform.SetParent(content.transform, false);
        var btnImg = btnGO.AddComponent<Image>();
        btnImg.sprite = _roundRect;
        btnImg.type = Image.Type.Sliced;
        btnImg.color = G_BTN;
        var btn = btnGO.AddComponent<Button>();
        var btnCG = btnGO.AddComponent<CanvasGroup>();
        Rect(btnGO, 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(0, -220), new Vector2(340, 70));
        var btnOutline = btnGO.AddComponent<Outline>();
        btnOutline.effectColor = H("72E2B8", 0.35f);
        btnOutline.effectDistance = new Vector2(1f, -1f);
        btnOutline.useGraphicAlpha = true;

        var btnColors = btn.colors;
        btnColors.normalColor = Color.white;
        btnColors.highlightedColor = new Color(0.95f, 1f, 0.97f, 1f);
        btnColors.pressedColor = new Color(0.86f, 0.95f, 0.9f, 1f);
        btnColors.selectedColor = btnColors.highlightedColor;
        btnColors.disabledColor = new Color(0.62f, 0.69f, 0.77f, 0.9f);
        btnColors.colorMultiplier = 1f;
        btnColors.fadeDuration = 0.1f;
        btn.colors = btnColors;
        btn.transition = Selectable.Transition.ColorTint;

        var btnGloss = UI("Gloss", btnGO.transform);
        var btnGlossImg = btnGloss.AddComponent<Image>();
        btnGlossImg.sprite = _roundRect;
        btnGlossImg.type = Image.Type.Sliced;
        btnGlossImg.color = H("FFFFFF", 0.15f);
        btnGlossImg.raycastTarget = false;
        Stretch(btnGloss);
        var btnGlossRT = btnGloss.GetComponent<RectTransform>();
        btnGlossRT.offsetMin = new Vector2(8, 40);
        btnGlossRT.offsetMax = new Vector2(-8, -8);

        var btnTxt = UI("Text", btnGO.transform);
        var btnTMP = btnTxt.AddComponent<TextMeshProUGUI>();
        btnTMP.text = "PLAY NOW";
        btnTMP.fontSize = 30;
        btnTMP.fontStyle = FontStyles.Bold;
        btnTMP.alignment = TextAlignmentOptions.Center;
        btnTMP.color = H("F5FFFB");
        btnTMP.characterSpacing = 5;
        var btnTextShadow = btnTxt.AddComponent<Shadow>();
        btnTextShadow.effectColor = new Color(0f, 0f, 0f, 0.3f);
        btnTextShadow.effectDistance = new Vector2(0f, -2f);
        btnTextShadow.useGraphicAlpha = true;
        Stretch(btnTxt);

        // ══════ VERSION ══════
        var verGO = UI("Version", content.transform);
        var verTMP = verGO.AddComponent<TextMeshProUGUI>();
        verTMP.text = "v1.0 — Gauntlet Tech Assignment";
        verTMP.fontSize = 13;
        verTMP.alignment = TextAlignmentOptions.Center;
        verTMP.color = TD;
        verTMP.characterSpacing = 0.5f;
        Rect(verGO, 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(0, -306), new Vector2(520, 24));

        // ══════ FADE OVERLAY ══════
        var fadeGO = UI("FadeOverlay", cv.transform);
        var fadeImg = fadeGO.AddComponent<Image>();
        fadeImg.color = new Color(0, 0, 0, 0);
        fadeImg.raycastTarget = false;
        Stretch(fadeGO);

        // ══════ WIRE HOMESCREENVIEW ══════
        var homeView = cv.AddComponent<HijackPoker.UI.HomeScreenView>();
        var so = new SerializedObject(homeView);
        so.FindProperty("_title").objectReferenceValue = titleTMP;
        so.FindProperty("_subtitle").objectReferenceValue = subTMP;
        so.FindProperty("_playButton").objectReferenceValue = btn;
        so.FindProperty("_playButtonImage").objectReferenceValue = btnImg;
        so.FindProperty("_heroCard1").objectReferenceValue = h1Img;
        so.FindProperty("_heroCard2").objectReferenceValue = h2Img;
        so.FindProperty("_scatteredGroup").objectReferenceValue = scatteredCG;
        so.FindProperty("_nameInput").objectReferenceValue = inputField;
        so.FindProperty("_nameInputGroup").objectReferenceValue = nameInputCG;
        so.FindProperty("_fadeOverlay").objectReferenceValue = fadeImg;
        so.ApplyModifiedProperties();

        // EventSystem
        if (!Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>())
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // Save scene
        EditorSceneManager.SaveScene(sc, "Assets/Scenes/HomeScene.unity");

        // Update build settings: HomeScene at 0, PokerTable at 1
        var scenes = new List<EditorBuildSettingsScene>
        {
            new("Assets/Scenes/HomeScene.unity", true),
            new("Assets/Scenes/PokerTable.unity", true)
        };
        EditorBuildSettings.scenes = scenes.ToArray();

        Debug.Log("Home scene built! Build settings updated: HomeScene(0), PokerTable(1)");
    }

    // ══════ HELPERS ══════
    static Color H(string hex, float a = 1f)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out var c);
        c.a = a;
        return c;
    }

    static GameObject UI(string n, Transform p)
    {
        var g = new GameObject(n, typeof(RectTransform));
        g.transform.SetParent(p, false);
        return g;
    }

    static void Rect(GameObject g, float a0, float a1, float a2, float a3, Vector2 pos, Vector2 size)
    {
        var r = g.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(a0, a1);
        r.anchorMax = new Vector2(a2, a3);
        r.anchoredPosition = pos;
        r.sizeDelta = size;
    }

    static void Stretch(GameObject g)
    {
        var r = g.GetComponent<RectTransform>();
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }

    static Sprite GenerateRoundedRect(string name, int w, int h, int radius)
    {
        string dir = "Assets/Sprites";
        if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
        string path = $"{dir}/{name}.png";

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = Mathf.Max(0, Mathf.Max(radius - x, x - (w - 1 - radius)));
                float dy = Mathf.Max(0, Mathf.Max(radius - y, y - (h - 1 - radius)));
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = 1f - Mathf.Clamp01((d - radius + 1.5f) / 1.5f);
                tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
            }
        }
        tex.Apply();
        System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.spriteBorder = new Vector4(radius + 2, radius + 2, radius + 2, radius + 2);
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
