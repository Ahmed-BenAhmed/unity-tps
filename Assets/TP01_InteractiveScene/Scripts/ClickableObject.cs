using UnityEngine;

/// <summary>
/// Attach to any obstacle to make it clickable.
/// Each click cycles through a palette of colours and logs to the console.
/// The player's FirstPersonController does the raycast; this script reacts.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class ClickableObject : MonoBehaviour
{
    // Colour palette cycled through on successive clicks
    private static readonly Color[] Palette =
    {
        new Color(0.93f, 0.23f, 0.23f),   // red
        new Color(0.18f, 0.80f, 0.44f),   // green
        new Color(0.20f, 0.60f, 0.95f),   // blue
        new Color(1.00f, 0.84f, 0.00f),   // yellow
        new Color(0.00f, 0.90f, 0.90f),   // cyan
        new Color(0.90f, 0.20f, 0.90f),   // magenta
        new Color(1.00f, 0.50f, 0.00f),   // orange
        new Color(0.60f, 0.20f, 0.80f),   // purple
    };

    private Renderer _renderer;
    private Color    _originalColor;
    private int      _clickCount;

    // ── Lifecycle ──────────────────────────────

    void Awake()
    {
        _renderer     = GetComponent<Renderer>();
        _originalColor = _renderer.material.color;
    }

    // ── Public API ─────────────────────────────

    /// <summary>Called by FirstPersonController when a left-click ray hits this object.</summary>
    public void OnClick()
    {
        _clickCount++;
        Color next = Palette[(_clickCount - 1) % Palette.Length];
        _renderer.material.color = next;

        Debug.Log($"[Click] \"{gameObject.name}\"  click #{_clickCount}  →  color = {ColorUtility.ToHtmlStringRGB(next)}");
    }

    /// <summary>Restore the original colour and reset the counter.</summary>
    public void ResetColor()
    {
        _clickCount = 0;
        _renderer.material.color = _originalColor;
    }
}
