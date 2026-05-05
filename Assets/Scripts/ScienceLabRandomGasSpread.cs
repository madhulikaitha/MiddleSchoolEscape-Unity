using System.Collections;
using UnityEngine;

// For Science Lab flasks / gas objects using Assets/SL-Animations/gas_sprite_transparent_0.controller (state "gasspread").
// Attach to each GameObject that has the Animator + SpriteRenderer for gas; each instance picks its own random delays
// so bursts are unsynchronized ("random at all times").
//
// If you use separate Flask-S and Gasspread-S objects per station, attach this only to the objects that should show
// the spreading gas animation (often the Gasspread-S objects), or to both if both should animate.
[DisallowMultipleComponent]
public class ScienceLabRandomGasSpread : MonoBehaviour
{
    [Tooltip("Animator state name from gas_sprite_transparent_0.controller")]
    public string gasStateName = "gasspread";

    [Tooltip("Clip length in seconds (gasspread.anim stop time ~3.17). Increase if you change the clip.")]
    public float burstDuration = 3.17f;

    [Tooltip("Minimum idle time after a burst before the next one")]
    public float minPauseSeconds = 1.5f;

    [Tooltip("Maximum idle time after a burst before the next one")]
    public float maxPauseSeconds = 5f;

    [Tooltip("Random delay before the first burst on this object (spreads out startup sync)")]
    public float maxInitialDelaySeconds = 4f;

    [Header("Player damage & audio")]
    [Tooltip("Radius from this transform while gas is spreading — player inside loses a life")]
    public float hazardRadius = 1.2f;

    [Tooltip("Played when the player is hit by spreading gas (e.g. drag Assets/Audio/gasspreadingsound.mp3 here)")]
    public AudioClip gasSpreadHitSound;

    private Animator _animator;
    private AudioSource _audioSource;
    private bool _hazardActive;
    private readonly Collider2D[] _overlapBuffer = new Collider2D[8];

    private void Awake()
    {
        _animator = GetComponent<Animator>();

        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null && gasSpreadHitSound != null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
        }
    }

    private void OnEnable()
    {
        if (_animator != null)
            StartCoroutine(GasBurstLoop());
    }

    private void OnDisable()
    {
        _hazardActive = false;
        StopAllCoroutines();
    }

    private void Update()
    {
        if (!_hazardActive) return;

        int count = Physics2D.OverlapCircleNonAlloc(transform.position, hazardRadius, _overlapBuffer);
        for (int i = 0; i < count; i++)
        {
            var hit = _overlapBuffer[i];
            if (hit == null || !hit.CompareTag("Player")) continue;

            var ph = PlayerHealth.Instance;
            if (ph == null) continue;

            int heartsBefore = ph.CurrentHearts;
            ph.TakeDamage();

            if (ph.CurrentHearts < heartsBefore && gasSpreadHitSound != null)
            {
                if (_audioSource == null)
                {
                    _audioSource = GetComponent<AudioSource>();
                    if (_audioSource == null)
                    {
                        _audioSource = gameObject.AddComponent<AudioSource>();
                        _audioSource.playOnAwake = false;
                    }
                }
                _audioSource.PlayOneShot(gasSpreadHitSound);
            }

            break;
        }
    }

    private IEnumerator GasBurstLoop()
    {
        if (_animator == null)
            yield break;

        yield return new WaitForSeconds(Random.Range(0f, maxInitialDelaySeconds));

        while (enabled)
        {
            // Fully reset the previous playback so the next burst never stacks or blends with the last one.
            StartBurstFromBeginning();
            _hazardActive = true;

            yield return null;

            float maxWait = burstDuration + 1f;
            float waited = 0f;
            while (waited < maxWait)
            {
                var info = _animator.GetCurrentAnimatorStateInfo(0);
                if (info.IsName(gasStateName) && info.normalizedTime >= 1f)
                    break;
                waited += Time.deltaTime;
                yield return null;
            }

            _hazardActive = false;

            // Hold last frame still until the next burst (avoids drift / partial updates).
            _animator.speed = 0f;

            float lo = Mathf.Min(minPauseSeconds, maxPauseSeconds);
            float hi = Mathf.Max(minPauseSeconds, maxPauseSeconds);
            yield return new WaitForSeconds(Random.Range(lo, hi));
        }
    }

    private void StartBurstFromBeginning()
    {
        _animator.speed = 0f;
        _animator.Play(gasStateName, 0, 0f);
        _animator.Update(0f);
        _animator.speed = 1f;
    }
}
