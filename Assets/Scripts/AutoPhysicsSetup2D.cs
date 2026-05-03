using System;
using UnityEngine;

public static class AutoPhysicsSetup2D
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void SetupScenePhysics()
    {
        try
        {
            EnsureEnvironmentColliders();
            EnsureMazeColliders();
            EnsurePlayerPhysics();
        }
        catch (Exception ex)
        {
            Debug.LogError($"AutoPhysicsSetup2D failed: {ex.Message}");
        }
    }

    public static void EnsurePlayerPhysicsNow()
    {
        EnsurePlayerPhysics();
    }

    private static void EnsureMazeColliders()
    {
        var renderers = UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);

        // When both exist, use Maze-H for pixel colliders so walls match that sprite (Tiles-H is often a separate/legacy floor layer).
        bool mazeHActive = false;
        foreach (var sr in renderers)
        {
            if (sr == null || sr.gameObject == null) continue;
            if (!sr.gameObject.scene.IsValid()) continue;
            if (!sr.gameObject.activeInHierarchy) continue;
            if (sr.gameObject.name.Equals("Maze-H", StringComparison.OrdinalIgnoreCase))
            {
                mazeHActive = true;
                break;
            }
        }

        foreach (var sr in renderers)
        {
            if (sr == null || sr.gameObject == null) continue;
            if (!sr.gameObject.scene.IsValid()) continue;

            var name = sr.gameObject.name;
            if (!IsMazeColliderTarget(name, mazeHActive)) continue;

            if (sr.gameObject.GetComponent<MazeColliderGenerator>() == null)
            {
                sr.gameObject.AddComponent<MazeColliderGenerator>();
                Debug.Log($"AutoPhysicsSetup2D: Attached MazeColliderGenerator to '{name}'");
            }
        }

        if (mazeHActive)
        {
            Debug.Log("AutoPhysicsSetup2D: Maze colliders follow active Maze-H (Tiles-H skipped to avoid duplicate walls).");
        }
    }

    /// <summary>
    /// Pixel-maze colliders attach only to Maze-H or Tiles-H — not other tile layers (avoids grout-line cages).
    /// </summary>
    private static bool IsMazeColliderTarget(string objectName, bool mazeHActiveInScene)
    {
        if (objectName.Equals("Maze-H", StringComparison.OrdinalIgnoreCase))
            return true;
        if (objectName.Equals("Tiles-H", StringComparison.OrdinalIgnoreCase))
            return !mazeHActiveInScene;
        return false;
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

            // Wall-* sprites are decorative; WallBoundary.cs creates the actual room colliders.
            // Adding BoxCollider2D to a wall sprite covers its entire bounding box, which
            // can contain the player spawn point and block movement.
            if (IsTableOrObstacle(objectName))
            {
                SetupObstacleCollider(spriteRenderer.gameObject);
            }
        }
    }

    private static void SetupObstacleCollider(GameObject obj)
    {
        if (obj.GetComponent<Collider2D>() == null)
        {
            var box = obj.AddComponent<BoxCollider2D>();
            box.isTrigger = false;
            Debug.Log($"AutoPhysicsSetup2D: Added BoxCollider2D to obstacle '{obj.name}'");
        }

        var rb = obj.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = obj.AddComponent<Rigidbody2D>();
        }
        rb.bodyType = RigidbodyType2D.Static;
        rb.gravityScale = 0f;
    }

    private static bool IsTableOrObstacle(string objectName)
    {
        return objectName.StartsWith("Table", StringComparison.OrdinalIgnoreCase)
            || objectName.StartsWith("Counter", StringComparison.OrdinalIgnoreCase)
            || objectName.IndexOf("obstacle", StringComparison.OrdinalIgnoreCase) >= 0
            || objectName.IndexOf("bench", StringComparison.OrdinalIgnoreCase) >= 0
            || objectName.IndexOf("locker", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static void EnsurePlayerPhysics()
    {
        GameObject player = null;
        try
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }
        catch (UnityException)
        {
            // Tag not defined — use name search below.
        }

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
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (player.GetComponent<Collider2D>() == null)
        {
            player.AddComponent<CapsuleCollider2D>();
        }
    }
}

