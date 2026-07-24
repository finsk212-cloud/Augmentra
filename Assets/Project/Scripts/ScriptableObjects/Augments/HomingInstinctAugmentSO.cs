using UnityEngine;

[CreateAssetMenu(fileName = "New_HomingInstinct", menuName = "ScriptableObjects/Augments/Homing Instinct")]
public class HomingInstinctAugmentSO : AugmentSO
{
    public float homingStrength = 0f;

    public override void Apply(PlayerController player)
    {
        player.isHoming = true;
        player.homingStrength = homingStrength;
    }
}
