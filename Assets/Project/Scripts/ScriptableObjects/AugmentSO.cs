using UnityEngine;

public abstract class AugmentSO : ScriptableObject
{
    public string augmentName;
    [TextArea(3, 8)]
    public string description;
    public Sprite icon;
    public Rarity rarity;
    [TextArea(2, 5)]
    public string flavourText;

    public enum Rarity { Common, Rare, Epic, Legendary }

    public abstract void Apply(PlayerController player);
}
