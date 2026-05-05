using System.Collections;
using MiddleSchoolEscape;
using UnityEngine;

public class PlayerHealth : MonoBehaviour, IHitHandler
{
    public static PlayerHealth Instance { get; private set; }

    [Tooltip("Lower = tighter runs; leaderboard stays time-based.")]
    public int MaxHearts = 4;
    public int CurrentHearts { get; private set; }

    // Invoked whenever hearts change so the HUD can refresh.
    public event System.Action OnHealthChanged;

    [Header("Collider vs maze")]
    [Tooltip("Capsule relative to sprite bounds. Lower = narrower corridors; tray hits stay usable.")]
    [SerializeField, Range(0.45f, 1f)] private float colliderVisualFitMultiplier = 0.62f;

    [Tooltip("Extra shrink on width only (helps hall corners without shortening hit height).")]
    [SerializeField, Range(0.65f, 1f)] private float horizontalColliderMultiplier = 0.78f;

    [Tooltip("Capsule width/height are scaled down uniformly so the longer axis does not exceed this (world units).")]
    [SerializeField] private float maxColliderExtent = 2f;

    [Header("Hit Feedback")]
    [Tooltip("Shorter windows mean hazards and trays can tag you again sooner.")]
    public float invincibilityDuration = 0.78f;
    public float flashInterval = 0.09f;

    private static PhysicsMaterial2D sharedPlayerPhysicsMaterial;

    private bool isInvincible;
    private SpriteRenderer spriteRenderer;

    private static PhysicsMaterial2D SharedFrictionlessMaterial()
    {
        if (sharedPlayerPhysicsMaterial == null)
        {
            sharedPlayerPhysicsMaterial = new PhysicsMaterial2D("Player_FrictionlessMaze")
            {
                friction = 0f,
                bounciness = 0f
            };
        }

        return sharedPlayerPhysicsMaterial;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        CurrentHearts = MaxHearts;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        FitCapsuleColliderToVisual();
        ApplyCharacterUnderHazardSortOrder();
    }

    private void ApplyCharacterUnderHazardSortOrder()
    {
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
            sr.sortingOrder = HazardVisualSortOrders.Player;
    }

    /// <summary>Call after scale/sprite changes (e.g. <see cref="PlayerSetupFlow"/> spawn) so the capsule matches the final size — avoids an oversized collider when script order varies.</summary>
    public void RefitColliderToSprite()
    {
        FitCapsuleColliderToVisual();
    }

    /// <summary>
    /// Scene/default capsules are often tiny vs. the sprite; tray hits only registered near the center.
    /// </summary>
    private void FitCapsuleColliderToVisual()
    {
        var cap = GetComponent<CapsuleCollider2D>();
        var sr = spriteRenderer != null ? spriteRenderer : GetComponent<SpriteRenderer>();
        if (cap == null || sr == null || sr.sprite == null)
            return;

        Bounds worldBounds = sr.bounds;
        Vector2 size = worldBounds.size * colliderVisualFitMultiplier;
        size.x *= horizontalColliderMultiplier;

        float major = Mathf.Max(size.x, size.y);
        if (major > maxColliderExtent && major > 1e-4f)
            size *= maxColliderExtent / major;

        cap.size = size;

        Vector3 localCenter = transform.InverseTransformPoint(worldBounds.center);
        cap.offset = new Vector2(localCenter.x, localCenter.y);

        cap.direction = size.y >= size.x ? CapsuleDirection2D.Vertical : CapsuleDirection2D.Horizontal;
        cap.sharedMaterial = SharedFrictionlessMaterial();
    }

    // Called by TrayProjectile when the tray hits the player.
    public void OnTrayHit()
    {
        int before = CurrentHearts;
        TakeDamage();
        if (CurrentHearts >= before)
            return;
        var narr = NarrativeDialogueController.Instance;
        if (narr != null && narr.GetCurrentZone() == NarrativeDialogueController.NarrativeZone.Cafeteria)
            narr.NotifyPlayerDamaged(NarrativeDialogueController.NarrativeZone.Cafeteria);
    }

    // Call this from any future mistake source.
    public void TakeDamage()
    {
        if (isInvincible || CurrentHearts <= 0) return;

        CurrentHearts = Mathf.Max(0, CurrentHearts - 1);
        OnHealthChanged?.Invoke();

        if (CurrentHearts <= 0)
        {
            StartCoroutine(HandleDeath());
        }
        else
        {
            StartCoroutine(InvincibilityFrames());
        }
    }

    private IEnumerator InvincibilityFrames()
    {
        isInvincible = true;

        float elapsed = 0f;
        while (elapsed < invincibilityDuration)
        {
            if (spriteRenderer != null)
                spriteRenderer.color = spriteRenderer.color == Color.white ? Color.red : Color.white;
            yield return new WaitForSeconds(flashInterval);
            elapsed += flashInterval;
        }

        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;

        isInvincible = false;
    }

    private IEnumerator HandleDeath()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = Color.red;

        yield return new WaitForSeconds(0.8f);

        // Keep name + character; respawn in lobby with full lives and timer from zero (no scene reload).
        transform.position = PlayerSessionData.LobbyWorldPosition;
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        CurrentHearts = MaxHearts;
        isInvincible = false;
        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;

        PlayerSessionData.ResetRunAtLobby();
        OnHealthChanged?.Invoke();
    }
}
