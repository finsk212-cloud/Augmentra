using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class AugmentManager : MonoBehaviour
{
    public static AugmentManager Instance { get; private set; }

    [SerializeField] private GameObject augmentOverlay;
    [SerializeField] private GameObject augmentCardPrefab;
    [SerializeField] private RectTransform cardContainer;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;

    private const float CorpseBombRadius = 3f;

    private List<AugmentSO> augmentPool;
    private AugmentSO selectedAugment;
    private readonly HashSet<System.Type> activeAugmentTypes = new HashSet<System.Type>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        augmentPool = new List<AugmentSO>(Resources.LoadAll<AugmentSO>("Augments"));

        if (augmentOverlay != null)
        {
            augmentOverlay.SetActive(false);
        }
    }

    public bool IsActive<T>() where T : AugmentSO
    {
        return activeAugmentTypes.Contains(typeof(T));
    }

    public void ShowAugmentPicker(int waveNumber)
    {
        Time.timeScale = 0f;

        if (titleText != null)
        {
            titleText.text = "CHOOSE AN AUGMENT";
        }

        if (subtitleText != null)
        {
            subtitleText.text = "Select a card to continue";
        }

        if (augmentOverlay != null)
        {
            augmentOverlay.SetActive(true);
        }

        for (int i = cardContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(cardContainer.GetChild(i).gameObject);
        }

        List<AugmentSO> available = new List<AugmentSO>(augmentPool);
        int cards = Mathf.Min(3, available.Count);

        for (int i = 0; i < cards; i++)
        {
            int index = Random.Range(0, available.Count);
            AugmentSO augment = available[index];
            available.RemoveAt(index);

            GameObject card = Instantiate(augmentCardPrefab, cardContainer);
            AugmentCard script = card.GetComponent<AugmentCard>();
            script.Initialize(augment, SelectAugment);
        }
    }

    private void SelectAugment(AugmentSO augment)
    {
        selectedAugment = augment;
        ApplyAugmentEffect();

        if (augmentOverlay != null)
        {
            augmentOverlay.SetActive(false);
        }

        Time.timeScale = 1f;

        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.OpenBreak();
        }
    }

    private void ApplyAugmentEffect()
    {
        activeAugmentTypes.Add(selectedAugment.GetType());

        PlayerController player = PlayerController.Instance;

        if (player == null)
        {
            return;
        }

        selectedAugment.Apply(player);
    }

    public void TriggerCorpseBomb(Vector3 position)
    {
        if (!IsActive<CorpseBombAugmentSO>() || PlayerController.Instance == null)
        {
            return;
        }

        float damage = PlayerController.Instance.corpsBombDamage;

        if (damage <= 0f)
        {
            return;
        }

        Collider[] hits = Physics.OverlapSphere(position, CorpseBombRadius);

        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("Enemy"))
            {
                continue;
            }

            Health health = hit.GetComponent<Health>();

            if (health != null)
            {
                health.TakeDamage(damage, true);
            }
        }
    }
}
