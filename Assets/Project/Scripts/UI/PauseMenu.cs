using Augmentra.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Augmentra.UI
{
    public sealed class PauseMenu : MonoBehaviour
    {
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private SettingsPanel settingsPanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private GameObject quitConfirmPanel;
        [SerializeField] private Button quitConfirmYesButton;
        [SerializeField] private Button quitConfirmNoButton;
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private static PauseMenu instance;
        private bool isPaused;
        private CursorLockMode previousCursorLockMode;
        private bool previousCursorVisible;

        public static bool IsGamePaused => instance != null && instance.isPaused;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogWarning("Duplicate PauseMenu disabled.", this);
                enabled = false;
                return;
            }

            instance = this;
            SettingsManager settings = SettingsManager.Instance;

            if (pausePanel != null)
            {
                pausePanel.SetActive(false);
            }

            if (quitConfirmPanel != null)
            {
                quitConfirmPanel.SetActive(false);
            }

            RegisterButtonListeners();
        }

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape))
            {
                return;
            }

            if (settingsPanel != null && settingsPanel.IsUnsavedChangesPromptOpen)
            {
                settingsPanel.KeepEditing();
                return;
            }

            if (quitConfirmPanel != null && quitConfirmPanel.activeSelf)
            {
                CancelQuit();
                return;
            }

            if (settingsPanel != null && settingsPanel.IsOpen)
            {
                settingsPanel.Close();
                return;
            }

            if (isPaused)
            {
                Resume();
            }
            else if (CanPause())
            {
                Pause();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                return; // don't auto-resume - let the player manually resume
                        // when they're ready, same as returning from Settings
            }

            if (CanPause())
            {
                Pause();
            }
        }

        private void OnDestroy()
        {
            if (instance != this)
            {
                return;
            }

            if (isPaused)
            {
                RestoreGameplayState();
            }

            instance = null;
        }

        public void Configure(
            GameObject panel,
            SettingsPanel settings,
            Button resume,
            Button settingsOpen,
            Button restart,
            Button mainMenu,
            Button quit,
            GameObject quitConfirm,
            Button quitConfirmYes,
            Button quitConfirmNo,
            string menuSceneName)
        {
            pausePanel = panel;
            settingsPanel = settings;
            resumeButton = resume;
            settingsButton = settingsOpen;
            restartButton = restart;
            mainMenuButton = mainMenu;
            quitButton = quit;
            quitConfirmPanel = quitConfirm;
            quitConfirmYesButton = quitConfirmYes;
            quitConfirmNoButton = quitConfirmNo;
            mainMenuSceneName = menuSceneName;
        }

        public void Pause()
        {
            if (isPaused || pausePanel == null)
            {
                return;
            }

            previousCursorLockMode = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            isPaused = true;
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            pausePanel.SetActive(true);
        }

        public void Resume()
        {
            if (!isPaused)
            {
                return;
            }

            if (settingsPanel != null && settingsPanel.IsOpen)
            {
                settingsPanel.Close();
            }

            if (pausePanel != null)
            {
                pausePanel.SetActive(false);
            }

            isPaused = false;
            RestoreGameplayState();
        }

        public void OpenSettings()
        {
            if (!isPaused || settingsPanel == null)
            {
                return;
            }

            pausePanel.SetActive(false);
            settingsPanel.Open(() =>
            {
                if (isPaused && pausePanel != null)
                {
                    pausePanel.SetActive(true);
                }
            });
        }

        public void RestartRun()
        {
            string sceneName = SceneManager.GetActiveScene().name;

            if (string.IsNullOrWhiteSpace(sceneName) ||
                !Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError("Cannot restart: the active gameplay scene is not in Build Settings.", this);
                return;
            }

            PrepareForSceneChange();
            SceneTransition.Instance.LoadScene(sceneName);
        }

        public void LoadMainMenu()
        {
            if (string.IsNullOrWhiteSpace(mainMenuSceneName) ||
                !Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
            {
                Debug.LogError(
                    "Cannot load the main menu. Check the PauseMenu scene name and Build Settings.",
                    this);
                return;
            }

            PrepareForSceneChange();
            SceneTransition.Instance.LoadScene(mainMenuSceneName);
        }

        public void QuitGame()
        {
            if (quitConfirmPanel != null)
            {
                quitConfirmPanel.SetActive(true);
            }
            else
            {
                // Fallback if no confirmation panel is wired up - quit directly
                // rather than silently doing nothing.
                ActuallyQuit();
            }
        }

        private void ActuallyQuit()
        {
#if UNITY_EDITOR
            Debug.Log("Quit Game requested. Application.Quit is ignored in the Unity Editor.", this);
#else
            Application.Quit();
#endif
        }

        public void ConfirmQuit()
        {
            ActuallyQuit();
        }

        public void CancelQuit()
        {
            if (quitConfirmPanel != null)
            {
                quitConfirmPanel.SetActive(false);
            }
        }

        private void RegisterButtonListeners()
        {
            resumeButton?.onClick.AddListener(Resume);
            settingsButton?.onClick.AddListener(OpenSettings);
            restartButton?.onClick.AddListener(RestartRun);
            mainMenuButton?.onClick.AddListener(LoadMainMenu);
            quitButton?.onClick.AddListener(QuitGame);
            quitConfirmYesButton?.onClick.AddListener(ConfirmQuit);
            quitConfirmNoButton?.onClick.AddListener(CancelQuit);
        }

        private bool CanPause()
        {
            if (GameManager.Instance != null &&
                GameManager.Instance.CurrentState == GameManager.GameState.GameOver)
            {
                return false;
            }

            return Time.timeScale > 0f;
        }

        private void PrepareForSceneChange()
        {
            isPaused = false;
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void RestoreGameplayState()
        {
            Time.timeScale = 1f;
            Cursor.lockState = previousCursorLockMode;
            Cursor.visible = previousCursorVisible;
        }
    }
}
