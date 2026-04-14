# Unity TPs — Game Development

> Unity **6000.3.11f1** · Universal Render Pipeline · New Input System

This repository contains all practical assignments (TPs) for the Game Development module.
Each TP lives in its own folder under `Assets/` and has its own scenes and scripts.

---

## Project Structure

```
Assets/
├── TP01_InteractiveScene/   # TP01 — Interactive 3D scene
│   ├── Scenes/
│   └── Scripts/
├── TP02_.../                # Future TPs added here
├── Shared/                  # Utilities shared across TPs
└── Settings/                # URP render pipeline settings
```

---

## TPs

| # | Name | Description | Status |
|---|------|-------------|--------|
| 01 | Interactive Scene | FPS movement, clickable objects, logging, AI agent | ✅ Done |

---

## TP01 — Interactive Scene

**Features:**
- 🌍 Procedural scene (floor + obstacles) built entirely via code (`SceneBuilder.cs`)
- 🎮 First-person controller — WASD, mouse look, sprint, jump
- 🖱️ Click interaction — objects cycle through colours on click
- 📊 Session logger — tracks clicks, time, player positions, interactions → saved to `session_log.json`
- 🤖 AI agent — follows the player or wanders randomly with obstacle avoidance

**Controls:**

| Key | Action |
|-----|--------|
| `WASD` / Arrows | Move |
| `Left Shift` | Sprint |
| `Space` | Jump |
| `Mouse` | Look |
| `Left Click` | Interact with object |
| `Escape` | Unlock cursor |

**Log file location:**
```
%APPDATA%\..\LocalLow\DefaultCompany\My project\session_log.json
```

---

## How to open a TP

1. Open the project in **Unity Hub** (version `6000.3.11f1`)
2. In the **Project** panel, navigate to `Assets/TP0X_.../Scenes/`
3. Double-click the scene to open it
4. Hit **Play**

---

## Setup (first time)

```bash
git clone <repo-url>
# Open in Unity Hub — let it reimport (Library is not committed)
```

> The `Library/` folder (~600 MB) is excluded from git and regenerated automatically by Unity on first open.
