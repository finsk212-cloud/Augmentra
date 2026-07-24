using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class AugmentUIBuilder
{
    private const string FontPath = "Assets/Resources/Fonts/Nexa-ExtraLight SDF.asset";
    private const string PrefabFolder = "Assets/Prefabs";
    private const string PrefabPath = "Assets/Prefabs/AugmentCard.prefab";

    [MenuItem("Tools/Augmentra/Build Augment UI")]
    public static void Build()
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

        if (font == null)
        {
            EditorUtility.DisplayDialog("Build Augment UI", "Could not find the Nexa Extra font asset at:\n" + FontPath, "OK");
            return;
        }

        GameObject existing = GameObject.Find("AugmentCanvas");
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }

        GameObject existingManager = GameObject.Find("AugmentManager");
        if (existingManager != null)
        {
            Object.DestroyImmediate(existingManager);
        }

        EnsureEventSystem();

        GameObject prefab = BuildCardPrefab(font);

        GameObject canvasGo = new GameObject("AugmentCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject overlay = NewUI("AugmentOverlay", canvasGo.transform);
        Stretch(overlay.GetComponent<RectTransform>());
        Image overlayImage = overlay.AddComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.8f);

        TextMeshProUGUI title = AddText("Title", overlay.transform, font, "CHOOSE AN AUGMENT", 54f, FontStyles.Bold, new Color(1f, 1f, 1f, 1f));
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -90f);
        titleRect.sizeDelta = new Vector2(1400f, 80f);

        TextMeshProUGUI subtitle = AddText("Subtitle", overlay.transform, font, "Select a card to continue", 28f, FontStyles.Normal, new Color(0.75f, 0.78f, 0.82f, 1f));
        RectTransform subtitleRect = subtitle.rectTransform;
        subtitleRect.anchorMin = new Vector2(0.5f, 1f);
        subtitleRect.anchorMax = new Vector2(0.5f, 1f);
        subtitleRect.pivot = new Vector2(0.5f, 1f);
        subtitleRect.anchoredPosition = new Vector2(0f, -175f);
        subtitleRect.sizeDelta = new Vector2(1200f, 50f);

        GameObject container = NewUI("CardContainer", overlay.transform);
        RectTransform containerRect = container.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.anchoredPosition = new Vector2(0f, -20f);
        containerRect.sizeDelta = new Vector2(960f, 400f);
        HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 40f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        GameObject managerGo = new GameObject("AugmentManager");
        AugmentManager manager = managerGo.AddComponent<AugmentManager>();

        SerializedObject so = new SerializedObject(manager);
        so.FindProperty("augmentOverlay").objectReferenceValue = overlay;
        so.FindProperty("augmentCardPrefab").objectReferenceValue = prefab;
        so.FindProperty("cardContainer").objectReferenceValue = containerRect;
        so.FindProperty("titleText").objectReferenceValue = title;
        so.FindProperty("subtitleText").objectReferenceValue = subtitle;
        so.ApplyModifiedPropertiesWithoutUndo();

        overlay.SetActive(false);

        EditorSceneManager.MarkSceneDirty(canvasGo.scene);
        Selection.activeGameObject = managerGo;

        EditorUtility.DisplayDialog("Build Augment UI",
            "Augment UI built and wired.\n\nCanvas + overlay created, AugmentCard.prefab saved, AugmentManager added and all fields assigned.\n\nSave the scene (Ctrl+S) to keep it.",
            "OK");
    }

    private static GameObject BuildCardPrefab(TMP_FontAsset font)
    {
        if (!AssetDatabase.IsValidFolder(PrefabFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        GameObject root = NewUI("AugmentCard", null);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(280f, 380f);
        Image bg = root.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.08f, 0.11f, 1f);
        Button button = root.AddComponent<Button>();
        button.targetGraphic = bg;
        AugmentCard card = root.AddComponent<AugmentCard>();

        GameObject icon = NewUI("Icon", root.transform);
        RectTransform iconRect = icon.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 1f);
        iconRect.anchorMax = new Vector2(0.5f, 1f);
        iconRect.pivot = new Vector2(0.5f, 1f);
        iconRect.anchoredPosition = new Vector2(0f, -20f);
        iconRect.sizeDelta = new Vector2(120f, 120f);
        Image iconImage = icon.AddComponent<Image>();
        iconImage.preserveAspect = true;

        TextMeshProUGUI name = AddText("Name", root.transform, font, "Augment Name", 26f, FontStyles.Bold, new Color(1f, 1f, 1f, 1f));
        TopRect(name.rectTransform, 150f, 36f);

        TextMeshProUGUI rarity = AddText("Rarity", root.transform, font, "Common", 16f, FontStyles.UpperCase, new Color(0.95f, 0.78f, 0.35f, 1f));
        TopRect(rarity.rectTransform, 188f, 24f);

        TextMeshProUGUI description = AddText("Description", root.transform, font, "Description", 15f, FontStyles.Normal, new Color(0.82f, 0.85f, 0.88f, 1f));
        description.alignment = TextAlignmentOptions.Top;
        TopRect(description.rectTransform, 218f, 96f);

        TextMeshProUGUI flavour = AddText("Flavour", root.transform, font, "Flavour text", 13f, FontStyles.Italic, new Color(0.55f, 0.58f, 0.62f, 1f));
        TopRect(flavour.rectTransform, 320f, 44f);

        card.iconImage = iconImage;
        card.nameText = name;
        card.rarityText = rarity;
        card.descriptionText = description;
        card.flavourText = flavour;

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        return saved;
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

    private static void TopRect(RectTransform rect, float top, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -top);
        rect.sizeDelta = new Vector2(-20f, height);
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
