using UnityEngine;

/// <summary>
/// Bonus (Part 5) — Simple AI agent with two modes:
///   • FollowPlayer  — pursues the player, stops within stopDistance
///   • RandomWander  — roams to random positions, picks a new one on arrival or timeout
///
/// Both modes include lightweight obstacle avoidance via side-ray feelers.
/// No NavMesh required — pure Transform-based movement.
/// </summary>
public class AIAgent : MonoBehaviour
{
    public enum AgentMode { FollowPlayer, RandomWander }

    // ── Inspector ──────────────────────────────

    [Header("Behaviour")]
    public AgentMode mode        = AgentMode.FollowPlayer;
    public Transform target;                  // Player transform (set by SceneBuilder)

    [Header("Movement")]
    public float moveSpeed       = 3f;
    public float rotateSpeed     = 6f;
    public float stopDistance    = 2.5f;      // Follow mode: personal-space radius
    public float groundY         = 0.75f;     // Keep agent at this Y (floor level)

    [Header("Wander")]
    public float wanderRadius    = 12f;       // Max distance from origin for new waypoints
    public float wanderInterval  = 4f;        // Seconds before forcing a new waypoint
    private Vector3 _wanderOrigin;
    private Vector3 _wanderTarget;
    private float   _wanderTimer;

    [Header("Obstacle Avoidance")]
    public float feelerRange     = 2.5f;      // Forward ray length
    public float feelerAngle     = 35f;       // Left/right feeler spread
    public LayerMask obstacleMask = ~0;       // All layers by default

    // ── Gizmo colour ───────────────────────────
    private Color _gizmoColor = new Color(1f, 0.5f, 0f, 0.6f);

    // ── Lifecycle ──────────────────────────────

    void Start()
    {
        _wanderOrigin = transform.position;
        PickNewWanderTarget();
    }

    void Update()
    {
        Vector3 desiredDir;

        switch (mode)
        {
            case AgentMode.FollowPlayer:
                desiredDir = ComputeFollowDirection();
                break;
            default:
                desiredDir = ComputeWanderDirection();
                break;
        }

        ApplyMovement(desiredDir);
    }

    // ── Follow ─────────────────────────────────

    Vector3 ComputeFollowDirection()
    {
        if (target == null) return Vector3.zero;

        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;

        if (toTarget.magnitude <= stopDistance) return Vector3.zero;

        return Deflect(toTarget.normalized);
    }

    // ── Wander ─────────────────────────────────

    Vector3 ComputeWanderDirection()
    {
        _wanderTimer -= Time.deltaTime;

        Vector3 flat = _wanderTarget - transform.position;
        flat.y = 0f;

        // Reached waypoint or timer expired → pick a new one
        if (flat.magnitude < 0.8f || _wanderTimer <= 0f)
            PickNewWanderTarget();

        return Deflect(flat.normalized);
    }

    void PickNewWanderTarget()
    {
        Vector2 rand   = Random.insideUnitCircle * wanderRadius;
        _wanderTarget  = _wanderOrigin + new Vector3(rand.x, 0f, rand.y);
        _wanderTimer   = wanderInterval + Random.Range(-1f, 1f);
    }

    // ── Obstacle Avoidance ─────────────────────

    /// <summary>
    /// Cast three feeler rays (centre, left, right).
    /// If any hit, rotate the desired direction away from the obstacle.
    /// </summary>
    Vector3 Deflect(Vector3 desired)
    {
        if (desired.sqrMagnitude < 0.001f) return desired;

        Vector3 origin = transform.position + Vector3.up * 0.5f;

        Vector3 fwd   = desired.normalized;
        Vector3 left  = Quaternion.Euler(0, -feelerAngle, 0) * fwd;
        Vector3 right = Quaternion.Euler(0,  feelerAngle, 0) * fwd;

        bool hitFwd   = Physics.Raycast(origin, fwd,   feelerRange, obstacleMask);
        bool hitLeft  = Physics.Raycast(origin, left,  feelerRange, obstacleMask);
        bool hitRight = Physics.Raycast(origin, right, feelerRange, obstacleMask);

        if (hitFwd || hitLeft || hitRight)
        {
            // Steer around: rotate 90° toward the free side
            float sign = hitRight ? -1f : 1f;
            desired = Quaternion.Euler(0, 90f * sign, 0) * desired;
        }

        return desired.normalized;
    }

    // ── Movement ───────────────────────────────

    void ApplyMovement(Vector3 dir)
    {
        if (dir.sqrMagnitude < 0.001f) return;

        // Translate
        Vector3 pos   = transform.position + dir * moveSpeed * Time.deltaTime;
        pos.y         = groundY;              // stay on floor
        transform.position = pos;

        // Rotate toward movement direction
        Quaternion look  = Quaternion.LookRotation(dir, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, rotateSpeed * Time.deltaTime);
    }

    // ── Gizmos ─────────────────────────────────

    void OnDrawGizmosSelected()
    {
        Gizmos.color = _gizmoColor;

        // Personal-space sphere (follow mode)
        if (mode == AgentMode.FollowPlayer)
            Gizmos.DrawWireSphere(transform.position, stopDistance);

        // Wander radius
        if (mode == AgentMode.RandomWander)
        {
            Gizmos.DrawWireSphere(_wanderOrigin, wanderRadius);
            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position, _wanderTarget);
        }

        // Feeler rays
        Gizmos.color = Color.yellow;
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Vector3 fwd    = transform.forward;
        Gizmos.DrawRay(origin, fwd * feelerRange);
        Gizmos.DrawRay(origin, Quaternion.Euler(0, -feelerAngle, 0) * fwd * feelerRange);
        Gizmos.DrawRay(origin, Quaternion.Euler(0,  feelerAngle, 0) * fwd * feelerRange);
    }
}
