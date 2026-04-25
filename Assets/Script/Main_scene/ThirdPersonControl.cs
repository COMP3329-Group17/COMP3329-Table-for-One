using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public enum PlayerState { Exploration, Inspecting, Minigame }

    [Header("Current State")]
    public PlayerState currentState = PlayerState.Exploration;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    public float itemRotateSpeed = 1500f;
    public float gravity = -9.81f;
    private float verticalVelocity;

    [Header("Camera Settings")]
    public Transform cameraTarget;
    public float mouseSensitivity = 2f;
    public float tpDistance = 1.5f;
    public float fpHeight = 1.8f;

    [Header("Inspection Settings")]
    public Transform inspectionTarget;
    public Light inspectionLight;
    public GameObject blurOverlay;
    private GameObject currentItem;

    // Internal References
    private CharacterController controller;
    private Transform cam;
    private Animator animator;
    private float pitch;
    private float yaw;
    private bool isFirstPerson = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();

        if (Camera.main != null) cam = Camera.main.transform;

        // Ensure the mouse is hidden at start
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        yaw = transform.eulerAngles.y;
    }

    void Update()
    {
        // 1. Toggle View (V-Key)
        if (Input.GetKeyDown(KeyCode.V) && currentState == PlayerState.Exploration)
        {
            isFirstPerson = !isFirstPerson;
        }

        // 2. State Machine Logic
        switch (currentState)
        {
            case PlayerState.Exploration:
                HandleCamera();
                HandleMovement();
                break;

            case PlayerState.Inspecting:
                HandleInspectionRotation();
                if (Input.GetKeyDown(KeyCode.E)) StopInspecting();
                break;

            case PlayerState.Minigame:
                // We can add a way to exit the minigame here if needed.
                if (Input.GetKeyDown(KeyCode.Space)) ExitMinigame();
                break;
        }
    }

    #region MOVEMENT & CAMERA

    void HandleMovement()
    {
        // Apply Gravity
        if (controller.isGrounded) verticalVelocity = -2f; // Small force to keep glued to floor
        else verticalVelocity += gravity * Time.deltaTime;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // Calculate direction relative to Camera orientation
        Vector3 moveDir = (cam.forward * v + cam.right * h).normalized;
        moveDir.y = 0;

        // Execute Move
        controller.Move(moveDir * moveSpeed * Time.deltaTime);
        controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);

        // Update Animator
        if (animator != null)
        {
            float speed01 = new Vector2(h, v).magnitude;
            animator.SetFloat("Speed", speed01, 0.1f, Time.deltaTime);
        }

        // Rotate character body to match movement direction (3rd Person Only)
        if (!isFirstPerson && moveDir.magnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }
    }

    void HandleCamera()
    {
        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, -70f, 70f);

        float scale = transform.lossyScale.y;

        if (isFirstPerson)
        {
            // 1. Calculate the base eye height (what you just adjusted to 1.6)
            Vector3 eyePosition = transform.position + Vector3.up * (fpHeight * scale);

            // 2. Add a small 'forward' offset so we aren't looking from inside the brain
            // 0.15f pushes it forward about 15cm. Adjust this if you still see his nose!
            float forwardOffset = 0.15f;
            Vector3 finalPosition = eyePosition + (transform.forward * forwardOffset);

            cam.position = finalPosition;
            cam.rotation = Quaternion.Euler(pitch, yaw, 0);

            // Turn the whole body with the camera in 1st person
            transform.rotation = Quaternion.Euler(0, yaw, 0);
        }
        else
        {
            // 3rd Person: Position camera behind player
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
            Vector3 targetPos = transform.position + Vector3.up * (fpHeight * scale);

            // Calculate distance based on scale
            Vector3 desiredPos = targetPos + (rotation * Vector3.back) * (tpDistance * scale);

            cam.position = desiredPos;
            cam.LookAt(targetPos);
        }
    }

    #endregion

    #region INSPECTION LOGIC

    public void StartInspecting(GameObject obj)
    {
        currentItem = obj;
        SetState(PlayerState.Inspecting);

        // Move item to the "Holder" in front of the camera
        obj.transform.position = inspectionTarget.position;
        obj.transform.SetParent(inspectionTarget);

        if (inspectionLight != null) inspectionLight.enabled = true;
        if (blurOverlay != null) blurOverlay.SetActive(true);

        if (obj.TryGetComponent<Rigidbody>(out Rigidbody rb)) rb.isKinematic = true;
    }

    void HandleInspectionRotation()
    {
        if (currentItem == null) return;

        // Left click to spin the item
        if (Input.GetMouseButton(0))
        {
            float rotX = Input.GetAxis("Mouse X") * itemRotateSpeed * Time.deltaTime;
            float rotY = Input.GetAxis("Mouse Y") * itemRotateSpeed * Time.deltaTime;

            currentItem.transform.Rotate(Vector3.up, -rotX, Space.World);
            currentItem.transform.Rotate(Vector3.right, rotY, Space.World);
        }
    }

    void StopInspecting()
    {
        if (currentItem != null)
        {
            if (inspectionLight != null) inspectionLight.enabled = false;
            if (blurOverlay != null) blurOverlay.SetActive(false);

            currentItem.SetActive(false); // Item goes to "Inventory"
            currentItem.transform.SetParent(null);
            currentItem = null;
        }
        SetState(PlayerState.Exploration);
    }
    // Mini game section
    public void EnterMinigame()
    {
        SetState(PlayerState.Minigame);
        // If your teammate's game uses a Canvas, you'd enable it here:
        // minigameCanvas.SetActive(true);
        Debug.Log("State changed to Minigame. Character frozen.");
    }

    public void ExitMinigame()
    {
        SetState(PlayerState.Exploration);
        Debug.Log("State changed to Exploration. Control restored.");
    }

    #endregion

    // Helper to change states and handle the cursor
    public void SetState(PlayerState newState)  
    {
        currentState = newState;
        bool isExploring = (newState == PlayerState.Exploration);

        Cursor.lockState = isExploring ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !isExploring;
    }
}