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

        // If already inspecting, let PlayerController handle the 'E' input
        if (pc.currentState != PlayerController.PlayerState.Exploration)
        {
            HideFeedback();
            return;
        }

        // Only run this if we are in Exploration mode
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.SphereCast(ray, sphereRadius, out hit, interactionDistance, ~playerLayer))
        {
            if (((1 << hit.collider.gameObject.layer) & interactableLayer) != 0)
            {
                ItemObject item = hit.collider.GetComponent<ItemObject>();
                if (item != null)
                {
                    ShowFeedback(hit.collider.gameObject);

                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        // This triggers pc.StartInspecting via item.OnInteract
                        item.OnInteract();
                    }
                }
                Lock lockScript = hit.collider.GetComponent<Lock>();
                if (lockScript != null)
                {
                    ShowFeedback(hit.collider.gameObject);
                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        // We pass the currently equipped item to the lock
                        lockScript.AttemptUnlock(InventoryMG.activeItem);
                    }
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