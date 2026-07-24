using System;
using UnityEngine;

public enum DamageType { Physical, Magic }

public class Health : MonoBehaviour
{
    public float maxHealth = 10f;

    private float currentHealth;
    private bool dead;

    public event Action<float, float> HealthChanged;
    public event Action<float, DamageType> Damaged;
    public event Action Died;

    public float CurrentHealth
    {
        get { return currentHealth; }
    }

    public bool IsDead => dead;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public float TakeDamage(float amount, DamageType damageType = DamageType.Physical)
    {
        if (dead || amount <= 0f)
        {
            return 0f;
        }

        PlayerController controller = GetComponent<PlayerController>();

        if (controller != null)
        {
            float defense = damageType == DamageType.Magic ? controller.magicDefense : controller.armor;
            float reduction = Mathf.Clamp(defense * 0.5f, 0f, 80f);
            amount *= 1f - reduction / 100f;
        }

        float appliedDamage = Mathf.Min(currentHealth, Mathf.Max(0f, amount));
        currentHealth -= appliedDamage;
        HealthChanged?.Invoke(currentHealth, maxHealth);
        Damaged?.Invoke(appliedDamage, damageType);

        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            dead = true;

            if (CompareTag("Enemy"))
            {
                DisableEnemyImmediately();

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.EnemyKilled();
                }

                if (WaveManager.Instance != null)
                {
                    WaveManager.Instance.EnemyDied();
                }

                Died?.Invoke();
                HitFlash feedback = GetComponent<HitFlash>();

                if (feedback == null || !feedback.isActiveAndEnabled)
                {
                    Destroy(gameObject);
                }
            }
            else if (CompareTag("Player"))
            {
                Died?.Invoke();
                GameManager.Instance?.PlayerDied();
                gameObject.SetActive(false);
            }
            else
            {
                Died?.Invoke();
            }
        }

        return appliedDamage;
    }

    public void Heal(float amount)
    {
        if (dead || amount <= 0f)
        {
            return;
        }

        float previous = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);

        if (!Mathf.Approximately(previous, currentHealth))
        {
            HealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }

    public void AddMaxHealth(float amount)
    {
        maxHealth = Mathf.Max(1f, maxHealth + amount);
        currentHealth = Mathf.Clamp(currentHealth + amount, 1f, maxHealth);
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void DisableEnemyImmediately()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();

        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }

        EnemyController controller = GetComponent<EnemyController>();

        if (controller != null)
        {
            controller.enabled = false;
        }

        Rigidbody body = GetComponent<Rigidbody>();

        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
        }
    }
}
