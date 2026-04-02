// Attach this script to any 3D model that we want the player to be able to pick up.
using UnityEngine;

public class ItemObject : MonoBehaviour
{
    public ItemData referenceData; // Drag your ScriptableObject here
    public GameObject hoverLight; // Drag a Point Light child here in the Inspector

    // Makes an object "glow" when the player looks at it
    // NOT WORKING ATM
    public void ToggleHighlight(bool state)
    {
        if (hoverLight != null) hoverLight.SetActive(state);
    }

    // This function runs when the player presses 'E' on the object.
    public void OnInteract()
    {
        // Find the inventory in the scene and add this data
        InventoryMG inventory = FindFirstObjectByType<InventoryMG>();
        PlayerController player = FindFirstObjectByType<PlayerController>();

        // If we found the inventory, proceed with picking it up.
        if (inventory != null)
        {
            inventory.AddItem(referenceData);

            // If the player script exists, switch them to 'Inspecting' mode (freezes movement).
            if (player != null)
            {
                player.SetState(PlayerController.PlayerState.Inspecting);
                Debug.Log("Now inspecting: " + referenceData.itemName);
            }

            // Hide the object from the world since it's now 'in the pocket'
            gameObject.SetActive(false);
        }
    }
}