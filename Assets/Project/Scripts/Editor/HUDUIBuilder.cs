using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class HUDUIBuilder
{
    private const string FontPath = "Assets/Resources/Fonts/Nexa-ExtraLight SDF.asset";

    private static readonly Color PanelDark = new Color(0.051f, 0.051f, 0.051f, 0.78f);
    private static readonly Color SlotDark = new Color(0.067f, 0.067f, 0.067f, 0.85f);
    private static readonly Color HpCrimson = new Color(0.6f, 0.08f, 0.12f, 1f);
    private static readonly Color GoldColor = new Color(0.95f, 0.78f, 0.35f, 1f);
    private static readonly Color SoftWhite = new Color(0.92f, 0.93f, 0.95f, 1f);
    private static readonly Color SoftGrey = new Color(0.7f, 0.72f, 0.75f, 1f);
    private static readonly Color BorderGrey = new Color(0.5f, 0.5f, 0.5f, 1f);

    [MenuItem("Tools/Augmentra/Build HUD")]
    public static void Build()
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

        if (font == null)
        {
            EditorUtility.DisplayDialog("Build HUD", "Could not find the Nexa Extra font asset at:\n" + FontPath, "OK");
            return;
        }

        Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        GameObject oldCanvas = GameObject.Find("HUDCanvas");
        if (oldCanvas != null)
        {
            Object.DestroyImmediate(oldCanvas);
        }

        GameObject oldManager = GameObject.Find("HUDManager");
        if (oldManager != null)
        {
            Object.DestroyImmediate(oldManager);
        }

        EnsureEventSystem();

        GameObject canvasGo = new GameObject("HUDCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject managerGo = new GameObject("HUDManager");
        HUDManager hud = managerGo.AddComponent<HUDManager>();

        BuildTopBar(canvasGo.transform, font, uiSprite, hud);
        BuildAbilityBar(canvasGo.transform, font, uiSprite);
        BuildGold(canvasGo.transform, font, uiSprite, hud);
        BuildInventory(canvasGo.transform, uiSprite, hud);
        BuildStats(canvasGo.transform, font, uiSprite, hud);
        BuildAnnouncement(canvasGo.transform, font, hud);

        EditorUtility.SetDirty(hud);
        EditorSceneManager.MarkSceneDirty(canvasGo.scene);
        Selection.activeGameObject = managerGo;

        EditorUtility.DisplayDialog("Build HUD",
            "HUD built and wired.\n\nTop bar, ability bar, gold, inventory and wave announcement created. HUDManager added and all fields assigned.\n\nSave the scene (Ctrl+S) to keep it.",
            "OK");
    }

    private static void BuildTopBar(Transform parent, TMP_FontAsset font, Sprite uiSprite, HUDManager hud)
    {
        GameObject bar = NewUI("TopBar", parent);
        SetRect(bar.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(0f, 60f));
        AddImage(bar, PanelDark, uiSprite, true);

        GameObject hpBg = NewUI("HPBarBg", bar.transform);
        SetRect(hpBg.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(340f, 22f));
        AddImage(hpBg, SlotDark, uiSprite, true);

        GameObject hpFillGo = NewUI("HPFill", hpBg.transform);
        RectTransform hpFillRect = hpFillGo.GetComponent<RectTransform>();
        Stretch(hpFillRect);
        hpFillRect.offsetMin = new Vector2(2f, 2f);
        hpFillRect.offsetMax = new Vector2(-2f, -2f);
        Image hpFill = AddImage(hpFillGo, HpCrimson, uiSprite, true);
        hpFill.type = Image.Type.Filled;
        hpFill.fillMethod = Image.FillMethod.Horizontal;
        hpFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        hpFill.fillAmount = 1f;

        TextMeshProUGUI hpText = AddText("HPText", hpBg.transform, font, "100 / 100", 14f, FontStyles.Normal, SoftWhite);
        Stretch(hpText.rectTransform);

        TextMeshProUGUI waveText = AddText("WaveText", bar.transform, font, "Wave 1", 18f, FontStyles.Normal, SoftWhite);
        SetRect(waveText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(380f, 0f), new Vector2(220f, 30f));
        waveText.alignment = TextAlignmentOptions.Left;

        hud.hpFill = hpFill;
        hud.hpText = hpText;
        hud.waveText = waveText;
    }

    private static void BuildAbilityBar(Transform parent, TMP_FontAsset font, Sprite uiSprite)
    {
        GameObject bar = NewUI("AbilityBar", parent);
        SetRect(bar.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(300f, 64f));
        HorizontalLayoutGroup layout = bar.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        string[] keys = { "Q", "W", "E", "R" };

        foreach (string key in keys)
        {
            GameObject slot = NewUI("AbilitySlot", bar.transform);
            slot.GetComponent<RectTransform>().sizeDelta = new Vector2(64f, 64f);
            AddImage(slot, SlotDark, uiSprite, true);

            TextMeshProUGUI label = AddText("Key", slot.transform, font, key, 13f, FontStyles.Normal, SoftGrey);
            SetRect(label.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(40f, 18f));
        }
    }

    private static void BuildGold(Transform parent, TMP_FontAsset font, Sprite knob, HUDManager hud)
    {
        GameObject gold = NewUI("GoldDisplay", parent);
        SetRect(gold.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(-180f, 46f), new Vector2(120f, 32f));
        HorizontalLayoutGroup layout = gold.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.MiddleRight;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        GameObject coin = NewUI("Coin", gold.transform);
        coin.GetComponent<RectTransform>().sizeDelta = new Vector2(22f, 22f);
        AddImage(coin, GoldColor, knob, false);

        TextMeshProUGUI goldText = AddText("GoldText", gold.transform, font, "0", 18f, FontStyles.Normal, GoldColor);
        goldText.rectTransform.sizeDelta = new Vector2(70f, 30f);
        goldText.alignment = TextAlignmentOptions.Left;

        hud.goldText = goldText;
    }

    private static void BuildInventory(Transform parent, Sprite uiSprite, HUDManager hud)
    {
        const float cell = 44f;
        const float spacing = 6f;
        float width = cell * 3f + spacing * 2f;
        float height = cell * 2f + spacing;

        GameObject inv = NewUI("Inventory", parent);
        SetRect(inv.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 20f), new Vector2(width, height));
        GridLayoutGroup grid = inv.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(cell, cell);
        grid.spacing = new Vector2(spacing, spacing);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;

        Image[] icons = new Image[6];
        Image[] borders = new Image[6];

        for (int i = 0; i < 6; i++)
        {
            GameObject slot = NewUI("InvSlot", inv.transform);
            borders[i] = AddImage(slot, BorderGrey, uiSprite, true);

            GameObject inner = NewUI("Inner", slot.transform);
            RectTransform innerRect = inner.GetComponent<RectTransform>();
            Stretch(innerRect);
            innerRect.offsetMin = new Vector2(3f, 3f);
            innerRect.offsetMax = new Vector2(-3f, -3f);
            AddImage(inner, SlotDark, uiSprite, true);

            GameObject iconGo = NewUI("Icon", inner.transform);
            RectTransform iconRect = iconGo.GetComponent<RectTransform>();
            Stretch(iconRect);
            iconRect.offsetMin = new Vector2(4f, 4f);
            iconRect.offsetMax = new Vector2(-4f, -4f);
            Image icon = AddImage(iconGo, Color.white, null, false);
            icon.enabled = false;
            icons[i] = icon;
        }

        hud.inventoryBorders = borders;
        hud.inventoryIcons = icons;
    }

    private static void BuildStats(Transform parent, TMP_FontAsset font, Sprite uiSprite, HUDManager hud)
    {
        GameObject panel = NewUI("StatsPanel", parent);
        SetRect(panel.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(20f, 20f), new Vector2(120f, 320f));
        AddImage(panel, PanelDark, uiSprite, true);
        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 6f;
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        TextMeshProUGUI[] texts = new TextMeshProUGUI[10];

        for (int i = 0; i < 10; i++)
        {
            GameObject row = NewUI("Row", panel.transform);
            row.GetComponent<RectTransform>().sizeDelta = new Vector2(120f, 24f);
            HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 8f;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlWidth = false;
            rowLayout.childControlHeight = false;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;

            GameObject iconGo = NewUI("Icon", row.transform);
            iconGo.GetComponent<RectTransform>().sizeDelta = new Vector2(20f, 20f);
            AddImage(iconGo, SlotDark, uiSprite, true);

            TextMeshProUGUI value = AddText("Value", row.transform, font, "0", 14f, FontStyles.Normal, SoftWhite);
            value.rectTransform.sizeDelta = new Vector2(70f, 22f);
            value.alignment = TextAlignmentOptions.Left;
            texts[i] = value;
        }

        hud.statTexts = texts;
    }

    private static void BuildAnnouncement(Transform parent, TMP_FontAsset font, HUDManager hud)
    {
        GameObject root = NewUI("WaveAnnouncement", parent);
        Stretch(root.GetComponent<RectTransform>());
        CanvasGroup group = root.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;

        GameObject vignette = NewUI("Vignette", root.transform);
        SetRect(vignette.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(900f, 220f));
        AddImage(vignette, new Color(0.02f, 0.02f, 0.02f, 0.55f), null, false);

        TextMeshProUGUI text = AddText("AnnouncementText", root.transform, font, "Wave 1", 90f, FontStyles.Normal, SoftWhite);
        SetRect(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(1200f, 200f));

        root.SetActive(false);

        hud.announcementGroup = group;
        hud.announcementText = text;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
    }

    private static GameObject NewUI(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));

        if (parent != null)
        {
            go.transform.SetParent(parent, false);
        }

        return go;
    }

    private static Image AddImage(GameObject go, Color color, Sprite sprite, bool sliced)
    {
        Image img = go.AddComponent<Image>();
        img.color = color;

        if (sprite != null)
        {
            img.sprite = sprite;
            img.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
        }

        return img;
    }

    private static TextMeshProUGUI AddText(string name, Transform parent, TMP_FontAsset font, string text, float size, FontStyles style, Color color)
    {
        GameObject go = NewUI(name, parent);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.font = font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
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
}
