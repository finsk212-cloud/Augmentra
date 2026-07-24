using System;
using UnityEngine;

namespace Augmentra.Settings
{
    [Serializable]
    public sealed class GameSettings
    {
        public const int UnlimitedFps = -1;

        public int MasterVolume = 100;
        public bool Fullscreen = true;
        public int ResolutionWidth;
        public int ResolutionHeight;
        public int ResolutionRefreshRate;
        public int QualityLevel;
        public bool VSync = true;
        public int FpsLimit = 144;
        public bool ScreenShake = true;

        public GameSettings Copy()
        {
            return (GameSettings)MemberwiseClone();
        }

        public void Clamp(int qualityLevelCount)
        {
            MasterVolume = Mathf.Clamp(MasterVolume, 0, 100);
            QualityLevel = Mathf.Clamp(QualityLevel, 0, Mathf.Max(0, qualityLevelCount - 1));

            if (!SettingsManager.IsSupportedFpsLimit(FpsLimit))
            {
                FpsLimit = 144;
            }
        }
    }
}
