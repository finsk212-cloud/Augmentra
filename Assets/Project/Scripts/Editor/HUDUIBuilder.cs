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
    private const string DamageNumberPrefabPath =
        "Assets/Project/Prefabs/FloatingDamageNumber.prefab";
    private const string EnemyPrefabPath = "Assets/Project/Prefabs/Enemy.prefab";
    private const string HudCanvasName = "HUDCanvas";
    private const string HudManagerName = "HUDManager";
    private const string DamagePoolName = "FloatingDamagePool";

    private static readonly Color PanelDark = Hex("#0D1014D8");
    private static readonly Color PanelBorder = Hex("#3A404C");
    private static readonly Color BarBackground = Hex("#171B22");
    private static readonly Color HealthColor = Hex("#B52E3E");
    private static readonly Color ManaColor = Hex("#337FC4");
    private static readonly Color ExperienceColor = Hex("#D0A842");
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
            "Gameplay HUD setup complete. SampleScene was saved with the new event-driven HUD, " +
            "player/enemy feedback, pooled damage numbers, and wave announcements. " +
            "The command is idempotent and safe to run again.");
    }

    [MenuItem("Tools/Augmentra/Build HUD")]
    public static void BuildLegacyAlias()
    {
        SetupGameplayHud();
    }

    private static void BuildGameplayScene()
    {
        EditorUtility.DisplayProgressBar("Augmentra HUD", "Loading gameplay scene...", 0.1f);
        Scene scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

        if (font == null)
        {
            throw new FileNotFoundException("Required project font was not found.", FontPath);
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
        HudParts parts = BuildHud(canvasObject.transform, font);

        GameObject managerObject = new GameObject(
            HudManagerName,
            typeof(HUDManager),
            typeof(GameplayHUD),
            typeof(PlayerDamageFeedback),
            typeof(AudioSource));
        HUDManager inventoryManager = managerObject.GetComponent<HUDManager>();
        inventoryManager.inventoryIcons = parts.inventoryIcons;
        inventoryManager.inventoryBorders = parts.inventoryBorders;

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

    private static HudParts BuildHud(Transform canvas, TMP_FontAsset font)
    {
        HudParts parts = new HudParts();
        BuildPlayerPanel(canvas, font, parts);
        BuildWavePanel(canvas, font, parts);
        BuildResourcePanel(canvas, font, parts);
        BuildInventory(canvas, parts);
        parts.announcement = BuildAnnouncement(canvas, font);
        parts.vignette = BuildDamageVignette(canvas);
        return parts;
    }

    private static void BuildPlayerPanel(Transform canvas, TMP_FontAsset font, HudParts parts)
    {
        GameObject panel = AddPanel(
            "PlayerStatus",
            canvas,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(24f, -24f),
            new Vector2(420f, 142f));

        TextMeshProUGUI title = AddText(
            "PlayerLabel", panel.transform, font, "PLAYER", 16f, FontStyles.Bold, SoftGrey);
        SetRect(
            title.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(18f, -12f),
            new Vector2(200f, 24f));
        title.alignment = TextAlignmentOptions.Left;

        BarParts health = AddBar(
            "HealthBar",
            panel.transform,
            font,
            "HEALTH",
            HealthColor,
            new Vector2(18f, -45f),
            new Vector2(384f, 34f),
            true);
        BarParts mana = AddBar(
            "ManaBar",
            panel.transform,
            font,
            "MANA",
            ManaColor,
            new Vector2(18f, -91f),
            new Vector2(384f, 34f),
            false);
        parts.healthBar = health.progressBar;
        parts.manaBar = mana.progressBar;
    }

    private static void BuildWavePanel(Transform canvas, TMP_FontAsset font, HudParts parts)
    {
        GameObject panel = AddPanel(
            "WaveStatus",
            canvas,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -24f),
            new Vector2(500f, 116f));

        parts.waveText = AddText(
            "WaveText", panel.transform, font, "WAVE 1", 26f, FontStyles.Bold, SoftWhite);
        SetRect(
            parts.waveText.rectTransform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -12f),
            new Vector2(460f, 34f));

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
            new Vector2(0f, -50f),
            new Vector2(460f, 26f));

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
            new Vector2(0f, -79f),
            new Vector2(460f, 24f));
    }

    private static void BuildResourcePanel(Transform canvas, TMP_FontAsset font, HudParts parts)
    {
        GameObject panel = AddPanel(
            "RunStatus",
            canvas,
            Vector2.one,
            Vector2.one,
            Vector2.one,
            new Vector2(-24f, -24f),
            new Vector2(380f, 156f));

        parts.goldText = AddValueRow(
            panel.transform, font, "Gold", "GOLD", "0", GoldColor, -16f);
        parts.levelText = AddValueRow(
            panel.transform, font, "Level", "LEVEL", "LEVEL 1", SoftWhite, -48f);
        parts.killsText = AddValueRow(
            panel.transform, font, "Kills", "COMBAT", "0 KILLS", SoftGrey, -80f);

        BarParts xp = AddBar(
            "ExperienceBar",
            panel.transform,
            font,
            "EXPERIENCE",
            ExperienceColor,
            new Vector2(18f, -112f),
            new Vector2(344f, 28f),
            false);
        parts.experienceBar = xp.progressBar;
    }

    private static TextMeshProUGUI AddValueRow(
        Transform parent,
        TMP_FontAsset font,
        string name,
        string label,
        string value,
        Color valueColor,
        float y)
    {
        TextMeshProUGUI labelText = AddText(
            name + "Label", parent, font, label, 15f, FontStyles.Bold, SoftGrey);
        SetRect(
            labelText.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(18f, y),
            new Vector2(150f, 26f));
        labelText.alignment = TextAlignmentOptions.Left;

        TextMeshProUGUI valueText = AddText(
            name + "Value", parent, font, value, 17f, FontStyles.Bold, valueColor);
        SetRect(
            valueText.rectTransform,
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-18f, y),
            new Vector2(190f, 26f));
        valueText.alignment = TextAlignmentOptions.Right;
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
        bool hasWarning)
    {
        GameObject root = NewUI(name, parent);
        SetRect(
            root.GetComponent<RectTransform>(),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            topLeftPosition,
            size);
        Image background = AddImage(root, BarBackground, BuiltinSprite(), true);
        background.raycastTarget = false;

        GameObject fillObject = NewUI("Fill", root.transform);
        StretchWithOffsets(fillObject.GetComponent<RectTransform>(), 3f, 3f, 3f, 3f);
        Image fill = AddImage(fillObject, fillColor, BuiltinSprite(), true);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 1f;
        fill.raycastTarget = false;

        TextMeshProUGUI labelText = AddText(
            "Label", root.transform, font, label, 12f, FontStyles.Bold, SoftGrey);
        SetRect(
            labelText.rectTransform,
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(10f, 0f),
            new Vector2(120f, size.y));
        labelText.alignment = TextAlignmentOptions.Left;

        TextMeshProUGUI valueText = AddText(
            "Value", root.transform, font, "100 / 100", 14f, FontStyles.Bold, SoftWhite);
        StretchWithOffsets(valueText.rectTransform, 120f, 10f, 0f, 0f);
        valueText.alignment = TextAlignmentOptions.Right;

        Image warning = null;

        if (hasWarning)
        {
            GameObject warningObject = NewUI("WarningBorder", root.transform);
            Stretch(warningObject.GetComponent<RectTransform>());
            warning = AddImage(warningObject, WarningRed, BuiltinSprite(), true);
            warning.raycastTarget = false;
            Color warningColor = WarningRed;
            warningColor.a = 0f;
            warning.color = warningColor;
        }

        UIProgressBar progressBar = root.AddComponent<UIProgressBar>();
        progressBar.Configure(fill, valueText, warning, fillColor);
        return new BarParts { progressBar = progressBar, valueText = valueText };
    }

    private static void BuildInventory(Transform canvas, HudParts parts)
    {
        GameObject panel = AddPanel(
            "Inventory",
            canvas,
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-24f, -196f),
            new Vector2(270f, 96f));

        GridLayoutGroup grid = panel.AddComponent<GridLayoutGroup>();
        grid.padding = new RectOffset(12, 12, 12, 12);
        grid.spacing = new Vector2(8f, 0f);
        grid.cellSize = new Vector2(34f, 68f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 6;
        grid.childAlignment = TextAnchor.MiddleCenter;

        parts.inventoryIcons = new Image[6];
        parts.inventoryBorders = new Image[6];

        for (int i = 0; i < 6; i++)
        {
            GameObject slot = NewUI("InventorySlot", panel.transform);
            Image border = AddImage(slot, PanelBorder, BuiltinSprite(), true);
            border.raycastTarget = false;

            GameObject iconObject = NewUI("Icon", slot.transform);
            StretchWithOffsets(iconObject.GetComponent<RectTransform>(), 3f, 3f, 20f, 3f);
            Image icon = AddImage(iconObject, Color.white, null, false);
            icon.enabled = false;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            parts.inventoryIcons[i] = icon;
            parts.inventoryBorders[i] = border;
        }
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
        Outline outline = panel.AddComponent<Outline>();
        outline.effectColor = PanelBorder;
        outline.effectDistance = new Vector2(1f, -1f);
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
