using UnityEngine;

/// <summary>
/// SceneBuilder — attach this script to an empty GameObject in a blank scene
/// and press Play. It constructs the entire interactive scene at runtime:
///
///   • Directional light + ambient
///   • Large Plane floor (green)
///   • Configurable mix of Cube and Sphere obstacles (random size/position/colour)
///     each with a ClickableObject component
///   • First-person Player (CharacterController + FirstPersonController)
///   • AI Agent capsule (AIAgent — follow or wander)
///   • GameLogger singleton
///
/// You can tweak all values in the Inspector before hitting Play.
/// </summary>
public class SceneBuilder : MonoBehaviour
{
    // ── Inspector ──────────────────────────────

    [Header("Scene")]
    [Tooltip("Half-size of the playable area (floor radius).")]
    public float sceneHalfSize = 18f;

    [Header("Obstacles")]
    public int cubeCount   = 5;
    public int sphereCount = 4;

    [Header("Player")]
    public float playerStartY    = 1f;
    public float mouseSensitivity = 0.15f;

    [Header("AI Agent")]
    public AIAgent.AgentMode aiMode     = AIAgent.AgentMode.FollowPlayer;
    public float             aiSpeed    = 3f;

    [Header("Logging")]
    public float positionSnapInterval = 5f;

    // ── Lifecycle ──────────────────────────────

    void Awake()
    {
        Build();
    }

    // ── Build ──────────────────────────────────

    void Build()
    {
        SetupLighting();
        CreateFloor();
        CreateObstacles();
        GameObject player = CreatePlayer();
        CreateAIAgent(player.transform);
        CreateLogger(player.transform);

        Debug.Log("[SceneBuilder] Scene ready.\n" +
                  "  WASD / Arrows = move   |   Shift = sprint   |   Space = jump\n" +
                  "  Mouse = look   |   Left-Click = interact   |   Escape = unlock cursor");
    }

    // ── Lighting ───────────────────────────────

    void SetupLighting()
    {
        // Directional light (sun)
        GameObject lightGO = new GameObject("Sun");
        Light sun           = lightGO.AddComponent<Light>();
        sun.type            = LightType.Directional;
        sun.intensity       = 1.2f;
        sun.color           = new Color(1f, 0.96f, 0.84f);
        sun.shadows         = LightShadows.Soft;
        lightGO.transform.rotation = Quaternion.Euler(52f, -30f, 0f);

        // Ambient
        RenderSettings.ambientMode  = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.28f, 0.28f, 0.33f);
    }

    // ── Floor ──────────────────────────────────

    void CreateFloor()
    {
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name       = "Floor";
        // Unity's Plane is 10 units — scale to cover the play area
        float s = sceneHalfSize * 0.2f;
        floor.transform.localScale = new Vector3(s, 1f, s);
        ApplyColor(floor, new Color(0.28f, 0.55f, 0.28f));
    }

    // ── Obstacles ──────────────────────────────

    void CreateObstacles()
    {
        for (int i = 0; i < cubeCount; i++)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name       = $"Cube_{i + 1}";
            float h         = Random.Range(1f, 3.2f);
            cube.transform.localScale = new Vector3(
                Random.Range(0.8f, 2.5f), h, Random.Range(0.8f, 2.5f));
            cube.transform.position   = SafeRandomPos(h * 0.5f);
            cube.transform.rotation   = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            ApplyColor(cube, RandomColor());
            cube.AddComponent<ClickableObject>();
        }

        for (int i = 0; i < sphereCount; i++)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name       = $"Sphere_{i + 1}";
            float r           = Random.Range(0.6f, 1.8f);
            sphere.transform.localScale = Vector3.one * r * 2f;
            sphere.transform.position   = SafeRandomPos(r);
            ApplyColor(sphere, RandomColor());
            sphere.AddComponent<ClickableObject>();
        }
    }

    // ── Player ─────────────────────────────────

    GameObject CreatePlayer()
    {
        // Root — holds CharacterController and FPS script
        GameObject player   = new GameObject("Player");
        player.tag          = "Player";
        player.transform.position = new Vector3(0f, playerStartY, 0f);

        CharacterController cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.4f;
        cc.center = new Vector3(0f, 0.9f, 0f);

        // Child camera at eye height
        GameObject camGO   = new GameObject("PlayerCamera");
        camGO.transform.SetParent(player.transform);
        camGO.transform.localPosition = new Vector3(0f, 1.62f, 0f);

        Camera cam         = camGO.AddComponent<Camera>();
        cam.nearClipPlane  = 0.08f;
        cam.fieldOfView    = 75f;
        cam.tag            = "MainCamera";
        camGO.AddComponent<AudioListener>();

        // FPS controller
        FirstPersonController fpc = player.AddComponent<FirstPersonController>();
        fpc.cameraTransform   = camGO.transform;
        fpc.mouseSensitivity  = mouseSensitivity;
        fpc.clickRange        = 14f;

        return player;
    }

    // ── AI Agent ───────────────────────────────

    void CreateAIAgent(Transform playerTransform)
    {
        GameObject agent = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        agent.name       = "AIAgent";
        agent.transform.position = new Vector3(6f, 0.75f, 6f);
        ApplyColor(agent, new Color(1f, 0.42f, 0.05f));  // vivid orange

        // Remove the capsule's MeshCollider (already has a CapsuleCollider from CreatePrimitive)

        AIAgent ai       = agent.AddComponent<AIAgent>();
        ai.mode          = aiMode;
        ai.target        = playerTransform;
        ai.moveSpeed     = aiSpeed;
        ai.groundY       = 0.75f;
        // obstacleMask left at ~0 (all layers) — will avoid everything
    }

    // ── Logger ─────────────────────────────────

    void CreateLogger(Transform playerTransform)
    {
        GameObject logGO   = new GameObject("GameLogger");
        GameLogger logger  = logGO.AddComponent<GameLogger>();
        logger.playerTransform      = playerTransform;
        logger.positionLogInterval  = positionSnapInterval;
    }

    // ── Helpers ────────────────────────────────

    /// <summary>Returns a random XZ position at least 3 units from the origin (player spawn).</summary>
    Vector3 SafeRandomPos(float yOffset)
    {
        Vector3 pos;
        float border = sceneHalfSize - 2f;
        int tries = 0;
        do
        {
            float x = Random.Range(-border, border);
            float z = Random.Range(-border, border);
            pos = new Vector3(x, yOffset, z);
            tries++;
        }
        while (new Vector2(pos.x, pos.z).magnitude < 3.5f && tries < 30);

        return pos;
    }

    static Color RandomColor() =>
        new Color(Random.Range(0.25f, 1f), Random.Range(0.25f, 1f), Random.Range(0.25f, 1f));

    /// <summary>Creates a new URP Lit material with the given colour and assigns it.</summary>
    static void ApplyColor(GameObject go, Color color)
    {
        Renderer r = go.GetComponent<Renderer>();
        if (r == null) return;

        // Try URP Lit first, fall back to Standard
        Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                     ?? Shader.Find("Standard");

        Material mat = new Material(shader) { color = color };
        r.material   = mat;
    }
}
