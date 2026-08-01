using UnityEngine;

public class BulwarkAuraController : MonoBehaviour
{
    public float shieldAmount = 20f;

    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.WaveStarted += HandleWaveStarted;
        }
    }

    private void OnDisable()
    {
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.WaveStarted -= HandleWaveStarted;
        }
    }

    private void HandleWaveStarted(int waveNumber)
    {
        if (health != null)
        {
            health.SetShield(shieldAmount);
        }
    }
}
