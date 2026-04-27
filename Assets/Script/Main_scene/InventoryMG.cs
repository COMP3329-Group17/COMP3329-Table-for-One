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

    // GLOBAL MEMORY: These stay alive during scene transitions
    public static List<ItemData> savedItems = new List<ItemData>();

    // renamed to activeItem to match your PlayerController's request
    public static ItemData activeItem;

    void Start()
    {
        if (inventoryPanel != null) inventoryPanel.SetActive(false);

        // Refresh visuals on start (useful when returning from minigames)
        RefreshUI();
        UpdateActiveSlotUI();
    }

    public void ToggleInventory()
    {
        if (inventoryPanel == null) return;
        bool isOpening = !inventoryPanel.activeSelf;
        inventoryPanel.SetActive(isOpening);

        if (isOpening)
        {
            RefreshUI();
            // Optional: Unlock cursor when inventory is open
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    public void AddItem(ItemData item)
    {
        if (!savedItems.Contains(item))
        {
            savedItems.Add(item);
            RefreshUI();
        }
    }

    public void RefreshUI()
    {
        if (slotParent == null || slotPrefab == null) return;

        // Clear existing visual slots
        foreach (Transform child in slotParent)
        {
            Destroy(child.gameObject);
        }

        // Rebuild icons from static memory
        foreach (ItemData item in savedItems)
        {
            GameObject newSlot = Instantiate(slotPrefab, slotParent);

            // Find the icon image child
            Transform iconTransform = newSlot.transform.Find("Backpack_icon");
            if (iconTransform != null)
            {
                Image icon = iconTransform.GetComponent<Image>();
                icon.sprite = item.icon;
            }

            // Set up the button to select this item
            Button btn = newSlot.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() => SelectItem(item));
            }
        }
    }

    public void SelectItem(ItemData item)
    {
        activeItem = item; // This is what the Door/Lock script looks for
        UpdateActiveSlotUI();
        Debug.Log("Equipped: " + item.itemName);
    }

    public void UpdateActiveSlotUI()
    {
        if (activeIconHUD == null) return;

        if (activeItem == null)
        {
            activeIconHUD.enabled = false;
        }
        else
        {
            activeIconHUD.enabled = true;
            activeIconHUD.sprite = activeItem.icon;
        }
    }
}