using UnityEngine;

[CreateAssetMenu(fileName = "New_FrenzyGland", menuName = "ScriptableObjects/Augments/Frenzy Gland")]
public class FrenzyGlandAugmentSO : AugmentSO
{
    public float bonusPerStack = 0.05f;

    public override void Apply(PlayerController player)
    {
        player.frenzyGlandActive = true;
        player.frenzyBonusPerStack = bonusPerStack;
    }
}
