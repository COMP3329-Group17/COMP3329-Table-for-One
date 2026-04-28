using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class ZodiacOrderGame : MonoBehaviour
{
    [Header("Steamers (拖进来)")]
    public List<GameObject> steamers = new List<GameObject>(); // Drag all 12 zodiac steamers in order
    
    [Header("Table & Positions")]
    public Transform table; // Your table object
    public List<Transform> targetCircles = new List<Transform>(); // Drag the 12 circle positions in order
    
    [Header("Settings")]
    public float snapDistance = 1.5f;
    public float snapHeightRange = 1.5f; // Y-axis range for snapping
    public float placementHeight = 0.5f; // Height above table/circle
    public float dragSpeed = 1f; // Speed for Y-axis movement while dragging
    
    [Header("Table Height (IMPORTANT - Set these correctly)")]
    public float tableTopY = 0.5f; // The Y position of your table's top surface
    public float steamerMinY = 0.6f; // Minimum Y (table top + small gap)
    public float steamerMaxY = 3.5f; // Maximum Y height
    
    [Header("Victory Popup")]
    public GameObject victoryPanel; // Drag your UI panel here
    public string victoryMessage = "The pig that stands on two legs is the tastiest!";
    private bool victoryShown = false;
    
    private GameObject selectedSteamer = null;
    private bool isDragging = false;
    private Vector3 dragOffset;
    private Vector3 lastMousePosition;
    private Camera mainCam;
    
    // Track which steamer belongs in which circle
    private Dictionary<GameObject, int> correctOrder = new Dictionary<GameObject, int>();
    private Dictionary<Transform, GameObject> currentPlacements = new Dictionary<Transform, GameObject>();
    
    // Store original positions for reset functionality
    private Dictionary<GameObject, Vector3> originalPositions = new Dictionary<GameObject, Vector3>();
    
    private string instructionsText = "Restore the forgotten cycle. Twelve celestial beasts. One order.";
    void Start()
    {
        mainCam = Camera.main;
        
        // Setup correct order (steamers list should be in chronological order: rat, ox, tiger, etc.)
        for (int i = 0; i < steamers.Count; i++)
        {
            if (steamers[i] != null)
            {
                correctOrder[steamers[i]] = i;
                SetupSteamer(steamers[i]);
                
                // Store original position
                originalPositions[steamers[i]] = steamers[i].transform.position;
                
                // Force all steamers to start above the table
                Vector3 startPos = steamers[i].transform.position;
                startPos.y = Mathf.Max(startPos.y, steamerMinY);
                steamers[i].transform.position = startPos;
            }
        }
        
        // Initialize circle tracking
        for (int i = 0; i < targetCircles.Count; i++)
        {
            currentPlacements[targetCircles[i]] = null;
        }
        
        // Update all circle colors on start
        UpdateAllCircleColors();
        
        // Hide victory panel at start
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(false);
        }
        victoryShown = false;
    }
    
    void SetupSteamer(GameObject obj)
    {
        if (obj == null) return;
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb == null) rb = obj.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        if (obj.GetComponent<Collider>() == null)
            obj.AddComponent<BoxCollider>();
    }
    
    void Update()
    {
        // Pick up steamer
        if (Input.GetMouseButtonDown(0) && !isDragging)
        {
            Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                GameObject hitSteamer = GetSteamerRoot(hit.collider.gameObject);
                if (hitSteamer != null)
                {
                    selectedSteamer = hitSteamer;
                    isDragging = true;
                    lastMousePosition = Input.mousePosition;
                    
                    // Remove from current placement if it was on a circle
                    foreach (var kvp in currentPlacements)
                    {
                        if (kvp.Value == selectedSteamer)
                        {
                            currentPlacements[kvp.Key] = null;
                            break;
                        }
                    }
                    
                    // Calculate drag offset on plane
                    Plane dragPlane = new Plane(Vector3.up, selectedSteamer.transform.position);
                    float distance;
                    Ray mouseRay = mainCam.ScreenPointToRay(Input.mousePosition);
                    if (dragPlane.Raycast(mouseRay, out distance))
                    {
                        Vector3 hitPoint = mouseRay.GetPoint(distance);
                        dragOffset = selectedSteamer.transform.position - hitPoint;
                    }
                }
            }
        }
        
        // Drag steamer
        if (isDragging && selectedSteamer != null)
        {
            // Allow Y-axis movement with mouse delta
            Vector3 mouseDelta = Input.mousePosition - lastMousePosition;
            float yMovement = mouseDelta.y * dragSpeed * Time.deltaTime;
            
            // Get X and Z from plane dragging
            Plane dragPlane = new Plane(Vector3.up, selectedSteamer.transform.position);
            float distance;
            Ray mouseRay = mainCam.ScreenPointToRay(Input.mousePosition);
            if (dragPlane.Raycast(mouseRay, out distance))
            {
                Vector3 hitPoint = mouseRay.GetPoint(distance);
                Vector3 newPos = hitPoint + dragOffset;
                
                // Apply Y movement
                newPos.y = selectedSteamer.transform.position.y + yMovement;
                
                // FORCE CLAMP Y POSITION - THIS PREVENTS GOING UNDER TABLE
                newPos.y = Mathf.Clamp(newPos.y, steamerMinY, steamerMaxY);
                
                selectedSteamer.transform.position = newPos;
            }
            
            lastMousePosition = Input.mousePosition;
            
            // Update highlights every frame while dragging
            UpdateAllCircleColors();
            HighlightNearestCircle();
        }
        
        // Release steamer
        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            TryPlaceAtCircle();
            isDragging = false;
            selectedSteamer = null;
            
            // Final color update after placement
            UpdateAllCircleColors();
            
            // Check victory AFTER placing a steamer
            CheckVictory();
        }
        
        // EXTRA SAFETY: Every frame, ensure ALL steamers are above the table
        foreach (var steamer in steamers)
        {
            if (steamer != null && !isDragging)
            {
                Vector3 pos = steamer.transform.position;
                if (pos.y < steamerMinY)
                {
                    pos.y = steamerMinY;
                    steamer.transform.position = pos;
                }
            }
        }
    }
    
    void TryPlaceAtCircle()
    {
        if (selectedSteamer == null) return;
        
        Transform nearestCircle = GetNearestCircle();
        
        if (nearestCircle != null)
        {
            // Check if circle is already occupied
            if (currentPlacements[nearestCircle] != null)
            {
                Debug.Log("Circle already occupied! Cannot place here.");
                return;
            }
            
            // Place current steamer on the circle
            Vector3 targetPos = nearestCircle.position;
            targetPos.y = placementHeight;
            selectedSteamer.transform.position = targetPos;
            
            // Record placement
            currentPlacements[nearestCircle] = selectedSteamer;
            
            // Check if correct order
            int circleIndex = targetCircles.IndexOf(nearestCircle);
            int actualSteamerIndex = correctOrder[selectedSteamer];
            
            if (actualSteamerIndex == circleIndex)
            {
                Debug.Log($"✓ Correct! {selectedSteamer.name} placed in position {circleIndex + 1}");
            }
            else
            {
                Debug.Log($"✗ Wrong! {selectedSteamer.name} should go in position {actualSteamerIndex + 1}, not {circleIndex + 1}");
            }
            
            // Update circle colors immediately after placement
            UpdateAllCircleColors();
        }
        else
        {
            Debug.Log("No circle nearby - steamer stays where it is");
        }
    }
    
    Transform GetNearestCircle()
    {
        Transform closest = null;
        float closestDist = snapDistance;
        
        foreach (var circle in targetCircles)
        {
            if (circle == null) continue;
            
            // Check both horizontal AND vertical distance for snapping
            float horizontalDist = Vector2.Distance(
                new Vector2(selectedSteamer.transform.position.x, selectedSteamer.transform.position.z),
                new Vector2(circle.position.x, circle.position.z));
            
            float verticalDist = Mathf.Abs(selectedSteamer.transform.position.y - placementHeight);
            
            // Only consider circle if within horizontal range AND within vertical range
            if (horizontalDist < snapDistance && verticalDist < snapHeightRange)
            {
                // Use combined distance for finding closest
                float combinedDist = horizontalDist + verticalDist;
                if (combinedDist < closestDist)
                {
                    closestDist = combinedDist;
                    closest = circle;
                }
            }
        }
        
        return closest;
    }
    
    void UpdateAllCircleColors()
    {
        foreach (var circle in targetCircles)
        {
            if (circle == null) continue;
            
            if (currentPlacements[circle] != null)
            {
                // Circle is occupied - check if correct or wrong
                int circleIndex = targetCircles.IndexOf(circle);
                GameObject placedSteamer = currentPlacements[circle];
                int actualSteamerIndex = correctOrder[placedSteamer];
                
                if (actualSteamerIndex == circleIndex)
                {
                    SetCircleColor(circle, Color.green);
                }
                else
                {
                    SetCircleColor(circle, Color.red);
                }
            }
            else
            {
                SetCircleColor(circle, Color.gray);
            }
        }
    }
    
    void SetCircleColor(Transform circle, Color color)
    {
        SpriteRenderer sr = circle.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = color;
        }
        
        Renderer renderer = circle.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
        }
    }
    
    void HighlightNearestCircle()
    {
        if (selectedSteamer == null) return;
        
        Transform nearest = null;
        float closestDist = snapDistance;
        
        foreach (var circle in targetCircles)
        {
            if (circle == null) continue;
            if (currentPlacements[circle] != null) continue; // Skip occupied circles
            
            float horizontalDist = Vector2.Distance(
                new Vector2(selectedSteamer.transform.position.x, selectedSteamer.transform.position.z),
                new Vector2(circle.position.x, circle.position.z));
            
            float verticalDist = Mathf.Abs(selectedSteamer.transform.position.y - placementHeight);
            
            if (horizontalDist < snapDistance && verticalDist < snapHeightRange)
            {
                float combinedDist = horizontalDist + verticalDist;
                if (combinedDist < closestDist)
                {
                    closestDist = combinedDist;
                    nearest = circle;
                }
            }
        }
        
        if (nearest != null)
        {
            SetCircleColor(nearest, Color.yellow);
        }
    }
    
    GameObject GetSteamerRoot(GameObject hit)
    {
        Transform t = hit.transform;
        while (t != null)
        {
            foreach (var steamer in steamers)
            {
                if (t.gameObject == steamer) return steamer;
            }
            t = t.parent;
        }
        return null;
    }
    
    // Public method to check if all are placed correctly
    public bool CheckAllCorrect()
    {
        for (int i = 0; i < targetCircles.Count; i++)
        {
            if (currentPlacements[targetCircles[i]] == null) return false;
            if (correctOrder[currentPlacements[targetCircles[i]]] != i) return false;
        }
        return true;
    }
    
    // Victory check method
    void CheckVictory()
    {
        // Only check if victory hasn't been shown yet
        if (victoryShown) return;
        
        // Check if all steamers are placed correctly
        if (CheckAllCorrect())
        {
            victoryShown = true;
            
            // Show the victory panel
            if (victoryPanel != null)
            {
                victoryPanel.SetActive(true);
                
                // Find and set the message text
                TextMeshProUGUI textComponent = victoryPanel.GetComponentInChildren<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.text = victoryMessage;
                }
                else
                {
                    // If using regular UI Text
                    Text uiText = victoryPanel.GetComponentInChildren<Text>();
                    if (uiText != null)
                    {
                        uiText.text = victoryMessage;
                    }
                }
            }
            
            Debug.Log($"🎉 VICTORY! {victoryMessage} 🎉");
        }
    }
    
    // Reset game - moves all steamers back to start
    public void ResetGame()
    {
        foreach (var circle in targetCircles)
        {
            currentPlacements[circle] = null;
        }
        
        foreach (var steamer in steamers)
        {
            if (steamer != null && originalPositions.ContainsKey(steamer))
            {
                Vector3 resetPos = originalPositions[steamer];
                resetPos.y = Mathf.Max(resetPos.y, steamerMinY);
                steamer.transform.position = resetPos;
            }
        }
        
        // Reset victory flag and hide panel
        victoryShown = false;
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(false);
        }
        
        UpdateAllCircleColors();
        Debug.Log("Game reset! All steamers returned to start positions.");
    }
void OnGUI()
{
    GUI.skin.box.fontSize = 42;  // Set to 24pt
    float boxWidth = 1500f;
    float boxHeight = 160f;
    float boxX = (Screen.width - boxWidth) / 2;
    float boxY = 120f;
    
    GUI.Box(new Rect(boxX, boxY, boxWidth, boxHeight), instructionsText);
}
}