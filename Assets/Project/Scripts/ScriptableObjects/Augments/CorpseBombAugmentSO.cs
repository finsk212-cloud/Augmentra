using UnityEngine;

[CreateAssetMenu(fileName = "New_CorpseBomb", menuName = "ScriptableObjects/Augments/Corpse Bomb")]
public class CorpseBombAugmentSO : AugmentSO
{
    public float bombDamage = 15f;

    public override void Apply(PlayerController player)
    {
        player.corpsBombActive = true;
        player.corpsBombDamage = bombDamage;
    }
}
