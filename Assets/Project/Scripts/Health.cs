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

    public void TakeDamage(float amount, DamageType damageType = DamageType.Physical)
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
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.EnemyKilled();
                }

                if (WaveManager.Instance != null)
                {
                    WaveManager.Instance.EnemyDied();
                }

                Destroy(gameObject);
            }
            else if (CompareTag("Player"))
            {
                GameManager.Instance?.PlayerDied();
                gameObject.SetActive(false);
            }
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
