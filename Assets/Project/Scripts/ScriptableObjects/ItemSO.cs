using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Augmentra/Shop Item")]
public class ItemSO : ScriptableObject
{
    public enum Rarity { Common, Epic, Excellent }
    public enum Category { Combat, Survival, Chaos, Boots, Elixir, Orb }
    public enum StatType { AttackDamage, Health, AttackSpeed, MovementSpeed, Lifesteal, AbilityPower, HealthRegen, Mana, ManaRegen, Armor, MagicDefense }

    [System.Serializable]
    public struct ItemStat
    {
        public StatType statType;
        public float statValue;
        public string statLabel;
    }

    [Header("Identity")]
    public string itemName;
    [TextArea(2, 4)] public string description;
    public Sprite icon;
    [TextArea(2, 4)] public string flavourText;

    [Header("Shop")]
    public int cost;
    public Rarity rarity;
    public Category category;

    [Header("Stats")]
    public List<ItemStat> stats = new List<ItemStat>();

    [Header("Passive")]
    public string passiveName;
    [TextArea(3, 6)] public string passiveDescription;
}
