using UnityEngine;

public class MinigameTrigger : MonoBehaviour
{
    [Header("Scene Setup")]
    public string SceneName;

    public void StartMinigame()
    {
        if (string.IsNullOrEmpty(SceneName))
        {
            Debug.LogError("No scene name assigned to " + gameObject.name);
            return;
        }

        // FIND the player in the scene
        PlayerController player = FindFirstObjectByType<PlayerController>();

        if (player != null)
        {
            // USE the player's function because IT handles the saving logic
            player.EnterMinigame(SceneName);
        }
        else
        {
            // Fallback: If no player is found, just load the scene anyway
            Debug.LogWarning("PlayerController not found! Position will NOT be saved.");
            UnityEngine.SceneManagement.SceneManager.LoadScene(SceneName);
        }
    }
}