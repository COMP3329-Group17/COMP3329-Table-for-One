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

            // Optional: Remove the item after use
            // InventoryMG.Instance.RemoveItem(playerActiveItem); 
        }
        else
        {
            Debug.Log(wrongItemMessage);
        }
    }
}