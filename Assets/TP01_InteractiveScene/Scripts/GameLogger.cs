using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// ─────────────────────────────────────────────
//  Data models (serialised to JSON)
// ─────────────────────────────────────────────

[Serializable]
public class PositionEntry
{
    public float time;
    public float x, y, z;
}

[Serializable]
public class InteractionEntry
{
    public float time;
    public string objectName;
    public string action;
}

[Serializable]
public class SessionData
{
    public string sessionStart;
    public string sessionEnd;
    public int    totalClicks;
    public float  timeInScene;
    public List<PositionEntry>   positionLog  = new List<PositionEntry>();
    public List<InteractionEntry> interactions = new List<InteractionEntry>();
}

// ─────────────────────────────────────────────
//  GameLogger — singleton, attach to any GO
// ─────────────────────────────────────────────

public class GameLogger : MonoBehaviour
{
    public static GameLogger Instance { get; private set; }

    [Header("Logging Settings")]
    [Tooltip("Interval (seconds) between automatic player-position snapshots.")]
    public float positionLogInterval = 5f;

    [Tooltip("Reference to the player Transform (set by SceneBuilder or manually).")]
    public Transform playerTransform;

    private SessionData _data;
    private float       _startTime;

    // ── Lifecycle ──────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _data             = new SessionData();
        _data.sessionStart = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        _startTime        = Time.time;
    }

    void Start()
    {
        StartCoroutine(PositionSnapRoutine());
    }

    IEnumerator PositionSnapRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(positionLogInterval);
            SnapPosition();
        }
    }

    void OnApplicationQuit()  => Save();
    void OnApplicationPause(bool p) { if (p) Save(); }

    // ── Public API ─────────────────────────────

    /// <summary>Record one click event (increments counter).</summary>
    public void LogClick() => _data.totalClicks++;

    /// <summary>Record an interaction with a named object.</summary>
    public void LogInteraction(string objectName, string action = "clicked")
    {
        _data.interactions.Add(new InteractionEntry
        {
            time       = Elapsed(),
            objectName = objectName,
            action     = action
        });
        Debug.Log($"[Logger] {action} → \"{objectName}\"  (t={Elapsed():F2}s)");
    }

    /// <summary>Manually snapshot the player position.</summary>
    public void SnapPosition()
    {
        if (playerTransform == null) return;
        Vector3 p = playerTransform.position;
        _data.positionLog.Add(new PositionEntry
        {
            time = Elapsed(),
            x    = Round(p.x),
            y    = Round(p.y),
            z    = Round(p.z)
        });
    }

    /// <summary>Write the session to a JSON file and print the save path.</summary>
    public void Save()
    {
        SnapPosition();   // final position before quitting
        _data.sessionEnd   = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        _data.timeInScene  = Elapsed();

        string json = JsonUtility.ToJson(_data, prettyPrint: true);
        string path = Path.Combine(Application.persistentDataPath, "session_log.json");

        try
        {
            File.WriteAllText(path, json);
            Debug.Log($"[Logger] Saved → {path}");
            Debug.Log($"[Logger] Summary  clicks={_data.totalClicks}  time={_data.timeInScene:F1}s" +
                      $"  positions={_data.positionLog.Count}  interactions={_data.interactions.Count}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Logger] Save failed: {e.Message}");
        }
    }

    // ── Helpers ────────────────────────────────

    float Elapsed()          => Time.time - _startTime;
    static float Round(float v) => Mathf.Round(v * 100f) / 100f;
}
