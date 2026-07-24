using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

    public Image hpFill;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI waveText;
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI[] statTexts;
    public Image[] inventoryIcons;
    public Image[] inventoryBorders;
    public CanvasGroup announcementGroup;
    public TextMeshProUGUI announcementText;

    private PlayerController player;
    private Health playerHealth;
    private int lastWave = 0;
    private int nextInventoryIndex;
    private Coroutine announceRoutine;

    private void Awake()
    {
        Instance = this;

        ApplyFont();

        if (announcementGroup != null)
        {
            announcementGroup.alpha = 0f;
            announcementGroup.blocksRaycasts = false;
            announcementGroup.interactable = false;
            announcementGroup.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();
        }

        if (player != null)
        {
            if (playerHealth == null)
            {
                playerHealth = player.GetComponent<Health>();
            }

            if (playerHealth != null)
            {
                UpdateHP(playerHealth.CurrentHealth, playerHealth.maxHealth);
            }

            UpdateStats();
        }

        if (GameManager.Instance != null)
        {
            UpdateGold(GameManager.Instance.gold);
        }

        if (WaveManager.Instance != null)
        {
            int wave = WaveManager.Instance.GetCurrentWave();

            if (wave != lastWave)
            {
                lastWave = wave;
                UpdateWave(wave);
            }
        }
    }

    public void UpdateHP(float current, float max)
    {
        float ratio = max > 0f ? Mathf.Clamp01(current / max) : 0f;

        if (hpFill != null)
        {
            hpFill.fillAmount = ratio;
        }

        if (hpText != null)
        {
            hpText.text = Mathf.CeilToInt(current) + " / " + Mathf.CeilToInt(max);
        }
    }

    public void UpdateWave(int waveNumber)
    {
        if (waveText != null)
        {
            waveText.text = "Wave " + waveNumber;
        }

        if (announcementGroup == null || announcementText == null)
        {
            return;
        }

        if (announceRoutine != null)
        {
            StopCoroutine(announceRoutine);
        }

        announceRoutine = StartCoroutine(AnnounceWave(waveNumber));
    }

    public void UpdateGold(int amount)
    {
        if (goldText != null)
        {
            goldText.text = amount.ToString();
        }
    }

    public void AddItemToInventory(ItemSO item)
    {
        if (item == null || inventoryIcons == null || nextInventoryIndex >= inventoryIcons.Length)
        {
            return;
        }

        int slot = nextInventoryIndex;

        if (inventoryIcons[slot] != null)
        {
            inventoryIcons[slot].sprite = item.icon;
            inventoryIcons[slot].enabled = item.icon != null;
        }

        if (inventoryBorders != null && slot < inventoryBorders.Length && inventoryBorders[slot] != null)
        {
            inventoryBorders[slot].color = RarityColor(item.rarity);
        }

        nextInventoryIndex++;
    }

    private void UpdateStats()
    {
        if (player == null || statTexts == null || statTexts.Length < 10)
        {
            return;
        }

        statTexts[0].text = player.attackDamage.ToString("0");
        statTexts[1].text = player.abilityPower.ToString("0");
        statTexts[2].text = player.attackSpeed.ToString("0.0");
        statTexts[3].text = player.movementSpeed.ToString("0.0");
        statTexts[4].text = string.Empty;
        statTexts[5].text = player.healthRegen.ToString("0.0");
        statTexts[6].text = player.Mana.ToString("0");
        statTexts[7].text = player.manaRegen.ToString("0.0");
        statTexts[8].text = player.armor.ToString("0");
        statTexts[9].text = player.magicDefense.ToString("0");
    }

    private void ApplyFont()
    {
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts/Nexa-ExtraLight SDF");

        if (font == null)
        {
            return;
        }

        ApplyFontTo(font, hpText);
        ApplyFontTo(font, waveText);
        ApplyFontTo(font, goldText);
        ApplyFontTo(font, announcementText);

        if (statTexts != null)
        {
            foreach (TextMeshProUGUI text in statTexts)
            {
                ApplyFontTo(font, text);
            }
        }
    }

    private void ApplyFontTo(TMP_FontAsset font, TextMeshProUGUI text)
    {
        if (text != null)
        {
            text.font = font;
        }
    }

    private Color RarityColor(ItemSO.Rarity rarity)
    {
        switch (rarity)
        {
            case ItemSO.Rarity.Epic:
                return new Color(0.6f, 0.3f, 0.9f, 1f);
            case ItemSO.Rarity.Excellent:
                return new Color(0.95f, 0.78f, 0.35f, 1f);
            default:
                return new Color(0.5f, 0.5f, 0.5f, 1f);
        }
    }

    private IEnumerator AnnounceWave(int waveNumber)
    {
        announcementText.text = "Wave " + waveNumber;
        announcementGroup.gameObject.SetActive(true);

        yield return Fade(0f, 1f, 0.25f);
        yield return new WaitForSecondsRealtime(2f);
        yield return Fade(1f, 0f, 0.4f);

        announcementGroup.gameObject.SetActive(false);
        announceRoutine = null;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            announcementGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        announcementGroup.alpha = to;
    }
}
