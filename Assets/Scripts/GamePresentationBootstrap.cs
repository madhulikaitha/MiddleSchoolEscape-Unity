using UnityEngine;

/// <summary>
/// Zoom-out orthographic camera, solid black letterbox clearing, and a huge follow-sprite backdrop
/// with sortingOrder -10000 (drawn behind gameplay).
/// </summary>
public static class GamePresentationBootstrap
{
    private const string BackdropName = "__BlackBackdrop2D";

    private const float TargetOrthographicSize = 7f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Apply()
    {
        var cam = Camera.main;
        if (cam == null || !cam.orthographic)
        {
            return;
        }

        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        if (cam.orthographicSize < TargetOrthographicSize)
        {
            cam.orthographicSize = TargetOrthographicSize;
        }

        EnsureBackdrop(cam);
    }

    private static void EnsureBackdrop(Camera cam)
    {
        if (GameObject.Find(BackdropName) != null)
        {
            return;
        }

        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
        tex.Apply(false, true);
        tex.hideFlags = HideFlags.HideAndDontSave;

        var sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f),
            pixelsPerUnit: 1f, extrude: 0u);

        sprite.hideFlags = HideFlags.HideAndDontSave;

        var go = new GameObject(BackdropName);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = Color.black;
        sr.sortingOrder = -10000;

        go.AddComponent<BlackBackdropScaler>();
    }

    /// <summary>Scale and reposition the fullscreen black sprite whenever the followed camera moves or zoom changes.</summary>
    private sealed class BlackBackdropScaler : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void LateUpdate()
        {
            Camera cam = Camera.main;
            if (cam == null || !cam.orthographic || spriteRenderer?.sprite == null)
            {
                return;
            }

            var ct = cam.transform;
            transform.SetPositionAndRotation(new Vector3(ct.position.x, ct.position.y, 0f),
                Quaternion.identity);

            float viewHeight = cam.orthographicSize * 2.2f;
            float viewWidth = viewHeight * cam.aspect;

            Vector2 extents = spriteRenderer.sprite.bounds.extents;
            float sx = Mathf.Max(viewWidth / (extents.x * 2f), 1f);
            float sy = Mathf.Max(viewHeight / (extents.y * 2f), 1f);
            transform.localScale = new Vector3(sx, sy, 1f);
        }
    }
}
