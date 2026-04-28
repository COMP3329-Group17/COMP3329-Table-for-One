using UnityEngine;
using System.Collections.Generic;

public class ItemObject : MonoBehaviour
{
    public ItemData referenceData;
    private Renderer objRenderer;
    private Color originalEmissionColor;

    [Header("Highlight Settings")]
    public Color highlightColor = Color.yellow;
    [Range(0, 5)] public float glowIntensity = 2f;

    // GLOBAL MEMORY: Static variables stay in memory across scene changes.
    private static List<string> pickedUpItems = new List<string>();

    void Start()
    {
        // 1. PERSISTENCE CHECK (By Data)
        // Check if this item is already in the static inventory list
        if (InventoryMG.savedItems != null)
        {
            foreach (ItemData item in InventoryMG.savedItems)
            {
                if (item == referenceData)
                {
                    Destroy(gameObject);
                    return;
                }
            }
        }

        // 2. PERSISTENCE CHECK (By Name backup)
        if (pickedUpItems.Contains(gameObject.name))
        {
            Destroy(gameObject);
            return;
        }

        // Setup Renderer for Highlighting
        objRenderer = GetComponent<Renderer>();
        if (objRenderer != null)
        {
            // Ensure the material can actually glow
            objRenderer.material.EnableKeyword("_EMISSION");
            originalEmissionColor = objRenderer.material.GetColor("_EmissionColor");
        }
    }

    public void ToggleHighlight(bool isOn)
    {
        if (objRenderer == null) return;

        if (isOn)
        {
            objRenderer.material.SetColor("_EmissionColor", highlightColor * glowIntensity);
        }
        else
        {
            objRenderer.material.SetColor("_EmissionColor", originalEmissionColor);
        }
    }

    public void OnInteract()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();

        if (player != null)
        {
            // Only trigger the start of inspection. 
            // The item is added to InventoryMG and destroyed inside PlayerController.CollectItem()
            player.StartInspecting(this.gameObject);
        }
    }

    // This can be called by the PlayerController when the item is finally collected
    public void RegisterPickup()
    {
        if (!pickedUpItems.Contains(gameObject.name))
        {
            pickedUpItems.Add(gameObject.name);
        }
    }
}