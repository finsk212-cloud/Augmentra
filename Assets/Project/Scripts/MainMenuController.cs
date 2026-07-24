using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Augmentra.Settings;
using Augmentra.UI;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private string gameplaySceneName = "SampleScene";
    [SerializeField] private GameObject mainMenuContent;
    [SerializeField] private SettingsPanel settingsPanel;
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;

    private void Awake()
    {
        _ = SettingsManager.Instance;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        RegisterRuntimeFallback(playButton, OnPlayClicked, nameof(OnPlayClicked));
        RegisterRuntimeFallback(settingsButton, OnSettingsClicked, nameof(OnSettingsClicked));
        RegisterRuntimeFallback(quitButton, OnQuitClicked, nameof(OnQuitClicked));
    }

    private void Start()
    {
        ValidateReferences();

        if (EventSystem.current != null && playButton != null)
        {
            EventSystem.current.SetSelectedGameObject(playButton.gameObject);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) &&
            settingsPanel != null &&
            settingsPanel.IsOpen)
        {
            settingsPanel.Close();
        }
    }

    public void Configure(
        GameObject content,
        SettingsPanel panel,
        Button play,
        Button settings,
        Button quit)
    {
        mainMenuContent = content;
        settingsPanel = panel;
        playButton = play;
        settingsButton = settings;
        quitButton = quit;
    }

    public void OnPlayClicked()
    {
        if (!Application.CanStreamedLevelBeLoaded(gameplaySceneName))
        {
            Debug.LogError(
                "Cannot start the game. '" + gameplaySceneName +
                "' is missing from Build Settings.",
                this);
            return;
        }

        SceneManager.LoadScene(gameplaySceneName);
    }

    public void OnSettingsClicked()
    {
        if (settingsPanel == null)
        {
            Debug.LogError(
                "MainMenuController cannot open Settings because its SettingsPanel reference is missing. " +
                "Run Tools/Augmentra/Repair Main Menu UI.",
                this);
            return;
        }

        if (mainMenuContent != null)
        {
            mainMenuContent.SetActive(false);
        }

        settingsPanel.Open(() =>
        {
            if (mainMenuContent != null)
            {
                mainMenuContent.SetActive(true);
            }

            if (EventSystem.current != null && settingsButton != null)
            {
                EventSystem.current.SetSelectedGameObject(settingsButton.gameObject);
            }
        });
    }

    public void OnQuitClicked()
    {
#if UNITY_EDITOR
        Debug.Log("Quit Game requested. Application.Quit is ignored in the Unity Editor.", this);
#else
        Application.Quit();
#endif
    }

    private void ValidateReferences()
    {
        bool valid = true;

        if (mainMenuContent == null)
        {
            Debug.LogError("MainMenuController is missing its button-container reference.", this);
            valid = false;
        }

        if (settingsPanel == null)
        {
            Debug.LogError("MainMenuController is missing its SettingsPanel reference.", this);
            valid = false;
        }

        if (playButton == null || settingsButton == null || quitButton == null)
        {
            Debug.LogError(
                "MainMenuController is missing one or more menu-button references. " +
                "Run Tools/Augmentra/Repair Main Menu UI.",
                this);
            valid = false;
        }

        if (!valid)
        {
            Debug.LogError(
                "Main-menu setup is incomplete. The UI remains enabled for inspection, " +
                "but one or more actions may be unavailable.",
                this);
        }
    }

    private void RegisterRuntimeFallback(
        Button button,
        UnityEngine.Events.UnityAction action,
        string methodName)
    {
        if (button == null || HasPersistentListener(button, methodName))
        {
            return;
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private bool HasPersistentListener(Button button, string methodName)
    {
        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
        {
            if (button.onClick.GetPersistentTarget(i) == this &&
                button.onClick.GetPersistentMethodName(i) == methodName)
            {
                return true;
            }
        }

        return false;
    }
}
