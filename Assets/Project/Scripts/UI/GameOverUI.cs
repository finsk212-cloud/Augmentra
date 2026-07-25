using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Augmentra.UI
{
    public sealed class GameOverUI : MonoBehaviour
    {
        public static GameOverUI Instance { get; private set; }

        public CanvasGroup group;
        public TextMeshProUGUI statsText;
        public Button restartButton;
        public Button mainMenuButton;
        [SerializeField] private float fadeInDuration = 0.35f;

        private Coroutine fadeRoutine;

        private void Awake()
        {
            Instance = this;

            if (group != null)
            {
                group.alpha = 0f;
                group.blocksRaycasts = false;
                group.interactable = false;
            }

            restartButton?.onClick.AddListener(() => GameManager.Instance?.RestartRun());
            mainMenuButton?.onClick.AddListener(() => GameManager.Instance?.ReturnToMainMenu());
        }

        public void Show(int kills, int gold, int level)
        {
            if (statsText != null)
            {
                statsText.text = "Kills: " + kills + "\nGold: " + gold + "\nLevel: " + level;
            }

            if (group == null)
            {
                return;
            }

            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
            }

            gameObject.SetActive(true);
            fadeRoutine = StartCoroutine(FadeIn());
        }

        private IEnumerator FadeIn()
        {
            group.blocksRaycasts = true;
            group.interactable = true;

            float elapsed = 0f;

            while (elapsed < fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, fadeInDuration));
                yield return null;
            }

            group.alpha = 1f;
            fadeRoutine = null;
        }
    }
}
