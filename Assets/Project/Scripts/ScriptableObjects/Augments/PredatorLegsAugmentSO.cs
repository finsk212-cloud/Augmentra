using UnityEngine;

[CreateAssetMenu(fileName = "New_PredatorLegs", menuName = "ScriptableObjects/Augments/Predator Legs")]
public class PredatorLegsAugmentSO : AugmentSO
{
    public float speedBonusPercent = 0.15f;

    public override void Apply(PlayerController player)
    {
        player.movementSpeed *= 1f + speedBonusPercent;
    }
}
