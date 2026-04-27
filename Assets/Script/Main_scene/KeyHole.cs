using UnityEngine;

public class KeyHole : MonoBehaviour
{
    [Header("Requirement")]
    public ItemData requiredItem; // Drag the "Key" ItemData asset here

    [Header("Action")]
    public Animator targetAnimator; // Drag the Door's Animator here
    public string animationTrigger = "Open"; // The name of the Trigger in the Animator

    [Header("Feedback")]
    public string unlockMessage = "Door Opened!";
    public string wrongItemMessage = "I need a specific item for this...";

    public void AttemptUnlock(ItemData playerActiveItem)
    {
        // Check if the player actually has an item selected
        if (playerActiveItem == null)
        {
            Debug.Log("I'm not holding anything.");
            return;
        }

        // Compare the Data assets
        if (playerActiveItem == requiredItem)
        {
            Debug.Log(unlockMessage);
            
            if (targetAnimator != null)
            {
                targetAnimator.SetTrigger(animationTrigger);
            }

            BoxCollider doorCollider = GetComponent<BoxCollider>();
            if (doorCollider != null)
            {
                Destroy(doorCollider);
                // Alternatively, use: doorCollider.enabled = false;
            }
            // REMOVE THE ITEM FROM MEMORY
            InventoryMG.savedItems.Remove(playerActiveItem);

            // CLEAR THE ACTIVE HAND (So the icon disappears from the HUD)
            InventoryMG.activeItem = null;

            // REFRESH THE BACKPACK UI
            // We find the manager in the scene and tell it to redraw the slots
            InventoryMG inv = FindFirstObjectByType<InventoryMG>();
            if (inv != null)
            {
                inv.RefreshUI();
                inv.UpdateActiveSlotUI();
            }
        }
        else
        {
            Debug.Log(wrongItemMessage);
        }
    }
}