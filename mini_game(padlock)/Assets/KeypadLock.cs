using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class KeypadLock : MonoBehaviour
{
    [Header("Code Settings")]
    [SerializeField] private string correctCode = "1234";
    [SerializeField] private int maxCodeLength = 4;
    
    [Header("Audio (Optional)")]
    [SerializeField] private AudioClip buttonPressSound;
    [SerializeField] private AudioClip correctSound;
    [SerializeField] private AudioClip wrongSound;
    [Range(0, 1)] [SerializeField] private float soundVolume = 0.7f;
    
    [Header("Lights (Optional)")]
    [SerializeField] private GameObject greenLight;
    [SerializeField] private GameObject redLight;
    [SerializeField] private float flashDuration = 0.3f;
    [SerializeField] private int flashCount = 3;
    
    [Header("Door Settings")]
    [SerializeField] private GameObject doorObject;
    [SerializeField] private float doorSlideDistance = 5f;   // How far to slide (5 = off-screen)
    [SerializeField] private float doorSlideDuration = 2f;   // Slow: 2 seconds for heavy door
    [SerializeField] private bool slideLeft = true;          // true = left, false = right
    
    [Header("References (Optional)")]
    [SerializeField] private Text displayText;
    [SerializeField] private GameObject unlockEffect;
    
    // Private variables
    private string currentInput = "";
    private AudioSource audioSource;
    private bool isUnlocked = false;
    private Material greenLightMaterial;
    private Material redLightMaterial;
    private Vector3 originalDoorPosition;
    
    void Start()
    {
        // Save original door position for reset
        if (doorObject != null)
        {
            originalDoorPosition = doorObject.transform.position;
        }
        
        // Setup Audio Source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && buttonPressSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Setup Green Light Material
        if (greenLight != null)
        {
            Renderer renderer = greenLight.GetComponent<Renderer>();
            if (renderer != null)
            {
                greenLightMaterial = renderer.material;
                greenLightMaterial.SetColor("_EmissionColor", Color.black);
            }
        }
        
        // Setup Red Light Material
        if (redLight != null)
        {
            Renderer renderer = redLight.GetComponent<Renderer>();
            if (renderer != null)
            {
                redLightMaterial = renderer.material;
                redLightMaterial.SetColor("_EmissionColor", Color.black);
            }
        }
        
        UpdateDisplay();
    }
    
    void Update()
    {
        // Raycast for mouse clicks on 3D colliders
        if (Input.GetMouseButtonDown(0) && !isUnlocked)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit))
            {
                string buttonName = hit.collider.gameObject.name;
                Debug.Log("Clicked: " + buttonName);
                
                // Check for number buttons
                if (buttonName == "0" || buttonName == "1" || buttonName == "2" || 
                    buttonName == "3" || buttonName == "4" || buttonName == "5" || 
                    buttonName == "6" || buttonName == "7" || buttonName == "8" || 
                    buttonName == "9")
                {
                    int number = int.Parse(buttonName);
                    PressNumber(number);
                }
                // Check for # button
                else if (buttonName == "#")
                {
                    PressHash();
                }
                // Check for * button
                else if (buttonName == "*")
                {
                    PressStar();
                }
                // Check for Clear button
                else if (buttonName == "Clear" || buttonName == "clear")
                {
                    PressClear();
                }
            }
        }
    }
    
    public void PressNumber(int number)
    {
        if (isUnlocked) return;
        
        PlaySound(buttonPressSound);
        
        if (currentInput.Length < maxCodeLength)
        {
            currentInput += number.ToString();
            UpdateDisplay();
            Debug.Log("Current Input: " + currentInput);
            
            // Auto-check after reaching max code length
            if (currentInput.Length == maxCodeLength)
            {
                Debug.Log("Auto-checking code...");
                CheckCode();
            }
        }
    }
    
    public void PressHash()
    {
        if (isUnlocked) return;
        PlaySound(buttonPressSound);
        Debug.Log("# pressed");
        // Uncomment to make # act like Enter:
        // CheckCode();
    }
    
    public void PressStar()
    {
        if (isUnlocked) return;
        PlaySound(buttonPressSound);
        Debug.Log("* pressed");
        // Makes * delete last digit (backspace)
        if (currentInput.Length > 0)
        {
            currentInput = currentInput.Substring(0, currentInput.Length - 1);
            UpdateDisplay();
        }
    }
    
    public void PressClear()
    {
        if (isUnlocked) return;
        
        PlaySound(buttonPressSound);
        currentInput = "";
        UpdateDisplay();
        Debug.Log("Cleared input");
    }
    
    private void CheckCode()
    {
        if (currentInput == correctCode)
        {
            UnlockSuccess();
        }
        else
        {
            WrongCode();
        }
    }
    
    private void UnlockSuccess()
    {
        isUnlocked = true;
        PlaySound(correctSound);
        
        // Flash green light and keep it on
        StartCoroutine(FlashLightAndStay(greenLight, greenLightMaterial, Color.green));
        
        // Turn off red light
        if (redLightMaterial != null)
        {
            redLightMaterial.SetColor("_EmissionColor", Color.black);
        }
        
        // Update display
        if (displayText != null)
        {
            displayText.text = "UNLOCKED!";
            displayText.color = Color.green;
        }
        
        // Slide the door horizontally (lock and lights move with door if they are children)
        if (doorObject != null)
        {
            // Disable door collider so player can walk through
            Collider doorCollider = doorObject.GetComponent<Collider>();
            if (doorCollider != null) doorCollider.enabled = false;
            
            // Slide door horizontally (right to left)
            StartCoroutine(MoveDoorHorizontal(doorObject, doorSlideDistance, doorSlideDuration, slideLeft));
        }
        
        // Spawn unlock effect
        if (unlockEffect != null)
        {
            Instantiate(unlockEffect, transform.position, Quaternion.identity);
        }
        
        Debug.Log("LOCK UNLOCKED!");
    }
    
    private void WrongCode()
    {
        PlaySound(wrongSound);
        
        // Flash red light
        StartCoroutine(FlashLight(redLight, redLightMaterial, Color.red));
        
        // Flash display red
        if (displayText != null)
        {
            StartCoroutine(FlashDisplayRed());
        }
        
        // Clear input
        currentInput = "";
        UpdateDisplay();
        
        Debug.Log("Wrong code entered!");
    }
    
    // COROUTINE: Move door horizontally (right to left or left to right)
    private IEnumerator MoveDoorHorizontal(GameObject door, float moveDistance, float duration, bool moveLeft)
    {
        Vector3 startPos = door.transform.position;
        Vector3 endPos;
        
        if (moveLeft)
        {
            // Move RIGHT to LEFT (negative X)
            endPos = startPos + new Vector3(-moveDistance, 0, 0);
        }
        else
        {
            // Move LEFT to RIGHT (positive X)
            endPos = startPos + new Vector3(moveDistance, 0, 0);
        }
        
        float elapsed = 0;
        
        // Slow, smooth movement
        while (elapsed < duration)
        {
            // Use SmoothStep for more realistic heavy door movement
            float t = Mathf.SmoothStep(0, 1, elapsed / duration);
            door.transform.position = Vector3.Lerp(startPos, endPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        door.transform.position = endPos;
    }
    
    // COROUTINE: Flash light temporarily
    private IEnumerator FlashLight(GameObject lightObject, Material lightMaterial, Color flashColor)
    {
        if (lightMaterial == null) yield break;
        
        for (int i = 0; i < flashCount; i++)
        {
            lightMaterial.SetColor("_EmissionColor", flashColor * 2f);
            yield return new WaitForSeconds(flashDuration / (flashCount * 2));
            lightMaterial.SetColor("_EmissionColor", Color.black);
            yield return new WaitForSeconds(flashDuration / (flashCount * 2));
        }
    }
    
    // COROUTINE: Flash light and stay on
    private IEnumerator FlashLightAndStay(GameObject lightObject, Material lightMaterial, Color color)
    {
        if (lightMaterial == null) yield break;
        
        for (int i = 0; i < flashCount; i++)
        {
            lightMaterial.SetColor("_EmissionColor", color * 2f);
            yield return new WaitForSeconds(flashDuration / (flashCount * 2));
            lightMaterial.SetColor("_EmissionColor", color);
            yield return new WaitForSeconds(flashDuration / (flashCount * 2));
        }
    }
    
    // COROUTINE: Flash display red
    private IEnumerator FlashDisplayRed()
    {
        if (displayText == null) yield break;
        
        Color originalColor = displayText.color;
        displayText.color = Color.red;
        yield return new WaitForSeconds(0.2f);
        displayText.color = originalColor;
    }
    
    // Update the display with asterisks
    private void UpdateDisplay()
    {
        if (displayText != null)
        {
            string display = "";
            foreach (char c in currentInput)
            {
                display += "*";
            }
            displayText.text = display.PadRight(maxCodeLength, '_');
        }
    }
    
    // Play a sound effect
    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, soundVolume);
        }
    }
    
    // Public method to reset the lock
    public void ResetLock()
    {
        isUnlocked = false;
        currentInput = "";
        UpdateDisplay();
        
        if (displayText != null)
        {
            displayText.color = Color.white;
            displayText.text = "____";
        }
        
        if (greenLightMaterial != null)
        {
            greenLightMaterial.SetColor("_EmissionColor", Color.black);
        }
        
        if (redLightMaterial != null)
        {
            redLightMaterial.SetColor("_EmissionColor", Color.black);
        }
        
        // Reset door position
        if (doorObject != null)
        {
            doorObject.transform.position = originalDoorPosition;
            
            Collider doorCollider = doorObject.GetComponent<Collider>();
            if (doorCollider != null) doorCollider.enabled = true;
        }
        
        Debug.Log("Lock reset");
    }
    
    // Check if unlocked
    public bool IsUnlocked()
    {
        return isUnlocked;
    }
}