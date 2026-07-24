using System;
using TMPro;
using UnityEngine;

public sealed class FloatingDamageNumber : MonoBehaviour
{
    [SerializeField] private TextMeshPro text;
    [SerializeField] private float lifetime = 0.8f;
    [SerializeField] private float riseSpeed = 1.25f;

    private Action<FloatingDamageNumber> release;
    private Camera targetCamera;
    private Color baseColor;
    private float elapsed;
    private float horizontalSpeed;

    public void Configure(TextMeshPro textComponent)
    {
        text = textComponent;
    }

    public void Play(
        Vector3 position,
        float damage,
        bool critical,
        Camera camera,
        Action<FloatingDamageNumber> releaseCallback)
    {
        if (text == null)
        {
            return;
        }

        transform.position = position;
        transform.localScale = critical ? Vector3.one * 1.25f : Vector3.one;
        targetCamera = camera;
        release = releaseCallback;
        elapsed = 0f;
        horizontalSpeed = UnityEngine.Random.Range(-0.35f, 0.35f);
        baseColor = critical
            ? new Color(1f, 0.75f, 0.2f, 1f)
            : new Color(0.95f, 0.95f, 1f, 1f);
        text.text = Mathf.Max(0f, damage).ToString("0.#");
        text.fontSize = critical ? 4.2f : 3.2f;
        text.color = baseColor;
        gameObject.SetActive(true);
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        transform.position +=
            (Vector3.up * riseSpeed + Vector3.right * horizontalSpeed) * Time.deltaTime;

        Color color = baseColor;
        color.a = 1f - Mathf.Clamp01(elapsed / lifetime);
        text.color = color;

        if (elapsed >= lifetime)
        {
            release?.Invoke(this);
        }
    }

    private void LateUpdate()
    {
        if (targetCamera != null)
        {
            transform.rotation = targetCamera.transform.rotation;
        }
    }
}
