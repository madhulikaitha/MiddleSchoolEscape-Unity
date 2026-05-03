using System.Collections;
using UnityEngine;

public class TrayProjectile : MonoBehaviour
{
    [Header("Tray Sprites")]
    public Sprite spriteFlight;
    public Sprite spriteSplat;

    [Header("Movement")]
    public float speed = 18f;
    public float lifetime = 8f;
    public float spinSpeed = 540f;

    [Header("Hit Effect")]
    public float splatDuration = 0.5f;

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private bool hit;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();

        // Dynamic with no gravity — most reliable for projectiles across all Unity versions
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var col = GetComponent<CircleCollider2D>();
        if (col == null)
            col = gameObject.AddComponent<CircleCollider2D>();
        col.radius = 0.3f;
        col.isTrigger = true;

        if (spriteFlight != null)
            spriteRenderer.sprite = spriteFlight;

        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        if (!hit)
            transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
    }

    public void Launch(Vector2 dir)
    {
        rb.linearVelocity = dir.normalized * speed;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // Disable the collider briefly so the tray doesn't immediately
        // trigger on the counter it's being thrown from
        StartCoroutine(EnableColliderAfterDelay());
    }

    private IEnumerator EnableColliderAfterDelay()
    {
        var col = GetComponent<CircleCollider2D>();
        if (col != null) col.enabled = false;
        yield return new WaitForSeconds(0.2f);
        if (col != null) col.enabled = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hit) return;

        // Never splat on the thrower or the counters
        var n = other.name;
        if (n.IndexOf("student", System.StringComparison.OrdinalIgnoreCase) >= 0) return;
        if (n.IndexOf("counter", System.StringComparison.OrdinalIgnoreCase) >= 0) return;
        if (n.IndexOf("throwpoint", System.StringComparison.OrdinalIgnoreCase) >= 0) return;

        hit = true;

        if (other.CompareTag("Player"))
        {
            other.GetComponent<IHitHandler>()?.OnTrayHit();
        }

        StartCoroutine(Splat());
    }

    private IEnumerator Splat()
    {
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Static;

        if (spriteSplat != null)
            spriteRenderer.sprite = spriteSplat;

        yield return new WaitForSeconds(splatDuration);
        Destroy(gameObject);
    }
}

public interface IHitHandler
{
    void OnTrayHit();
}
