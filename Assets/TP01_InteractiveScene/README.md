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

---

# Part 2 — Git & GitHub

## Table of Contents
1. [What is Git?](#1-what-is-git)
2. [What is GitHub?](#2-what-is-github)
3. [How the repo was set up](#3-how-the-repo-was-set-up)
4. [The .gitignore file](#4-the-gitignore-file)
5. [Core Git commands used](#5-core-git-commands-used)
6. [Rewriting history](#6-rewriting-history)
7. [Handling large files](#7-handling-large-files)
8. [Project structure decision](#8-project-structure-decision)
9. [Workflow for future TPs](#9-workflow-for-future-tps)
10. [Git concepts cheat sheet](#10-git-concepts-cheat-sheet)

---

## 1. What is Git?

**Git** is a version control system — it tracks every change you make to your files over time.  
Think of it like a timeline of your project: you can go back to any point, compare versions, and work on multiple features in parallel.

```
[Initial commit] ──► [TP01 added] ──► [README added] ──► [fix: large files removed]
     a279992               881c4ed           308353d              7fbadff
```

Each point on that timeline is called a **commit** — a snapshot of your project at a given moment.

### Why use Git for a Unity project?
- Your professor can see your progress commit by commit
- If you break something, you can roll back to the last working state
- Multiple TPs stay organised in one place without overwriting each other
- You never lose work

---

## 2. What is GitHub?

**GitHub** is a website that hosts your Git repository online (the "remote").  
Your local repo lives on your machine — GitHub is the cloud backup that your professor can access.

```
Your machine  ──── git push ────►  GitHub (remote)
              ◄─── git pull ────
```

---

## 3. How the repo was set up

### Step 1 — Initialise Git locally
```bash
git init
# Creates a hidden .git/ folder — this is what makes the folder a Git repo
```

### Step 2 — Configure identity
```bash
git config user.name  "Ahmed-BenAhmed"
git config user.email "..."
# Git needs to know who is making commits
```

### Step 3 — Stage files
```bash
git add .gitignore README.md Assets/ Packages/ ProjectSettings/
# "Staging" = selecting which files to include in the next commit
# Think of it like putting items in a box before sealing it
```

### Step 4 — Commit
```bash
git commit -m "Initial commit — Unity project setup + TP01"
# "Seals the box" and adds it to the timeline with a label (the message)
```

### Step 5 — Create the remote and push
```bash
gh repo create unity-tps --public   # create the repo on GitHub
git push -u origin master           # upload local commits to GitHub
# -u sets "origin/master" as the default remote branch (only needed once)
```

---

## 4. The `.gitignore` file

Git would normally track **every** file in the project folder.  
For Unity, that's a problem — some folders are huge and should never be committed:

| Folder | Size | Why excluded |
|---|---|---|
| `Library/` | ~600 MB | Generated cache — rebuilt automatically by Unity on every machine |
| `Temp/` | variable | Temporary build files — deleted by Unity itself |
| `Logs/` | small | Editor logs — irrelevant to the project |
| `UserSettings/` | small | Personal editor preferences (window layout, etc.) |
| `.vs/` | small | Visual Studio cache |
| `*.csproj`, `*.sln` | small | IDE project files — regenerated by Unity |

The `.gitignore` file tells Git: **"ignore these paths, never track them."**

```gitignore
# Example entries from our .gitignore
/[Ll]ibrary/        # the L can be uppercase or lowercase
/[Tt]emp/
*.csproj
*.sln
```

> Without `.gitignore`, a `git add .` would try to commit 600 MB of cache every time. With it, only your actual source files (scripts, scenes, settings) are tracked.

### What IS committed
```
Assets/          ← your scenes, scripts, materials, textures (your work)
Packages/        ← package list (manifest.json) — tells Unity what to install
ProjectSettings/ ← Unity project configuration
.gitignore       ← the ignore rules themselves
README.md        ← documentation
```

---

## 5. Core Git commands used

### `git status`
Shows which files have changed since the last commit:
```bash
git status
# M  = modified
# A  = added (new file staged)
# ?? = untracked (not yet added to git)
```

### `git add`
Stages files for the next commit:
```bash
git add Assets/TP01_InteractiveScene/   # stage a specific folder
git add .gitignore README.md            # stage specific files
# Never use "git add ." blindly — you might accidentally stage secrets or large files
```

### `git commit`
Creates a snapshot (commit) from staged files:
```bash
git commit -m "short description of what changed and why"
# The message should explain WHY, not just WHAT
# Good:  "fix: remove large textures to avoid GitHub 50MB limit"
# Bad:   "changed files"
```

### `git push`
Uploads local commits to GitHub:
```bash
git push              # push to the tracked remote branch
git push -u origin master  # first push — sets the upstream
```

### `git log`
Displays the commit history:
```bash
git log --oneline
# 7fbadff fix: remove Starfield Skybox asset from tracking
# 881c4ed docs(TP01): add full tutorial README
# a279992 Initial commit
```

### `git rm --cached`
Stops tracking a file/folder **without deleting it from disk**:
```bash
git rm -r --cached "Assets/Starfield Skybox"
# The folder stays on your machine but Git forgets it ever existed
# Combined with .gitignore → it will never be tracked again
```

---

## 6. Rewriting history

### `git commit --amend`
Modifies the **last** commit (message or content) before pushing:
```bash
git commit --amend -m "corrected commit message"
# Only safe to use before pushing — never amend a commit that's already on GitHub
```

### `git filter-branch --msg-filter`
Rewrites **all** commit messages in history using a shell command:
```bash
git filter-branch --msg-filter 'sed "/Co-Authored-By:/d"' -- --all
# sed = stream editor, removes every line matching the pattern
# -- --all = apply to every commit in every branch
```

> This was used to remove the `Co-Authored-By:` lines from both commits after they were created. Rewriting history changes the commit hash (the SHA like `a279992`) — that's why a force-push would normally be needed, but since the repo was recreated fresh it wasn't necessary.

---

## 7. Handling large files

GitHub has a **50 MB soft limit** per file (100 MB hard limit).  
The Starfield Skybox asset contained textures of 51–55 MB each, which triggered warnings.

### Solution: remove from tracking + add to `.gitignore`

```bash
# 1. Untrack the folder (files stay on disk)
git rm -r --cached "Assets/Starfield Skybox"

# 2. Add the rule to .gitignore so it's never tracked again
echo "/Assets/Starfield Skybox/" >> .gitignore

# 3. Commit the removal
git commit -m "fix: remove large third-party textures from tracking"

# 4. Push — no more warnings
git push
```

### Why not use Git LFS?
Git LFS (Large File Storage) is the "proper" solution for large binary files in Git.  
For this project it's overkill — the Starfield Skybox is a downloaded asset package that any developer can re-import. It doesn't need to be in version control at all.

**Rule of thumb for Unity projects:**
- ✅ Track: your scripts, your scenes, your materials, small textures you created
- ❌ Don't track: downloaded asset packages, generated files, anything > 50 MB

---

## 8. Project structure decision

### Why one repo for all TPs?

| Option | Pros | Cons |
|---|---|---|
| **One repo, folders per TP** ✅ | One `Library/` (600MB), shared packages, clean history | — |
| One repo per TP | Total isolation | Duplicates 600MB Library per TP, messy for professor |
| One branch per TP | Clean separation | Can't see multiple TPs at once, complex checkout |

### Why move scripts to `TP01_InteractiveScene/`?
Unity tracks every file by a **GUID** (a unique ID stored in the `.meta` file next to each asset).  
When you move files, you **must** move the `.meta` file alongside them — otherwise Unity loses the reference and shows errors.

```bash
# Correct way (outside Unity editor):
mv Scripts/MyScript.cs       TP01/Scripts/MyScript.cs
mv Scripts/MyScript.cs.meta  TP01/Scripts/MyScript.cs.meta
# Both files moved together → GUID preserved → no broken references
```

> The safest way is always to move files **inside the Unity editor** (drag & drop in the Project panel) — Unity handles the `.meta` automatically. Moving via file explorer or bash is fine too, as long as you move the `.meta` file at the same time.

---

## 9. Workflow for future TPs

When you receive TP02, TP03…:

```bash
# 1. Create the folder structure (inside Unity editor or via bash)
mkdir -p Assets/TP02_YourTopicName/Scripts
mkdir -p Assets/TP02_YourTopicName/Scenes

# 2. Work on the TP — write scripts, build scenes

# 3. Check what changed
git status

# 4. Stage only your TP folder
git add Assets/TP02_YourTopicName/

# 5. Commit with a clear message
git commit -m "TP02 — brief description of what the TP covers"

# 6. Push to GitHub
git push
```

Your professor will see one commit per TP, clean and readable:
```
7fbadff  fix: remove large textures
308353d  docs(TP01): add tutorial README
881c4ed  docs(TP01): add full explanation
a279992  Initial commit — TP01 Interactive Scene
```

---

## 10. Git concepts cheat sheet

| Concept | Definition |
|---|---|
| **Repository (repo)** | A folder tracked by Git — contains your project + its full history |
| **Commit** | A saved snapshot of the project at a point in time |
| **Staging area** | The "waiting room" for files before they become a commit |
| **Branch** | A parallel timeline — `master` is the default |
| **Remote** | A copy of the repo hosted elsewhere (GitHub) |
| **Origin** | The default name Git gives to the remote repo |
| **Push** | Upload local commits to the remote |
| **Pull** | Download remote commits to your local machine |
| **Clone** | Download a full copy of a remote repo for the first time |
| **.gitignore** | A file listing paths that Git should never track |
| **.meta file** | Unity's file that stores the GUID of each asset — always commit alongside the asset |
| **GUID** | A unique ID Unity uses to reference assets regardless of their filename |
| `git rm --cached` | Stop tracking a file without deleting it from disk |
| `git commit --amend` | Modify the last commit before pushing |
| `git filter-branch` | Rewrite multiple commits in history |
| **Git LFS** | Extension for storing large binary files (>50 MB) in Git |
