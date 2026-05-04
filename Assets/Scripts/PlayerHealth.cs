using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour, IHitHandler
{
    public static PlayerHealth Instance { get; private set; }

    public int MaxHearts = 5;
    public int CurrentHearts { get; private set; }

    // Invoked whenever hearts change so the HUD can refresh.
    public event System.Action OnHealthChanged;

    [Header("Hit Feedback")]
    public float invincibilityDuration = 1.2f;
    public float flashInterval = 0.1f;

    private bool isInvincible;
    private SpriteRenderer spriteRenderer;

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

    // Called by TrayProjectile when the tray hits the player.
    public void OnTrayHit() => TakeDamage();

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
