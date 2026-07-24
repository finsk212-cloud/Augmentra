using System;
using System.Collections.Generic;
using UnityEngine;

namespace Augmentra.Settings
{
    [DefaultExecutionOrder(-1000)]
    public sealed class SettingsManager : MonoBehaviour
    {
        private const string PrefsPrefix = "Augmentra.Settings.";
        private const string VolumeKey = PrefsPrefix + "MasterVolume";
        private const string FullscreenKey = PrefsPrefix + "Fullscreen";
        private const string ResolutionWidthKey = PrefsPrefix + "ResolutionWidth";
        private const string ResolutionHeightKey = PrefsPrefix + "ResolutionHeight";
        private const string ResolutionRefreshKey = PrefsPrefix + "ResolutionRefreshRate";
        private const string QualityKey = PrefsPrefix + "QualityLevel";
        private const string VSyncKey = PrefsPrefix + "VSync";
        private const string FpsLimitKey = PrefsPrefix + "FpsLimit";
        private const string ScreenShakeKey = PrefsPrefix + "ScreenShake";

        public static readonly int[] SupportedFpsLimits =
        {
            30, 60, 120, 144, 165, 240, GameSettings.UnlimitedFps
        };

        private static SettingsManager instance;
        private static int projectDefaultQualityLevel;

        private readonly List<Resolution> supportedResolutions = new List<Resolution>();

        public static SettingsManager Instance
        {
            get
            {
                EnsureInstance();
                return instance;
            }
        }

        public GameSettings Current { get; private set; }
        public IReadOnlyList<Resolution> SupportedResolutions => supportedResolutions;
        public event Action<GameSettings> SettingsApplied;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            projectDefaultQualityLevel = QualitySettings.GetQualityLevel();
            EnsureInstance();
        }

        private static void EnsureInstance()
        {
            if (instance != null)
            {
                return;
            }

            SettingsManager existing = FindFirstObjectByType<SettingsManager>();

            if (existing != null)
            {
                instance = existing;
                return;
            }

            GameObject managerObject = new GameObject("SettingsManager");
            instance = managerObject.AddComponent<SettingsManager>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            if (projectDefaultQualityLevel < 0 ||
                projectDefaultQualityLevel >= QualitySettings.names.Length)
            {
                projectDefaultQualityLevel = QualitySettings.GetQualityLevel();
            }

            RefreshSupportedResolutions();
            Current = Load();
            Apply(Current, false);
        }

        public GameSettings CreateDefaults()
        {
            Resolution nativeResolution = Screen.currentResolution;

            return new GameSettings
            {
                MasterVolume = 100,
                Fullscreen = true,
                ResolutionWidth = nativeResolution.width,
                ResolutionHeight = nativeResolution.height,
                ResolutionRefreshRate = RoundedRefreshRate(nativeResolution),
                QualityLevel = Mathf.Clamp(
                    projectDefaultQualityLevel,
                    0,
                    Mathf.Max(0, QualitySettings.names.Length - 1)),
                VSync = true,
                FpsLimit = 144,
                ScreenShake = true
            };
        }

        public void ApplyAndSave(GameSettings settings)
        {
            Apply(settings, true);
        }

        public int FindResolutionIndex(GameSettings settings)
        {
            if (supportedResolutions.Count == 0)
            {
                return 0;
            }

            int sameSizeIndex = -1;

            for (int i = 0; i < supportedResolutions.Count; i++)
            {
                Resolution resolution = supportedResolutions[i];

                if (resolution.width != settings.ResolutionWidth ||
                    resolution.height != settings.ResolutionHeight)
                {
                    continue;
                }

                sameSizeIndex = i;

                if (RoundedRefreshRate(resolution) == settings.ResolutionRefreshRate)
                {
                    return i;
                }
            }

            return sameSizeIndex >= 0 ? sameSizeIndex : FindCurrentResolutionIndex();
        }

        public int FindCurrentResolutionIndex()
        {
            Resolution current = Screen.currentResolution;

            for (int i = 0; i < supportedResolutions.Count; i++)
            {
                Resolution resolution = supportedResolutions[i];

                if (resolution.width == current.width &&
                    resolution.height == current.height &&
                    RoundedRefreshRate(resolution) == RoundedRefreshRate(current))
                {
                    return i;
                }
            }

            return Mathf.Max(0, supportedResolutions.Count - 1);
        }

        public static int RoundedRefreshRate(Resolution resolution)
        {
            return Mathf.RoundToInt((float)resolution.refreshRateRatio.value);
        }

        public static bool IsSupportedFpsLimit(int value)
        {
            for (int i = 0; i < SupportedFpsLimits.Length; i++)
            {
                if (SupportedFpsLimits[i] == value)
                {
                    return true;
                }
            }

            return false;
        }

        private GameSettings Load()
        {
            GameSettings defaults = CreateDefaults();
            GameSettings loaded = new GameSettings
            {
                MasterVolume = PlayerPrefs.GetInt(VolumeKey, defaults.MasterVolume),
                Fullscreen = ReadBool(FullscreenKey, defaults.Fullscreen),
                ResolutionWidth = PlayerPrefs.GetInt(
                    ResolutionWidthKey, defaults.ResolutionWidth),
                ResolutionHeight = PlayerPrefs.GetInt(
                    ResolutionHeightKey, defaults.ResolutionHeight),
                ResolutionRefreshRate = PlayerPrefs.GetInt(
                    ResolutionRefreshKey, defaults.ResolutionRefreshRate),
                QualityLevel = PlayerPrefs.GetInt(QualityKey, defaults.QualityLevel),
                VSync = ReadBool(VSyncKey, defaults.VSync),
                FpsLimit = PlayerPrefs.GetInt(FpsLimitKey, defaults.FpsLimit),
                ScreenShake = ReadBool(ScreenShakeKey, defaults.ScreenShake)
            };

            loaded.Clamp(QualitySettings.names.Length);
            return loaded;
        }

        private void Apply(GameSettings settings, bool save)
        {
            if (settings == null)
            {
                Debug.LogWarning("SettingsManager ignored a null settings request.", this);
                return;
            }

            GameSettings applied = settings.Copy();
            applied.Clamp(QualitySettings.names.Length);
            ResolveUnsupportedResolution(applied);

            AudioListener.volume = applied.MasterVolume / 100f;
            QualitySettings.SetQualityLevel(applied.QualityLevel, true);
            QualitySettings.vSyncCount = applied.VSync ? 1 : 0;
            Application.targetFrameRate = applied.FpsLimit;

            Resolution resolution = supportedResolutions.Count > 0
                ? supportedResolutions[FindResolutionIndex(applied)]
                : Screen.currentResolution;
            FullScreenMode mode = applied.Fullscreen
                ? FullScreenMode.FullScreenWindow
                : FullScreenMode.Windowed;

            Screen.SetResolution(
                resolution.width,
                resolution.height,
                mode,
                resolution.refreshRateRatio);

            applied.ResolutionWidth = resolution.width;
            applied.ResolutionHeight = resolution.height;
            applied.ResolutionRefreshRate = RoundedRefreshRate(resolution);
            Current = applied;

            if (save)
            {
                Save(applied);
            }

            SettingsApplied?.Invoke(Current.Copy());
        }

        private void Save(GameSettings settings)
        {
            PlayerPrefs.SetInt(VolumeKey, settings.MasterVolume);
            PlayerPrefs.SetInt(FullscreenKey, settings.Fullscreen ? 1 : 0);
            PlayerPrefs.SetInt(ResolutionWidthKey, settings.ResolutionWidth);
            PlayerPrefs.SetInt(ResolutionHeightKey, settings.ResolutionHeight);
            PlayerPrefs.SetInt(ResolutionRefreshKey, settings.ResolutionRefreshRate);
            PlayerPrefs.SetInt(QualityKey, settings.QualityLevel);
            PlayerPrefs.SetInt(VSyncKey, settings.VSync ? 1 : 0);
            PlayerPrefs.SetInt(FpsLimitKey, settings.FpsLimit);
            PlayerPrefs.SetInt(ScreenShakeKey, settings.ScreenShake ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void RefreshSupportedResolutions()
        {
            supportedResolutions.Clear();
            Resolution[] resolutions = Screen.resolutions;

            for (int i = 0; i < resolutions.Length; i++)
            {
                Resolution candidate = resolutions[i];
                bool duplicate = false;

                for (int j = 0; j < supportedResolutions.Count; j++)
                {
                    Resolution existing = supportedResolutions[j];

                    if (existing.width == candidate.width &&
                        existing.height == candidate.height &&
                        RoundedRefreshRate(existing) == RoundedRefreshRate(candidate))
                    {
                        duplicate = true;
                        break;
                    }
                }

                if (!duplicate)
                {
                    supportedResolutions.Add(candidate);
                }
            }

            if (supportedResolutions.Count == 0)
            {
                supportedResolutions.Add(Screen.currentResolution);
            }
        }

        private void ResolveUnsupportedResolution(GameSettings settings)
        {
            int index = FindResolutionIndex(settings);
            Resolution resolved = supportedResolutions[index];
            settings.ResolutionWidth = resolved.width;
            settings.ResolutionHeight = resolved.height;
            settings.ResolutionRefreshRate = RoundedRefreshRate(resolved);
        }

        private static bool ReadBool(string key, bool fallback)
        {
            return PlayerPrefs.GetInt(key, fallback ? 1 : 0) != 0;
        }
    }
}
