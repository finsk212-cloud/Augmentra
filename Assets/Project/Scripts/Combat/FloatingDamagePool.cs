using System.Collections.Generic;
using UnityEngine;

public sealed class FloatingDamagePool : MonoBehaviour
{
    [SerializeField] private FloatingDamageNumber prefab;
    [SerializeField, Min(1)] private int initialSize = 24;

    private static FloatingDamagePool instance;
    private readonly Queue<FloatingDamageNumber> available = new Queue<FloatingDamageNumber>();
    private Camera targetCamera;

    public static FloatingDamagePool Instance => instance;

    public void Configure(FloatingDamageNumber numberPrefab, int prewarmCount)
    {
        prefab = numberPrefab;
        initialSize = Mathf.Max(1, prewarmCount);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning("Duplicate FloatingDamagePool disabled.", this);
            enabled = false;
            return;
        }

        instance = this;
        targetCamera = Camera.main;

        if (prefab == null)
        {
            Debug.LogError(
                "FloatingDamagePool has no number prefab. Run Tools/Augmentra/Setup Gameplay HUD.",
                this);
            return;
        }

        for (int i = 0; i < initialSize; i++)
        {
            available.Enqueue(CreateNumber());
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public void Show(Vector3 position, float damage, bool critical)
    {
        if (prefab == null || damage <= 0f)
        {
            return;
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        FloatingDamageNumber number = available.Count > 0
            ? available.Dequeue()
            : CreateNumber();
        number.Play(position, damage, critical, targetCamera, Release);
    }

    private FloatingDamageNumber CreateNumber()
    {
        FloatingDamageNumber number = Instantiate(prefab, transform);
        number.gameObject.SetActive(false);
        return number;
    }

    private void Release(FloatingDamageNumber number)
    {
        number.gameObject.SetActive(false);
        number.transform.SetParent(transform, false);
        available.Enqueue(number);
    }
}
