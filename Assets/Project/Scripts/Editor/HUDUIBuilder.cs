using System.IO;
using Augmentra.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class HUDUIBuilder
{
    private const string GameplayScenePath = "Assets/Scenes/SampleScene.unity";
    private const string FontPath = "Assets/Resources/Fonts/Nexa-ExtraLight SDF.asset";
    private const string BarFontPath = "Assets/Project/Fonts/Nexa-Heavy SDF.asset";
    private const string DamageNumberPrefabPath =
        "Assets/Project/Prefabs/FloatingDamageNumber.prefab";
    private const string EnemyPrefabPath = "Assets/Project/Prefabs/Enemy.prefab";
    private const string HudCanvasName = "HUDCanvas";
    private const string HudManagerName = "HUDManager";
    private const string DamagePoolName = "FloatingDamagePool";

    private static readonly Color PanelDark = Hex("#0D101466");
    private static readonly Color PanelBorder = Hex("#3A404C");
    private static readonly Color BarFrame = Hex("#3A4452");
    private static readonly Color BarBackground = Hex("#090D13");
    private static readonly Color HealthColor = Hex("#B33A4E");
    private static readonly Color ManaColor = Hex("#307FB7");
    private static readonly Color ExperienceColor = Hex("#C79B48");
    private static readonly Color GoldColor = Hex("#F2C759");
    private static readonly Color SoftWhite = Hex("#EDF0F5");
    private static readonly Color SoftGrey = Hex("#A6ADB8");
    private static readonly Color WarningRed = Hex("#F04455");

    private sealed class BarParts
    {
        public UIProgressBar progressBar;
        public TextMeshProUGUI valueText;
    }

    private sealed class HudParts
    {
        public UIProgressBar healthBar;
        public UIProgressBar manaBar;
        public UIProgressBar experienceBar;
        public TextMeshProUGUI waveText;
        public TextMeshProUGUI enemyText;
        public TextMeshProUGUI countdownText;
        public TextMeshProUGUI goldText;
        public TextMeshProUGUI levelText;
        public TextMeshProUGUI killsText;
        public WaveAnnouncementUI announcement;
        public CanvasGroup vignette;
        public Image[] inventoryIcons;
        public Image[] inventoryBorders;
        public GameObject inventoryRoot;
        public GameObject[] inventorySlots;
    }

    [MenuItem("Tools/Augmentra/Setup Gameplay HUD")]
    public static void SetupGameplayHud()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.Log("Gameplay HUD setup cancelled; open scene changes were not saved.");
            return;
        }

        if (!File.Exists(GameplayScenePath))
        {
            Debug.LogError("Gameplay HUD setup failed: missing " + GameplayScenePath);
            return;
        }

        string originalScenePath = SceneManager.GetActiveScene().path;

        try
        {
            BuildGameplayScene();
        }
        finally
        {
            if (!string.IsNullOrEmpty(originalScenePath) &&
                File.Exists(originalScenePath) &&
                originalScenePath != GameplayScenePath)
            {
                EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
            }

            EditorUtility.ClearProgressBar();
        }

        Debug.Log(
            "Gameplay HUD setup complete. SampleScene was saved with the compact event-driven HUD, " +
            "layered polished bars, hidden empty inventory slots, existing feedback, and wave announcements. " +
            "The command is idempotent and safe to run again.");
    }

    [MenuItem("Tools/Augmentra/Build HUD")]
    public static void BuildLegacyAlias()
    {
        SetupGameplayHud();
    }

    [MenuItem("Tools/Augmentra/Repair Gameplay HUD")]
    public static void RepairGameplayHud()
    {
        SetupGameplayHud();
    }

    private static void BuildGameplayScene()
    {
        EditorUtility.DisplayProgressBar("Augmentra HUD", "Loading gameplay scene...", 0.1f);
        Scene scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        TMP_FontAsset barFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BarFontPath);

        if (font == null || barFont == null)
        {
            throw new FileNotFoundException(
                "A required project font was not found.",
                font == null ? FontPath : BarFontPath);
        }

        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
        WaveManager waveManager = Object.FindFirstObjectByType<WaveManager>();
        Camera camera = Camera.main != null
            ? Camera.main
            : Object.FindFirstObjectByType<Camera>();

        if (player == null || gameManager == null || waveManager == null || camera == null)
        {
            Debug.LogError(
                "Gameplay HUD setup requires PlayerController, GameManager, WaveManager, " +
                "and a gameplay Camera in SampleScene. No HUD changes were saved.");
            return;
        }

        Health playerHealth = player.GetComponent<Health>();

        if (playerHealth == null)
        {
            Debug.LogError("Gameplay HUD setup requires Health on the Player.");
            return;
        }

        DestroyNamedObject(HudCanvasName);
        DestroyNamedObject(HudManagerName);
        DestroyNamedObject(DamagePoolName);
        EnsureEventSystem();

        EditorUtility.DisplayProgressBar("Augmentra HUD", "Building responsive HUD...", 0.35f);
        GameObject canvasObject = CreateCanvas();
        HudParts parts = BuildHud(canvasObject.transform, font, barFont);

        GameObject managerObject = new GameObject(
            HudManagerName,
            typeof(HUDManager),
            typeof(GameplayHUD),
            typeof(PlayerDamageFeedback),
            typeof(AudioSource));
        HUDManager inventoryManager = managerObject.GetComponent<HUDManager>();
        inventoryManager.inventoryIcons = parts.inventoryIcons;
        inventoryManager.inventoryBorders = parts.inventoryBorders;
        inventoryManager.inventoryRoot = parts.inventoryRoot;
        inventoryManager.inventorySlots = parts.inventorySlots;

        GameplayHUD gameplayHud = managerObject.GetComponent<GameplayHUD>();
        gameplayHud.Configure(
            player,
            playerHealth,
            gameManager,
            waveManager,
            parts.healthBar,
            parts.manaBar,
            parts.experienceBar,
            parts.waveText,
            parts.enemyText,
            parts.countdownText,
            parts.goldText,
            parts.levelText,
            parts.killsText,
            parts.announcement);

        AudioSource audioSource = managerObject.GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        PlayerDamageFeedback damageFeedback = managerObject.GetComponent<PlayerDamageFeedback>();
        damageFeedback.Configure(playerHealth, camera, parts.vignette, audioSource);

        EnsureHitFlash(player.gameObject);
        FloatingDamageNumber numberPrefab = CreateDamageNumberPrefab(font);
        GameObject poolObject = new GameObject(DamagePoolName);
        FloatingDamagePool pool = poolObject.AddComponent<FloatingDamagePool>();
        pool.Configure(numberPrefab, 24);

        EditorUtility.DisplayProgressBar("Augmentra HUD", "Updating enemy feedback...", 0.75f);
        EnsureEnemyPrefabFeedback();

        EditorUtility.SetDirty(inventoryManager);
        EditorUtility.SetDirty(gameplayHud);
        EditorUtility.SetDirty(damageFeedback);
        EditorUtility.SetDirty(pool);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = managerObject;
    }

    private static HudParts BuildHud(
        Transform canvas,
        TMP_FontAsset font,
        TMP_FontAsset barFont)
    {
        HudParts parts = new HudParts();
        BuildPlayerPanel(canvas, barFont, parts);
        BuildWavePanel(canvas, font, parts);
        BuildResourcePanel(canvas, font, barFont, parts);
        BuildInventory(canvas, parts);
        parts.announcement = BuildAnnouncement(canvas, font);
        parts.vignette = BuildDamageVignette(canvas);
        return parts;
    }

    private static void BuildPlayerPanel(Transform canvas, TMP_FontAsset font, HudParts parts)
    {
        GameObject group = NewUI("PlayerStatus", canvas);
        SetRect(
            group.GetComponent<RectTransform>(),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(24f, -24f),
            new Vector2(270f, 58f));

        BarParts health = AddBar(
            "HealthBar",
            group.transform,
            font,
            "HP",
            HealthColor,
            Vector2.zero,
            new Vector2(270f, 26f),
            true,
            false);
        BarParts mana = AddBar(
            "ManaBar",
            group.transform,
            font,
            "MP",
            ManaColor,
            new Vector2(0f, -34f),
            new Vector2(270f, 22f),
            false,
            false);
        parts.healthBar = health.progressBar;
        parts.manaBar = mana.progressBar;
    }

    private static void BuildWavePanel(Transform canvas, TMP_FontAsset font, HudParts parts)
    {
        GameObject panel = NewUI("WaveStatus", canvas);
        SetRect(
            panel.GetComponent<RectTransform>(),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -18f),
            new Vector2(360f, 76f));
        Image backing = AddImage(
            panel,
            new Color(PanelDark.r, PanelDark.g, PanelDark.b, 0.28f),
            BuiltinSprite(),
            true);
        backing.raycastTarget = false;

        parts.waveText = AddText(
            "WaveText", panel.transform, font, "WAVE 1", 26f, FontStyles.Bold, SoftWhite);
        SetRect(
            parts.waveText.rectTransform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -4f),
            new Vector2(340f, 34f));

        parts.enemyText = AddText(
            "EnemyCountText",
            panel.transform,
            font,
            "0 ENEMIES REMAINING",
            17f,
            FontStyles.Normal,
            SoftGrey);
        SetRect(
            parts.enemyText.rectTransform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -36f),
            new Vector2(340f, 22f));

        parts.countdownText = AddText(
            "CountdownText",
            panel.transform,
            font,
            "NEXT WAVE IN 3",
            15f,
            FontStyles.Bold,
            GoldColor);
        SetRect(
            parts.countdownText.rectTransform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -57f),
            new Vector2(340f, 18f));
    }

    private static void BuildResourcePanel(
        Transform canvas,
        TMP_FontAsset font,
        TMP_FontAsset barFont,
        HudParts parts)
    {
        GameObject panel = AddPanel(
            "RunStatus",
            canvas,
            Vector2.one,
            Vector2.one,
            Vector2.one,
            new Vector2(-24f, -24f),
            new Vector2(350f, 76f));

        AddCompactLabel(panel.transform, font, "GoldLabel", "GOLD", new Vector2(12f, -8f), 48f);
        parts.goldText = AddCompactValue(
            panel.transform, font, "GoldValue", "0", GoldColor, new Vector2(62f, -8f), 72f);
        AddCompactLabel(panel.transform, font, "LevelLabel", "LEVEL", new Vector2(154f, -8f), 58f);
        parts.levelText = AddCompactValue(
            panel.transform, font, "LevelValue", "1", SoftWhite, new Vector2(214f, -8f), 40f);
        AddCompactLabel(panel.transform, font, "KillsLabel", "KILLS", new Vector2(268f, -8f), 48f);
        parts.killsText = AddCompactValue(
            panel.transform, font, "KillsValue", "0", SoftWhite, new Vector2(318f, -8f), 24f);

        BarParts xp = AddBar(
            "ExperienceBar",
            panel.transform,
            barFont,
            "XP",
            ExperienceColor,
            new Vector2(12f, -50f),
            new Vector2(205f, 14f),
            false,
            true);
        parts.experienceBar = xp.progressBar;
    }

    private static void AddCompactLabel(
        Transform parent,
        TMP_FontAsset font,
        string name,
        string content,
        Vector2 position,
        float width)
    {
        TextMeshProUGUI labelText = AddText(
            name, parent, font, content, 13f, FontStyles.Bold, SoftGrey);
        SetRect(
            labelText.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            position,
            new Vector2(width, 24f));
        labelText.alignment = TextAlignmentOptions.Left;
    }

    private static TextMeshProUGUI AddCompactValue(
        Transform parent,
        TMP_FontAsset font,
        string name,
        string content,
        Color color,
        Vector2 position,
        float width)
    {
        TextMeshProUGUI valueText = AddText(
            name, parent, font, content, 15f, FontStyles.Bold, color);
        SetRect(
            valueText.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            position,
            new Vector2(width, 24f));
        valueText.alignment = TextAlignmentOptions.Left;
        return valueText;
    }

    private static BarParts AddBar(
        string name,
        Transform parent,
        TMP_FontAsset font,
        string label,
        Color fillColor,
        Vector2 topLeftPosition,
        Vector2 size,
        bool hasWarning,
        bool valueOutside)
    {
        const float innerTrackInset = 1f;

        GameObject root = NewUI(name, parent);
        SetRect(
            root.GetComponent<RectTransform>(),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            topLeftPosition,
            size);

        GameObject frameObject = NewUI("Frame", root.transform);
        Stretch(frameObject.GetComponent<RectTransform>());
        Image frame = AddImage(frameObject, BarFrame, BuiltinSprite(), true);
        frame.raycastTarget = false;

        GameObject trackObject = NewUI("Track", frameObject.transform);
        StretchWithOffsets(
            trackObject.GetComponent<RectTransform>(),
            innerTrackInset,
            innerTrackInset,
            innerTrackInset,
            innerTrackInset);
        Image track = AddImage(trackObject, BarBackground, BuiltinSprite(), true);
        track.raycastTarget = false;

        GameObject fillArea = NewUI("FillArea", frameObject.transform);
        StretchWithOffsets(
            fillArea.GetComponent<RectTransform>(),
            innerTrackInset,
            innerTrackInset,
            innerTrackInset,
            innerTrackInset);

        GameObject fillObject = NewUI("Fill", fillArea.transform);
        Stretch(fillObject.GetComponent<RectTransform>());
        Image fill = AddImage(fillObject, fillColor, BuiltinSprite(), true);
        fill.raycastTarget = false;

        TextMeshProUGUI valueText;

        if (valueOutside)
        {
            valueText = AddText(
                "Value", parent, font, label + "  0 / 5", 12f, FontStyles.Bold, SoftWhite);
            SetRect(
                valueText.rectTransform,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(topLeftPosition.x + size.x + 8f, topLeftPosition.y + 2f),
                new Vector2(80f, 18f));
            valueText.alignment = TextAlignmentOptions.Left;
        }
        else
        {
            valueText = AddText(
                "Value",
                frameObject.transform,
                font,
                label + "  100 / 100",
                13f,
                FontStyles.Bold,
                SoftWhite);
            StretchWithOffsets(valueText.rectTransform, 8f, 8f, 0f, 0f);
            valueText.alignment = TextAlignmentOptions.Center;
        }

        Image warning = null;

        if (hasWarning)
        {
            GameObject warningObject = NewUI("WarningBorder", frameObject.transform);
            Stretch(warningObject.GetComponent<RectTransform>());
            warning = AddImage(warningObject, WarningRed, BuiltinSprite(), true);
            warning.raycastTarget = false;
            Color warningColor = WarningRed;
            warningColor.a = 0f;
            warning.color = warningColor;
        }

        UIProgressBar progressBar = root.AddComponent<UIProgressBar>();
        progressBar.Configure(fill, valueText, warning, fillColor, null, true, label + "  ");
        return new BarParts { progressBar = progressBar, valueText = valueText };
    }

    private static void BuildInventory(Transform canvas, HudParts parts)
    {
        GameObject panel = NewUI("Inventory", canvas);
        SetRect(
            panel.GetComponent<RectTransform>(),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-24f, -110f),
            new Vector2(244f, 36f));

        GridLayoutGroup grid = panel.AddComponent<GridLayoutGroup>();
        grid.padding = new RectOffset(0, 0, 0, 0);
        grid.spacing = new Vector2(8f, 0f);
        grid.cellSize = new Vector2(34f, 34f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 6;
        grid.childAlignment = TextAnchor.MiddleCenter;

        parts.inventoryIcons = new Image[6];
        parts.inventoryBorders = new Image[6];
        parts.inventorySlots = new GameObject[6];
        parts.inventoryRoot = panel;

        for (int i = 0; i < 6; i++)
        {
            GameObject slot = NewUI("InventorySlot", panel.transform);
            Color slotColor = PanelBorder;
            slotColor.a = 0.72f;
            Image border = AddImage(slot, slotColor, BuiltinSprite(), true);
            border.raycastTarget = false;

            GameObject iconObject = NewUI("Icon", slot.transform);
            StretchWithOffsets(iconObject.GetComponent<RectTransform>(), 3f, 3f, 3f, 3f);
            Image icon = AddImage(iconObject, Color.white, null, false);
            icon.enabled = false;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            parts.inventoryIcons[i] = icon;
            parts.inventoryBorders[i] = border;
            parts.inventorySlots[i] = slot;
            slot.SetActive(false);
        }

        panel.SetActive(false);
    }

    private static WaveAnnouncementUI BuildAnnouncement(Transform canvas, TMP_FontAsset font)
    {
        GameObject root = NewUI("WaveAnnouncement", canvas);
        SetRect(
            root.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 90f),
            new Vector2(620f, 104f));
        AddImage(root, new Color(0.03f, 0.04f, 0.05f, 0.72f), null, false)
            .raycastTarget = false;
        CanvasGroup group = root.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;

        TextMeshProUGUI text = AddText(
            "AnnouncementText", root.transform, font, "WAVE 1", 52f, FontStyles.Bold, SoftWhite);
        Stretch(text.rectTransform);

        AudioSource audioSource = root.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        WaveAnnouncementUI announcement = root.AddComponent<WaveAnnouncementUI>();
        announcement.Configure(group, text, audioSource);
        return announcement;
    }

    private static CanvasGroup BuildDamageVignette(Transform canvas)
    {
        GameObject root = NewUI("DamageVignette", canvas);
        Stretch(root.GetComponent<RectTransform>());
        CanvasGroup group = root.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;

        AddEdge(root.transform, "Top", new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(0f, 70f));
        AddEdge(root.transform, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(0f, 70f));
        AddEdge(root.transform, "Left", new Vector2(0f, 0f), new Vector2(0f, 1f),
            new Vector2(0f, 0.5f), Vector2.zero, new Vector2(70f, 0f));
        AddEdge(root.transform, "Right", new Vector2(1f, 0f), new Vector2(1f, 1f),
            new Vector2(1f, 0.5f), Vector2.zero, new Vector2(70f, 0f));
        return group;
    }

    private static void AddEdge(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 position,
        Vector2 size)
    {
        GameObject edge = NewUI(name, parent);
        SetRect(edge.GetComponent<RectTransform>(), anchorMin, anchorMax, pivot, position, size);
        Image image = AddImage(edge, new Color(0.65f, 0.02f, 0.04f, 0.55f), null, false);
        image.raycastTarget = false;
    }

    private static FloatingDamageNumber CreateDamageNumberPrefab(TMP_FontAsset font)
    {
        GameObject root = new GameObject("FloatingDamageNumber");
        TextMeshPro text = root.AddComponent<TextMeshPro>();
        text.font = font;
        text.fontSize = 3.2f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = SoftWhite;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.rectTransform.sizeDelta = new Vector2(3f, 1.2f);
        MeshRenderer renderer = root.GetComponent<MeshRenderer>();

        if (renderer != null)
        {
            renderer.sortingOrder = 100;
        }

        FloatingDamageNumber number = root.AddComponent<FloatingDamageNumber>();
        number.Configure(text);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, DamageNumberPrefabPath);
        Object.DestroyImmediate(root);
        return prefab.GetComponent<FloatingDamageNumber>();
    }

    private static void EnsureEnemyPrefabFeedback()
    {
        if (!File.Exists(EnemyPrefabPath))
        {
            Debug.LogWarning("Enemy feedback was not added because Enemy.prefab is missing.");
            return;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(EnemyPrefabPath);

        try
        {
            EnsureHitFlash(root);

            if (root.GetComponent<AudioSource>() == null)
            {
                AudioSource source = root.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0.65f;
            }

            PrefabUtility.SaveAsPrefabAsset(root, EnemyPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void EnsureHitFlash(GameObject target)
    {
        if (target.GetComponent<HitFlash>() == null)
        {
            target.AddComponent<HitFlash>();
        }
    }

    private static GameObject CreateCanvas()
    {
        GameObject canvasObject = new GameObject(
            HudCanvasName,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return canvasObject;
    }

    private static GameObject AddPanel(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 position,
        Vector2 size)
    {
        GameObject panel = NewUI(name, parent);
        SetRect(panel.GetComponent<RectTransform>(), anchorMin, anchorMax, pivot, position, size);
        Image image = AddImage(panel, PanelDark, BuiltinSprite(), true);
        image.raycastTarget = false;
        return panel;
    }

    private static void EnsureEventSystem()
    {
        EventSystem[] systems = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);

        if (systems.Length == 0)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
        else if (systems.Length > 1)
        {
            Debug.LogWarning(
                "Multiple EventSystems already exist. Gameplay HUD setup did not create another one.");
        }
    }

    private static void DestroyNamedObject(string name)
    {
        GameObject existing = GameObject.Find(name);

        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }
    }

    private static GameObject NewUI(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static Image AddImage(
        GameObject target,
        Color color,
        Sprite sprite,
        bool sliced)
    {
        Image image = target.AddComponent<Image>();
        image.color = color;

        if (sprite != null)
        {
            image.sprite = sprite;
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
        }

        return image;
    }

    private static TextMeshProUGUI AddText(
        string name,
        Transform parent,
        TMP_FontAsset font,
        string content,
        float size,
        FontStyles style,
        Color color)
    {
        GameObject textObject = NewUI(name, parent);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = content;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }

    private static void SetRect(
        RectTransform rect,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 sizeDelta)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void StretchWithOffsets(
        RectTransform rect,
        float left,
        float right,
        float bottom,
        float top)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static Sprite BuiltinSprite()
    {
        return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
    }

    private static Color Hex(string value)
    {
        ColorUtility.TryParseHtmlString(value, out Color color);
        return color;
    }
}
