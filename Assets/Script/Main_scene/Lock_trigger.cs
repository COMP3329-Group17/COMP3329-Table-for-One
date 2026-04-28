using UnityEngine;

public class Lock : MonoBehaviour
{
    [Header("Requirement")]
    public ItemData requiredItem; // Drag the "Key" ItemData asset here

    [Header("Action")]
    public Animator targetAnimator; // Drag the Door's Animator here
    public string animationTrigger = "Open"; // The name of the Trigger in the Animator

    [Header("Feedback")]
    public string unlockMessage = "Door Opened! Starting Minigame...";
    public string wrongItemMessage = "I need a specific item for this...";

    [Header("Minigame Settings")]
    private string targetScene = "Capboard"; // Hardcoded as requested

    public void AttemptUnlock(ItemData playerActiveItem)
    {
        // 1. Check if the player actually has an item selected
        if (playerActiveItem == null)
        {
            Debug.Log("I'm not holding anything.");
            return;
        }

        // 2. Compare the Data assets
        if (playerActiveItem == requiredItem)
        {
            HandleUnlockSequence(playerActiveItem);
        }
        else
        {
            Debug.Log(wrongItemMessage);
        }
    }

    private void HandleUnlockSequence(ItemData itemToUse)
    {
        Debug.Log(unlockMessage);

        // A. Visuals: Trigger Animation
        if (targetAnimator != null)
        {
            targetAnimator.SetTrigger(animationTrigger);
        }

        // B. Physics: Disable Collider so it can't be clicked twice
        Collider myCollider = GetComponent<Collider>();
        if (myCollider != null)
        {
            myCollider.enabled = false;
        }

        // C. Data: Remove the item from the static list
        if (InventoryMG.savedItems.Contains(itemToUse))
        {
            InventoryMG.savedItems.Remove(itemToUse);
        }

        // D. Data: Clear the global active item reference
        InventoryMG.activeItem = null;

        // E. UI: Find manager and refresh visuals so the backpack looks correct
        InventoryMG inv = FindFirstObjectByType<InventoryMG>();
        if (inv != null)
        {
            inv.RefreshUI();
            inv.UpdateActiveSlotUI();
        }

        // F. Transition: Start the minigame
        StartMinigame();
    }

    private void StartMinigame()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();

        if (player != null)
        {
            // The PlayerController handles saving the current position before loading "Capboard"
            player.EnterMinigame(targetScene);
        }
        else
        {
            // Fallback if testing in a scene without the PlayerController
            Debug.LogWarning("PlayerController missing! Loading scene directly.");
            UnityEngine.SceneManagement.SceneManager.LoadScene(targetScene);
        }
    }
}