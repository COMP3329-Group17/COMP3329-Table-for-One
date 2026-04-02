using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    // These Enums help us switch the game "Mode" (e.g. Walking vs. Looking at a Clue)
    public enum PlayerState { Exploration, Inspecting, Menu }
    
    [Header("Current State")]
    public PlayerState currentState = PlayerState.Exploration;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    public float gravity = -20f;
    private float verticalVelocity;

    [Header("Camera Settings")]
    public Transform cameraTarget; // The "Look At" point for 3rd person
    public float mouseSensitivity = 2f;
    public float tpDistance = 4f; // How far the camera is in 3rd person
    public float fpHeight = 1.6f; // How high the eyes are in 1st person
    
    private CharacterController controller;
    private Transform cam;
    private float pitch; // Up/Down rotation
    private float yaw;   // Left/Right rotation
    private bool isFirstPerson = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        
        // Find the Main Camera automatically so you don't have to drag it in
        if (Camera.main != null) cam = Camera.main.transform;

        // ERROR FIX: If you forgot to assign a target, this creates one so the game doesn't crash
        if (cameraTarget == null)
        {
            GameObject tempTarget = new GameObject("AutoCameraTarget");
            tempTarget.transform.SetParent(this.transform);
            tempTarget.transform.localPosition = new Vector3(0, fpHeight, 0);
            cameraTarget = tempTarget.transform;
        }

        Cursor.lockState = CursorLockMode.Locked;
        yaw = transform.eulerAngles.y; // Start looking the same way the player is facing
    }

    void Update()
    {
        // MODE TOGGLE: Only allow switching V-key if we aren't busy inspecting a clue
        if (Input.GetKeyDown(KeyCode.V) && currentState == PlayerState.Exploration)
        {
            isFirstPerson = !isFirstPerson;
        }

        // STATE MACHINE: This controls what the player is ALLOWED to do
        switch (currentState)
        {
            case PlayerState.Exploration:
                HandleCamera();   // Move Camera
                HandleMovement(); // Move Body
                break;

            case PlayerState.Inspecting:
                isFirstPerson = true; // Force 1st person for that "horror" close-up
                HandleCamera();       // Still let them look around a bit
                // Notice: HandleMovement() is NOT called here. Player is frozen!
                break;
        }
    }

    void HandleCamera()
    {
        // Get mouse movement
        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, -70f, 70f); // Stop the neck from snapping 360 degrees

        if (isFirstPerson)
        {
            // Position camera at "eye level"
            cam.position = transform.position + Vector3.up * fpHeight;
            cam.rotation = Quaternion.Euler(pitch, yaw, 0);
            
            // In 1st person, the whole body turns when we look left/right
            transform.rotation = Quaternion.Euler(0, yaw, 0);
        }
        else
        {
            // 3RD PERSON MATH: Position the camera in a circle around the 'cameraTarget'
            Vector3 targetPos = cameraTarget.position;
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
            Vector3 offset = rotation * new Vector3(0, 0, -tpDistance);
            
            // WALL CHECK: If a wall is between the player and camera, move camera in front of wall
            if (Physics.Raycast(targetPos, offset.normalized, out RaycastHit hit, tpDistance))
                cam.position = hit.point + hit.normal * 0.2f;
            else
                cam.position = targetPos + offset;

            cam.LookAt(targetPos);
        }
    }

    void HandleMovement()
    {
        // Gravity Logic
        if (!controller.isGrounded) verticalVelocity += gravity * Time.deltaTime;
        else verticalVelocity = -0.5f;

        // Input
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        
        // Calculate direction based on where the CAMERA is looking
        Vector3 dir = (cam.forward * v + cam.right * h).normalized;
        dir.y = 0; // Don't let the player fly upward

        // Move the CharacterController
        controller.Move(dir * moveSpeed * Time.deltaTime);
        controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);

        // 3RD PERSON ROTATION: Turn the character to face the direction they are walking
        if (!isFirstPerson && dir.magnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }
    }

    // Call this from your Inventory/Item script: SetState(PlayerController.PlayerState.Inspecting);
    public void SetState(PlayerState newState) 
    {
        currentState = newState;
        
        // If we are in a Menu, show the mouse. If playing, hide it.
        Cursor.lockState = (newState == PlayerState.Exploration) ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = (newState != PlayerState.Exploration);
    }
}