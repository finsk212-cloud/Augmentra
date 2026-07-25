using Augmentra.UI;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class GameOverUIBuilder
{
    private const string FontPath = "Assets/Resources/Fonts/Nexa-ExtraLight SDF.asset";

    private static readonly Color PanelBg = new Color(0.02f, 0.02f, 0.02f, 0.9f);
    private static readonly Color ButtonGreen = new Color(0.102f, 0.478f, 0.290f, 1f);
    private static readonly Color ButtonGrey = new Color(0.3f, 0.3f, 0.32f, 1f);
    private static readonly Color SoftWhite = new Color(0.92f, 0.93f, 0.95f, 1f);
    private static readonly Color HpCrimson = new Color(0.6f, 0.08f, 0.12f, 1f);

    [MenuItem("Tools/Augmentra/Build Game Over UI")]
    public static void Build()
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

        if (font == null)
        {
            EditorUtility.DisplayDialog("Build Game Over UI", "Could not find the Nexa Extra font asset at:\n" + FontPath, "OK");
            return;
        }

        Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        GameObject oldCanvas = GameObject.Find("GameOverCanvas");
        if (oldCanvas != null)
        {
            Object.DestroyImmediate(oldCanvas);
        }

        GameObject oldManager = GameObject.Find("GameOverUI");
        if (oldManager != null)
        {
            Object.DestroyImmediate(oldManager);
        }

        EnsureEventSystem();

        GameObject canvasGo = new GameObject("GameOverCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject managerGo = new GameObject("GameOverUI");
        GameOverUI gameOverUI = managerGo.AddComponent<GameOverUI>();

        GameObject panel = NewUI("Panel", canvasGo.transform);
        Stretch(panel.GetComponent<RectTransform>());
        AddImage(panel, PanelBg, null, false);
        CanvasGroup panelGroup = panel.AddComponent<CanvasGroup>();
        panelGroup.alpha = 0f;
        panelGroup.blocksRaycasts = false;
        panelGroup.interactable = false;

        TextMeshProUGUI title = AddText("Title", panel.transform, font, "GAME OVER", 64f, FontStyles.Bold, HpCrimson);
        SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 160f), new Vector2(700f, 80f));

        TextMeshProUGUI stats = AddText("StatsText", panel.transform, font, "Kills: 0\nGold: 0\nLevel: 1", 28f, FontStyles.Normal, SoftWhite);
        SetRect(stats.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(500f, 140f));

        Button restartButton = AddButton("RestartButton", panel.transform, font, "RESTART", ButtonGreen, uiSprite, new Vector2(0f, -100f));
        Button mainMenuButton = AddButton("MainMenuButton", panel.transform, font, "MAIN MENU", ButtonGrey, uiSprite, new Vector2(0f, -170f));

        gameOverUI.group = panelGroup;
        gameOverUI.statsText = stats;
        gameOverUI.restartButton = restartButton;
        gameOverUI.mainMenuButton = mainMenuButton;

        EditorUtility.SetDirty(gameOverUI);
        EditorSceneManager.MarkSceneDirty(canvasGo.scene);
        Selection.activeGameObject = managerGo;

        EditorUtility.DisplayDialog("Build Game Over UI",
            "Game Over UI built and wired.\n\nGameOverUI added with panel, stats text and Restart/Main Menu buttons assigned.\n\nSave the scene (Ctrl+S) to keep it.",
            "OK");
    }

    private static Button AddButton(string name, Transform parent, TMP_FontAsset font, string label, Color color, Sprite uiSprite, Vector2 pos)
    {
        GameObject go = NewUI(name, parent);
        SetRect(go.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(260f, 56f));
        AddImage(go, color, uiSprite, true);
        Button button = go.AddComponent<Button>();

        TextMeshProUGUI text = AddText("Label", go.transform, font, label, 20f, FontStyles.Normal, Color.white);
        Stretch(text.rectTransform);

        return button;
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
