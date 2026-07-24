using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class AugmentCard : MonoBehaviour
{
    public Image iconImage;
    public TMP_Text nameText;
    public TMP_Text rarityText;
    public TMP_Text descriptionText;
    public TMP_Text flavourText;

    public void Initialize(AugmentSO augment, System.Action<AugmentSO> onSelect)
    {
        if (iconImage != null)
        {
            iconImage.sprite = augment.icon;
            iconImage.enabled = augment.icon != null;
        }

        if (nameText != null)
        {
            nameText.text = augment.augmentName;
        }

        if (rarityText != null)
        {
            rarityText.text = augment.rarity.ToString();
        }

        if (descriptionText != null)
        {
            descriptionText.text = augment.description;
        }

        if (flavourText != null)
        {
            flavourText.text = augment.flavourText;
        }

        ApplyRarityVisuals(augment.rarity);

        GetComponent<Button>().onClick.AddListener(() => onSelect(augment));
    }

    private void ApplyRarityVisuals(AugmentSO.Rarity rarity)
    {
        Color background;
        Color border;
        Color rarityColor;
        Color nameColor;
        bool hasGlow;
        Color glowColor;
        float glowBleed;
        bool shimmer;

        switch (rarity)
        {
            case AugmentSO.Rarity.Rare:
                background = ParseColor("#0D1A2E");
                border = ParseColor("#1E6FFF");
                rarityColor = ParseColor("#1E6FFF");
                nameColor = Color.white;
                hasGlow = true;
                glowColor = ParseColor("#1E6FFF", 0.25f);
                glowBleed = 6f;
                shimmer = false;
                break;
            case AugmentSO.Rarity.Epic:
                background = ParseColor("#150D2E");
                border = ParseColor("#9B30FF");
                rarityColor = ParseColor("#9B30FF");
                nameColor = Color.white;
                hasGlow = true;
                glowColor = ParseColor("#9B30FF", 0.3f);
                glowBleed = 8f;
                shimmer = false;
                break;
            case AugmentSO.Rarity.Legendary:
                background = ParseColor("#1A1200");
                border = ParseColor("#FFB800");
                rarityColor = ParseColor("#FFB800");
                nameColor = Color.white;
                hasGlow = true;
                glowColor = ParseColor("#FFB800", 0.4f);
                glowBleed = 12f;
                shimmer = true;
                break;
            default:
                background = ParseColor("#1A1A1A");
                border = ParseColor("#555555");
                rarityColor = ParseColor("#888888");
                nameColor = Color.white;
                hasGlow = false;
                glowColor = Color.clear;
                glowBleed = 0f;
                shimmer = false;
                break;
        }

        Image root = GetComponent<Image>();

        if (root == null)
        {
            root = gameObject.AddComponent<Image>();
        }

        root.color = background;
        root.raycastTarget = true;

        int siblingIndex = 0;

        if (hasGlow)
        {
            CreateOverlay("RarityGlow", -glowBleed, glowColor, siblingIndex);
            siblingIndex++;
        }

        CreateOverlay("RarityBorder", 0f, border, siblingIndex);
        siblingIndex++;

        Image backgroundImage = CreateOverlay("CardBackground", 2f, background, siblingIndex);

        if (rarityText != null)
        {
            rarityText.color = rarityColor;
        }

        if (nameText != null)
        {
            nameText.color = nameColor;
        }

        if (shimmer)
        {
            StartCoroutine(ShimmerRoutine(backgroundImage, ParseColor("#1A1200"), ParseColor("#2A1E00"), 1.5f));
        }
    }

    private Image CreateOverlay(string overlayName, float inset, Color color, int siblingIndex)
    {
        GameObject go = new GameObject(overlayName, typeof(RectTransform), typeof(Image));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(transform, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
        rt.SetSiblingIndex(siblingIndex);

        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private IEnumerator ShimmerRoutine(Image target, Color low, Color high, float period)
    {
        float half = period * 0.5f;

        while (target != null)
        {
            float t = Mathf.PingPong(Time.unscaledTime, half) / half;
            target.color = Color.Lerp(low, high, t);
            yield return null;
        }
    }

    private static Color ParseColor(string hex, float alpha = 1f)
    {
        ColorUtility.TryParseHtmlString(hex, out Color color);
        color.a = alpha;
        return color;
    }
}
