using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    [Header("Walk Bob")]
    public float bobSpeed = 9f;
    public float bobAmplitude = 0.04f;

    private SpriteRenderer spriteRenderer;
    private CharacterSprites characterSprites;
    private Sprite currentSprite;
    private float bobTimer;
    private Vector3 baseLocalScale;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        baseLocalScale = transform.localScale;
    }

    public void SetSprites(CharacterSprites sprites)
    {
        characterSprites = sprites;
        if (spriteRenderer != null && sprites != null)
        {
            spriteRenderer.sprite = sprites.Front;
            currentSprite = sprites.Front;
        }
    }

    private void Update()
    {
        if (characterSprites == null || spriteRenderer == null)
        {
            return;
        }

        var movement = GetMovementInput();

        if (movement.sqrMagnitude > 0.01f)
        {
            UpdateSpriteForDirection(movement.normalized);
            bobTimer += Time.deltaTime * bobSpeed;
            float bob = Mathf.Sin(bobTimer) * bobAmplitude;
            transform.localScale = baseLocalScale + new Vector3(0f, bob, 0f);
        }
        else
        {
            bobTimer = 0f;
            transform.localScale = Vector3.Lerp(transform.localScale, baseLocalScale, 12f * Time.deltaTime);
        }
    }

    private Vector2 GetMovementInput()
    {
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            return rb.linearVelocity;
        }
        return Vector2.zero;
    }

    private void UpdateSpriteForDirection(Vector2 direction)
    {
        Sprite newSprite = currentSprite;

        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            if (direction.x > 0)
            {
                newSprite = characterSprites.Right;
            }
            else
            {
                newSprite = characterSprites.Left;
            }
        }
        else
        {
            if (direction.y > 0)
            {
                newSprite = characterSprites.Back;
            }
            else
            {
                newSprite = characterSprites.Front;
            }
        }

        if (newSprite != currentSprite)
        {
            currentSprite = newSprite;
            spriteRenderer.sprite = currentSprite;
        }
    }
}
