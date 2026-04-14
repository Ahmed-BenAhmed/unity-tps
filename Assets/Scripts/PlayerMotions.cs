using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 9f;
    public float acceleration = 10f;   // smooth speed ramp
    public float gravity = -20f;
    public float jumpHeight = 1.2f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 0.3f;  // much lower than before — new Input System gives raw delta, not frame-scaled
    public Transform playerCamera;
    [Range(0f, 90f)] public float maxLookUp = 85f;
    [Range(0f, 90f)] public float maxLookDown = 85f;

    [Header("Ground Check")]
    public float groundCheckRadius = 0.3f;
    public LayerMask groundMask;

    // ── internal state ──
    private CharacterController _cc;
    private Vector2 _moveInput;
    private Vector2 _lookInput;
    private bool _sprintHeld;
    private bool _jumpRequested;

    private float _xRotation;
    private float _currentSpeed;
    private Vector3 _velocity;          // vertical (gravity / jump)
    private bool _isGrounded;

    // ═══════════════════════════════════════
    //  LIFECYCLE
    // ═══════════════════════════════════════

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerCamera == null)
        {
            // auto-find if not assigned
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null) playerCamera = cam.transform;
            else Debug.LogError("PlayerMovement: No camera assigned and none found in children.");
        }
    }

    void Update()
    {
        GroundCheck();
        HandleLook();
        HandleMovement();
        HandleGravityAndJump();
    }

    // ═══════════════════════════════════════
    //  INPUT CALLBACKS  (wire these in the PlayerInput component or via C# generated class)
    // ═══════════════════════════════════════

    // Action name: "Move"  — Value, Vector2, WASD / left stick
    public void OnMove(InputAction.CallbackContext ctx)
    {
        _moveInput = ctx.ReadValue<Vector2>();
    }

    // Action name: "Look"  — Value, Vector2, mouse delta / right stick
    public void OnLook(InputAction.CallbackContext ctx)
    {
        _lookInput = ctx.ReadValue<Vector2>();
    }

    // Action name: "Jump"  — Button
    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (ctx.started) _jumpRequested = true;
    }

    // Action name: "Sprint" — Button (hold)
    public void OnSprint(InputAction.CallbackContext ctx)
    {
        if (ctx.started) _sprintHeld = true;
        if (ctx.canceled) _sprintHeld = false;
    }

    // ═══════════════════════════════════════
    //  CORE LOGIC
    // ═══════════════════════════════════════

    private void GroundCheck()
    {
        // sphere cast from the bottom of the CharacterController
        Vector3 origin = transform.position + Vector3.down * (_cc.height * 0.5f - _cc.radius + 0.05f);
        _isGrounded = Physics.CheckSphere(origin, groundCheckRadius, groundMask, QueryTriggerInteraction.Ignore);
    }

    private void HandleLook()
    {
        if (playerCamera == null) return;

        float dx = _lookInput.x * mouseSensitivity;
        float dy = _lookInput.y * mouseSensitivity;

        // vertical — clamp
        _xRotation -= dy;
        _xRotation = Mathf.Clamp(_xRotation, -maxLookUp, maxLookDown);
        playerCamera.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);

        // horizontal — rotate body
        transform.Rotate(Vector3.up * dx);
    }

    private void HandleMovement()
    {
        float targetSpeed = _sprintHeld ? sprintSpeed : walkSpeed;

        // smooth speed transitions (no instant snap between walk ↔ sprint)
        _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, acceleration * Time.deltaTime);

        Vector3 move = transform.right * _moveInput.x + transform.forward * _moveInput.y;
        if (move.sqrMagnitude > 1f) move.Normalize();   // prevent diagonal speed boost

        _cc.Move(move * _currentSpeed * Time.deltaTime);
    }

    private void HandleGravityAndJump()
    {
        if (_isGrounded && _velocity.y < 0f)
        {
            _velocity.y = -2f;   // small downward force to stay grounded
        }

        if (_jumpRequested && _isGrounded)
        {
            // v = sqrt(2 * |gravity| * jumpHeight)
            _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        _jumpRequested = false;

        _velocity.y += gravity * Time.deltaTime;
        _cc.Move(_velocity * Time.deltaTime);
    }

    // ═══════════════════════════════════════
    //  GIZMOS  (visualize ground check in Scene view)
    // ═══════════════════════════════════════

    private void OnDrawGizmosSelected()
    {
        if (_cc == null) _cc = GetComponent<CharacterController>();
        if (_cc == null) return;

        Gizmos.color = _isGrounded ? Color.green : Color.red;
        Vector3 origin = transform.position + Vector3.down * (_cc.height * 0.5f - _cc.radius + 0.05f);
        Gizmos.DrawWireSphere(origin, groundCheckRadius);
    }
}
