using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AutoPhysicsSetup2D
{
    private const string BoundsRootName = "__LevelBounds2D";
    private const float BoundsPadding = 0.5f;
    private const float BoundsThickness = 1.0f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void SetupScenePhysics()
    {
        try
        {
            EnsureEnvironmentColliders();
            EnsurePlayerPhysics();
            EnsureCameraBounds();
        }
        catch (Exception ex)
        {
            Debug.LogError($"AutoPhysicsSetup2D failed: {ex.Message}");
        }
    }

    private static void EnsureEnvironmentColliders()
    {
        var renderers = UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
        foreach (var spriteRenderer in renderers)
        {
            if (spriteRenderer == null || spriteRenderer.gameObject == null)
            {
                continue;
            }

            if (!spriteRenderer.gameObject.scene.IsValid())
            {
                continue;
            }

            var objectName = spriteRenderer.gameObject.name;
            if (!LooksLikeEnvironment(objectName))
            {
                continue;
            }

            if (NeedsPolygonCollider(objectName))
            {
                if (spriteRenderer.GetComponent<PolygonCollider2D>() == null)
                {
                    spriteRenderer.gameObject.AddComponent<PolygonCollider2D>();
                }
            }
            else
            {
                if (spriteRenderer.GetComponent<BoxCollider2D>() == null)
                {
                    spriteRenderer.gameObject.AddComponent<BoxCollider2D>();
                }
            }

            var rb = spriteRenderer.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = spriteRenderer.gameObject.AddComponent<Rigidbody2D>();
            }

            rb.bodyType = RigidbodyType2D.Static;
            rb.gravityScale = 0f;
        }
    }

    private static void EnsurePlayerPhysics()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            var allTransforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
            foreach (var t in allTransforms)
            {
                if (t != null && t.name.IndexOf("player", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    player = t.gameObject;
                    break;
                }
            }
        }

        if (player == null)
        {
            Debug.LogWarning("AutoPhysicsSetup2D: No Player object found. Add a Player-tagged object to apply gravity/movement physics.");
            return;
        }

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = player.AddComponent<Rigidbody2D>();
        }

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (player.GetComponent<Collider2D>() == null)
        {
            player.AddComponent<CapsuleCollider2D>();
        }
    }

    private static void EnsureCameraBounds()
    {
        var cam = Camera.main;
        if (cam == null || !cam.orthographic)
        {
            return;
        }

        var scene = SceneManager.GetActiveScene();
        var boundsRoot = GameObject.Find(BoundsRootName);
        if (boundsRoot == null)
        {
            boundsRoot = new GameObject(BoundsRootName);
            SceneManager.MoveGameObjectToScene(boundsRoot, scene);
        }

        var rb = boundsRoot.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = boundsRoot.AddComponent<Rigidbody2D>();
        }

        rb.bodyType = RigidbodyType2D.Static;
        rb.gravityScale = 0f;

        var halfHeight = cam.orthographicSize + BoundsPadding;
        var halfWidth = cam.orthographicSize * cam.aspect + BoundsPadding;
        var center = cam.transform.position;
        var z = 0f;

        CreateOrUpdateBound(boundsRoot, "Top", new Vector2(center.x, center.y + halfHeight), new Vector2(halfWidth * 2f, BoundsThickness), z);
        CreateOrUpdateBound(boundsRoot, "Bottom", new Vector2(center.x, center.y - halfHeight), new Vector2(halfWidth * 2f, BoundsThickness), z);
        CreateOrUpdateBound(boundsRoot, "Left", new Vector2(center.x - halfWidth, center.y), new Vector2(BoundsThickness, halfHeight * 2f), z);
        CreateOrUpdateBound(boundsRoot, "Right", new Vector2(center.x + halfWidth, center.y), new Vector2(BoundsThickness, halfHeight * 2f), z);
    }

    private static void CreateOrUpdateBound(GameObject root, string name, Vector2 worldPosition, Vector2 size, float z)
    {
        var child = root.transform.Find(name);
        if (child == null)
        {
            var go = new GameObject(name);
            child = go.transform;
            child.SetParent(root.transform);
        }

        child.position = new Vector3(worldPosition.x, worldPosition.y, z);
        child.localRotation = Quaternion.identity;
        child.localScale = Vector3.one;

        var collider = child.GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = child.gameObject.AddComponent<BoxCollider2D>();
        }

        collider.size = size;
        collider.isTrigger = false;
    }

    private static bool LooksLikeEnvironment(string objectName)
    {
        return objectName.StartsWith("Wall", StringComparison.OrdinalIgnoreCase)
            || objectName.StartsWith("Tiles", StringComparison.OrdinalIgnoreCase)
            || objectName.StartsWith("Base", StringComparison.OrdinalIgnoreCase)
            || objectName.IndexOf("maze", StringComparison.OrdinalIgnoreCase) >= 0
            || objectName.IndexOf("chatgpt image", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool NeedsPolygonCollider(string objectName)
    {
        return objectName.IndexOf("maze", StringComparison.OrdinalIgnoreCase) >= 0
            || objectName.IndexOf("chatgpt image", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}

