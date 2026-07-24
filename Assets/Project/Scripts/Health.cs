using UnityEngine;

public enum DamageType { Physical, Magic }

public class Health : MonoBehaviour
{
    public float maxHealth = 10f;

    private float currentHealth;
    private bool dead;

    public float CurrentHealth
    {
        get { return currentHealth; }
    }

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount, bool fromCorpseBomb = false, DamageType damageType = DamageType.Physical)
    {
        if (dead) return;

        PlayerController controller = GetComponent<PlayerController>();

        if (controller != null)
        {
            float defense = damageType == DamageType.Magic ? controller.magicDefense : controller.armor;
            float reduction = Mathf.Clamp(defense * 0.5f, 0f, 80f);
            amount *= 1f - reduction / 100f;
        }

        currentHealth -= amount;

        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            dead = true;

            if (CompareTag("Enemy"))
            {
                Vector3 deathPosition = transform.position;

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.EnemyKilled();
                }

                if (WaveManager.Instance != null)
                {
                    WaveManager.Instance.EnemyDied();
                }

                if (AugmentManager.Instance != null)
                {
                    if (AugmentManager.Instance.IsActive("frenzy_gland") && PlayerController.Instance != null)
                    {
                        PlayerController.Instance.AddFrenzyStack();
                    }

                    if (!fromCorpseBomb && AugmentManager.Instance.IsActive("corpse_bomb"))
                    {
                        AugmentManager.Instance.TriggerCorpseBomb(deathPosition);
                    }
                }
            }

            Destroy(gameObject);
        }
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }

    public void AddMaxHealth(float amount)
    {
        maxHealth = Mathf.Max(1f, maxHealth + amount);
        currentHealth = Mathf.Clamp(currentHealth + amount, 1f, maxHealth);
    }
}
