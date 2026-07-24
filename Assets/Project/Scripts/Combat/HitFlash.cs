using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Health))]
public sealed class HitFlash : MonoBehaviour
{
    [SerializeField] private float flashDuration = 0.09f;
    [SerializeField] private float scalePunch = 0.045f;
    [SerializeField] private float deathDuration = 0.22f;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hitClip = null;
    [SerializeField] private AudioClip deathClip = null;

    private Health health;
    private Renderer[] renderers;
    private MaterialPropertyBlock[] blocks;
    private Color[] originalColors;
    private int[] colorPropertyIds;
    private Vector3 originalScale;
    private Coroutine feedbackRoutine;

    public float DeathDuration => Mathf.Max(0f, deathDuration);

    private void Awake()
    {
        health = GetComponent<Health>();
        originalScale = transform.localScale;

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        Renderer[] found = GetComponentsInChildren<Renderer>(true);
        int usableCount = 0;

        for (int i = 0; i < found.Length; i++)
        {
            if (!found[i].gameObject.name.StartsWith("HealthBar"))
            {
                usableCount++;
            }
        }

        renderers = new Renderer[usableCount];
        blocks = new MaterialPropertyBlock[usableCount];
        originalColors = new Color[usableCount];
        colorPropertyIds = new int[usableCount];
        int index = 0;

        for (int i = 0; i < found.Length; i++)
        {
            Renderer renderer = found[i];

            if (renderer.gameObject.name.StartsWith("HealthBar"))
            {
                continue;
            }

            renderers[index] = renderer;
            blocks[index] = new MaterialPropertyBlock();
            Material material = renderer.sharedMaterial;
            int propertyId = material != null && material.HasProperty("_BaseColor")
                ? Shader.PropertyToID("_BaseColor")
                : Shader.PropertyToID("_Color");
            colorPropertyIds[index] = propertyId;
            originalColors[index] =
                material != null && material.HasProperty(propertyId)
                    ? material.GetColor(propertyId)
                    : Color.white;
            index++;
        }
    }

    private void OnEnable()
    {
        health.Damaged += OnDamaged;
        health.Died += OnDied;
    }

    private void OnDisable()
    {
        health.Damaged -= OnDamaged;
        health.Died -= OnDied;
        RestoreRenderers();
        transform.localScale = originalScale;
    }

    private void OnDamaged(float amount, DamageType type)
    {
        if (feedbackRoutine != null)
        {
            StopCoroutine(feedbackRoutine);
        }

        feedbackRoutine = StartCoroutine(PlayHit());

        if (hitClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(hitClip);
        }
    }

    private void OnDied()
    {
        if (feedbackRoutine != null)
        {
            StopCoroutine(feedbackRoutine);
        }

        feedbackRoutine = StartCoroutine(PlayDeath());

        if (deathClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(deathClip);
        }
    }

    private IEnumerator PlayHit()
    {
        float elapsed = 0f;

        while (elapsed < flashDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, flashDuration));
            float pulse = Mathf.Sin(normalized * Mathf.PI);
            ApplyFlash(pulse);
            transform.localScale = originalScale * (1f + pulse * scalePunch);
            yield return null;
        }

        RestoreRenderers();
        transform.localScale = originalScale;
        feedbackRoutine = null;
    }

    private IEnumerator PlayDeath()
    {
        float elapsed = 0f;

        while (elapsed < DeathDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, DeathDuration));
            ApplyFlash(1f - normalized);
            transform.localScale = originalScale * (1f - normalized);
            yield return null;
        }

        feedbackRoutine = null;
        Destroy(gameObject);
    }

    private void ApplyFlash(float amount)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == null)
            {
                continue;
            }

            renderer.GetPropertyBlock(blocks[i]);
            blocks[i].SetColor(
                colorPropertyIds[i],
                Color.Lerp(originalColors[i], Color.white, amount * 0.8f));
            renderer.SetPropertyBlock(blocks[i]);
        }
    }

    private void RestoreRenderers()
    {
        if (renderers == null)
        {
            return;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
            {
                continue;
            }

            renderers[i].GetPropertyBlock(blocks[i]);
            blocks[i].SetColor(colorPropertyIds[i], originalColors[i]);
            renderers[i].SetPropertyBlock(blocks[i]);
        }
    }
}
