using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Augmentra.UI;

public static class ShopCanvasBuilder
{
    private const string PrefabFolder = "Assets/Prefabs";
    private const string PrefabPath = "Assets/Prefabs/ShopCanvas.prefab";

    private static readonly Color ShopBg = new Color(0.039f, 0.039f, 0.078f, 0.97f);
    private static readonly Color CardBg = new Color(0.051f, 0.106f, 0.165f, 1f);
    private static readonly Color Border = new Color(0.118f, 0.227f, 0.373f, 1f);
    private static readonly Color FieldBg = new Color(0.051f, 0.067f, 0.090f, 1f);
    private static readonly Color Grey = new Color(0.478f, 0.498f, 0.541f, 1f);
    private static readonly Color Gold = new Color(0.949f, 0.788f, 0.298f, 1f);
    private static readonly Color Green = new Color(0.102f, 0.478f, 0.290f, 1f);
    private static readonly Color BreakGreen = new Color(0.286f, 0.871f, 0.502f, 1f);
    private static readonly Color CombatAccent = new Color(0.753f, 0.224f, 0.169f, 1f);
    private static readonly Color SurvivalAccent = new Color(0.102f, 0.478f, 0.290f, 1f);
    private static readonly Color ChaosAccent = new Color(0.482f, 0.184f, 0.745f, 1f);
    private static readonly Color AllAccent = new Color(0.769f, 0.635f, 0.208f, 1f);
    private static readonly Color DescBg = new Color(0.039f, 0.059f, 0.102f, 1f);

    [MenuItem("Augmentra/Build Shop Canvas Prefab")]
    public static void Build()
    {
        GameObject root = new GameObject("ShopCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject breakRoot = Child("BreakRoot", root.transform);
        RectTransform breakRT = breakRoot.GetComponent<RectTransform>();
        Stretch(breakRT);
        Text("BreakHint", breakRoot.transform, "Press B to open shop", 26f, BreakGreen, new Vector2(520f, 40f), new Vector2(0.5f, 0f), new Vector2(0f, 90f), TextAlignmentOptions.Center);

        GameObject panel = Image("ShopPanel", root.transform, Border, new Vector2(822f, 482f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 110f));
        RectTransform panelRT = panel.GetComponent<RectTransform>();
        panelRT.pivot = new Vector2(0.5f, 0f);
        panelRT.anchoredPosition = new Vector2(0f, 110f);
        panel.AddComponent<PanelDragger>().target = panelRT;

        GameObject bg = Image("ShopBg", panel.transform, ShopBg, new Vector2(820f, 480f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
        bg.AddComponent<RectMask2D>();
        Image bgImage = bg.GetComponent<Image>();
        bgImage.raycastTarget = false;
        Transform p = bg.transform;

        BuildSearch(p);
        TextMeshProUGUI gold = Text("Gold", p, "0 g", 26f, Gold, new Vector2(220f, 36f), new Vector2(0.5f, 0.5f), new Vector2(150f, 205f), TextAlignmentOptions.Right);
        gold.raycastTarget = false;
        Text("CloseHint", p, "B to close", 18f, Grey, new Vector2(120f, 30f), new Vector2(0.5f, 0.5f), new Vector2(345f, 205f), TextAlignmentOptions.Right);

        BuildSidebar(p);
        BuildGrid(p);
        BuildDescription(p);
        BuildReady(p);

        Empty(p);

        Directory.CreateDirectory(PrefabFolder);
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.Refresh();

        Debug.Log("ShopCanvas prefab built at " + PrefabPath);
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
    }

    private static void BuildSearch(Transform parent)
    {
        GameObject go = Image("Search", parent, FieldBg, new Vector2(220f, 32f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-290f, 205f));
        TMP_InputField input = go.AddComponent<TMP_InputField>();

        GameObject area = Child("TextArea", go.transform);
        area.AddComponent<RectMask2D>();
        RectTransform art = area.GetComponent<RectTransform>();
        art.anchorMin = Vector2.zero;
        art.anchorMax = Vector2.one;
        art.offsetMin = new Vector2(10f, 2f);
        art.offsetMax = new Vector2(-10f, -2f);

        TextMeshProUGUI placeholder = StretchText("Placeholder", area.transform, "Search items...", Grey);
        TextMeshProUGUI text = StretchText("Text", area.transform, "", Color.white);

        input.textViewport = art;
        input.textComponent = text;
        input.placeholder = placeholder;
        input.pointSize = 16f;
        input.text = "";
    }

    private static void BuildSidebar(Transform parent)
    {
        GameObject sidebar = Child("Sidebar", parent);
        RectTransform rt = sidebar.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(130f, 200f);
        rt.anchoredPosition = new Vector2(-345f, 75f);

        Tab("Tab_ALL", sidebar.transform, "ALL", AllAccent, new Vector2(0f, 75f));
        Tab("Tab_COMBAT", sidebar.transform, "COMBAT", CombatAccent, new Vector2(0f, 25f));
        Tab("Tab_SURVIVAL", sidebar.transform, "SURVIVAL", SurvivalAccent, new Vector2(0f, -25f));
        Tab("Tab_CHAOS", sidebar.transform, "CHAOS", ChaosAccent, new Vector2(0f, -75f));
    }

    private static void Tab(string name, Transform parent, string label, Color accent, Vector2 pos)
    {
        GameObject go = Image(name, parent, CardBg, new Vector2(130f, 44f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos);
        go.AddComponent<Button>().transition = Selectable.Transition.None;

        GameObject acc = Image("Accent", go.transform, accent, new Vector2(3f, 0f), new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero);
        RectTransform art = acc.GetComponent<RectTransform>();
        art.pivot = new Vector2(0f, 0.5f);
        art.anchoredPosition = Vector2.zero;

        TextMeshProUGUI t = Text("Label", go.transform, label, 16f, Color.white, new Vector2(130f, 44f), new Vector2(0.5f, 0.5f), Vector2.zero, TextAlignmentOptions.Center);
        t.raycastTarget = false;
    }

    private static void BuildGrid(Transform parent)
    {
        GameObject scroll = Image("Grid", parent, new Color(0f, 0f, 0f, 0f), new Vector2(680f, 410f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(65f, -18f));
        scroll.AddComponent<RectMask2D>();
        ScrollRect sr = scroll.AddComponent<ScrollRect>();
        sr.horizontal = false;
        sr.vertical = true;
        sr.scrollSensitivity = 28f;
        sr.movementType = ScrollRect.MovementType.Clamped;

        GameObject content = Child("Content", scroll.transform);
        RectTransform crt = content.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0f, 1f);
        crt.anchorMax = new Vector2(1f, 1f);
        crt.pivot = new Vector2(0.5f, 1f);
        crt.anchoredPosition = Vector2.zero;
        crt.sizeDelta = Vector2.zero;

        GridLayoutGroup grid = content.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(160f, 180f);
        grid.spacing = new Vector2(8f, 10f);
        grid.padding = new RectOffset(4, 4, 4, 4);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;
        grid.childAlignment = TextAnchor.UpperCenter;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        sr.viewport = scroll.GetComponent<RectTransform>();
        sr.content = crt;

        BuildCardTemplate(content.transform);
    }

    private static void BuildCardTemplate(Transform parent)
    {
        GameObject card = Image("CardTemplate", parent, Border, new Vector2(160f, 180f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
        card.AddComponent<Button>().transition = Selectable.Transition.None;

        GameObject cbg = Child("Bg", card.transform);
        RectTransform brt = cbg.GetComponent<RectTransform>();
        brt.anchorMin = Vector2.zero;
        brt.anchorMax = Vector2.one;
        brt.offsetMin = new Vector2(1f, 1f);
        brt.offsetMax = new Vector2(-1f, -1f);
        cbg.AddComponent<Image>().color = CardBg;

        GameObject icon = Image("Icon", cbg.transform, Color.white, new Vector2(64f, 64f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 38f));
        Image iconImg = icon.GetComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;

        Text("Name", cbg.transform, "Item", 13f, Color.white, new Vector2(150f, 36f), new Vector2(0.5f, 0.5f), new Vector2(0f, -22f), TextAlignmentOptions.Center).raycastTarget = false;
        Text("Cost", cbg.transform, "0 g", 14f, Gold, new Vector2(150f, 24f), new Vector2(0.5f, 0.5f), new Vector2(0f, -64f), TextAlignmentOptions.Center).raycastTarget = false;

        card.SetActive(false);
    }

    private static void BuildDescription(Transform parent)
    {
        GameObject desc = Image("DescPanel", parent, DescBg, new Vector2(220f, 480f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(300f, 0f));
        Transform d = desc.transform;

        GameObject lb = Image("LeftBorder", d, Border, new Vector2(2f, 0f), new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero);
        lb.GetComponent<RectTransform>().pivot = new Vector2(0f, 0.5f);
        lb.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

        GameObject icon = Image("DescIcon", d, Color.white, new Vector2(80f, 80f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 150f));
        icon.GetComponent<Image>().preserveAspect = true;

        Text("DescName", d, "", 18f, Color.white, new Vector2(200f, 46f), new Vector2(0.5f, 0.5f), new Vector2(0f, 95f), TextAlignmentOptions.Center);
        Divider(d, new Vector2(0f, 70f));

        GameObject badge = Image("Badge", d, ChaosAccent, new Vector2(110f, 26f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 46f));
        Text("BadgeText", badge.transform, "", 13f, Color.white, new Vector2(110f, 26f), new Vector2(0.5f, 0.5f), Vector2.zero, TextAlignmentOptions.Center);

        Text("Stat0", d, "", 14f, BreakGreen, new Vector2(190f, 22f), new Vector2(0.5f, 0.5f), new Vector2(0f, 12f), TextAlignmentOptions.Left);
        Text("Stat1", d, "", 14f, BreakGreen, new Vector2(190f, 22f), new Vector2(0.5f, 0.5f), new Vector2(0f, -12f), TextAlignmentOptions.Left);
        Text("Stat2", d, "", 14f, BreakGreen, new Vector2(190f, 22f), new Vector2(0.5f, 0.5f), new Vector2(0f, -36f), TextAlignmentOptions.Left);

        Divider(d, new Vector2(0f, -60f));

        TextMeshProUGUI flavour = Text("Flavour", d, "", 13f, Grey, new Vector2(190f, 90f), new Vector2(0.5f, 0.5f), new Vector2(0f, -120f), TextAlignmentOptions.Top);
        flavour.fontStyle = FontStyles.Italic;

        Text("DescCost", d, "", 24f, Gold, new Vector2(190f, 34f), new Vector2(0.5f, 0.5f), new Vector2(0f, -190f), TextAlignmentOptions.Center);
        Text("DescHint", d, "", 14f, Grey, new Vector2(190f, 24f), new Vector2(0.5f, 0.5f), new Vector2(0f, -218f), TextAlignmentOptions.Center);
    }

    private static void BuildReady(Transform parent)
    {
        GameObject go = Image("ReadyButton", parent, Green, new Vector2(118f, 44f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(8f, 10f));
        go.GetComponent<RectTransform>().pivot = new Vector2(0f, 0f);
        go.GetComponent<RectTransform>().anchoredPosition = new Vector2(8f, 10f);
        Button readyButton = go.AddComponent<Button>();
        readyButton.targetGraphic = go.GetComponent<Image>();
        UIColorUtility.ApplyConsistentColors(readyButton, Green);
        Text("ReadyLabel", go.transform, "READY", 18f, Color.white, new Vector2(118f, 44f), new Vector2(0.5f, 0.5f), Vector2.zero, TextAlignmentOptions.Center).raycastTarget = false;
    }

    private static void Empty(Transform parent)
    {
        GameObject go = Text("Empty", parent, "No items found", 22f, Grey, new Vector2(400f, 40f), new Vector2(0.5f, 0.5f), new Vector2(65f, 0f), TextAlignmentOptions.Center).gameObject;
        go.SetActive(false);
    }

    private static void Divider(Transform parent, Vector2 pos)
    {
        Image("Divider", parent, Border, new Vector2(180f, 2f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos);
    }

    private static GameObject Child(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.GetComponent<RectTransform>().SetParent(parent, false);
        return go;
    }

    private static GameObject Image(string name, Transform parent, Color color, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        go.GetComponent<Image>().color = color;
        return go;
    }

    private static TextMeshProUGUI Text(string name, Transform parent, string content, float size, Color color, Vector2 sizeDelta, Vector2 anchor, Vector2 pos, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = sizeDelta;
        rt.anchoredPosition = pos;

        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.text = content;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        return t;
    }

    private static TextMeshProUGUI StretchText(string name, Transform parent, string content, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.text = content;
        t.color = color;
        t.fontSize = 16f;
        t.alignment = TextAlignmentOptions.Left;
        return t;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
