using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class InventoryMG : MonoBehaviour
{
    [Header("UI Windows")]
    public GameObject inventoryPanel;
    public Image activeIconHUD;

    [Header("Grid Setup")]
    public Transform slotParent;
    public GameObject slotPrefab;

    // GLOBAL MEMORY: This list stays alive even if the script is reset
    public static List<ItemData> savedItems = new List<ItemData>();
    public static ItemData savedHeldItem;

    void Start()
    {
        if (inventoryPanel != null) inventoryPanel.SetActive(false);

        // When the scene loads, refresh the UI using the static memory
        RefreshUI();
        UpdateActiveSlotUI();
    }

    public void ToggleInventory()
    {
        if (inventoryPanel == null) return;
        bool isOpening = !inventoryPanel.activeSelf;
        inventoryPanel.SetActive(isOpening);
        if (isOpening) RefreshUI();
    }

    public void AddItem(ItemData item)
    {
        // Add to the static list, not a local one
        savedItems.Add(item);
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (slotParent == null || slotPrefab == null) return;

        // Clear existing visual slots
        foreach (Transform child in slotParent) Destroy(child.gameObject);

        // Rebuild from static memory
        foreach (ItemData item in savedItems)
        {
            GameObject newSlot = Instantiate(slotPrefab, slotParent);
            Image icon = newSlot.transform.Find("Backpack_icon").GetComponent<Image>();
            icon.sprite = item.icon;

            Button btn = newSlot.GetComponent<Button>();
            btn.onClick.AddListener(() => SelectItem(item));
        }
    }

    void SelectItem(ItemData item)
    {
        savedHeldItem = item;
        UpdateActiveSlotUI();
    }

    void UpdateActiveSlotUI()
    {
        if (activeIconHUD == null) return;

        if (savedHeldItem == null)
        {
            activeIconHUD.enabled = false;
        }
        else
        {
            activeIconHUD.enabled = true;
            activeIconHUD.sprite = savedHeldItem.icon;
        }
    }
}