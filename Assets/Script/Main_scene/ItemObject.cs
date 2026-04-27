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
        // 1. PERSISTENCE CHECK
        // If this item's name is in our list, it was picked up in a previous session.
        if (pickedUpItems.Contains(gameObject.name))
        {
            Destroy(gameObject);
            return;
        }

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
        InventoryMG inventory = FindFirstObjectByType<InventoryMG>();
        PlayerController player = FindFirstObjectByType<PlayerController>();

        if (inventory != null)
        {
            inventory.AddItem(referenceData);

            // 2. ADD TO MEMORY
            if (!pickedUpItems.Contains(gameObject.name))
            {
                pickedUpItems.Add(gameObject.name);
            }

            if (player != null)
            {
                player.StartInspecting(this.gameObject);
            }
        }
    }
}