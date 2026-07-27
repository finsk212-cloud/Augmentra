using UnityEngine;
using UnityEngine.UI;

namespace Augmentra.UI
{
    public static class UIColorUtility
    {
        // Applies a complete, consistent ColorBlock to a button based on a
        // single base color - hover is a lightened version, press is a
        // darkened version, so every button's interaction states always
        // look like proper variations of its own theme color instead of
        // Unity's unrelated defaults.
        public static void ApplyConsistentColors(Button button, Color baseColor)
        {
            if (button == null)
            {
                return;
            }

            Image image = button.targetGraphic as Image;

            if (image != null)
            {
                image.color = baseColor;
            }

            ColorBlock colors = button.colors;
            colors.normalColor = baseColor;
            colors.highlightedColor = Lighten(baseColor, 0.15f);
            colors.pressedColor = Darken(baseColor, 0.15f);
            colors.selectedColor = baseColor;
            colors.disabledColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.35f);
            button.colors = colors;
        }

        private static Color Lighten(Color c, float amount)
        {
            return new Color(
                Mathf.Clamp01(c.r + amount),
                Mathf.Clamp01(c.g + amount),
                Mathf.Clamp01(c.b + amount),
                c.a);
        }

        private static Color Darken(Color c, float amount)
        {
            return new Color(
                Mathf.Clamp01(c.r - amount),
                Mathf.Clamp01(c.g - amount),
                Mathf.Clamp01(c.b - amount),
                c.a);
        }
    }
}
