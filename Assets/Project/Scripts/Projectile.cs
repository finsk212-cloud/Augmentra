using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    public float lifetime = 3f;

    private Rigidbody rb;
    private float damage;
    private float lifeSteal;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Setup(Vector3 direction, float speed, float newDamage, float newLifeSteal)
    {
        damage = newDamage;
        lifeSteal = newLifeSteal;
        rb.linearVelocity = direction.normalized * speed;

        Destroy(gameObject, lifetime);
    }

    private void FixedUpdate()
    {
        if (AugmentManager.Instance == null || !AugmentManager.Instance.IsActive("homing_instinct")) return;
        if (PlayerController.Instance == null) return;

        float strength = PlayerController.Instance.homingStrength;

        if (strength <= 0f) return;

        Transform target = FindNearestEnemy();

        if (target == null) return;

        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude < 0.001f) return;

        Vector3 velocity = rb.linearVelocity;
        Vector3 flat = new Vector3(velocity.x, 0f, velocity.z);
        float speed = flat.magnitude;

        if (speed < 0.01f) return;

        Vector3 currentDir = flat / speed;
        Vector3 desiredDir = toTarget.normalized;
        Vector3 newDir = Vector3.RotateTowards(currentDir, desiredDir, strength * Time.fixedDeltaTime, 0f);

        rb.linearVelocity = new Vector3(newDir.x * speed, velocity.y, newDir.z * speed);
    }

    private Transform FindNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Transform nearest = null;
        float nearestSqr = float.MaxValue;

        foreach (GameObject enemy in enemies)
        {
            float sqr = (enemy.transform.position - transform.position).sqrMagnitude;

            if (sqr < nearestSqr)
            {
                nearestSqr = sqr;
                nearest = enemy.transform;
            }
        }

        return nearest;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            Health enemyHealth = other.GetComponent<Health>();

            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);
            }

            if (lifeSteal > 0f)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

                if (playerObject != null)
                {
                    Health playerHealth = playerObject.GetComponent<Health>();

                    if (playerHealth != null)
                    {
                        playerHealth.Heal(damage * lifeSteal);
                    }
                }
            }

            Destroy(gameObject);
        }
    }
}
