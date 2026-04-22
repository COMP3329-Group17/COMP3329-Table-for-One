using UnityEngine;
using UnityEngine.SceneManagement;

public class MinigameTrigger : MonoBehaviour
{
    [Header("Scene Setup")]
    [Tooltip("Type the exact name of the scene you want to load for THIS object.")]
    public string SceneName;

    public void StartMinigame()
    {
        if (!string.IsNullOrEmpty(SceneName))
        {
            // We use the variable here instead of a hardcoded "Name"
            SceneManager.LoadScene(SceneName);
        }
        else
        {
            Debug.LogError("No scene name assigned to " + gameObject.name);
        }
    }
}