using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Augmentra.UI
{
    public sealed class SceneTransition : MonoBehaviour
    {
        private static SceneTransition instance;

        [SerializeField] private CanvasGroup group;
        [SerializeField] private Image fadeImage;
        [SerializeField] private float fadeDuration = 0.4f;

        public static SceneTransition Instance
        {
            get
            {
                EnsureInstance();
                return instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            EnsureInstance();
        }

        private static void EnsureInstance()
        {
            if (instance != null)
            {
                return;
            }

            SceneTransition existing = FindFirstObjectByType<SceneTransition>();

            if (existing != null)
            {
                instance = existing;
                return;
            }

            GameObject go = new GameObject("SceneTransition");
            instance = go.AddComponent<SceneTransition>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            if (group == null)
            {
                BuildUI();
            }
        }

        private void BuildUI()
        {
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10000; // always render on top of everything else

            gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
            gameObject.AddComponent<GraphicRaycaster>();

            group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            GameObject imageGO = new GameObject("FadeImage", typeof(Image));
            imageGO.transform.SetParent(transform, false);
            fadeImage = imageGO.GetComponent<Image>();
            fadeImage.color = Color.black;

            RectTransform rect = imageGO.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
        }

        private bool isTransitioning;

        public void LoadScene(string sceneName)
        {
            if (isTransitioning)
            {
                return;
            }

            StartCoroutine(TransitionRoutine(sceneName));
        }

        private IEnumerator TransitionRoutine(string sceneName)
        {
            isTransitioning = true;

            yield return Fade(0f, 1f);

            AsyncOperation load = SceneManager.LoadSceneAsync(sceneName);

            while (load != null && !load.isDone)
            {
                yield return null;
            }

            yield return Fade(1f, 0f);

            isTransitioning = false;
        }

        private IEnumerator Fade(float from, float to)
        {
            group.blocksRaycasts = true;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / fadeDuration));
                yield return null;
            }

            group.alpha = to;
            group.blocksRaycasts = to > 0.5f;
        }
    }
}
