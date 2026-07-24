using UnityEngine;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

    public Image[] inventoryIcons;
    public Image[] inventoryBorders;
    public GameObject inventoryRoot;
    public GameObject[] inventorySlots;

    private int nextInventoryIndex;

    private void Awake()
    {
        Instance = this;
        SetInventoryVisibility();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void AddItemToInventory(ItemSO item)
    {
        if (item == null || inventoryIcons == null || nextInventoryIndex >= inventoryIcons.Length)
        {
            return;
        }

        int slot = nextInventoryIndex;

        if (inventoryRoot != null)
        {
            inventoryRoot.SetActive(true);
        }

        if (inventorySlots != null && slot < inventorySlots.Length && inventorySlots[slot] != null)
        {
            inventorySlots[slot].SetActive(true);
        }

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

    private void SetInventoryVisibility()
    {
        if (inventoryRoot != null)
        {
            inventoryRoot.SetActive(false);
        }

        if (inventorySlots == null)
        {
            return;
        }

        foreach (GameObject slot in inventorySlots)
        {
            if (slot != null)
            {
                slot.SetActive(false);
            }
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
}
