using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Augmentra.UI
{
    public sealed class UIProgressBar : MonoBehaviour
    {
        [SerializeField] private Image fill;
        [SerializeField] private Image highlight;
        [SerializeField] private TextMeshProUGUI valueText;
        [SerializeField] private Image warningBorder;
        [SerializeField] private float fillSmoothTime = 0.12f;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color normalHighlightColor = new Color(1f, 1f, 1f, 0.2f);
        [SerializeField] private Color unavailableColor = new Color(0.35f, 0.4f, 0.5f, 1f);
        [SerializeField] private bool useRectTransformFill;
        [SerializeField] private string valuePrefix;

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
            Color color,
            Image highlightImage = null,
            bool driveRectTransformWidth = false,
            string prefix = "")
        {
            fill = fillImage;
            highlight = highlightImage;
            valueText = text;
            warningBorder = warning;
            normalColor = color;
            useRectTransformFill = driveRectTransformWidth;
            valuePrefix = prefix;

            if (highlight != null)
            {
                normalHighlightColor = highlight.color;
            }
        }

        public void SetValue(float current, float maximum)
        {
            targetFill = maximum > 0f ? Mathf.Clamp01(current / maximum) : 0f;
            UpdateValueText(current, maximum);
        }

        public void SetValueImmediate(float current, float maximum)
        {
            targetFill = maximum > 0f ? Mathf.Clamp01(current / maximum) : 0f;
            displayedFill = targetFill;
            fillVelocity = 0f;
            UpdateValueText(current, maximum);
            ApplyVisuals();
        }

        private void UpdateValueText(float current, float maximum)
        {
            if (valueText != null)
            {
                valueText.text =
                    valuePrefix +
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
                ApplyFillGeometry(fill, displayedFill);
                Color baseColor = unavailable ? unavailableColor : normalColor;
                fill.color = Color.Lerp(baseColor, Color.white, damagePulse * 0.65f);
            }

            if (highlight != null)
            {
                ApplyFillGeometry(highlight, displayedFill);
                Color baseColor = unavailable
                    ? new Color(unavailableColor.r, unavailableColor.g, unavailableColor.b, normalHighlightColor.a)
                    : normalHighlightColor;
                highlight.color = Color.Lerp(baseColor, Color.white, damagePulse * 0.35f);
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

        private void ApplyFillGeometry(Image image, float amount)
        {
            if (!useRectTransformFill)
            {
                image.fillAmount = amount;
                return;
            }

            RectTransform rect = image.rectTransform;
            Vector2 anchorMax = rect.anchorMax;
            anchorMax.x = Mathf.Clamp01(amount);
            rect.anchorMax = anchorMax;

            // The solid images have no sprite border or transparent padding.
            image.fillAmount = 1f;
        }
    }
}
