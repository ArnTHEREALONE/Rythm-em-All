using UnityEngine;

/// <summary>
/// Manages the color of a child outline sprite based on whether
/// the enemy is within the player's attack range.
/// Attach this to the enemy prefab root and assign the outline sprite.
/// </summary>
public class EnemyOutlineRange : MonoBehaviour
{
    [Header("=== Outline Sprite ===")]
    [Tooltip("Reference to the child SpriteRenderer used as the range outline.")]
    [SerializeField] private SpriteRenderer outlineSprite;

    [Header("=== Colors ===")]
    [Tooltip("Color applied when the enemy is outside the player's hit range.")]
    public Color outOfRangeColor = Color.white;

    [Tooltip("Color applied when the enemy is inside the player's hit range.")]
    public Color inRangeColor = Color.green;

    /// <summary>Cached reference to the player's combat component.</summary>
    private PlayerCombat player;

    private void Start()
    {
        player = FindFirstObjectByType<PlayerCombat>();
    }

    private void Update()
    {
        if (outlineSprite == null || player == null) return;

        float distance = Vector3.Distance(transform.position, player.transform.position);

        outlineSprite.color = distance <= player.hitRange
            ? inRangeColor
            : outOfRangeColor;
    }
}
