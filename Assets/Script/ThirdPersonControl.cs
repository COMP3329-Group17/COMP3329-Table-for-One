using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    // These Enums help us switch the game "Mode" (e.g. Walking vs. Looking at a Clue)
    public enum PlayerState { Exploration, Inspecting, Minigame, Menu }
    
    [Header("Current State")]
    public PlayerState currentState = PlayerState.Exploration;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    public float itemRotateSpeed = 1500f; // Speed for spinning the clue
    public float gravity = -20f;
    private float verticalVelocity;

    [Header("Camera Settings")]
    public Transform cameraTarget; // The "Look At" point for 3rd person
    public float mouseSensitivity = 2f;
    [Tooltip("Third-person distance in normal (unscaled) units. With Man scale=100, this becomes 100x farther automatically.")]
    public float tpDistance = 1.5f;

    [Tooltip("First-person eye height in normal (unscaled) units. With Man scale=100, this becomes 100x higher automatically.")]
    public float fpHeight = 1.8f;

    // Keep the inspector simple: these are authored in normal units and scaled by transform scale automatically.
    private const float TpExtraHeight = 0.01f;
    private const float TpShoulderOffset = 0.5f;
    private const float FpForwardOffset = 0.12f;
    private const float CameraCollisionRadius = 0.02f;
    private const bool FpUseHeadBone = true;

    [Header("Inspection Settings")]
    public Transform inspectionTarget; // The "Holder" child of the camera
    private GameObject currentItem;    // The actual object we are holding

    private CharacterController controller;
    private Transform cam;
    private Animator animator;
    private float pitch; // Up/Down rotation
    private float yaw;   // Left/Right rotation
    private bool isFirstPerson = false;

    [Header("Character Setup Fixes")]
    [Tooltip("Auto-fixes the CharacterController so the capsule sits on the ground (prevents floating when pivot is at feet).")]
    public bool autoFixCharacterController = true;

    [Tooltip("Auto-fit CharacterController size/center from child renderer bounds. Use this when the model/world is scaled (e.g. 100x).")]
    public bool autoFitControllerToModelBounds = true;

    [Tooltip("Snap the controller so the capsule bottom touches the ground at start.")]
    public bool snapToGroundOnStart = true;

    [Tooltip("Which layers count as ground for snapping.")]
    public LayerMask groundLayers = ~0;

    [Tooltip("Keep the capsule bottom slightly above the floor to prevent visible foot clipping.")]
    public float groundClearance = 0.02f;

    [Tooltip("Extra downward push when grounded so the controller stays glued to slopes/floor.")]
    public float stickToGroundForce = 2.0f;

    [Tooltip("Small downward move when grounded to avoid hovering/falling when starting to move.")]
    public float stickToGroundDistance = 0.02f;

    [Tooltip("If enabled, drive Animator parameters for more natural skeletal motion (requires an Animator with matching params).")]
    public bool driveAnimator = true;

    [Tooltip("Animator float parameter name for movement speed.")]
    public string animatorSpeedParam = "Speed";

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        
        // Find the Main Camera automatically so you don't have to drag it in
        if (Camera.main != null) cam = Camera.main.transform;
        AutoConfigureCameraClipPlanes();

        // Order matters:
        // 1) Fit to model bounds (handles scaled FBX/world).
        // 2) Fix obvious bad values.
        // 3) Snap to ground to remove initial hovering.
        if (autoFitControllerToModelBounds)
            FitControllerToModelBounds();
        if (autoFixCharacterController)
            FixControllerCapsule();
        if (snapToGroundOnStart)
            SnapControllerToGround();

        // ERROR FIX: If you forgot to assign a target, this creates one so the game doesn't crash
        if (cameraTarget == null)
        {
            GameObject tempTarget = new GameObject("AutoCameraTarget");
            tempTarget.transform.SetParent(this.transform);
            tempTarget.transform.localPosition = new Vector3(0, GetScaledFpHeight(), 0);
            cameraTarget = tempTarget.transform;
        }
        else
        {
            // Keep target at the correct height for the current scale.
            Vector3 lp = cameraTarget.localPosition;
            lp.y = GetScaledFpHeight();
            cameraTarget.localPosition = lp;
        }

        Cursor.lockState = CursorLockMode.Locked;
        yaw = transform.eulerAngles.y; // Start looking the same way the player is facing
    }

    private void AutoConfigureCameraClipPlanes()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null) return;

        // Large-scale scenes (e.g. models scaled 100x) can exceed the default far clip (1000),
        // making the Game view appear empty. Scale far clip to scene scale.
        float scale = Mathf.Max(1f, GetScaleFactor());

        mainCam.nearClipPlane = Mathf.Clamp(mainCam.nearClipPlane, 0.01f, 5f);
        mainCam.farClipPlane = Mathf.Max(mainCam.farClipPlane, 5000f * scale);
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
                HandleInspectionRotation(); // Spin the object!

                // EXIT: Press E to put it away
                if (Input.GetKeyDown(KeyCode.E))
                {
                    StopInspecting();
                }
                break;

            case PlayerState.Minigame:
                // Mini-game owns the input + UI; we just freeze movement/camera here.
                break;
        }
    }

    [Header("Inspection Visuals")]
    public Light inspectionLight;      // Drag a small light here
    public GameObject blurOverlay;    // Drag your "InspectionOverlay" UI here

    // Inspecting logic
    public void StartInspecting(GameObject obj)
    {
        currentItem = obj   ;
        SetState(PlayerState.Inspecting);

        // Snap item to the holder in front of camera
        obj.transform.position = inspectionTarget.position;
        obj.transform.SetParent(inspectionTarget);

        if (inspectionLight != null) { 
            inspectionLight.enabled = true;
            Debug.Log("UI Overlay should be VISIBLE now.");
        }
        if (blurOverlay != null) blurOverlay.SetActive(true); // Show the dark screen

        // Optional: Disable physics so it doesn't fly away
        if (obj.TryGetComponent<Rigidbody>(out Rigidbody rb)) rb.isKinematic = true;
    }

    // Item rotation while in inspecting state
    void HandleInspectionRotation()
    {
        if (currentItem == null) return;

        // Rotate object when Left Mouse is held
        if (Input.GetMouseButton(0))
        {
            float rotX = Input.GetAxis("Mouse X") * itemRotateSpeed * Time.deltaTime;
            float rotY = Input.GetAxis("Mouse Y") * itemRotateSpeed * Time.deltaTime;

            currentItem.transform.Rotate(Vector3.up, -rotX, Space.World);
            currentItem.transform.Rotate(Vector3.right, rotY, Space.World);
        }
    }

    // Exit inspecting state
    void StopInspecting()
    {
        if (currentItem != null)
        {

            if (inspectionLight != null) inspectionLight.enabled = false;
            if(blurOverlay != null) blurOverlay.SetActive(false); // Hide the dark screen
            currentItem.SetActive(false); // Make it disappear (into inventory)
            currentItem.transform.SetParent(null);
            currentItem = null;
        }
        SetState(PlayerState.Exploration);
    }


    void HandleCamera()
    {
        // Get mouse movement
        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, -70f, 70f); // Stop the neck from snapping 360 degrees

        if (cam == null) return;

        if (isFirstPerson)
        {
            Quaternion lookRot = Quaternion.Euler(pitch, yaw, 0);

            // Position camera at "eye level" (prefer head bone if humanoid rig exists).
            Vector3 basePos;
            if (FpUseHeadBone && animator != null && animator.isHuman)
            {
                Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
                basePos = head != null ? head.position : (transform.position + Vector3.up * GetScaledFpHeight());
            }
            else
            {
                basePos = transform.position + Vector3.up * GetScaledFpHeight();
            }

            float scale = GetScaleFactor();
            cam.position = basePos + (lookRot * Vector3.forward) * (FpForwardOffset * scale);
            cam.rotation = lookRot;
            
            // In 1st person, the whole body turns when we look left/right
            transform.rotation = Quaternion.Euler(0, yaw, 0);
        }
        else
        {
            // 3RD PERSON MATH: Position the camera in a circle around the 'cameraTarget'
            float scale = GetScaleFactor();
            float dist = GetScaledTpDistance();
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);

            // We use the player's position + eye height (fpHeight)
            Vector3 targetPos = transform.position + Vector3.up * GetScaledFpHeight();

            // Add a little extra "headroom" so we aren't looking at the middle of the back
            targetPos += Vector3.up * (TpExtraHeight * scale);

            // Desired camera position behind + slight shoulder offset.
            Vector3 shoulder = (transform.right * (TpShoulderOffset * scale));
            Vector3 desiredPos = targetPos + shoulder + (rotation * Vector3.back) * dist;

            // This bitwise operation says: "Hit everything EXCEPT the layer this object is on"
            //int mask = ~(1 << gameObject.layer);

            // We removed the Collision check! The camera now just goes to desiredPos.
            cam.position = desiredPos;
            cam.LookAt(targetPos);

        }
    }

    private bool TryResolveCameraCollision(Vector3 targetPos,
        Vector3 desiredPos,
        Vector3 dirNorm,
        float maxDist,
        int layerMask,
        float scale,
        out Vector3 resolvedPos)
    {
        resolvedPos = desiredPos;
        float radius = CameraCollisionRadius * scale;
        if (maxDist <= 0.0001f) return false;

        // FIX: Push the start point further out. 
        // If your character is huge, the radius needs to be bigger to clear the shoulders.
        float startOffset = (controller != null ? controller.radius : 0.5f) * scale + (radius * 2f);

        Vector3 castOrigin = targetPos + dirNorm * startOffset;
        float castDist = maxDist - startOffset;

        // If the distance is too short, just put the camera at the origin 
        // (but slightly above to avoid looking at the floor)
        if (castDist <= 0.01f)
        {
            resolvedPos = targetPos + dirNorm * 0.1f;
            return true;
        }

        // IMPORTANT: Make sure the Raycast IGNORES the Player Layer
        // Change this line to exclude the player layer (usually Layer 3)
        int mask = layerMask & ~(1 << gameObject.layer);

        if (Physics.SphereCast(castOrigin, radius, dirNorm, out RaycastHit hit, castDist, mask, QueryTriggerInteraction.Ignore))
        {
            resolvedPos = hit.point - dirNorm * radius;
            return true;
        }

        return false;
    }

    void HandleMovement()
    {
        // Gravity Logic
        if (!controller.isGrounded)
        {
            verticalVelocity += gravity * Time.deltaTime;
        }
        else
        {
            // Keep a small negative velocity so we remain grounded.
            verticalVelocity = -Mathf.Max(0.5f, stickToGroundForce);
        }

        // Input
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        
        // Calculate direction based on where the CAMERA is looking
        Vector3 dir = (cam.forward * v + cam.right * h).normalized;
        dir.y = 0; // Don't let the player fly upward

        // Move the CharacterController
        controller.Move(dir * moveSpeed * Time.deltaTime);
        controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);

        // Stick-to-ground pass: helps prevent "start falling when moving" on uneven meshes.
        if (controller.isGrounded)
        {
            controller.Move(Vector3.down * (GetScaleFactor() * Mathf.Max(0.001f, stickToGroundDistance)));
        }

        // Optional: drive Animator for natural skeletal movement
        if (driveAnimator && animator != null)
        {
            // Use normalized input magnitude (0..1). Your blend tree can scale actual speed.
            float speed01 = Mathf.Clamp01(new Vector2(h, v).magnitude);
            animator.SetFloat(animatorSpeedParam, speed01, 0.1f, Time.deltaTime);
        }

        // 3RD PERSON ROTATION: Turn the character to face the direction they are walking
        if (!isFirstPerson && dir.magnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }
    }

    private void FixControllerCapsule()
    {
        // Common case: model pivot is at feet, but CharacterController center is left at (0,0,0).
        // That places half the capsule underground at start, and Unity will push it up => "floating".
        if (controller == null) return;

        float height = Mathf.Max(0.5f, controller.height);
        Vector3 center = controller.center;

        // If center is near zero, assume it's wrong for a feet-pivot humanoid and snap it.
        if (Mathf.Abs(center.y) < 0.001f)
        {
            center.y = height * 0.5f;
        }

        // Keep radius sane: capsule must be at least diameter <= height.
        controller.radius = Mathf.Min(controller.radius, height * 0.5f - 0.01f);
        controller.center = center;
    }

    private void FitControllerToModelBounds()
    {
        if (controller == null) return;

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0) return;

        // Combine world-space bounds, then convert to player-local space.
        Bounds worldBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            worldBounds.Encapsulate(renderers[i].bounds);

        Vector3 localMin = transform.InverseTransformPoint(worldBounds.min);
        Vector3 localMax = transform.InverseTransformPoint(worldBounds.max);

        // Ensure min/max ordering in case of negative scale (unlikely, but safe).
        float minY = Mathf.Min(localMin.y, localMax.y);
        float maxY = Mathf.Max(localMin.y, localMax.y);
        float minX = Mathf.Min(localMin.x, localMax.x);
        float maxX = Mathf.Max(localMin.x, localMax.x);
        float minZ = Mathf.Min(localMin.z, localMax.z);
        float maxZ = Mathf.Max(localMin.z, localMax.z);

        float height = Mathf.Max(0.5f, maxY - minY);
        float radius = Mathf.Max(0.05f, Mathf.Min((maxX - minX), (maxZ - minZ)) * 0.25f);

        controller.height = height;
        controller.radius = Mathf.Min(radius, height * 0.5f - 0.01f);
        controller.center = new Vector3(0f, (minY + maxY) * 0.5f, 0f);

        // Scale-friendly defaults.
        controller.skinWidth = Mathf.Max(0.01f, controller.radius * 0.08f);
        controller.stepOffset = Mathf.Clamp(controller.height * 0.15f, 0.05f, controller.height * 0.5f);
    }

    private void SnapControllerToGround()
    {
        if (controller == null) return;

        // Start the ray a little above the capsule top to avoid starting inside colliders.
        float rayStart = Mathf.Max(0.5f, controller.height) + 0.5f;
        Vector3 origin = transform.position + Vector3.up * rayStart;

        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, rayStart + 10f, groundLayers, QueryTriggerInteraction.Ignore))
            return;

        // Capsule bottom in local space is center.y - height/2
        float bottomLocalY = controller.center.y - controller.height * 0.5f;
        float bottomWorldY = transform.position.y + bottomLocalY;

        // Leave a tiny clearance to avoid visible mesh clipping due to skin width / bounds.
        float desiredBottomWorldY = hit.point.y + GetScaledGroundClearance();
        float deltaY = desiredBottomWorldY - bottomWorldY;

        // Move using controller to respect collisions.
        controller.Move(Vector3.up * deltaY);
    }

    private float GetScaleFactor()
    {
        // Project assumption: the character/world is scaled (e.g. Man scale=100).
        // Always scale camera offsets by the transform's scale so distances remain correct.
        return Mathf.Max(0.0001f, transform.lossyScale.y);
    }

    private float GetScaledTpDistance()
    {
        return tpDistance * GetScaleFactor();
    }

    private float GetScaledFpHeight()
    {
        return fpHeight * GetScaleFactor();
    }

    private float GetScaledGroundClearance()
    {
        return groundClearance * GetScaleFactor();
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