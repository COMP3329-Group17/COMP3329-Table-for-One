using UnityEngine;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    public float interactionDistance = 4f; // How far the player can reach
    public float sphereRadius = 0.3f; // The area of the ray shot from the camera
    public LayerMask interactableLayer;    // Set this to a layer called 'Interactable'
    public LayerMask playerLayer;

    [Header("UI & Feedback")]
    public GameObject interactPromptUI;
    private GameObject lastHighlightedObject;

    void Update()
    {
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        if (pc == null) return;

        if (pc.currentState != PlayerController.PlayerState.Exploration)
        {
            HideFeedback();
            return;
        }

        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.SphereCast(ray, sphereRadius, out hit, interactionDistance, ~playerLayer))
        {
            // Check if the layer is correct
            if (((1 << hit.collider.gameObject.layer) & interactableLayer) != 0)
            {
                ShowFeedback(hit.collider.gameObject);

                if (Input.GetKeyDown(KeyCode.E))
                {
                    // 1. Check for normal Items (Inspect)
                    ItemObject item = hit.collider.GetComponent<ItemObject>();
                    if (item != null) item.OnInteract();

                    // 2. Check for Locks (Use Key)
                    Lock lockScript = hit.collider.GetComponent<Lock>();
                    if (lockScript != null) lockScript.AttemptUnlock(InventoryMG.activeItem);

                    // 3. Check for direct Minigame Triggers (like the Cupboard/Table)
                    MinigameTrigger trigger = hit.collider.GetComponent<MinigameTrigger>();
                    if (trigger != null) trigger.StartMinigame();
                }
            }
            else { HideFeedback(); }
        }
        else { HideFeedback(); }
    }

    // UI function
    void ShowFeedback(GameObject obj)
    {
        // Show "Press E" UI
        if (interactPromptUI != null) interactPromptUI.SetActive(true);

        // Get the Item component and turn on its highlight light.
        ItemObject item = obj.GetComponent<ItemObject>();
        if (item != null) item.ToggleHighlight(true);
        lastHighlightedObject = obj;
    }

    void HideFeedback()
    {
        if (interactPromptUI != null) interactPromptUI.SetActive(false);
        if (lastHighlightedObject != null)
        {
            ItemObject item = lastHighlightedObject.GetComponent<ItemObject>();
            if (item != null) item.ToggleHighlight(false);
        }
        lastHighlightedObject = null;
    }
}