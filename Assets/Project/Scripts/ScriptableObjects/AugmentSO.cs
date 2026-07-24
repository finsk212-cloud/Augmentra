using UnityEngine;

[CreateAssetMenu(fileName = "New_Augment", menuName = "ScriptableObjects/Augment", order = 1)]
public class AugmentSO : ScriptableObject
{
    public string augmentName;
    [TextArea(3, 8)]
    public string description;
    public Sprite icon;
    public Rarity rarity;
    public string effectTag;
    public float value;
    [TextArea(2, 5)]
    public string flavourText;

    public enum Rarity { Common, Rare, Epic, Legendary }
}
