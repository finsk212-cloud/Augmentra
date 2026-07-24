using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Augmentra.UI
{
    public sealed class UIProgressBar : MonoBehaviour
    {
        [SerializeField] private Image fill;
        [SerializeField] private TextMeshProUGUI valueText;
        [SerializeField] private Image warningBorder;
        [SerializeField] private float fillSmoothTime = 0.12f;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color unavailableColor = new Color(0.35f, 0.4f, 0.5f, 1f);

        private float targetFill = 1f;
        private float displayedFill = 1f;
        private float fillVelocity;
        private float damagePulse;
        private bool lowHealth;
        private bool unavailable;

        public void Configure(
            Image fillImage,
            TextMeshProUGUI text,
            Image warning,
            Color color)
        {
            fill = fillImage;
            valueText = text;
            warningBorder = warning;
            normalColor = color;
        }

        public void SetValue(float current, float maximum)
        {
            targetFill = maximum > 0f ? Mathf.Clamp01(current / maximum) : 0f;

            if (valueText != null)
            {
                valueText.text =
                    Mathf.CeilToInt(Mathf.Max(0f, current)) + " / " +
                    Mathf.CeilToInt(Mathf.Max(0f, maximum));
            }
        }

        public void SetLowHealth(bool active)
        {
            lowHealth = active;
        }

        public void SetUnavailable(bool active)
        {
            unavailable = active;
        }

        public void PulseDamage()
        {
            damagePulse = 1f;
        }

        private void OnEnable()
        {
            displayedFill = targetFill;
            ApplyVisuals();
        }

        private void Update()
        {
            displayedFill = Mathf.SmoothDamp(
                displayedFill,
                targetFill,
                ref fillVelocity,
                Mathf.Max(0.01f, fillSmoothTime),
                Mathf.Infinity,
                Time.unscaledDeltaTime);
            damagePulse = Mathf.MoveTowards(damagePulse, 0f, Time.unscaledDeltaTime * 4.5f);
            ApplyVisuals();
        }

        private void ApplyVisuals()
        {
            if (fill != null)
            {
                fill.fillAmount = displayedFill;
                Color baseColor = unavailable ? unavailableColor : normalColor;
                fill.color = Color.Lerp(baseColor, Color.white, damagePulse * 0.65f);
            }

            if (warningBorder != null)
            {
                float lowPulse = lowHealth
                    ? 0.35f + Mathf.Sin(Time.unscaledTime * 4f) * 0.18f
                    : 0f;
                Color color = warningBorder.color;
                color.a = Mathf.Max(lowPulse, damagePulse * 0.8f);
                warningBorder.color = color;
                warningBorder.enabled = color.a > 0.001f;
            }
        }
    }
}
