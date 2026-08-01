using System.Collections.Generic;
using UnityEngine;

public class OrbitalOrb : MonoBehaviour
{
    public float damage = 5f;
    public float hitCooldown = 0.5f;

    private readonly Dictionary<Collider, float> lastHitTime = new Dictionary<Collider, float>();
    private readonly List<Collider> pruneBuffer = new List<Collider>();

    private const float CleanupInterval = 10f;

    private float nextCleanupTime;

    private void Update()
    {
        if (Time.time < nextCleanupTime) return;

        nextCleanupTime = Time.time + CleanupInterval;
        PruneDestroyedEntries();
    }

    private void PruneDestroyedEntries()
    {
        pruneBuffer.Clear();

        foreach (Collider key in lastHitTime.Keys)
        {
            if (key == null)
            {
                pruneBuffer.Add(key);
            }
        }

        for (int i = 0; i < pruneBuffer.Count; i++)
        {
            lastHitTime.Remove(pruneBuffer[i]);
        }

        pruneBuffer.Clear();
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Enemy")) return;

        if (lastHitTime.TryGetValue(other, out float last) && Time.time - last < hitCooldown)
        {
            return;
        }

        Health enemyHealth = other.GetComponentInParent<Health>();
        if (enemyHealth == null) return;

        float appliedDamage = enemyHealth.TakeDamage(damage);
        lastHitTime[other] = Time.time;

        if (appliedDamage > 0f && FloatingDamagePool.Instance != null)
        {
            Vector3 numberPosition = other.bounds.center + Vector3.up * 0.8f;
            FloatingDamagePool.Instance.Show(numberPosition, appliedDamage, false);
        }
    }
}
