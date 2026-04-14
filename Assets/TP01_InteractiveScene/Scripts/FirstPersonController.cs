using UnityEngine;
using UnityEngine.InputSystem;   // New Input System (already installed in this project)

/// <summary>
/// First-person controller: WASD movement, mouse look, left-click to interact with ClickableObjects.
/// Uses the New Input System polling API — no Input Actions asset required.
///
/// Required setup:
///   • CharacterController on the same GameObject
///   • A child Camera GameObject assigned to cameraTransform
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    // ── Inspector ──────────────────────────────

    [Header("Movement")]
    public float walkSpeed   = 5f;
    public float sprintSpeed = 9f;
    public float gravity     = -20f;
    public float jumpHeight  = 1.2f;

    [Header("Mouse Look")]
    public Transform cameraTransform;
    public float     mouseSensitivity = 0.15f;
    [Range(10f, 90f)]
    public float     maxPitch         = 85f;

    [Header("Interaction")]
    [Tooltip("Max distance for object raycasting.")]
    public float clickRange = 12f;

    // ── Private state ──────────────────────────

    private CharacterController _cc;
    private float _pitch;
    private float _verticalVelocity;

    // ── Lifecycle ──────────────────────────────

    void Awake()
    {
        _cc = GetComponent<CharacterController>();

        // Auto-find camera in children if not assigned
        if (cameraTransform == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null) cameraTransform = cam.transform;
            else Debug.LogError("[FPC] No camera assigned and none found in children.");
        }

        LockCursor();
    }

    void Update()
    {
        HandleCursorToggle();
        HandleLook();
        HandleMovement();
        HandleClick();
    }

    // ── Cursor ─────────────────────────────────

    void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    void HandleCursorToggle()
    {
        // Escape → unlock | click while unlocked → re-lock
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }
        else if (Mouse.current.leftButton.wasPressedThisFrame
                 && Cursor.lockState == CursorLockMode.None)
        {
            LockCursor();
        }
    }

    // ── Look ───────────────────────────────────

    void HandleLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;

        Vector2 delta = Mouse.current.delta.ReadValue();

        // Horizontal — rotate whole body
        transform.Rotate(Vector3.up, delta.x * mouseSensitivity, Space.World);

        // Vertical — rotate camera only, clamped
        _pitch -= delta.y * mouseSensitivity;
        _pitch  = Mathf.Clamp(_pitch, -maxPitch, maxPitch);
        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    // ── Movement ───────────────────────────────

    void HandleMovement()
    {
        var kb = Keyboard.current;

        // WASD / arrow keys
        float x = 0f, z = 0f;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    z += 1f;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  z -= 1f;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  x -= 1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;

        float speed = kb.leftShiftKey.isPressed ? sprintSpeed : walkSpeed;
        Vector3 move = (transform.right * x + transform.forward * z);
        if (move.sqrMagnitude > 1f) move.Normalize();
        _cc.Move(move * speed * Time.deltaTime);

        // Gravity + jump
        bool grounded = _cc.isGrounded;
        if (grounded && _verticalVelocity < 0f) _verticalVelocity = -2f;

        if (kb.spaceKey.wasPressedThisFrame && grounded)
            _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

        _verticalVelocity += gravity * Time.deltaTime;
        _cc.Move(Vector3.up * _verticalVelocity * Time.deltaTime);
    }

    // ── Click / Interaction ─────────────────────

    void HandleClick()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;
        if (cameraTransform == null) return;

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, clickRange))
        {
            var clickable = hit.collider.GetComponent<ClickableObject>();
            if (clickable != null)
            {
                clickable.OnClick();
                GameLogger.Instance?.LogClick();
                GameLogger.Instance?.LogInteraction(hit.collider.gameObject.name);
            }
        }
    }
}
