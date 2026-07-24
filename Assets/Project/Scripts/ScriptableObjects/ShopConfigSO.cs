using TMPro;
using UnityEngine;

[CreateAssetMenu(fileName = "ShopConfig", menuName = "Augmentra/Shop Config")]
public class ShopConfigSO : ScriptableObject
{
    public TMP_FontAsset font;

    public float titleFontSize = 18f;
    public float itemNameFontSize = 13f;
    public float costFontSize = 14f;
    public float descriptionFontSize = 14f;
    public float categoryButtonFontSize = 16f;
    public float goldFontSize = 26f;

    public Color shopBackgroundColor = new Color(0.039f, 0.039f, 0.078f, 0.97f);
    public Color cardBackgroundColor = new Color(0.051f, 0.106f, 0.165f, 1f);
    public Color borderColor = new Color(0.118f, 0.227f, 0.373f, 1f);
    public Color affordableColor = new Color(0.949f, 0.788f, 0.298f, 1f);
    public Color unaffordableColor = new Color(0.878f, 0.290f, 0.290f, 1f);
    public Color combatAccentColor = new Color(0.753f, 0.224f, 0.169f, 1f);
    public Color survivalAccentColor = new Color(0.102f, 0.478f, 0.290f, 1f);
    public Color chaosAccentColor = new Color(0.482f, 0.184f, 0.745f, 1f);
    public Color allAccentColor = new Color(0.769f, 0.635f, 0.208f, 1f);
    public Color readyButtonColor = new Color(0.102f, 0.478f, 0.290f, 1f);
    public Color selectedCategoryColor = new Color(0.102f, 0.227f, 0.361f, 1f);

    public float descriptionPanelSlideSpeed = 14f;
    public bool showFlavourText = true;
    public bool showRarityBadge = true;
    public bool allowMultiplePurchases = true;
    public string readyButtonText = "READY";
}
