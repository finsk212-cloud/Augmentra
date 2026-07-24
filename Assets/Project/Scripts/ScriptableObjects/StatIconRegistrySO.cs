using System.Collections.Generic;
using UnityEngine;

public enum StatType
{
    AttackDamage,
    AbilityPower,
    AttackSpeed,
    MovementSpeed,
    Lifesteal,
    Health,
    HealthRegeneration,
    Mana,
    ManaRegeneration,
    Armor,
    MagicDefense
}

[CreateAssetMenu(fileName = "StatIconRegistry", menuName = "Augmentra/Stat Icon Registry")]
public class StatIconRegistrySO : ScriptableObject
{
    [System.Serializable]
    public struct Entry
    {
        public StatType statType;
        public Sprite icon;
        public Color color;
    }

    public List<Entry> entries = new List<Entry>();

    public Sprite GetIcon(StatType type)
    {
        foreach (Entry entry in entries)
        {
            if (entry.statType == type)
            {
                return entry.icon;
            }
        }

        return null;
    }

    public Color GetColor(StatType type)
    {
        foreach (Entry entry in entries)
        {
            if (entry.statType == type)
            {
                return entry.color;
            }
        }

        return Color.white;
    }
}
