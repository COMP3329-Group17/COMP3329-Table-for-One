using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitMinigame : MonoBehaviour
{
    public string mainSceneName = "MainScene"; // The name of your restaurant scene

    void Update()
    {
        // Option A: Exit using the Space key (as we planned)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ReturnToMain();
        }
    }
    public void ReturnToMain()
    {
        SceneManager.LoadScene(mainSceneName);
    }
}
