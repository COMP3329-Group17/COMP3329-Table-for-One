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
        // 1. Assign references FIRST
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        if (Camera.main != null) cam = Camera.main.transform;

        // 2. Teleport logic AFTER references are set
        if (hasSavedPosition)
        {
            controller.enabled = false;
            transform.position = savedPosition;
            transform.rotation = savedRotation;
            controller.enabled = true;
        }

        // Always show mouse
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

        // Move relative to character's forward (which follows mouse)
        Vector3 moveDir = (transform.forward * v + transform.right * h).normalized;
        moveDir.y = 0;

        controller.Move(moveDir * moveSpeed * Time.deltaTime);
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

        // Make character body rotate with mouse
        transform.rotation = Quaternion.Euler(0, yaw, 0);

        if (isFirstPerson)
        {
            Vector3 eyePosition = transform.position + Vector3.up * (fpHeight * scale);
            cam.position = eyePosition + (transform.forward * 0.15f);
            cam.rotation = Quaternion.Euler(pitch, yaw, 0);
        }
        else
        {
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

        obj.transform.position = inspectionTarget.position;
        obj.transform.SetParent(inspectionTarget);

        if (inspectionLight != null) inspectionLight.enabled = true;
        if (blurOverlay != null) blurOverlay.SetActive(true);
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
        // Change "MainScene" to your actual scene name!
        SceneManager.LoadScene("MainScene");
    }

    public void SetState(PlayerState newState)
    {
        currentState = newState;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    #endregion
}