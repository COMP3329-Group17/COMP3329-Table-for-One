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
        // Only allow interaction if we aren't already busy inspecting something
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        if (pc != null && pc.currentState != PlayerController.PlayerState.Exploration)
        {
            HideFeedback();
            return;
        }

        // Shoot a ray from the center of the screen
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;
        // This tells the ray: "Hit everything EXCEPT the player."
        if (Physics.SphereCast(ray, sphereRadius, out hit, interactionDistance, ~playerLayer))
        {

            // Check if the ray is hitting on a interectable object 
            if (((1 << hit.collider.gameObject.layer) & interactableLayer) != 0)
            {
                // trigger a 'Press E to Pick Up' UI text here
                ShowFeedback(hit.collider.gameObject);

                //if the player click "E"
                if (Input.GetKeyDown(KeyCode.E))
                {
                    ItemObject item = hit.collider.GetComponent<ItemObject>();
                    if (item != null)
                    {
                        HideFeedback();
                        item.OnInteract();
                    }
                }
            }
            else
            {
                HideFeedback();

            }
        }
        else
        {
            HideFeedback();
        }
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