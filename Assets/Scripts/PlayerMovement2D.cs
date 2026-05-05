using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement2D : MonoBehaviour
{
    public float moveSpeed = 6.85f;
    public float acceleration = 17f;
    public float deceleration = 24f;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 smoothVelocityRef;
    private bool _notifiedFirstMovement;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        SetupRigidbody();
    }

    private void SetupRigidbody()
    {
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void Update()
    {
        moveInput = ReadMoveInput();
        if (moveInput.magnitude > 1f)
        {
            moveInput.Normalize();
        }

        if (!_notifiedFirstMovement && moveInput.sqrMagnitude > 0.02f)
        {
            _notifiedFirstMovement = true;
            NarrativeDialogueController.Instance?.NotifyFirstMovement();
        }
    }

    private static Vector2 ReadMoveInput()
    {
        Vector2 v = Vector2.zero;

        var gp = Gamepad.current;
        if (gp != null)
        {
            v = gp.leftStick.ReadValue();
            const float dz = 0.22f;
            if (v.magnitude >= dz)
            {
                float mag = Mathf.Min(1f, (v.magnitude - dz) / (1f - dz));
                v = v.normalized * mag;
            }
            else
            {
                v = Vector2.zero;
                Vector2 dpad = gp.dpad.ReadValue();
                if (dpad.sqrMagnitude > 0.01f)
                    v = new Vector2(Mathf.Clamp(dpad.x, -1f, 1f), Mathf.Clamp(dpad.y, -1f, 1f));
            }
        }

        if (v.sqrMagnitude < 0.01f)
        {
            float x = Input.GetAxisRaw("Horizontal");
            float y = Input.GetAxisRaw("Vertical");

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) x = 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) x = -1f;
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) y = 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) y = -1f;
            }

            v = new Vector2(x, y);
        }

        return v;
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        Vector2 target = moveInput * moveSpeed;
        float smoothTime = target.magnitude > 0.01f
            ? 1f / acceleration
            : 1f / deceleration;
        rb.linearVelocity = Vector2.SmoothDamp(rb.linearVelocity, target, ref smoothVelocityRef, smoothTime);
    }
}
