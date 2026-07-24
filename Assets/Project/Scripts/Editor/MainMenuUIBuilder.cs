using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using UnityEngine.SceneManagement;
using TMPro;

public static class MainMenuUIBuilder
{
    private const string FontPath = "Assets/Resources/Fonts/Nexa-ExtraLight SDF.asset";
    private const string ScenesFolder = "Assets/Scenes";
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string GameplayScenePath = "Assets/Scenes/SampleScene.unity";

    private static readonly Color PanelBg = new Color(0.02f, 0.02f, 0.02f, 1f);
    private static readonly Color ButtonGreen = new Color(0.102f, 0.478f, 0.290f, 1f);
    private static readonly Color SoftWhite = new Color(0.92f, 0.93f, 0.95f, 1f);
    private static readonly Color GoldColor = new Color(0.95f, 0.78f, 0.35f, 1f);

    [MenuItem("Tools/Augmentra/Build Main Menu Scene")]
    public static void Build()
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

        if (font == null)
        {
            EditorUtility.DisplayDialog("Build Main Menu Scene", "Could not find the Nexa Extra font asset at:\n" + FontPath, "OK");
            return;
        }

        Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        EnsureEventSystem();

        GameObject canvasGo = new GameObject("MainMenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject bg = NewUI("Background", canvasGo.transform);
        Stretch(bg.GetComponent<RectTransform>());
        AddImage(bg, PanelBg, null, false);

        TextMeshProUGUI title = AddText("Title", canvasGo.transform, font, "AUGMENTRA", 96f, FontStyles.Bold, GoldColor);
        SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(900f, 120f));

        GameObject playGo = NewUI("PlayButton", canvasGo.transform);
        SetRect(playGo.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(260f, 64f));
        AddImage(playGo, ButtonGreen, uiSprite, true);
        Button playButton = playGo.AddComponent<Button>();

        TextMeshProUGUI playLabel = AddText("Label", playGo.transform, font, "PLAY", 26f, FontStyles.Normal, Color.white);
        Stretch(playLabel.rectTransform);

        GameObject controllerGo = new GameObject("MainMenuController");
        MainMenuController controller = controllerGo.AddComponent<MainMenuController>();

        UnityEventTools.AddPersistentListener(playButton.onClick, controller.OnPlayClicked);

        Directory.CreateDirectory(ScenesFolder);
        EditorSceneManager.SaveScene(scene, ScenePath);

        AddScenesToBuildSettings();

        EditorUtility.DisplayDialog("Build Main Menu Scene",
            "MainMenu scene built at " + ScenePath + " and added to Build Settings at index 0.\n\nThe Play button loads '" + GameplayScenePath + "'.",
            "OK");
    }

    private static void AddScenesToBuildSettings()
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        scenes.RemoveAll(s => s.path == ScenePath || s.path == GameplayScenePath);
        scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));

        if (File.Exists(GameplayScenePath))
        {
            scenes.Add(new EditorBuildSettingsScene(GameplayScenePath, true));
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
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
