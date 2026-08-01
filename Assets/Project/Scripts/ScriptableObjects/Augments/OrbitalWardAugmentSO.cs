using UnityEngine;

[CreateAssetMenu(fileName = "New_OrbitalWard", menuName = "ScriptableObjects/Augments/Orbital Ward")]
public class OrbitalWardAugmentSO : AugmentSO
{
    public int orbCount = 2;
    public float orbitRadius = 2.5f;
    public float orbitSpeed = 90f;
    public float orbDamage = 5f;

    public override void Apply(PlayerController player)
    {
        OrbitalWardController controller = player.gameObject.AddComponent<OrbitalWardController>();
        controller.orbCount = orbCount;
        controller.orbitRadius = orbitRadius;
        controller.orbitSpeed = orbitSpeed;
        controller.orbDamage = orbDamage;
    }
}
