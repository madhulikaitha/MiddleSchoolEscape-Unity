using System.Collections;
using UnityEngine;

// Attach to an Explodingtile-S GameObject.
// Configure phaseSprites with the 6 phases from a820050d-407f-4085-b982-1705ff566f06.
// The tile starts on phaseSprites[0], then transitions through phases smoothly when
// the player gets close. After the last phase, it switches to hole mode.
//
// Science Lab SL tiles: set crackAnimatorStateName (e.g. explodingtilesl) to drive
// Assets/SL-Animations clips; optional crackLeadSound / crackHitSound play in order.
public class CrackingTile : MonoBehaviour
{
    [Header("Detection")]
    [Tooltip("How close the player must get to trigger the crack animation")]
    public float detectionRadius = 2f;
    [Tooltip("Radius of the open hole that damages the player after animation completes")]
    public float holeRadius = 0.6f;

    [Header("Timing")]
    [Tooltip("Total seconds for all cracking phases before hole opens")]
    public float crackAnimDuration = 1.1f;

    [Header("Phase Visuals")]
    [Tooltip("6 exploding phases in order (phase 1 -> phase 6)")]
    public Sprite[] phaseSprites;
    [Tooltip("Optional hole sprite (e.g. 93944b61-c7bf-4e00-b936-3ae80dbdd8fd)")]
    public Sprite holeSprite;
    [Tooltip("Optional existing hole tile object in the hierarchy to enable when open")]
    public GameObject holeTileObject;
    [Tooltip("If true and a holeTileObject is set, exploding sprite is hidden after crack")]
    public bool hideExplodingTileWhenOpen = true;

    [Header("Animator (Science Lab / SL)")]
    [Tooltip("Animator state name to play when set (e.g. explodingtilesl). Overrides phase sprite swap.")]
    public string crackAnimatorStateName = "";

    [Header("Audio")]
    public AudioClip crackLeadSound;
    [Tooltip("Played after crackLeadSound finishes")]
    public AudioClip crackHitSound;

    private Animator anim;
    private SpriteRenderer sr;
    private AudioSource audioSource;
    private CircleCollider2D zoneTrigger;
    private bool hasCracked;
    private bool isHoleOpen;

    private bool UsesAnimatorCrack => anim != null && !string.IsNullOrEmpty(crackAnimatorStateName);

    private void Awake()
    {
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (crackLeadSound != null || crackHitSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        if (UsesAnimatorCrack)
        {
            if (anim != null)
            {
                anim.enabled = true;
                anim.speed = 0f;
            }
        }
        else if (phaseSprites != null && phaseSprites.Length > 0 && sr != null)
        {
            sr.sprite = phaseSprites[0];
            if (anim != null)
                anim.enabled = false;
        }
        else if (anim != null)
        {
            anim.speed = 0f;
        }

        if (holeTileObject != null)
            holeTileObject.SetActive(false);

        zoneTrigger = gameObject.AddComponent<CircleCollider2D>();
        zoneTrigger.radius = detectionRadius;
        zoneTrigger.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other) => HandleContact(other);
    private void OnTriggerStay2D(Collider2D other) => HandleContact(other);

    private void HandleContact(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (isHoleOpen)
        {
            PlayerHealth.Instance?.TakeDamage();
            return;
        }

        if (!hasCracked)
            StartCoroutine(CrackSequence());
    }

    private IEnumerator CrackSequence()
    {
        hasCracked = true;

        bool usesPhaseSprites = !UsesAnimatorCrack && phaseSprites != null && phaseSprites.Length > 1 && sr != null;

        if (UsesAnimatorCrack)
        {
            anim.enabled = true;
            anim.speed = 1f;
            anim.Play(crackAnimatorStateName, 0, 0f);

            if (audioSource != null && crackLeadSound != null)
                audioSource.PlayOneShot(crackLeadSound);

            if (crackLeadSound != null)
                yield return new WaitForSeconds(crackLeadSound.length);

            if (audioSource != null && crackHitSound != null)
                audioSource.PlayOneShot(crackHitSound);

            yield return StartCoroutine(WaitForAnimatorStateComplete(crackAnimatorStateName));
        }
        else if (usesPhaseSprites)
        {
            float perPhase = crackAnimDuration / (phaseSprites.Length - 1);
            perPhase = Mathf.Max(0.02f, perPhase);

            for (int i = 1; i < phaseSprites.Length; i++)
            {
                sr.sprite = phaseSprites[i];
                yield return new WaitForSeconds(perPhase);
            }
        }
        else if (anim != null)
        {
            anim.speed = 1f;
            anim.Play("Exploding tile", 0, 0f);
            yield return new WaitForSeconds(crackAnimDuration);
        }

        if (holeTileObject != null)
            holeTileObject.SetActive(true);

        if (holeSprite != null && sr != null)
            sr.sprite = holeSprite;

        // Show hole state: either explicit hole sprite, or reveal separate hole tile.
        if (sr != null)
        {
            if (holeSprite != null)
                sr.enabled = true;
            else if (holeTileObject != null && hideExplodingTileWhenOpen)
                sr.enabled = false;
            else if (holeTileObject == null)
                sr.enabled = false;
        }

        // Shrink trigger to the hole size for the damage zone
        zoneTrigger.radius = holeRadius;
        isHoleOpen = true;
    }

    private IEnumerator WaitForAnimatorStateComplete(string stateName)
    {
        yield return null;

        float timeout = 10f;
        while (timeout > 0f)
        {
            AnimatorStateInfo si = anim.GetCurrentAnimatorStateInfo(0);
            if (si.IsName(stateName) && si.normalizedTime >= 1f)
                yield break;

            timeout -= Time.deltaTime;
            yield return null;
        }
    }
}
