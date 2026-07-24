using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    public float lifetime = 3f;

    private Rigidbody rb;
    private float damage;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Setup(Vector3 direction, float speed, float newDamage)
    {
        damage = newDamage;
        rb.linearVelocity = direction.normalized * speed;

        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            Health enemyHealth = other.GetComponentInParent<Health>();

            if (enemyHealth != null)
            {
                float appliedDamage = enemyHealth.TakeDamage(damage);

                if (appliedDamage > 0f && FloatingDamagePool.Instance != null)
                {
                    Vector3 numberPosition = other.bounds.center + Vector3.up * 0.8f;
                    FloatingDamagePool.Instance.Show(numberPosition, appliedDamage, false);
                }
            }

            Destroy(gameObject);
        }
    }
}
