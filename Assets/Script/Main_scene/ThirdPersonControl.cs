using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    // PERSISTENCE DATA (Global Memory)
    public static Vector3 savedPosition;
    public static Quaternion savedRotation;
    public static bool hasSavedPosition = false;

    public enum PlayerState { Exploration, Inspecting, Minigame }

    [Header("Current State")]
    public PlayerState currentState = PlayerState.Exploration;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f; // Speed of the U-Turn
    public float itemRotateSpeed = 1500f;
    public float gravity = -9.81f;
    private float verticalVelocity;

    [Header("Camera Settings")]
    public Transform cameraTarget;
    public float mouseSensitivity = 2f;
    public float tpDistance = 3.0f; // Increased for better view
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
    private float stateChangeTime;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        if (Camera.main != null) cam = Camera.main.transform;

        if (hasSavedPosition)
        {
            controller.enabled = false;
            transform.position = savedPosition;
            transform.rotation = savedRotation;
            controller.enabled = true;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        yaw = transform.eulerAngles.y;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.V) && currentState == PlayerState.Exploration)
        {
            isFirstPerson = !isFirstPerson;
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            InventoryMG inv = FindFirstObjectByType<InventoryMG>();
            if (inv != null) inv.ToggleInventory();
        }

        //trying to unlock a door
        if (Input.GetKeyDown(KeyCode.F) && currentState == PlayerState.Exploration)
        {
            CheckForLock();
        }

        switch (currentState)
        {
            case PlayerState.Exploration:
                HandleCamera();
                HandleMovement();
                break;
            case PlayerState.Inspecting:
                HandleInspectionRotation();
                // Only allow exit if at least 0.2 seconds have passed
                if (Input.GetKeyDown(KeyCode.E) && Time.time > stateChangeTime + 0.2f)
                {
                    CollectItem(); // Use a new function to add to bag
                }
                break;
            case PlayerState.Minigame:
                if (Input.GetKeyDown(KeyCode.Space)) ExitMinigame();
                break;
        }
    }

    #region MOVEMENT & CAMERA

    void HandleMovement()
    {
        if (controller.isGrounded) verticalVelocity = -2f;
        else verticalVelocity += gravity * Time.deltaTime;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // 1. Get Camera directions (ignoring tilt)
        Vector3 camForward = cam.forward;
        Vector3 camRight = cam.right;
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        // 2. Calculate Move Direction based on Input
        Vector3 desiredMoveDir = (camForward * v + camRight * h).normalized;

        if (desiredMoveDir.magnitude >= 0.1f)
        {
            if (isFirstPerson)
            {
                // In First Person, the body still snaps to the mouse yaw
                transform.rotation = Quaternion.Euler(0, yaw, 0);
            }
            else
            {
                // IN THIRD PERSON: This creates the U-Turn effect
                // The character rotates to face the direction of movement
                Quaternion targetRotation = Quaternion.LookRotation(desiredMoveDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            controller.Move(desiredMoveDir * moveSpeed * Time.deltaTime);
        }

        // Apply Gravity
        controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);

        if (animator != null)
        {
            float speed01 = new Vector2(h, v).magnitude;
            animator.SetFloat("Speed", speed01, 0.1f, Time.deltaTime);
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
            // Body follows mouse yaw instantly in FP
            transform.rotation = Quaternion.Euler(0, yaw, 0);

            Vector3 eyePosition = transform.position + Vector3.up * (fpHeight * scale);
            cam.position = eyePosition + (transform.forward * 0.15f);
            cam.rotation = Quaternion.Euler(pitch, yaw, 0);
        }
        else
        {
            // THIRD PERSON: Camera orbits, but body does NOT follow mouse instantly
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
            Vector3 targetPos = transform.position + Vector3.up * (fpHeight * scale);
            Vector3 desiredPos = targetPos + (rotation * Vector3.back) * (tpDistance * scale);

            cam.position = desiredPos;
            cam.LookAt(targetPos);
        }
    }

    #endregion

    #region INSPECTION & MINIGAME

    public void StartInspecting(GameObject obj)
    {
        currentItem = obj;
        SetState(PlayerState.Inspecting);

        obj.transform.SetParent(inspectionTarget);
        // CRITICAL: Force the object to the exact center of your anchor
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;

        if (inspectionLight != null) inspectionLight.enabled = true;
        if (blurOverlay != null) blurOverlay.SetActive(true);

        // Disable physics so it doesn't fall out of your hands
        if (obj.TryGetComponent<Rigidbody>(out Rigidbody rb)) rb.isKinematic = true;
    }

    void HandleInspectionRotation()
    {
        if (currentItem == null) return;
        if (Input.GetMouseButton(0))
        {
            float rotX = Input.GetAxis("Mouse X") * itemRotateSpeed * Time.deltaTime;
            float rotY = Input.GetAxis("Mouse Y") * itemRotateSpeed * Time.deltaTime;
            currentItem.transform.Rotate(Vector3.up, -rotX, Space.World);
            currentItem.transform.Rotate(Vector3.right, rotY, Space.World);
        }
    }

    public void StopInspecting()
    {
        if (currentItem != null)
        {
            if (inspectionLight != null) inspectionLight.enabled = false;
            if (blurOverlay != null) blurOverlay.SetActive(false);
            currentItem.SetActive(false);
            currentItem.transform.SetParent(null);
            currentItem = null;
        }
        SetState(PlayerState.Exploration);
    }

    public void EnterMinigame(string sceneName)
    {
        savedPosition = transform.position;
        savedRotation = transform.rotation;
        hasSavedPosition = true;

        SetState(PlayerState.Minigame);
        SceneManager.LoadScene(sceneName);
    }

    public void ExitMinigame()
    {
        SceneManager.LoadScene("MainScene");
    }

   

    public void SetState(PlayerState newState)
    {
        currentState = newState;
        stateChangeTime = Time.time; // Track when the state changed
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    void CheckForLock()
    {
        Ray ray = new Ray(cam.position, cam.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, 3f))
        {
            if (hit.collider.TryGetComponent(out KeyHole lockScript))
            {
                // We find the InventoryMG script
                InventoryMG inv = FindFirstObjectByType<InventoryMG>();
                if (inv != null)
                {
                    // We pass the activeItem we just created above!
                    lockScript.AttemptUnlock(InventoryMG.activeItem);
                }
            }
        }
    }

    public void CollectItem()
    {
        if (currentItem != null)
        {
            ItemObject itemScript = currentItem.GetComponent<ItemObject>();
            if (itemScript != null)
            {
                InventoryMG inv = FindFirstObjectByType<InventoryMG>();
                if (inv != null) inv.AddItem(itemScript.referenceData); // Add data to bag
            }

            if (inspectionLight != null) inspectionLight.enabled = false;
            if (blurOverlay != null) blurOverlay.SetActive(false);

            Destroy(currentItem); // Permanently remove from world
            currentItem = null;
        }
        SetState(PlayerState.Exploration);
    }

    #endregion
}