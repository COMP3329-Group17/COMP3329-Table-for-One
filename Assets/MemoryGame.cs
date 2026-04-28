using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class MemoryGame : MonoBehaviour
{
    [Header("=== Drag Your Objects Here ===")]
    public GameObject[] jars;
    public GameObject lockObject;
    public Camera mainCamera;
    // unlockButton removed
    public GameObject winPanel;

    [Header("=== Camera Zoom Settings ===")]
    public Vector3 zoomPosition = new Vector3(0, 1.8f, 2.5f);
    public float zoomDuration = 1f;

    [Header("=== Game Settings ===")]
    public int sequenceLength = 8;
    public float glowDuration = 0.5f;
    public float delayBetweenGlows = 0.3f;

    private string displayText = "THE JARS REMEMBER. YOU MUST REMEMBER TOO.";

    private List<int> sequence = new List<int>();
    private int currentStep = 0;
    private bool isShowingSequence = false;
    private bool waitingForPlayer = false;
    private bool gameStarted = false;
    private Vector3 originalCameraPos;
    private List<Vector3> originalScales = new List<Vector3>();

    void Start()
    {
        originalCameraPos = mainCamera.transform.position;

        // Button setup removed

        for (int i = 0; i < jars.Length; i++)
        {
            GameObject jar = jars[i];
            if (jar != null)
            {
                jar.SetActive(false);
                originalScales.Add(jar.transform.localScale);

                if (jar.GetComponent<Collider>() == null)
                {
                    BoxCollider collider = jar.AddComponent<BoxCollider>();
                    collider.size = new Vector3(0.5f, 0.8f, 0.5f);
                }

                JarClick click = jar.GetComponent<JarClick>();
                if (click == null) click = jar.AddComponent<JarClick>();
                click.Init(this, i);
            }
        }

        if (winPanel != null) winPanel.SetActive(false);

        // AUTO-START LOGIC: Trigger the intro sequence immediately
        StartCoroutine(DropLockThenZoom());
    }

    void OnGUI()
{
    GUI.skin.box.fontSize = 42;  // Set to 24pt
    float boxWidth = 1500f;
    float boxHeight = 160f;
    float boxX = (Screen.width - boxWidth) / 2;
    float boxY = 120f;
    
    GUI.Box(new Rect(boxX, boxY, boxWidth, boxHeight), displayText);
}

    public void OnJarClicked(int jarIndex)
    {
        if (!gameStarted || !waitingForPlayer || isShowingSequence) return;

        if (jarIndex == sequence[currentStep])
        {
            StartCoroutine(ScaleJar(jarIndex));
            currentStep++;

            displayText = $"Correct! ({currentStep}/{sequence.Count})";
            StartCoroutine(ResetDisplayAfterDelay(0.8f));

            if (currentStep >= sequence.Count)
            {
                WinGame();
            }
        }
        else
        {
            StartCoroutine(WrongAnswer());
        }
    }

    IEnumerator ResetDisplayAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!gameStarted || !waitingForPlayer) yield break;
        displayText = $"Click jars in order: {currentStep + 1}/{sequence.Count}";
    }

    // UseKey function removed

    IEnumerator ScaleJar(int jarIndex)
    {
        Vector3 originalScale = originalScales[jarIndex];
        jars[jarIndex].transform.localScale = originalScale * 1.3f;
        yield return new WaitForSeconds(0.15f);
        jars[jarIndex].transform.localScale = originalScale;
    }

    IEnumerator DropLockThenZoom()
    {
        // Added a tiny delay so the player can see the scene before it starts moving
        yield return new WaitForSeconds(0.5f);

        if (lockObject != null)
        {
            Rigidbody rb = lockObject.GetComponent<Rigidbody>();
            if (rb == null) rb = lockObject.AddComponent<Rigidbody>();
            rb.useGravity = true;
            rb.AddForce(Vector3.down * 3f, ForceMode.Impulse);
        }

        yield return new WaitForSeconds(0.8f);

        float elapsed = 0;
        Vector3 startPos = mainCamera.transform.position;

        while (elapsed < zoomDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / zoomDuration;
            mainCamera.transform.position = Vector3.Lerp(startPos, zoomPosition, t);
            yield return null;
        }

        foreach (GameObject jar in jars) if (jar != null) jar.SetActive(true);

        StartCoroutine(StartGame());
    }

    IEnumerator StartGame()
    {
        gameStarted = true;
        displayText = "Watch the sequence...";
        yield return new WaitForSeconds(2f);

        sequence.Clear();
        for (int i = 0; i < sequenceLength; i++)
            sequence.Add(Random.Range(0, jars.Length));

        Debug.Log("SEQUENCE: " + string.Join(" → ", sequence));

        isShowingSequence = true;
        displayText = "";

        foreach (int jarIndex in sequence)
        {
            yield return StartCoroutine(ScaleJar(jarIndex));
            yield return new WaitForSeconds(delayBetweenGlows);
        }

        isShowingSequence = false;
        waitingForPlayer = true;
        currentStep = 0;

        displayText = $"Your turn! Click jars in order (1/{sequence.Count})";
        Debug.Log("Waiting for player input...");
    }

    IEnumerator WrongAnswer()
    {
        waitingForPlayer = false;
        displayText = "Wrong! Restarting sequence...";
        yield return new WaitForSeconds(1.5f);

        currentStep = 0;
        isShowingSequence = true;
        displayText = "Watch again...";

        foreach (int jarIndex in sequence)
        {
            yield return StartCoroutine(ScaleJar(jarIndex));
            yield return new WaitForSeconds(delayBetweenGlows);
        }

        isShowingSequence = false;
        waitingForPlayer = true;
        displayText = $"Try again! (1/{sequence.Count})";
    }

    void WinGame()
    {
        gameStarted = false;
        waitingForPlayer = false;
        displayText = "";

        if (winPanel != null) winPanel.SetActive(true);

        PlayerPrefs.SetInt("MemoryGameWin", 1);
        PlayerPrefs.Save();

        Debug.Log("YOU WIN!");
    }
}

public class JarClick : MonoBehaviour
{
    private MemoryGame game;
    private int jarIndex;

    public void Init(MemoryGame g, int index)
    {
        game = g;
        jarIndex = index;
    }

    void OnMouseDown()
    {
        if (game != null) game.OnJarClicked(jarIndex);
    }
}