using System;
using System.Collections.Generic;
using Augmentra.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Augmentra.UI
{
    public sealed class SettingsPanel : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject panelRoot;

        [Header("Controls")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private TextMeshProUGUI masterVolumeValue;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private TMP_Dropdown qualityDropdown;
        [SerializeField] private Toggle vSyncToggle;
        [SerializeField] private TMP_Dropdown fpsLimitDropdown;
        [SerializeField] private Toggle screenShakeToggle;

        [Header("Actions")]
        [SerializeField] private Button applyButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button backButton;

        private GameSettings workingCopy;
        private Action onClosed;
        private bool initialized;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        private void Awake()
        {
            Initialize();
        }

        public void Configure(
            GameObject root,
            Slider volume,
            TextMeshProUGUI volumeValue,
            Toggle fullscreen,
            TMP_Dropdown resolution,
            TMP_Dropdown quality,
            Toggle vSync,
            TMP_Dropdown fpsLimit,
            Toggle screenShake,
            Button apply,
            Button reset,
            Button back)
        {
            panelRoot = root;
            masterVolumeSlider = volume;
            masterVolumeValue = volumeValue;
            fullscreenToggle = fullscreen;
            resolutionDropdown = resolution;
            qualityDropdown = quality;
            vSyncToggle = vSync;
            fpsLimitDropdown = fpsLimit;
            screenShakeToggle = screenShake;
            applyButton = apply;
            resetButton = reset;
            backButton = back;
        }

        public void Open(Action closedCallback = null)
        {
            onClosed = closedCallback;

            if (panelRoot == null)
            {
                Debug.LogWarning("SettingsPanel has no panel root assigned.", this);
                return;
            }

            panelRoot.SetActive(true);
            Initialize();
            workingCopy = SettingsManager.Instance.Current.Copy();
            RefreshControls();

            if (EventSystem.current != null && masterVolumeSlider != null)
            {
                EventSystem.current.SetSelectedGameObject(masterVolumeSlider.gameObject);
            }
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            panelRoot.SetActive(false);
            Action callback = onClosed;
            onClosed = null;
            callback?.Invoke();
        }

        private void Initialize()
        {
            if (initialized)
            {
                return;
            }

            if (!ReferencesAreValid())
            {
                Debug.LogWarning(
                    "SettingsPanel is missing one or more UI references. " +
                    "Run Tools/Augmentra/Setup Settings And Pause UI.",
                    this);
                return;
            }

            initialized = true;
            masterVolumeSlider.minValue = 0f;
            masterVolumeSlider.maxValue = 100f;
            masterVolumeSlider.wholeNumbers = true;

            PopulateDropdowns();

            masterVolumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            fullscreenToggle.onValueChanged.AddListener(value => workingCopy.Fullscreen = value);
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            qualityDropdown.onValueChanged.AddListener(value => workingCopy.QualityLevel = value);
            vSyncToggle.onValueChanged.AddListener(value => workingCopy.VSync = value);
            fpsLimitDropdown.onValueChanged.AddListener(OnFpsLimitChanged);
            screenShakeToggle.onValueChanged.AddListener(value => workingCopy.ScreenShake = value);
            applyButton.onClick.AddListener(Apply);
            resetButton.onClick.AddListener(ResetToDefaults);
            backButton.onClick.AddListener(Close);
        }

        private bool ReferencesAreValid()
        {
            return panelRoot != null &&
                   masterVolumeSlider != null &&
                   masterVolumeValue != null &&
                   fullscreenToggle != null &&
                   resolutionDropdown != null &&
                   qualityDropdown != null &&
                   vSyncToggle != null &&
                   fpsLimitDropdown != null &&
                   screenShakeToggle != null &&
                   applyButton != null &&
                   resetButton != null &&
                   backButton != null;
        }

        private void PopulateDropdowns()
        {
            SettingsManager manager = SettingsManager.Instance;

            resolutionDropdown.ClearOptions();
            List<string> resolutionOptions = new List<string>();

            for (int i = 0; i < manager.SupportedResolutions.Count; i++)
            {
                Resolution resolution = manager.SupportedResolutions[i];
                resolutionOptions.Add(
                    resolution.width + " × " + resolution.height + " @ " +
                    SettingsManager.RoundedRefreshRate(resolution) + " Hz");
            }

            resolutionDropdown.AddOptions(resolutionOptions);

            qualityDropdown.ClearOptions();
            qualityDropdown.AddOptions(new List<string>(QualitySettings.names));

            fpsLimitDropdown.ClearOptions();
            List<string> fpsOptions = new List<string>();

            for (int i = 0; i < SettingsManager.SupportedFpsLimits.Length; i++)
            {
                int limit = SettingsManager.SupportedFpsLimits[i];
                fpsOptions.Add(limit == GameSettings.UnlimitedFps ? "Unlimited" : limit.ToString());
            }

            fpsLimitDropdown.AddOptions(fpsOptions);
        }

        private void RefreshControls()
        {
            if (!initialized || workingCopy == null)
            {
                return;
            }

            masterVolumeSlider.SetValueWithoutNotify(workingCopy.MasterVolume);
            UpdateVolumeLabel(workingCopy.MasterVolume);
            fullscreenToggle.SetIsOnWithoutNotify(workingCopy.Fullscreen);
            resolutionDropdown.SetValueWithoutNotify(
                SettingsManager.Instance.FindResolutionIndex(workingCopy));
            qualityDropdown.SetValueWithoutNotify(workingCopy.QualityLevel);
            vSyncToggle.SetIsOnWithoutNotify(workingCopy.VSync);
            fpsLimitDropdown.SetValueWithoutNotify(FindFpsLimitIndex(workingCopy.FpsLimit));
            screenShakeToggle.SetIsOnWithoutNotify(workingCopy.ScreenShake);

            resolutionDropdown.RefreshShownValue();
            qualityDropdown.RefreshShownValue();
            fpsLimitDropdown.RefreshShownValue();
        }

        private void OnVolumeChanged(float value)
        {
            if (workingCopy == null)
            {
                return;
            }

            workingCopy.MasterVolume = Mathf.RoundToInt(value);
            UpdateVolumeLabel(workingCopy.MasterVolume);
        }

        private void OnResolutionChanged(int index)
        {
            if (workingCopy == null ||
                index < 0 ||
                index >= SettingsManager.Instance.SupportedResolutions.Count)
            {
                return;
            }

            Resolution resolution = SettingsManager.Instance.SupportedResolutions[index];
            workingCopy.ResolutionWidth = resolution.width;
            workingCopy.ResolutionHeight = resolution.height;
            workingCopy.ResolutionRefreshRate = SettingsManager.RoundedRefreshRate(resolution);
        }

        private void OnFpsLimitChanged(int index)
        {
            if (workingCopy == null ||
                index < 0 ||
                index >= SettingsManager.SupportedFpsLimits.Length)
            {
                return;
            }

            workingCopy.FpsLimit = SettingsManager.SupportedFpsLimits[index];
        }

        private void Apply()
        {
            if (workingCopy == null)
            {
                return;
            }

            SettingsManager.Instance.ApplyAndSave(workingCopy);
            workingCopy = SettingsManager.Instance.Current.Copy();
            RefreshControls();
        }

        private void ResetToDefaults()
        {
            workingCopy = SettingsManager.Instance.CreateDefaults();
            RefreshControls();
        }

        private void UpdateVolumeLabel(int value)
        {
            masterVolumeValue.text = Mathf.Clamp(value, 0, 100) + "%";
        }

        private static int FindFpsLimitIndex(int fpsLimit)
        {
            for (int i = 0; i < SettingsManager.SupportedFpsLimits.Length; i++)
            {
                if (SettingsManager.SupportedFpsLimits[i] == fpsLimit)
                {
                    return i;
                }
            }

            return 3;
        }
    }
}
