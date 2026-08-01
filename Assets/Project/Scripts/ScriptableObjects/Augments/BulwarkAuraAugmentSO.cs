using UnityEngine;

[CreateAssetMenu(fileName = "New_BulwarkAura", menuName = "ScriptableObjects/Augments/Bulwark Aura")]
public class BulwarkAuraAugmentSO : AugmentSO
{
    public float shieldAmount = 20f;

    public override void Apply(PlayerController player)
    {
        BulwarkAuraController controller = player.gameObject.AddComponent<BulwarkAuraController>();
        controller.shieldAmount = shieldAmount;

        if (player.GetComponent<ShieldVisual>() == null)
        {
            player.gameObject.AddComponent<ShieldVisual>();
        }

        Health health = player.GetComponent<Health>();

        if (health != null)
        {
            health.SetShield(shieldAmount);
        }
    }
}
