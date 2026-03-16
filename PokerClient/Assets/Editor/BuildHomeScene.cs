using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using System.Collections.Generic;

public static class BuildHomeScene
{
    static readonly Color BG = H("2A1215");
    static readonly Color GOLD = H("FFD700");
    static readonly Color ORANGE = H("FFB347");
    static readonly Color G_BTN = H("2E7D32");
    static readonly Color TD = H("999999");

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
        Camera.main.backgroundColor = BG;
        Camera.main.clearFlags = CameraClearFlags.SolidColor;

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
        Stretch(bgImg);

        // Subtle felt glow in center
        if (_oval)
        {
            var glow = UI("FeltGlow", cv.transform);
            var glowImg = glow.AddComponent<Image>();
            glowImg.sprite = _oval;
            glowImg.color = H("1B6B35", 0.08f);
            Rect(glow, 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(0, -20), new Vector2(1200, 700));
        }

        // ══════ SCATTERED DECORATIONS ══════
        var scattered = UI("Scattered", cv.transform);
        Stretch(scattered);
        var scatteredCG = scattered.AddComponent<CanvasGroup>();

        // Card backs
        string[] cardBacks = { "cardBack_red1", "cardBack_red2", "cardBack_red3", "cardBack_red4", "cardBack_red5", "cardBack_blue3" };
        Vector2[] cardPositions = {
            new(-650, 300), new(600, -280), new(-500, -350),
            new(700, 200), new(-200, -400), new(550, 380)
        };
        float[] cardRotations = { -25f, 15f, 40f, -10f, 30f, -35f };

        for (int i = 0; i < cardBacks.Length; i++)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Resources/Cards/{cardBacks[i]}.png");
            if (!sprite) continue;
            var card = UI($"ScatteredCard{i}", scattered.transform);
            var cImg = card.AddComponent<Image>();
            cImg.sprite = sprite;
            cImg.color = new Color(1, 1, 1, 0.18f);
            cImg.preserveAspect = true;
            var cRT = card.GetComponent<RectTransform>();
            cRT.anchorMin = cRT.anchorMax = new Vector2(0.5f, 0.5f);
            cRT.anchoredPosition = cardPositions[i];
            cRT.sizeDelta = new Vector2(80, 112);
            cRT.localRotation = Quaternion.Euler(0, 0, cardRotations[i]);
        }

        // Chips
        string[] chipNames = { "chipRedWhite", "chipBlackWhite", "chipGreenWhite", "chipBlueWhite" };
        Vector2[] chipPositions = { new(-750, -100), new(750, 50), new(-350, 420), new(400, -420) };
        float[] chipScales = { 0.6f, 0.5f, 0.55f, 0.5f };

        for (int i = 0; i < chipNames.Length; i++)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Sprites/Chips/{chipNames[i]}.png");
            if (!sprite) continue;
            var chip = UI($"ScatteredChip{i}", scattered.transform);
            var chImg = chip.AddComponent<Image>();
            chImg.sprite = sprite;
            chImg.color = new Color(1, 1, 1, 0.22f);
            chImg.preserveAspect = true;
            var chRT = chip.GetComponent<RectTransform>();
            chRT.anchorMin = chRT.anchorMax = new Vector2(0.5f, 0.5f);
            chRT.anchoredPosition = chipPositions[i];
            chRT.sizeDelta = new Vector2(80, 80);
            chRT.localScale = Vector3.one * chipScales[i];
        }

        // ══════ TITLE ══════
        var titleGO = UI("Title", cv.transform);
        var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text = "HIJACK POKER";
        titleTMP.fontSize = 72;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.color = GOLD;
        titleTMP.characterSpacing = 8;
        titleTMP.enableAutoSizing = false;
        Rect(titleGO, 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(0, 200), new Vector2(800, 100));

        // ══════ SUBTITLE ══════
        var subGO = UI("Subtitle", cv.transform);
        var subTMP = subGO.AddComponent<TextMeshProUGUI>();
        subTMP.text = "Texas Hold'em Viewer";
        subTMP.fontSize = 24;
        subTMP.alignment = TextAlignmentOptions.Center;
        subTMP.color = ORANGE;
        subTMP.fontStyle = FontStyles.Italic;
        Rect(subGO, 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(0, 140), new Vector2(600, 40));

        // ══════ HERO CARDS ══════
        var heroArea = UI("HeroCards", cv.transform);
        Rect(heroArea, 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(0, 10), new Vector2(300, 200));

        var spadesA = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Cards/cardSpadesA.png");
        var heartsA = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Cards/cardHeartsA.png");

        var hero1 = UI("AceSpades", heroArea.transform);
        var h1Img = hero1.AddComponent<Image>();
        h1Img.sprite = spadesA;
        h1Img.preserveAspect = true;
        var h1RT = hero1.GetComponent<RectTransform>();
        h1RT.anchorMin = h1RT.anchorMax = new Vector2(0.5f, 0.5f);
        h1RT.anchoredPosition = new Vector2(-45, 0);
        h1RT.sizeDelta = new Vector2(120, 168);
        h1RT.localRotation = Quaternion.Euler(0, 0, -8f);

        var hero2 = UI("AceHearts", heroArea.transform);
        var h2Img = hero2.AddComponent<Image>();
        h2Img.sprite = heartsA;
        h2Img.preserveAspect = true;
        var h2RT = hero2.GetComponent<RectTransform>();
        h2RT.anchorMin = h2RT.anchorMax = new Vector2(0.5f, 0.5f);
        h2RT.anchoredPosition = new Vector2(45, 0);
        h2RT.sizeDelta = new Vector2(120, 168);
        h2RT.localRotation = Quaternion.Euler(0, 0, 8f);

        // ══════ NAME INPUT ══════
        var nameInputGO = UI("NameInput", cv.transform);
        var nameInputCG = nameInputGO.AddComponent<CanvasGroup>();
        Rect(nameInputGO, 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(0, -117), new Vector2(320, 48));

        // Input background
        var nameInputBg = nameInputGO.AddComponent<Image>();
        nameInputBg.sprite = _roundRect;
        nameInputBg.type = Image.Type.Sliced;
        nameInputBg.color = H("1A0A0A", 0.9f);

        // Text area
        var textArea = UI("TextArea", nameInputGO.transform);
        Stretch(textArea);
        var textAreaRT = textArea.GetComponent<RectTransform>();
        textAreaRT.offsetMin = new Vector2(14, 4);
        textAreaRT.offsetMax = new Vector2(-14, -4);
        textArea.AddComponent<RectMask2D>();

        // Placeholder
        var placeholder = UI("Placeholder", textArea.transform);
        var phTMP = placeholder.AddComponent<TextMeshProUGUI>();
        phTMP.text = "Enter your name...";
        phTMP.fontSize = 18;
        phTMP.fontStyle = FontStyles.Italic;
        phTMP.color = H("666666");
        phTMP.alignment = TextAlignmentOptions.Left;
        phTMP.verticalAlignment = VerticalAlignmentOptions.Middle;
        Stretch(placeholder);

        // Input text
        var inputText = UI("Text", textArea.transform);
        var inputTMP = inputText.AddComponent<TextMeshProUGUI>();
        inputTMP.fontSize = 18;
        inputTMP.color = Color.white;
        inputTMP.alignment = TextAlignmentOptions.Left;
        inputTMP.verticalAlignment = VerticalAlignmentOptions.Middle;
        Stretch(inputText);

        var inputField = nameInputGO.AddComponent<TMP_InputField>();
        inputField.textViewport = textArea.GetComponent<RectTransform>();
        inputField.textComponent = inputTMP;
        inputField.placeholder = phTMP;
        inputField.characterLimit = 20;
        inputField.contentType = TMP_InputField.ContentType.Name;
        inputField.caretColor = GOLD;
        inputField.selectionColor = H("FFD700", 0.3f);

        // ══════ PLAY BUTTON ══════
        var btnGO = new GameObject("PlayButton", typeof(RectTransform));
        btnGO.transform.SetParent(cv.transform, false);
        var btnImg = btnGO.AddComponent<Image>();
        btnImg.sprite = _roundRect;
        btnImg.type = Image.Type.Sliced;
        btnImg.color = G_BTN;
        var btn = btnGO.AddComponent<Button>();
        var btnCG = btnGO.AddComponent<CanvasGroup>();
        Rect(btnGO, 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(0, -183), new Vector2(260, 60));

        var btnTxt = UI("Text", btnGO.transform);
        var btnTMP = btnTxt.AddComponent<TextMeshProUGUI>();
        btnTMP.text = "PLAY NOW";
        btnTMP.fontSize = 28;
        btnTMP.fontStyle = FontStyles.Bold;
        btnTMP.alignment = TextAlignmentOptions.Center;
        btnTMP.color = Color.white;
        btnTMP.characterSpacing = 4;
        Stretch(btnTxt);

        // ══════ VERSION ══════
        var verGO = UI("Version", cv.transform);
        var verTMP = verGO.AddComponent<TextMeshProUGUI>();
        verTMP.text = "v1.0 — Gauntlet Tech Assignment";
        verTMP.fontSize = 12;
        verTMP.alignment = TextAlignmentOptions.Center;
        verTMP.color = TD;
        var verRT = verGO.GetComponent<RectTransform>();
        verRT.anchorMin = new Vector2(0, 0);
        verRT.anchorMax = new Vector2(1, 0);
        verRT.pivot = new Vector2(0.5f, 0);
        verRT.anchoredPosition = new Vector2(0, 10);
        verRT.sizeDelta = new Vector2(0, 24);

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
