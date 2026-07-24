using UnityEngine;

[CreateAssetMenu(fileName = "New_VampiricRound", menuName = "ScriptableObjects/Augments/Vampiric Round")]
public class VampiricRoundAugmentSO : AugmentSO
{
    public float lifeStealAmount = 0.08f;

    public override void Apply(PlayerController player)
    {
        player.lifeSteal = lifeStealAmount;
    }
}
