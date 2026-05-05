using System.Collections;
using UnityEngine;

// Attach to any GameObject placed at a fire location.
// Add a SpriteRenderer and assign a fire sprite to it — it will be hidden at start
// and revealed when the player gets close.
// No animator needed, but if you add one it will be used automatically.
// The script adds its own CircleCollider2D trigger at runtime.
public class FireHazard : MonoBehaviour
{
    [Header("Detection")]
    [Tooltip("How close the player must get to ignite the fire")]
    public float detectionRadius = 2.95f;
    [Tooltip("Radius of the actual fire hazard — player inside this loses a life")]
    public float fireRadius = 1f;

    [Header("Timing")]
    [Tooltip("Brief delay after igniting before damage kicks in")]
    public float igniteDelay = 0.35f;

    private Animator anim;
    private SpriteRenderer sr;
    private CircleCollider2D trigger;
    private bool isActive;
    private bool dealingDamage;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();

        // Hide sprite until triggered
        if (sr != null)
            sr.enabled = false;

        trigger = gameObject.AddComponent<CircleCollider2D>();
        trigger.radius = detectionRadius;
        trigger.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (dealingDamage)
        {
            PlayerHealth.Instance?.TakeDamage();
            return;
        }

        if (!isActive)
            StartCoroutine(IgniteSequence());
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (dealingDamage && other.CompareTag("Player"))
            PlayerHealth.Instance?.TakeDamage();
    }

    private IEnumerator IgniteSequence()
    {
        isActive = true;

        if (sr != null)
            sr.enabled = true;

        if (anim != null)
            anim.speed = 1f;

        yield return new WaitForSeconds(igniteDelay);

        // Shrink trigger to the actual fire zone
        trigger.radius = fireRadius;
        dealingDamage = true;
    }
}
