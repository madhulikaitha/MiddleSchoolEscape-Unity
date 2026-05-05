using System.Collections;
using MiddleSchoolEscape;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(20)]
public class ScienceLabRandomGasSpread : MonoBehaviour
{
    [Tooltip("Animator state name from gas_sprite_transparent_0.controller")]
    public string gasStateName = "gasspread";

    [Tooltip("Clip length in seconds (gasspread.anim stop time ~3.17). Increase if you change the clip.")]
    public float burstDuration = 3.17f;

    [Tooltip("Minimum idle time between bursts (solo mode only)")]
    public float minPauseSeconds = 0.85f;

    [Tooltip("Maximum idle time between bursts (solo mode only)")]
    public float maxPauseSeconds = 3.15f;

    [Tooltip("Startup stagger for solo playback")]
    public float maxInitialDelaySeconds = 2.4f;

    [Header("Scheduling")]
    [Tooltip("When enough peers join, ScienceLabGasOrchestrator pairs bursts in waves.")]
    public bool coordinateLabBurstSchedule = true;

    [Header("Player damage & audio")]
    [Tooltip("Collider radius used for grazing contact checks.")]
    public float hazardRadius = 1.48f;

    [Tooltip("Treat touches within this separation as hits.")]
    public float contactSlopWorld = 0.04f;

    [Tooltip("Played when gas damage resolves on the player.")]
    public AudioClip gasSpreadHitSound;

    [Header("Burst VO (solo + coordinated; runs only after science-lab entry trigger)")]
    [Range(0f, 1f)] public float flaskBubblingOneShotVolume = 0.28f;

    [Range(0f, 1f)] public float gasSpreadOneShotVolume = 0.26f;

    private Animator _animator;
    private AudioSource _audioSource;
    private SpriteRenderer _spriteRenderer;
    private CircleCollider2D _hazardCollider;

    private Coroutine _soloLoop;

    private bool _hazardActive;
    private CapsuleCollider2D _playerColliderCache;

    public float EstimatedBurstSeconds => burstDuration + 0.35f;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        ApplyGasSortingOrder();

        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f;
        }

        _hazardCollider = GetComponent<CircleCollider2D>();
        if (_hazardCollider == null)
            _hazardCollider = gameObject.AddComponent<CircleCollider2D>();
        _hazardCollider.isTrigger = true;
        _hazardCollider.radius = hazardRadius;
    }

    private void OnValidate()
    {
        if (_hazardCollider != null)
            _hazardCollider.radius = hazardRadius;
        else if (enabled && gameObject.scene.isLoaded)
        {
            _hazardCollider = GetComponent<CircleCollider2D>();
            if (_hazardCollider != null)
                _hazardCollider.radius = hazardRadius;
        }
    }

    private void Start()
    {
        StartCoroutine(SoloKickoffRoutine());
    }

    private IEnumerator SoloKickoffRoutine()
    {
        yield return null;
        yield return null;

        ScienceLabGasOrchestrator orch = Object.FindFirstObjectByType<ScienceLabGasOrchestrator>();
        bool managed = coordinateLabBurstSchedule && orch != null && orch.isActiveAndEnabled;
        if (managed)
            yield break;

        if (_soloLoop == null)
            _soloLoop = StartCoroutine(SoloBurstLoopCoroutineBody());
    }

    private IEnumerator SoloBurstLoopCoroutineBody()
    {
        if (_animator == null)
            yield break;

        AudioClip bubbly = Resources.Load<AudioClip>("Audio/beforegasexplosion-flaskbubbling");
        AudioClip ambient = Resources.Load<AudioClip>("Audio/gasspreadingsound");

        while (enabled && !ScienceLabHazardPlaybackGate.LabHazardAndAudioAllowed)
            yield return null;

        yield return new WaitForSeconds(Random.Range(0f, maxInitialDelaySeconds));

        while (enabled)
        {
            while (enabled && !ScienceLabHazardPlaybackGate.LabHazardAndAudioAllowed)
                yield return null;

            yield return CoordinatedBurst(bubbly, 0.45f, ambient, true);

            float lo = Mathf.Min(minPauseSeconds, maxPauseSeconds);
            float hi = Mathf.Max(minPauseSeconds, maxPauseSeconds);
            yield return new WaitForSeconds(Random.Range(lo, hi));
        }
    }

    public void AbortStandaloneGasLoop()
    {
        if (_soloLoop != null)
        {
            StopCoroutine(_soloLoop);
            _soloLoop = null;
        }
    }

    private void OnDisable()
    {
        AbortStandaloneGasLoop();
        StopAllCoroutines();

        _hazardActive = false;
        if (_animator != null)
            _animator.speed = 0f;
    }

    private void ApplyGasSortingOrder()
    {
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
            sr.sortingOrder = HazardVisualSortOrders.GasSpread;
    }

    public IEnumerator CoordinatedBurst(AudioClip bubbly, float bubblyDelay, AudioClip ambientSpread, bool debutNarrativeFlag)
    {
        if (_animator == null)
            yield break;

        while (!ScienceLabHazardPlaybackGate.LabHazardAndAudioAllowed)
            yield return null;

        if (bubbly != null && bubblyDelay > 0f)
        {
            _audioSource.PlayOneShot(bubbly, flaskBubblingOneShotVolume);
            yield return new WaitForSeconds(bubblyDelay);
        }
        else if (bubbly != null)
        {
            _audioSource.PlayOneShot(bubbly, flaskBubblingOneShotVolume);
        }

        if (debutNarrativeFlag)
            NarrativeDialogueController.Instance?.NotifyFirstGasSpreadBurst();

        BeginAnimatorBurst();
        if (ambientSpread != null)
            _audioSource.PlayOneShot(ambientSpread, gasSpreadOneShotVolume);

        _hazardActive = true;

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
        _animator.speed = 0f;
    }

    private void Update()
    {
        if (!_hazardActive)
            return;

        PlayerHealth ph = PlayerHealth.Instance;
        if (ph == null)
            return;

        _playerColliderCache ??= ph.GetComponent<CapsuleCollider2D>();
        if (_playerColliderCache == null)
            return;

        if (_hazardCollider == null || !TouchesPlayer(_playerColliderCache))
            return;

        int heartsBefore = ph.CurrentHearts;
        ph.TakeDamage();

        if (ph.CurrentHearts < heartsBefore)
        {
            NarrativeDialogueController.Instance?.NotifyGasDamagedPlayer();
            if (gasSpreadHitSound != null)
                _audioSource.PlayOneShot(gasSpreadHitSound);
        }
    }

    private bool TouchesPlayer(CapsuleCollider2D playerCapsule)
    {
        ColliderDistance2D dist = Physics2D.Distance(_hazardCollider, playerCapsule);
        if (dist.isOverlapped)
            return true;

        return dist.distance <= contactSlopWorld;
    }

    private void BeginAnimatorBurst()
    {
        _animator.speed = 0f;
        _animator.Play(gasStateName, 0, 0f);
        _animator.Update(0f);
        _animator.speed = 1f;
    }
}
