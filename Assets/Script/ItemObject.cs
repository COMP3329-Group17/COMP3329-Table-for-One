// Attach this script to any 3D model that we want the player to be able to pick up.
using UnityEngine;

public class ItemObject : MonoBehaviour
{
    public ItemData referenceData; // Drag your ScriptableObject here
    private Renderer objRenderer;
    private Color originalEmissionColor;

    [Header("Highlight Settings")]
    public Color highlightColor = Color.yellow;
    [Range(0, 5)] public float glowIntensity = 2f;

    // Makes an object "glow" when the player looks at it
    // NOT WORKING ATM
    void Start()
    {
        objRenderer = GetComponent<Renderer>();
        // Store the starting color (usually black/off)
        if (objRenderer != null)
        {
            originalEmissionColor = objRenderer.material.GetColor("_EmissionColor");
        }
    }

    public void ToggleHighlight(bool isOn)
    {
        if (objRenderer == null) return;

        if (isOn)
        {
            // Enable the glow by multiplying color by intensity
            objRenderer.material.SetColor("_EmissionColor", highlightColor * glowIntensity);
            objRenderer.material.EnableKeyword("_EMISSION");
        }
        else
        {
            // Turn the glow back to original (off)
            objRenderer.material.SetColor("_EmissionColor", originalEmissionColor);
        }
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
                // Handle item rotation
                player.StartInspecting(this.gameObject);
            }


     
        }
    }
}