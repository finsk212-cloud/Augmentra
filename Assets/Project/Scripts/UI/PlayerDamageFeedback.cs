using System.Collections;
using Augmentra.Settings;
using UnityEngine;

namespace Augmentra.UI
{
    [DefaultExecutionOrder(100)]
    public sealed class PlayerDamageFeedback : MonoBehaviour
    {
        [SerializeField] private Health playerHealth;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private CanvasGroup vignette;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip playerHitClip = null;
        [SerializeField] private float shakeDuration = 0.1f;
        [SerializeField] private float shakeStrength = 0.055f;

        private Coroutine vignetteRoutine;
        private float shakeRemaining;

        public void Configure(
            Health health,
            Camera camera,
            CanvasGroup vignetteGroup,
            AudioSource source)
        {
            playerHealth = health;
            targetCamera = camera;
            vignette = vignetteGroup;
            audioSource = source;
        }

        private void OnEnable()
        {
            if (playerHealth != null)
            {
                playerHealth.Damaged += OnPlayerDamaged;
            }

            if (vignette != null)
            {
                vignette.alpha = 0f;
                vignette.blocksRaycasts = false;
                vignette.interactable = false;
            }
        }

        private void OnDisable()
        {
            if (playerHealth != null)
            {
                playerHealth.Damaged -= OnPlayerDamaged;
            }
        }

        private void LateUpdate()
        {
            if (shakeRemaining <= 0f || targetCamera == null)
            {
                return;
            }

            shakeRemaining -= Time.unscaledDeltaTime;
            float fade = Mathf.Clamp01(shakeRemaining / Mathf.Max(0.01f, shakeDuration));
            Vector2 offset = Random.insideUnitCircle * shakeStrength * fade;
            targetCamera.transform.position += new Vector3(offset.x, offset.y, 0f);
        }

        private void OnPlayerDamaged(float amount, DamageType type)
        {
            if (vignetteRoutine != null)
            {
                StopCoroutine(vignetteRoutine);
            }

            vignetteRoutine = StartCoroutine(FlashVignette());

            if (SettingsManager.Instance.Current.ScreenShake)
            {
                shakeRemaining = shakeDuration;
            }

            if (playerHitClip != null && audioSource != null)
            {
                audioSource.PlayOneShot(playerHitClip);
            }
        }

        private IEnumerator FlashVignette()
        {
            if (vignette == null)
            {
                yield break;
            }

            float elapsed = 0f;
            const float duration = 0.32f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float normalized = Mathf.Clamp01(elapsed / duration);
                vignette.alpha = Mathf.Sin(normalized * Mathf.PI) * 0.42f;
                yield return null;
            }

            vignette.alpha = 0f;
            vignetteRoutine = null;
        }
    }
}
