using System.IO;
using Augmentra.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SettingsAndPauseUISetup
{
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string GameplayScenePath = "Assets/Scenes/SampleScene.unity";
    private const string FontPath = "Assets/Resources/Fonts/Nexa-ExtraLight SDF.asset";
    private const string MainSettingsCanvasName = "__AugmentraSettingsUI";
    private const string MainActionsName = "__AugmentraMainMenuActions";
    private const string PauseCanvasName = "__AugmentraPauseUI";

    private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
    private static readonly Color Backdrop = Hex("#050608E8");
    private static readonly Color Panel = Hex("#0D0F14");
    private static readonly Color PanelBorder = Hex("#353945");
    private static readonly Color Control = Hex("#191C24");
    private static readonly Color ControlHover = Hex("#252A35");
    private static readonly Color ControlPressed = Hex("#101218");
    private static readonly Color Green = Hex("#1A7A4A");
    private static readonly Color Grey = Hex("#3B3E47");
    private static readonly Color GreyHover = Hex("#50545F");
    private static readonly Color Red = Hex("#8A2432");
    private static readonly Color Gold = Hex("#F2C759");
    private static readonly Color SoftWhite = Hex("#ECEFF4");
    private static readonly Color SoftGrey = Hex("#A7ADB8");

    private sealed class SettingsControls
    {
        public SettingsPanel panel;
        public Slider volume;
        public TextMeshProUGUI volumeValue;
        public Toggle fullscreen;
        public TMP_Dropdown resolution;
        public TMP_Dropdown quality;
        public Toggle vSync;
        public TMP_Dropdown fpsLimit;
        public Toggle screenShake;
        public Button apply;
        public Button reset;
        public Button back;
        public GameObject displayConfirmPanel;
        public TextMeshProUGUI displayConfirmCountdown;
        public Button displayConfirmKeep;
        public Button displayConfirmRevert;
        public Button settingsTab;
        public Button guideTab;
        public GameObject settingsPage;
        public GameObject guidePage;
        public GameObject unsavedChangesPanel;
        public Button unsavedChangesDiscard;
        public Button unsavedChangesKeepEditing;
    }

    [MenuItem("Tools/Augmentra/Setup Settings And Pause UI")]
    public static void Setup()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.Log("Augmentra settings/pause setup cancelled; open scene changes were not saved.");
            return;
        }

        string originalScenePath = SceneManager.GetActiveScene().path;

        try
        {
            SetupMainMenuScene();
            SetupGameplayScene();
        }
        finally
        {
            if (!string.IsNullOrEmpty(originalScenePath) && File.Exists(originalScenePath))
            {
                EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
            }

            EditorUtility.ClearProgressBar();
        }

        Debug.Log(
            "Augmentra settings and pause UI setup complete. MainMenu now has Settings/Quit, " +
            "and SampleScene has the Escape pause menu. The command is safe to run again; " +
            "it replaces only roots prefixed with '__Augmentra'.");
    }

    [MenuItem("Tools/Augmentra/Repair Main Menu UI")]
    public static void RepairMainMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.Log("Augmentra main-menu repair cancelled; open scene changes were not saved.");
            return;
        }

        string originalScenePath = SceneManager.GetActiveScene().path;

        try
        {
            SetupMainMenuScene();
        }
        finally
        {
            if (!string.IsNullOrEmpty(originalScenePath) && File.Exists(originalScenePath))
            {
                EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
            }
        }

        Debug.Log(
            "Augmentra main-menu repair complete. Play, Settings, and Quit Game now share " +
            "one centered VerticalLayoutGroup and have persistent plus runtime-safe actions.");
    }

    private static void SetupMainMenuScene()
    {
        if (!File.Exists(MainMenuScenePath))
        {
            Debug.LogError("Settings setup skipped: missing " + MainMenuScenePath);
            return;
        }

        EditorUtility.DisplayProgressBar("Augmentra UI Setup", "Updating MainMenu...", 0.25f);
        Scene scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        TMP_FontAsset font = LoadFont();
        EnsureEventSystem();

        DestroyOwnedRoot(MainSettingsCanvasName);
        DestroyOwnedRoot(MainActionsName);
        DestroyOwnedRoot("GameOverCanvas"); // stray leftover from Game Over UI once built into this scene

        Canvas existingCanvas = FindExistingMainMenuCanvas();

        if (existingCanvas == null)
        {
            Debug.LogError("No existing main-menu Canvas was found. MainMenu was not changed.");
            return;
        }

        ConfigureScaler(existingCanvas.GetComponent<CanvasScaler>());

        GameObject actions = NewUI(MainActionsName, existingCanvas.transform);
        RectTransform actionsRect = actions.GetComponent<RectTransform>();
        SetRect(
            actionsRect,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, -90f),
            new Vector2(340f, 0f));

        VerticalLayoutGroup layout = actions.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 16f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = actions.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Button playButton = FindExistingPlayButton(existingCanvas.transform);

        if (playButton == null)
        {
            playButton = AddMenuButton("PlayButton", actions.transform, font, "PLAY");
        }
        else
        {
            playButton.transform.SetParent(actions.transform, false);
            NormalizeMenuButton(playButton, font, "PLAY");
        }

        Button settingsButton = AddMenuButton(
            "SettingsButton", actions.transform, font, "SETTINGS");
        Button quitButton = AddMenuButton(
            "QuitButton", actions.transform, font, "QUIT GAME");

        GameObject settingsCanvasObject = CreateCanvas(MainSettingsCanvasName, 200);
        SettingsControls controls = BuildSettingsPanel(settingsCanvasObject.transform, font);

        GameObject quitConfirmPanel;
        Button quitConfirmYes;
        Button quitConfirmNo;
        BuildQuitConfirmPanel(
            settingsCanvasObject.transform, font, out quitConfirmPanel, out quitConfirmYes, out quitConfirmNo);

        MainMenuController controller = Object.FindFirstObjectByType<MainMenuController>();

        if (controller == null)
        {
            controller = new GameObject("MainMenuController").AddComponent<MainMenuController>();
        }

        controller.Configure(
            actions,
            controls.panel,
            playButton,
            settingsButton,
            quitButton,
            quitConfirmPanel,
            quitConfirmYes,
            quitConfirmNo);
        SetPersistentListener(playButton, controller, controller.OnPlayClicked);
        SetPersistentListener(settingsButton, controller, controller.OnSettingsClicked);
        SetPersistentListener(quitButton, controller, controller.OnQuitClicked);
        SetPersistentListener(quitConfirmYes, controller, controller.ConfirmQuit);
        SetPersistentListener(quitConfirmNo, controller, controller.CancelQuit);
        ConfigureMenuNavigation(playButton, settingsButton, quitButton);

        controls.panel.gameObject.SetActive(false);
        quitConfirmPanel.SetActive(false);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static Button FindExistingPlayButton(Transform mainCanvas)
    {
        Button[] buttons = mainCanvas.GetComponentsInChildren<Button>(true);

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].gameObject.name == "PlayButton")
            {
                return buttons[i];
            }
        }

        return null;
    }

    private static Button AddMenuButton(
        string name,
        Transform parent,
        TMP_FontAsset font,
        string label)
    {
        Button button = AddButton(
            name,
            parent,
            font,
            label,
            Grey,
            Vector2.zero,
            new Vector2(340f, 62f));
        AddMenuLayoutElement(button.gameObject);
        return button;
    }

    private static void NormalizeMenuButton(
        Button button,
        TMP_FontAsset font,
        string label)
    {
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(340f, 62f);

        Image image = button.GetComponent<Image>();

        if (image == null)
        {
            image = button.gameObject.AddComponent<Image>();
        }

        image.color = Grey;
        image.sprite = BuiltinSprite();
        image.type = Image.Type.Sliced;
        button.targetGraphic = image;
        UIColorUtility.ApplyConsistentColors(button, Grey);

        TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>(true);

        if (text == null)
        {
            text = AddText(
                "Label",
                button.transform,
                font,
                label,
                21f,
                FontStyles.Bold,
                SoftWhite);
            Stretch(text.rectTransform);
        }

        if (font != null)
        {
            text.font = font;
        }

        text.text = label;
        text.fontSize = 21f;
        text.fontStyle = FontStyles.Bold;
        text.color = SoftWhite;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;

        if (button.GetComponent<UIButtonFeedback>() == null)
        {
            button.gameObject.AddComponent<UIButtonFeedback>();
        }

        AddMenuLayoutElement(button.gameObject);
    }

    private static void AddMenuLayoutElement(GameObject buttonObject)
    {
        LayoutElement element = buttonObject.GetComponent<LayoutElement>();

        if (element == null)
        {
            element = buttonObject.AddComponent<LayoutElement>();
        }

        element.minWidth = 340f;
        element.preferredWidth = 340f;
        element.flexibleWidth = 0f;
        element.minHeight = 62f;
        element.preferredHeight = 62f;
        element.flexibleHeight = 0f;
    }

    private static void ConfigureMenuNavigation(
        Button play,
        Button settings,
        Button quit)
    {
        Navigation playNavigation = new Navigation
        {
            mode = Navigation.Mode.Explicit,
            selectOnUp = quit,
            selectOnDown = settings
        };
        play.navigation = playNavigation;

        Navigation settingsNavigation = new Navigation
        {
            mode = Navigation.Mode.Explicit,
            selectOnUp = play,
            selectOnDown = quit
        };
        settings.navigation = settingsNavigation;

        Navigation quitNavigation = new Navigation
        {
            mode = Navigation.Mode.Explicit,
            selectOnUp = settings,
            selectOnDown = play
        };
        quit.navigation = quitNavigation;
    }

    private static void SetPersistentListener(
        Button button,
        MainMenuController controller,
        UnityEngine.Events.UnityAction action)
    {
        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
        {
            UnityEventTools.RemovePersistentListener(button.onClick, i);
        }

        UnityEventTools.AddPersistentListener(button.onClick, action);
        EditorUtility.SetDirty(button);
        EditorUtility.SetDirty(controller);
    }

    private static void SetupGameplayScene()
    {
        if (!File.Exists(GameplayScenePath))
        {
            Debug.LogError("Pause setup skipped: missing " + GameplayScenePath);
            return;
        }

        EditorUtility.DisplayProgressBar("Augmentra UI Setup", "Updating SampleScene...", 0.7f);
        Scene scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);
        TMP_FontAsset font = LoadFont();
        EnsureEventSystem();

        DestroyOwnedRoot(PauseCanvasName);

        GameObject canvasObject = CreateCanvas(PauseCanvasName, 300);
        PauseMenu pauseMenu = canvasObject.AddComponent<PauseMenu>();

        GameObject pausePanel = NewUI("PausePanel", canvasObject.transform);
        Stretch(pausePanel.GetComponent<RectTransform>());
        AddImage(pausePanel, Backdrop, null, false);

        GameObject card = NewUI("PauseCard", pausePanel.transform);
        SetRect(
            card.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(520f, 720f));
        AddImage(card, Panel, BuiltinSprite(), true);
        AddOutline(card, PanelBorder);

        TextMeshProUGUI title = AddText(
            "Title",
            card.transform,
            font,
            "PAUSED",
            54f,
            FontStyles.Bold,
            Gold);
        SetRect(
            title.rectTransform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -62f),
            new Vector2(440f, 74f));

        Button resume = AddButton(
            "ResumeButton", card.transform, font, "RESUME", Green,
            new Vector2(0f, 150f), new Vector2(360f, 64f));
        Button settings = AddButton(
            "SettingsButton", card.transform, font, "SETTINGS", Grey,
            new Vector2(0f, 70f), new Vector2(360f, 64f));
        Button restart = AddButton(
            "RestartButton", card.transform, font, "RESTART RUN", Grey,
            new Vector2(0f, -10f), new Vector2(360f, 64f));
        Button mainMenu = AddButton(
            "MainMenuButton", card.transform, font, "MAIN MENU", Grey,
            new Vector2(0f, -90f), new Vector2(360f, 64f));
        Button quit = AddButton(
            "QuitButton", card.transform, font, "QUIT GAME", Grey,
            new Vector2(0f, -170f), new Vector2(360f, 64f));

        TextMeshProUGUI hint = AddText(
            "Hint",
            card.transform,
            font,
            "ESC  Resume",
            20f,
            FontStyles.Normal,
            SoftGrey);
        SetRect(
            hint.rectTransform,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 34f),
            new Vector2(400f, 36f));

        SettingsControls controls = BuildSettingsPanel(canvasObject.transform, font);

        GameObject quitConfirmPanel;
        Button quitConfirmYes;
        Button quitConfirmNo;
        BuildQuitConfirmPanel(
            canvasObject.transform, font, out quitConfirmPanel, out quitConfirmYes, out quitConfirmNo);

        pauseMenu.Configure(
            pausePanel,
            controls.panel,
            resume,
            settings,
            restart,
            mainMenu,
            quit,
            quitConfirmPanel,
            quitConfirmYes,
            quitConfirmNo,
            "MainMenu");

        pausePanel.SetActive(false);
        controls.panel.gameObject.SetActive(false);
        quitConfirmPanel.SetActive(false);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static SettingsControls BuildSettingsPanel(Transform parent, TMP_FontAsset font)
    {
        SettingsControls result = new SettingsControls();
        GameObject root = NewUI("SettingsPanel", parent);
        Stretch(root.GetComponent<RectTransform>());
        AddImage(root, Backdrop, null, false);

        GameObject card = NewUI("SettingsCard", root.transform);
        SetRect(
            card.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(940f, 900f));
        AddImage(card, Panel, BuiltinSprite(), true);
        AddOutline(card, PanelBorder);

        TextMeshProUGUI title = AddText(
            "Title", card.transform, font, "SETTINGS", 48f, FontStyles.Bold, Gold);
        SetRect(
            title.rectTransform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -35f),
            new Vector2(820f, 72f));

        result.settingsTab = AddButton(
            "SettingsTabButton", card.transform, font, "SETTINGS", Gold,
            new Vector2(-105f, 310f), new Vector2(190f, 46f));
        result.guideTab = AddButton(
            "GuideTabButton", card.transform, font, "GUIDE", Grey,
            new Vector2(105f, 310f), new Vector2(190f, 46f));

        // SettingsPanel drives these two buttons' colors directly every time the
        // active tab changes; UIButtonFeedback's separate select/deselect tint
        // (cached at Awake, hardcoded gold) fights that and produces stale,
        // mismatched colors, so it's removed for the tabs specifically.
        RemoveButtonFeedback(result.settingsTab);
        RemoveButtonFeedback(result.guideTab);

        GameObject settingsPage = NewUI("SettingsPage", card.transform);
        Stretch(settingsPage.GetComponent<RectTransform>());
        result.settingsPage = settingsPage;

        GameObject guidePage = NewUI("GuidePage", card.transform);
        Stretch(guidePage.GetComponent<RectTransform>());
        result.guidePage = guidePage;

        float firstRowY = -215f;
        float rowSpacing = 78f;
        result.volume = AddSliderRow(
            settingsPage.transform, font, "MASTER VOLUME", firstRowY, out result.volumeValue);
        result.fullscreen = AddToggleRow(
            settingsPage.transform, font, "FULLSCREEN", firstRowY - rowSpacing);
        result.resolution = AddDropdownRow(
            settingsPage.transform, font, "RESOLUTION", firstRowY - rowSpacing * 2f);
        result.quality = AddDropdownRow(
            settingsPage.transform, font, "QUALITY", firstRowY - rowSpacing * 3f);
        result.vSync = AddToggleRow(
            settingsPage.transform, font, "VSYNC", firstRowY - rowSpacing * 4f);
        result.fpsLimit = AddDropdownRow(
            settingsPage.transform, font, "FPS LIMIT", firstRowY - rowSpacing * 5f);
        result.screenShake = AddToggleRow(
            settingsPage.transform, font, "SCREEN SHAKE", firstRowY - rowSpacing * 6f);

        result.back = AddButton(
            "BackButton", card.transform, font, "BACK", Grey,
            new Vector2(-280f, -385f), new Vector2(220f, 60f));
        result.reset = AddButton(
            "ResetButton", settingsPage.transform, font, "RESET DEFAULTS", Grey,
            new Vector2(0f, -385f), new Vector2(280f, 60f));
        result.apply = AddButton(
            "ApplyButton", settingsPage.transform, font, "APPLY", Green,
            new Vector2(280f, -385f), new Vector2(220f, 60f));

        BuildGuidePage(guidePage.transform, font);

        BuildDisplayConfirmPanel(
            root.transform,
            font,
            out result.displayConfirmPanel,
            out result.displayConfirmCountdown,
            out result.displayConfirmKeep,
            out result.displayConfirmRevert);

        BuildUnsavedChangesPanel(
            root.transform,
            font,
            out result.unsavedChangesPanel,
            out result.unsavedChangesDiscard,
            out result.unsavedChangesKeepEditing);

        SettingsPanel settingsPanel = root.AddComponent<SettingsPanel>();
        settingsPanel.Configure(
            root,
            result.volume,
            result.volumeValue,
            result.fullscreen,
            result.resolution,
            result.quality,
            result.vSync,
            result.fpsLimit,
            result.screenShake,
            result.apply,
            result.reset,
            result.back,
            result.displayConfirmPanel,
            result.displayConfirmCountdown,
            result.displayConfirmKeep,
            result.displayConfirmRevert,
            result.settingsTab,
            result.guideTab,
            result.settingsPage,
            result.guidePage,
            result.unsavedChangesPanel,
            result.unsavedChangesDiscard,
            result.unsavedChangesKeepEditing);
        UnityEventTools.AddPersistentListener(result.back.onClick, settingsPanel.Close);
        result.panel = settingsPanel;
        return result;
    }

    private static void RemoveButtonFeedback(Button button)
    {
        UIButtonFeedback feedback = button.GetComponent<UIButtonFeedback>();

        if (feedback != null)
        {
            Object.DestroyImmediate(feedback);
        }
    }

    private static void BuildGuidePage(Transform parent, TMP_FontAsset font)
    {
        GameObject scrollRoot = NewUI("GuideScroll", parent);
        SetRect(
            scrollRoot.GetComponent<RectTransform>(),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -170f),
            new Vector2(-80f, 580f));

        GameObject viewport = NewUI("Viewport", scrollRoot.transform);
        Stretch(viewport.GetComponent<RectTransform>());
        Image viewportImage = AddImage(viewport, Color.white, null, false);
        viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
        Mask mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        GameObject content = NewUI("Content", viewport.transform);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 20f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.padding = new RectOffset(4, 4, 4, 4);

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scrollRect = scrollRoot.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.viewport = viewport.GetComponent<RectTransform>();
        scrollRect.content = contentRect;

        AddGuideSection(content.transform, font, "MOVEMENT", "WASD to move");
        AddGuideSection(content.transform, font, "AIMING", "Mouse to aim");
        AddGuideSection(content.transform, font, "SHOOTING", "Left Click to shoot");
    }

    private static void AddGuideSection(Transform parent, TMP_FontAsset font, string header, string body)
    {
        GameObject section = NewUI(header.Replace(" ", string.Empty) + "Section", parent);
        VerticalLayoutGroup layout = section.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 4f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = section.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TextMeshProUGUI headerText = AddText(
            "Header", section.transform, font, header, 22f, FontStyles.Bold, Gold);
        headerText.alignment = TextAlignmentOptions.Left;
        LayoutElement headerElement = headerText.gameObject.AddComponent<LayoutElement>();
        headerElement.preferredHeight = 30f;

        TextMeshProUGUI bodyText = AddText(
            "Body", section.transform, font, body, 20f, FontStyles.Normal, SoftWhite);
        bodyText.alignment = TextAlignmentOptions.Left;
        LayoutElement bodyElement = bodyText.gameObject.AddComponent<LayoutElement>();
        bodyElement.preferredHeight = 28f;
    }

    private static void BuildDisplayConfirmPanel(
        Transform parent,
        TMP_FontAsset font,
        out GameObject panel,
        out TextMeshProUGUI countdownText,
        out Button keepButton,
        out Button revertButton)
    {
        GameObject root = NewUI("DisplayConfirmPanel", parent);
        Stretch(root.GetComponent<RectTransform>());
        AddImage(root, Backdrop, null, false);
        panel = root;

        GameObject card = NewUI("DisplayConfirmCard", root.transform);
        SetRect(
            card.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(560f, 240f));
        AddImage(card, Panel, BuiltinSprite(), true);
        AddOutline(card, PanelBorder);

        countdownText = AddText(
            "CountdownText",
            card.transform,
            font,
            "Keep these display settings? Reverting in 15s",
            24f,
            FontStyles.Bold,
            SoftWhite);
        SetRect(
            countdownText.rectTransform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -75f),
            new Vector2(500f, 100f));
        countdownText.textWrappingMode = TextWrappingModes.Normal;

        revertButton = AddButton(
            "RevertButton", card.transform, font, "REVERT", Red,
            new Vector2(-140f, -80f), new Vector2(200f, 60f));
        keepButton = AddButton(
            "KeepButton", card.transform, font, "KEEP", Green,
            new Vector2(140f, -80f), new Vector2(200f, 60f));
    }

    private static void BuildQuitConfirmPanel(
        Transform parent,
        TMP_FontAsset font,
        out GameObject panel,
        out Button yesButton,
        out Button noButton)
    {
        GameObject root = NewUI("QuitConfirmPanel", parent);
        Stretch(root.GetComponent<RectTransform>());
        AddImage(root, Backdrop, null, false);
        panel = root;

        GameObject card = NewUI("QuitConfirmCard", root.transform);
        SetRect(
            card.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(460f, 220f));
        AddImage(card, Panel, BuiltinSprite(), true);
        AddOutline(card, PanelBorder);

        TextMeshProUGUI message = AddText(
            "Message", card.transform, font, "Quit to desktop?", 26f, FontStyles.Bold, SoftWhite);
        SetRect(
            message.rectTransform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -60f),
            new Vector2(400f, 60f));

        noButton = AddButton(
            "NoButton", card.transform, font, "NO", Grey,
            new Vector2(-110f, -70f), new Vector2(180f, 60f));
        yesButton = AddButton(
            "YesButton", card.transform, font, "YES", Red,
            new Vector2(110f, -70f), new Vector2(180f, 60f));
    }

    private static void BuildUnsavedChangesPanel(
        Transform parent,
        TMP_FontAsset font,
        out GameObject panel,
        out Button discardButton,
        out Button keepEditingButton)
    {
        GameObject root = NewUI("UnsavedChangesPanel", parent);
        Stretch(root.GetComponent<RectTransform>());
        AddImage(root, Backdrop, null, false);
        panel = root;

        GameObject card = NewUI("UnsavedChangesCard", root.transform);
        SetRect(
            card.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(460f, 260f));
        AddImage(card, Panel, BuiltinSprite(), true);
        AddOutline(card, PanelBorder);

        TextMeshProUGUI message = AddText(
            "Message", card.transform, font, "You have unsaved changes. Discard them?",
            24f, FontStyles.Bold, SoftWhite);
        SetRect(
            message.rectTransform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -70f),
            new Vector2(400f, 100f));
        message.textWrappingMode = TextWrappingModes.Normal;
        message.alignment = TextAlignmentOptions.Center;

        keepEditingButton = AddButton(
            "KeepEditingButton", card.transform, font, "KEEP EDITING", Grey,
            new Vector2(-110f, -90f), new Vector2(180f, 60f));
        discardButton = AddButton(
            "DiscardButton", card.transform, font, "DISCARD", Red,
            new Vector2(110f, -90f), new Vector2(180f, 60f));
    }

    private static Slider AddSliderRow(
        Transform parent,
        TMP_FontAsset font,
        string label,
        float y,
        out TextMeshProUGUI valueText)
    {
        AddRowLabel(parent, font, label, y);

        GameObject sliderObject = NewUI("MasterVolumeSlider", parent);
        SetRect(
            sliderObject.GetComponent<RectTransform>(),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(165f, y),
            new Vector2(400f, 38f));

        GameObject background = NewUI("Background", sliderObject.transform);
        SetRect(
            background.GetComponent<RectTransform>(),
            new Vector2(0f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(0f, 12f));
        AddImage(background, Control, BuiltinSprite(), true);

        GameObject fillArea = NewUI("Fill Area", sliderObject.transform);
        StretchWithOffsets(fillArea.GetComponent<RectTransform>(), 8f, 8f, 8f, 8f);
        GameObject fill = NewUI("Fill", fillArea.transform);
        Stretch(fill.GetComponent<RectTransform>());
        AddImage(fill, Green, BuiltinSprite(), true);

        GameObject handleArea = NewUI("Handle Slide Area", sliderObject.transform);
        StretchWithOffsets(handleArea.GetComponent<RectTransform>(), 10f, 10f, 10f, 10f);
        GameObject handle = NewUI("Handle", handleArea.transform);
        SetRect(
            handle.GetComponent<RectTransform>(),
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(28f, 28f));
        Image handleImage = AddImage(handle, SoftWhite, BuiltinSprite(), true);

        Slider slider = sliderObject.AddComponent<Slider>();
        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.handleRect = handle.GetComponent<RectTransform>();
        slider.targetGraphic = handleImage;
        slider.direction = Slider.Direction.LeftToRight;

        valueText = AddText(
            "VolumeValue", parent, font, "100%", 22f, FontStyles.Bold, SoftWhite);
        SetRect(
            valueText.rectTransform,
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-46f, y),
            new Vector2(90f, 38f));
        valueText.alignment = TextAlignmentOptions.Right;
        return slider;
    }

    private static Toggle AddToggleRow(
        Transform parent,
        TMP_FontAsset font,
        string label,
        float y)
    {
        AddRowLabel(parent, font, label, y);

        GameObject toggleObject = NewUI(label.Replace(" ", string.Empty) + "Toggle", parent);
        SetRect(
            toggleObject.GetComponent<RectTransform>(),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-80f, y),
            new Vector2(76f, 40f));
        Image background = AddImage(toggleObject, Control, BuiltinSprite(), true);

        GameObject checkObject = NewUI("Checkmark", toggleObject.transform);
        StretchWithOffsets(checkObject.GetComponent<RectTransform>(), 5f, 5f, 5f, 5f);
        Image checkmark = AddImage(checkObject, Green, BuiltinSprite(), true);

        Toggle toggle = toggleObject.AddComponent<Toggle>();
        toggle.targetGraphic = background;
        toggle.graphic = checkmark;
        SetSelectableColors(toggle, Control, ControlHover, ControlPressed);
        return toggle;
    }

    private static TMP_Dropdown AddDropdownRow(
        Transform parent,
        TMP_FontAsset font,
        string label,
        float y)
    {
        AddRowLabel(parent, font, label, y);

        GameObject root = NewUI(label.Replace(" ", string.Empty) + "Dropdown", parent);
        SetRect(
            root.GetComponent<RectTransform>(),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-46f, y),
            new Vector2(470f, 48f));
        Image rootImage = AddImage(root, Control, BuiltinSprite(), true);

        TextMeshProUGUI caption = AddText(
            "Label", root.transform, font, string.Empty, 20f, FontStyles.Normal, SoftWhite);
        StretchWithOffsets(caption.rectTransform, 16f, 50f, 4f, 4f);
        caption.alignment = TextAlignmentOptions.Left;

        TextMeshProUGUI arrow = AddText(
            "Arrow", root.transform, font, "▼", 17f, FontStyles.Normal, Gold);
        SetRect(
            arrow.rectTransform,
            new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(-14f, 0f),
            new Vector2(30f, 30f));

        GameObject template = NewUI("Template", root.transform);
        SetRect(
            template.GetComponent<RectTransform>(),
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -4f),
            new Vector2(0f, 230f));
        AddImage(template, Panel, BuiltinSprite(), true);
        ScrollRect scrollRect = template.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;

        GameObject viewport = NewUI("Viewport", template.transform);
        StretchWithOffsets(viewport.GetComponent<RectTransform>(), 5f, 5f, 5f, 5f);
        Image viewportImage = AddImage(viewport, Color.white, null, false);
        viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
        Mask mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        GameObject content = NewUI("Content", viewport.transform);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 34f);

        GameObject item = NewUI("Item", content.transform);
        RectTransform itemRect = item.GetComponent<RectTransform>();
        itemRect.anchorMin = new Vector2(0f, 0.5f);
        itemRect.anchorMax = new Vector2(1f, 0.5f);
        itemRect.pivot = new Vector2(0.5f, 0.5f);
        itemRect.anchoredPosition = Vector2.zero;
        itemRect.sizeDelta = new Vector2(0f, 34f);
        Image itemBackground = AddImage(item, Control, null, false);

        TextMeshProUGUI checkmark = AddText(
            "Item Checkmark", item.transform, font, "●", 14f, FontStyles.Normal, Gold);
        SetRect(
            checkmark.rectTransform,
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(10f, 0f),
            new Vector2(24f, 28f));

        TextMeshProUGUI itemLabel = AddText(
            "Item Label", item.transform, font, "Option", 18f, FontStyles.Normal, SoftWhite);
        StretchWithOffsets(itemLabel.rectTransform, 38f, 8f, 2f, 2f);
        itemLabel.alignment = TextAlignmentOptions.Left;

        Toggle itemToggle = item.AddComponent<Toggle>();
        itemToggle.targetGraphic = itemBackground;
        itemToggle.graphic = checkmark;
        SetSelectableColors(itemToggle, Control, ControlHover, ControlPressed);

        scrollRect.viewport = viewport.GetComponent<RectTransform>();
        scrollRect.content = contentRect;

        TMP_Dropdown dropdown = root.AddComponent<TMP_Dropdown>();
        dropdown.targetGraphic = rootImage;
        dropdown.template = template.GetComponent<RectTransform>();
        dropdown.captionText = caption;
        dropdown.itemText = itemLabel;
        SetSelectableColors(dropdown, Control, ControlHover, ControlPressed);
        template.SetActive(false);
        return dropdown;
    }

    private static TextMeshProUGUI AddRowLabel(
        Transform parent,
        TMP_FontAsset font,
        string label,
        float y)
    {
        TextMeshProUGUI text = AddText(
            label.Replace(" ", string.Empty) + "Label",
            parent,
            font,
            label,
            21f,
            FontStyles.Normal,
            SoftGrey);
        SetRect(
            text.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(46f, y),
            new Vector2(310f, 48f));
        text.alignment = TextAlignmentOptions.Left;
        return text;
    }

    private static Button AddButton(
        string name,
        Transform parent,
        TMP_FontAsset font,
        string label,
        Color color,
        Vector2 position,
        Vector2 size)
    {
        GameObject buttonObject = NewUI(name, parent);
        SetRect(
            buttonObject.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            position,
            size);
        Image image = AddImage(buttonObject, color, BuiltinSprite(), true);
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        UIColorUtility.ApplyConsistentColors(button, color);
        buttonObject.AddComponent<UIButtonFeedback>();

        TextMeshProUGUI buttonLabel = AddText(
            "Label", buttonObject.transform, font, label, 21f, FontStyles.Bold, SoftWhite);
        Stretch(buttonLabel.rectTransform);
        return button;
    }

    private static GameObject CreateCanvas(string name, int sortingOrder)
    {
        GameObject canvasObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        ConfigureScaler(canvasObject.GetComponent<CanvasScaler>());
        return canvasObject;
    }

    private static void ConfigureScaler(CanvasScaler scaler)
    {
        if (scaler == null)
        {
            return;
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }

    private static Canvas FindExistingMainMenuCanvas()
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);

        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i].gameObject.name != MainSettingsCanvasName)
            {
                return canvases[i];
            }
        }

        return null;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }

    private static void DestroyOwnedRoot(string name)
    {
        GameObject existing = GameObject.Find(name);

        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }
    }

    private static TMP_FontAsset LoadFont()
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

        if (font == null)
        {
            Debug.LogWarning(
                "Nexa font was not found at " + FontPath +
                ". TextMeshPro will use its default font.");
        }

        return font;
    }

    private static GameObject NewUI(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));

        if (parent != null)
        {
            gameObject.transform.SetParent(parent, false);
        }

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

        if (font != null)
        {
            text.font = font;
        }

        text.text = content;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }

    private static void AddOutline(GameObject target, Color color)
    {
        Outline outline = target.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(2f, -2f);
    }

    private static void SetSelectableColors(
        Selectable selectable,
        Color normal,
        Color highlighted,
        Color pressed)
    {
        ColorBlock colors = selectable.colors;
        colors.normalColor = normal;
        colors.highlightedColor = highlighted;
        colors.selectedColor = highlighted;
        colors.pressedColor = pressed;
        colors.disabledColor = new Color(normal.r, normal.g, normal.b, 0.45f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        selectable.colors = colors;
        selectable.transition = Selectable.Transition.ColorTint;
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
