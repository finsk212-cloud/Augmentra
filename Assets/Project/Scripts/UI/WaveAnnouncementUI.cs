using System.Collections;
using TMPro;
using UnityEngine;

namespace Augmentra.UI
{
    public sealed class WaveAnnouncementUI : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip waveStartClip = null;
        [SerializeField] private AudioClip waveClearedClip = null;

        private Coroutine animationRoutine;

        public void Configure(
            CanvasGroup canvasGroup,
            TextMeshProUGUI text,
            AudioSource source)
        {
            group = canvasGroup;
            label = text;
            audioSource = source;
        }

        private void Awake()
        {
            if (group != null)
            {
                group.alpha = 0f;
                group.blocksRaycasts = false;
                group.interactable = false;
            }
        }

        public void ShowWave(int wave)
        {
            Show("WAVE " + wave, waveStartClip);
        }

        public void ShowCleared()
        {
            Show("WAVE CLEARED", waveClearedClip);
        }

        private void Show(string message, AudioClip clip)
        {
            if (group == null || label == null)
            {
                return;
            }

            if (animationRoutine != null)
            {
                StopCoroutine(animationRoutine);
            }

            label.text = message;
            animationRoutine = StartCoroutine(Animate());

            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        private IEnumerator Animate()
        {
            gameObject.SetActive(true);
            float elapsed = 0f;
            const float fadeIn = 0.18f;

            while (elapsed < fadeIn)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Clamp01(elapsed / fadeIn);
                label.rectTransform.localScale =
                    Vector3.one * Mathf.Lerp(0.92f, 1f, group.alpha);
                yield return null;
            }

            group.alpha = 1f;
            yield return new WaitForSecondsRealtime(0.75f);

            elapsed = 0f;
            const float fadeOut = 0.32f;

            while (elapsed < fadeOut)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = 1f - Mathf.Clamp01(elapsed / fadeOut);
                yield return null;
            }

            group.alpha = 0f;
            animationRoutine = null;
        }
    }
}
