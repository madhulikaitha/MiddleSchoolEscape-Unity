using UnityEngine;

/// <summary>
/// Attach to a student enemy placed behind a cafeteria counter.
/// Cycles idle → windup → throw, spawning a TrayProjectile aimed at the player.
/// Assign the three student pose sprites and the TrayProjectile prefab in the Inspector.
/// </summary>
public class TrayThrower : MonoBehaviour
{
    [Header("Student Sprites (slice your 3-frame sheet in Sprite Editor)")]
    public Sprite poseIdle;     // Frame 1 – holding tray, crouched
    public Sprite poseWindup;   // Frame 2 – arms raised
    public Sprite poseThrow;    // Frame 3 – releasing, speed lines

    [Header("Tray Projectile")]
    public GameObject trayProjectilePrefab;
    [Tooltip("Where the tray spawns when thrown (place a child Transform just in front of the student)")]
    public Transform throwPoint;

    [Header("Timing (seconds)")]
    [Tooltip("Total pause between throw cycles (idle time)")]
    public float throwCooldown = 2.65f;
    [Tooltip("How long the windup pose is shown before the tray launches")]
    public float windupDuration = 0.5f;
    [Tooltip("How long the throw-flash pose is shown before returning to idle")]
    public float throwFlashDuration = 0.2f;

    private SpriteRenderer spriteRenderer;
    private Transform player;
    private float timer;
    private Vector3 baseScale;

    private enum State { Idle, Windup, ThrowFlash }
    private State state = State.Idle;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
    }

    private void Start()
    {
        FindPlayer();
        ShowPose(poseIdle);
        baseScale = transform.localScale;
        timer = Random.Range(0f, throwCooldown);
    }

    private void FindPlayer()
    {
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go == null)
        {
            var all = FindObjectsByType<Transform>(FindObjectsSortMode.None);
            foreach (var t in all)
            {
                if (t != null && t.name.IndexOf("player", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    player = t;
                    return;
                }
            }
        }
        else
        {
            player = go.transform;
        }
    }

    private void Update()
    {
        if (player == null) { FindPlayer(); return; }

        timer += Time.deltaTime;

        switch (state)
        {
            case State.Idle:
                if (timer >= throwCooldown)
                {
                    timer = 0f;
                    state = State.Windup;
                    ShowPose(poseWindup);
                }
                break;

            case State.Windup:
                float pulse = 1f + Mathf.Sin(timer * Mathf.PI * 6f) * 0.07f;
                transform.localScale = baseScale * pulse;
                if (timer >= windupDuration)
                {
                    timer = 0f;
                    transform.localScale = baseScale;
                    ThrowTray();
                    state = State.ThrowFlash;
                    ShowPose(poseThrow);
                }
                break;

            case State.ThrowFlash:
                if (timer >= throwFlashDuration)
                {
                    timer = 0f;
                    state = State.Idle;
                    transform.localScale = baseScale;
                    ShowPose(poseIdle);
                }
                break;
        }
    }

    private void ThrowTray()
    {
        if (trayProjectilePrefab == null) return;

        var origin = throwPoint != null ? throwPoint.position : transform.position;
        var tray = Instantiate(trayProjectilePrefab, origin, Quaternion.identity);

        // Aim at player's position the moment the throw happens (not tracking)
        var dir = (player.position - origin).normalized;
        var proj = tray.GetComponent<TrayProjectile>();
        if (proj != null)
            proj.Launch(dir);
    }

    private void ShowPose(Sprite pose)
    {
        if (spriteRenderer != null && pose != null)
            spriteRenderer.sprite = pose;
    }
}
