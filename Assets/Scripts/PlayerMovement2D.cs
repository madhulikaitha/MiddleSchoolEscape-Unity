using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement2D : MonoBehaviour
{
    public float moveSpeed = 8f;
    public float acceleration = 20f;
    public float deceleration = 28f;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 smoothVelocityRef;

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

    }

    private static Vector2 ReadMoveInput()
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

        return new Vector2(x, y);
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
