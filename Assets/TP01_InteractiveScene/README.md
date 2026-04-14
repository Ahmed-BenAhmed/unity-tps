# TP01 — Interactive Scene
### Unity 6 · URP · New Input System

---

## Table of Contents
1. [File Structure](#file-structure)
2. [How to Run](#how-to-run)
3. [Controls](#controls)
4. [SceneBuilder.cs — Build the scene via code](#1-scenebuilderscs--build-the-scene-via-code)
5. [FirstPersonController.cs — Player movement & click](#2-firstpersoncontrollercs--player-movement--click)
6. [ClickableObject.cs — React to clicks](#3-clickableobjectcs--react-to-clicks)
7. [GameLogger.cs — Record session data](#4-gameloggercs--record-session-data)
8. [AIAgent.cs — Simple AI behaviour](#5-aiagentcs--simple-ai-behaviour)
9. [How everything connects](#how-everything-connects)
10. [Key Unity concepts cheat sheet](#key-unity-concepts-cheat-sheet)

---

## File Structure

```
Assets/TP01_InteractiveScene/
├── Scenes/                      ← Put your TP01 .unity scene here
└── Scripts/
    ├── SceneBuilder.cs          ← Part 2 : builds the scene
    ├── FirstPersonController.cs ← Part 2 & 3 : player + click
    ├── ClickableObject.cs       ← Part 3 : reacts to clicks
    ├── GameLogger.cs            ← Part 4 : session logging
    └── AIAgent.cs               ← Part 5 (Bonus) : AI
```

---

## How to Run

1. Open the project in **Unity Hub** (version `6000.3.11f1`)
2. Create a **new empty scene** (`File → New Scene → Empty`)
3. In the Hierarchy, create an **empty GameObject** (`GameObject → Create Empty`), rename it `_SceneBuilder`
4. Drag **`SceneBuilder.cs`** onto it
5. Hit **Play** — the entire scene is generated automatically

> **Note:** `Library/` (~600 MB of generated cache) is excluded from git and rebuilt automatically on first open.

---

## Controls

| Key / Input | Action |
|---|---|
| `W A S D` / Arrow keys | Move |
| `Left Shift` | Sprint |
| `Space` | Jump |
| `Mouse` | Look around |
| `Left Click` | Interact with an object |
| `Escape` | Unlock the cursor |

---

## Log file location

Saved automatically when you stop the game:
```
%APPDATA%\..\LocalLow\DefaultCompany\My project\session_log.json
```

---

## 1. `SceneBuilder.cs` — Build the scene via code

In Unity, you normally place objects **by hand** in the editor.  
Here we do everything **in code** inside `Awake()` — which Unity calls automatically at startup.

```csharp
void Awake()
{
    Build(); // called automatically by Unity at startup
}

void Build()
{
    SetupLighting();   // directional light (sun)
    CreateFloor();     // the floor (Plane)
    CreateObstacles(); // random cubes and spheres
    CreatePlayer();    // FPS player with camera
    CreateAIAgent();   // the AI agent
    CreateLogger();    // the logging system
}
```

### Creating a primitive object
```csharp
// Unity can create basic shapes with CreatePrimitive
GameObject cube   = GameObject.CreatePrimitive(PrimitiveType.Cube);
GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
GameObject floor  = GameObject.CreatePrimitive(PrimitiveType.Plane);
// Also available: Capsule, Cylinder
```

### Positioning, scaling, rotating
```csharp
cube.transform.position   = new Vector3(x, y, z);       // world position
cube.transform.localScale = new Vector3(1f, 2f, 1f);    // width, height, depth
cube.transform.rotation   = Quaternion.Euler(0, 45f, 0); // rotation in degrees
```

### Applying a colour (URP project)
```csharp
Renderer r = cube.GetComponent<Renderer>();
// This project uses URP — use the URP shader, not "Standard"
Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
mat.color    = new Color(1f, 0f, 0f); // red
r.material   = mat;
```

### Creating the player
```csharp
GameObject player = new GameObject("Player");      // empty object
player.AddComponent<CharacterController>();        // handles collisions & movement
player.AddComponent<FirstPersonController>();      // our control script

// Camera is a child of the player
GameObject camGO = new GameObject("PlayerCamera");
camGO.transform.SetParent(player.transform);       // attach to player
camGO.transform.localPosition = new Vector3(0f, 1.62f, 0f); // eye height
camGO.AddComponent<Camera>();
```

---

## 2. `FirstPersonController.cs` — Player movement & click

### Lock the cursor
```csharp
Cursor.lockState = CursorLockMode.Locked; // hide and lock cursor to center
Cursor.visible   = false;
```

### Reading input with the New Input System
The project uses Unity's **new input system** (`com.unity.inputsystem`).  
We can poll devices directly without any configuration file:

```csharp
using UnityEngine.InputSystem;

// Keyboard
Keyboard.current.wKey.isPressed              // is W held down?
Keyboard.current.spaceKey.wasPressedThisFrame // was Space just pressed?

// Mouse
Mouse.current.delta.ReadValue()                  // mouse movement (Vector2)
Mouse.current.leftButton.wasPressedThisFrame     // left click?
```

### Camera rotation (mouse look)
```csharp
Vector2 delta = Mouse.current.delta.ReadValue();

// Horizontal → rotate the whole player body
transform.Rotate(Vector3.up, delta.x * sensitivity, Space.World);

// Vertical → tilt only the camera, with a clamp so you can't look fully backwards
_pitch -= delta.y * sensitivity;
_pitch  = Mathf.Clamp(_pitch, -85f, 85f);
cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
```

### Movement with CharacterController
```csharp
float x = 0f, z = 0f;
if (Keyboard.current.wKey.isPressed) z += 1f; // forward
if (Keyboard.current.sKey.isPressed) z -= 1f; // backward
if (Keyboard.current.aKey.isPressed) x -= 1f; // left
if (Keyboard.current.dKey.isPressed) x += 1f; // right

Vector3 move = (transform.right * x + transform.forward * z).normalized;
_cc.Move(move * speed * Time.deltaTime);
// Time.deltaTime = time since last frame → smooth movement regardless of FPS
```

### Gravity and jump
```csharp
// Accumulate downward speed (gravity)
_verticalVelocity += gravity * Time.deltaTime; // gravity = -20

// Jump: real physics formula  v = √(2 * |g| * height)
if (spacePressed && isGrounded)
    _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

_cc.Move(Vector3.up * _verticalVelocity * Time.deltaTime);
```

### Detecting a click on an object (Raycast)
```csharp
// Fire an invisible ray from the camera straight ahead
Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

if (Physics.Raycast(ray, out RaycastHit hit, clickRange))
{
    // hit contains the object that was struck
    var clickable = hit.collider.GetComponent<ClickableObject>();
    if (clickable != null)
        clickable.OnClick(); // call the method on the hit object
}
```

> A **Raycast** is like firing an invisible arrow. If it hits something, Unity gives you all the info: which object, where it was hit, the surface normal…

---

## 3. `ClickableObject.cs` — React to clicks

This script is attached to **every obstacle**.  
It stores the original colour and cycles through a palette on each click:

```csharp
private static readonly Color[] Palette = {
    Color.red, Color.green, Color.blue, Color.yellow, // etc.
};

private int _clickCount; // how many times this object has been clicked

public void OnClick()
{
    _clickCount++;
    Color next = Palette[(_clickCount - 1) % Palette.Length];
    // % (modulo) = wraps back to the start when we pass the end of the array

    _renderer.material.color = next;
    Debug.Log($"[Click] \"{gameObject.name}\" → {next}");
}
```

> `gameObject.name` = the object's name in the Unity Hierarchy  
> `Debug.Log()` = prints a message in the Unity Console

---

## 4. `GameLogger.cs` — Record session data

### Singleton pattern
A singleton is a class with **exactly one instance** in the entire program.  
Useful for global systems like the logger:

```csharp
public static GameLogger Instance { get; private set; }

void Awake()
{
    if (Instance != null && Instance != this)
    {
        Destroy(gameObject); // destroy any duplicate
        return;
    }
    Instance = this;             // register as the unique instance
    DontDestroyOnLoad(gameObject); // survive scene changes
}
```

Any other script can now do:
```csharp
GameLogger.Instance?.LogClick();
// ?. means "call LogClick() only if Instance is not null"
```

### Recording the position automatically (Coroutine)
A **Coroutine** is a method that can **pause itself** and resume later — without blocking the game:

```csharp
IEnumerator PositionSnapRoutine()
{
    while (true)                          // infinite loop
    {
        yield return new WaitForSeconds(5f); // pause for 5 seconds
        SnapPosition();                      // then record the position
    }
}

// Start the coroutine
StartCoroutine(PositionSnapRoutine());
```

### Saving to JSON
```csharp
string json = JsonUtility.ToJson(_data, prettyPrint: true);
// JsonUtility converts a C# object to a JSON string

string path = Path.Combine(Application.persistentDataPath, "session_log.json");
// Application.persistentDataPath = Unity's save folder (AppData/LocalLow/...)

File.WriteAllText(path, json); // write to disk
```

Data classes must be marked `[Serializable]` so `JsonUtility` can convert them:
```csharp
[Serializable]
public class PositionEntry
{
    public float time;
    public float x, y, z;
}
```

### Sample output
```json
{
    "sessionStart": "2026-04-14 20:15:25",
    "sessionEnd":   "2026-04-14 20:15:43",
    "totalClicks":  14,
    "timeInScene":  15.35,
    "positionLog": [
        { "time": 5.0,  "x": 0.78, "y": 0.08, "z": 11.34 },
        { "time": 10.0, "x": 9.58, "y": 0.08, "z": 5.32  }
    ],
    "interactions": [
        { "time": 4.24, "objectName": "Cube_5",   "action": "clicked" },
        { "time": 7.86, "objectName": "Cube_1",   "action": "clicked" },
        { "time": 10.47,"objectName": "Sphere_2", "action": "clicked" }
    ]
}
```

---

## 5. `AIAgent.cs` — Simple AI behaviour

### Two modes
```csharp
public enum AgentMode { FollowPlayer, RandomWander }
// enum = a named list of choices
```

### Follow the player
```csharp
Vector3 toTarget = target.position - transform.position; // vector toward player
toTarget.y = 0f; // ignore height difference (stay on the floor)

if (toTarget.magnitude <= stopDistance) return; // close enough → stop

transform.position += toTarget.normalized * moveSpeed * Time.deltaTime;
```

### Random wander
```csharp
// Pick a random point inside a circle
Vector2 rand      = Random.insideUnitCircle * wanderRadius;
_wanderTarget     = origin + new Vector3(rand.x, 0f, rand.y);

// When we arrive → pick a new point
if (distanceToTarget < 0.8f || timer <= 0f)
    PickNewWanderTarget();
```

### Obstacle avoidance (feeler rays)
Three rays are cast ahead to "feel" for obstacles:

```csharp
bool hitFwd   = Physics.Raycast(origin, forward,       feelerRange);
bool hitLeft  = Physics.Raycast(origin, leftDiagonal,  feelerRange);
bool hitRight = Physics.Raycast(origin, rightDiagonal, feelerRange);

if (hitFwd || hitLeft || hitRight)
{
    // Turn 90° away from the obstacle
    float sign = hitRight ? -1f : 1f;
    desired = Quaternion.Euler(0, 90f * sign, 0) * desired;
}
```

### Smooth rotation
```csharp
Quaternion targetRotation = Quaternion.LookRotation(direction);
// Slerp = Spherical Linear Interpolation → gradual, natural rotation
transform.rotation = Quaternion.Slerp(
    transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
```

---

## How everything connects

```
SceneBuilder (Awake)
    │
    ├── creates ──► Floor (Plane)
    │
    ├── creates ──► Cube_1 … Cube_5  ──► ClickableObject  ◄── Raycast ──┐
    ├── creates ──► Sphere_1 … Sphere_4 ► ClickableObject  ◄── Raycast ──┤
    │                                                                      │
    ├── creates ──► Player ──► FirstPersonController ──────────────────────┘
    │                               │
    │                               ├──► GameLogger.LogClick()
    │                               └──► GameLogger.LogInteraction()
    │
    ├── creates ──► AIAgent ──► follows Player  OR  wanders randomly
    │                               └──► feeler Raycasts for obstacle avoidance
    │
    └── creates ──► GameLogger ──► position snapshot every 5 s
                                   saves session_log.json on quit
```

---

## Key Unity concepts cheat sheet

| Concept | Definition |
|---|---|
| `MonoBehaviour` | Base class for all Unity scripts |
| `Awake()` | Called first, before `Start()`, even if the script is disabled |
| `Start()` | Called just before the first frame |
| `Update()` | Called **every frame** (~60×/sec) |
| `Time.deltaTime` | Time since last frame → use it to make movement frame-rate independent |
| `Transform` | Holds the position, rotation and scale of a GameObject |
| `Raycast` | Fire an invisible ray and detect what it hits |
| `CharacterController` | Moves a character while handling collisions, without needing a Rigidbody |
| `Coroutine` | A method that can pause (`yield`) without blocking the game loop |
| `Singleton` | Design pattern — guarantees one and only one instance of a class |
| `[Serializable]` | Marks a class so Unity / JsonUtility can convert it to/from JSON |
| `Debug.Log()` | Prints a message to the Unity Console |
| `GetComponent<T>()` | Retrieves a component attached to the same GameObject |
| `AddComponent<T>()` | Attaches a new component to a GameObject at runtime |
