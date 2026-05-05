using System.Collections;
using UnityEngine;

// Attach to a flask_transparent_0 GameObject (SpriteRenderer + Animator, flask_transparent_0.controller).
// Requires a child GameObject named "GasCloud" with:
//   - SpriteRenderer (green_gas_transparent.png)
//   - Animator (green_gas_transparent_3.controller)
//
// When the player walks near the flask:
//   1. Flask bubbles (flaskfume animation plays)
//   2. Gas cloud grows from scale 0 -> 1 over growDuration seconds
//   3. Damage zone is active while gas is at full size
//   4. Gas shrinks and resets after gasDuration seconds
public class GasHazard : MonoBehaviour
{
    [Header("Timing")]
    public float bubbleDuration = 1.5f;
    [Tooltip("Seconds for the cloud to grow from nothing to full size")]
    public float growDuration = 1.5f;
    [Tooltip("Seconds the gas cloud stays at full size and deals damage")]
    public float gasDuration = 5.25f;
    [Tooltip("Seconds for the cloud to shrink back down")]
    public float shrinkDuration = 1f;

    [Header("Detection & Damage")]
    [Tooltip("How close the player must be to trigger the flask")]
    public float detectionRadius = 3f;
    [Tooltip("Radius used for damage overlap check while gas is active")]
    public float gasRadius = 2.05f;

    [Header("References (auto-found if empty)")]
    public SpriteRenderer gasCloudRenderer;
    public Animator gasCloudAnimator;

    private enum GasState { Idle, Activating, Active, Dissipating }
    private GasState state = GasState.Idle;

    private Animator flaskAnimator;
    private Transform gasCloudTransform;
    private CircleCollider2D detectionTrigger;

    private void Awake()
    {
        if (GetComponent<ScienceLabRandomGasSpread>() != null ||
            GetComponentInChildren<ScienceLabRandomGasSpread>(true) != null)
        {
            enabled = false;
            return;
        }

        flaskAnimator = GetComponent<Animator>();
        if (flaskAnimator != null)
            flaskAnimator.speed = 0f;

        var child = transform.Find("GasCloud");
        if (child != null)
        {
            gasCloudTransform = child;
            if (gasCloudRenderer == null)
                gasCloudRenderer = child.GetComponent<SpriteRenderer>();
            if (gasCloudAnimator == null)
                gasCloudAnimator = child.GetComponent<Animator>();
        }

        if (gasCloudRenderer != null)
            gasCloudRenderer.enabled = false;
        if (gasCloudTransform != null)
            gasCloudTransform.localScale = Vector3.zero;
        if (gasCloudAnimator != null)
            gasCloudAnimator.speed = 0f;

        detectionTrigger = gameObject.AddComponent<CircleCollider2D>();
        detectionTrigger.radius = detectionRadius;
        detectionTrigger.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (state == GasState.Idle && other.CompareTag("Player"))
            StartCoroutine(GasSequence());
    }

    // Damage is checked every frame via overlap while gas is active,
    // so invincibility frames in PlayerHealth prevent rapid health loss.
    private void Update()
    {
        if (state != GasState.Active) return;

        int hits = Physics2D.OverlapCircle(transform.position, gasRadius, ContactFilter2D.noFilter, _overlapBuffer);
        for (int i = 0; i < hits; i++)
        {
            Collider2D col = _overlapBuffer[i];
            if (col != null && col.CompareTag("Player"))
                PlayerHealth.Instance?.TakeDamage();
        }
    }

    private readonly Collider2D[] _overlapBuffer = new Collider2D[8];

    private IEnumerator GasSequence()
    {
        state = GasState.Activating;

        // 1. Flask bubbles
        if (flaskAnimator != null)
        {
            flaskAnimator.speed = 1f;
            flaskAnimator.Play("flaskfume", 0, 0f);
        }

        yield return new WaitForSeconds(bubbleDuration);

        // 2. Gas cloud grows from scale 0 to 1
        if (gasCloudRenderer != null)
            gasCloudRenderer.enabled = true;

        if (gasCloudAnimator != null)
        {
            gasCloudAnimator.speed = 1f;
            gasCloudAnimator.Play("gascloud", 0, 0f);
        }

        float elapsed = 0f;
        while (elapsed < growDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / growDuration);
            if (gasCloudTransform != null)
                gasCloudTransform.localScale = Vector3.one * t;
            yield return null;
        }

        if (gasCloudTransform != null)
            gasCloudTransform.localScale = Vector3.one;

        // 3. Active — damage handled in Update
        state = GasState.Active;
        yield return new WaitForSeconds(gasDuration);

        // 4. Shrink and reset
        state = GasState.Dissipating;
        elapsed = 0f;
        while (elapsed < shrinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / shrinkDuration);
            if (gasCloudTransform != null)
                gasCloudTransform.localScale = Vector3.one * (1f - t);
            yield return null;
        }

        if (gasCloudTransform != null)
            gasCloudTransform.localScale = Vector3.zero;
        if (gasCloudRenderer != null)
            gasCloudRenderer.enabled = false;
        if (gasCloudAnimator != null)
            gasCloudAnimator.speed = 0f;
        if (flaskAnimator != null)
            flaskAnimator.speed = 0f;

        state = GasState.Idle;
    }
}
