using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuSceneBuilder
{
    [MenuItem("Tools/Augmentra/Create Main Menu Scene")]
    public static void CreateMainMenuScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var cameraGO = new GameObject("Main Camera", typeof(Camera));
        cameraGO.tag = "MainCamera";
        var cam = cameraGO.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.orthographic = true;

        var eventSystemGO = new GameObject("EventSystem",
            typeof(UnityEngine.EventSystems.EventSystem),
            typeof(UnityEngine.EventSystems.StandaloneInputModule));

        var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        var titleGO = new GameObject("TitleText", typeof(TextMeshProUGUI));
        titleGO.transform.SetParent(canvasGO.transform, false);
        var titleText = titleGO.GetComponent<TextMeshProUGUI>();
        titleText.text = "AUGMENTRA";
        titleText.fontSize = 90;
        titleText.alignment = TextAlignmentOptions.Center;
        var titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.7f);
        titleRect.anchorMax = new Vector2(0.5f, 0.7f);
        titleRect.sizeDelta = new Vector2(800, 150);
        titleRect.anchoredPosition = Vector2.zero;

        var buttonGO = new GameObject("PlayButton", typeof(Image), typeof(Button));
        buttonGO.transform.SetParent(canvasGO.transform, false);
        var buttonRect = buttonGO.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.4f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.4f);
        buttonRect.sizeDelta = new Vector2(300, 80);
        buttonRect.anchoredPosition = Vector2.zero;

        var buttonTextGO = new GameObject("Text", typeof(TextMeshProUGUI));
        buttonTextGO.transform.SetParent(buttonGO.transform, false);
        var buttonText = buttonTextGO.GetComponent<TextMeshProUGUI>();
        buttonText.text = "PLAY";
        buttonText.fontSize = 36;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.color = Color.black;
        var buttonTextRect = buttonTextGO.GetComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.sizeDelta = Vector2.zero;

        var controllerGO = new GameObject("MainMenuController", typeof(MainMenuController));
        var controller = controllerGO.GetComponent<MainMenuController>();
        var button = buttonGO.GetComponent<Button>();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick, controller.OnPlayClicked);

        string scenePath = "Assets/Scenes/MainMenu.unity";
        EditorSceneManager.SaveScene(scene, scenePath);

        var existingScenes = EditorBuildSettings.scenes;
        bool alreadyInBuild = false;
        foreach (var s in existingScenes)
        {
            if (s.path == scenePath) alreadyInBuild = true;
        }

        if (!alreadyInBuild)
        {
            var newList = new System.Collections.Generic.List<EditorBuildSettingsScene>();
            newList.Add(new EditorBuildSettingsScene(scenePath, true));
            foreach (var s in existingScenes)
                newList.Add(s);
            EditorBuildSettings.scenes = newList.ToArray();
        }

        Debug.Log("MainMenu scene created and added to Build Settings at index 0.");
    }
}
