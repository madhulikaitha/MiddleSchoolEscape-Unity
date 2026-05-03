using UnityEngine;

/// <summary>
/// Per-room invisible BoxCollider2D bounds. Edit values in the Inspector on this component
/// (place one instance in each gameplay scene); they are saved with the scene and used every run.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-2000)]
public class WallBoundary : MonoBehaviour
{
    [System.Serializable]
    public struct RoomBoundaryDefinition
    {
        [Tooltip("World-space center of the room rectangle.")]
        public Vector2 center;

        [Tooltip("Total interior width (left/right collider span).")]
        public float width;

        [Tooltip("Total interior height (top/bottom collider span).")]
        public float height;

        [Tooltip("Horizontal gap for door segments on edges that use doors.")]
        public float doorGap;

        public bool doorOnTopEdge;
        public bool doorOnBottomEdge;
    }

    [SerializeField]
    private RoomBoundaryDefinition lobby = new RoomBoundaryDefinition
    {
        center = new Vector2(0f, 0f),
        width = 40f,
        height = 25f,
        doorGap = 4f,
        doorOnTopEdge = true,
        doorOnBottomEdge = false
    };

    [SerializeField]
    private RoomBoundaryDefinition cafeteria = new RoomBoundaryDefinition
    {
        center = new Vector2(0f, 28f),
        width = 60f,
        height = 35f,
        doorGap = 4f,
        doorOnTopEdge = true,
        doorOnBottomEdge = true
    };

    [SerializeField]
    private RoomBoundaryDefinition hallway = new RoomBoundaryDefinition
    {
        center = new Vector2(0f, 70f),
        width = 55f,
        height = 25f,
        doorGap = 4f,
        doorOnTopEdge = true,
        doorOnBottomEdge = true
    };

    [SerializeField]
    private RoomBoundaryDefinition scienceLab = new RoomBoundaryDefinition
    {
        center = new Vector2(0f, 110f),
        width = 55f,
        height = 35f,
        doorGap = 4f,
        doorOnTopEdge = false,
        doorOnBottomEdge = true
    };

#if UNITY_EDITOR
    private void Reset()
    {
        ApplyLegacyBuiltinDefaults();
    }
#endif

    /// <summary>Restores the same numbers the old hardcoded script used (handy Reset / migration).</summary>
    [ContextMenu("Reset rooms to legacy built-in defaults")]
    public void ApplyLegacyBuiltinDefaults()
    {
        lobby = new RoomBoundaryDefinition
        {
            center = new Vector2(0f, 0f),
            width = 40f,
            height = 25f,
            doorGap = 4f,
            doorOnTopEdge = true,
            doorOnBottomEdge = false
        };
        cafeteria = new RoomBoundaryDefinition
        {
            center = new Vector2(0f, 28f),
            width = 60f,
            height = 35f,
            doorGap = 4f,
            doorOnTopEdge = true,
            doorOnBottomEdge = true
        };
        hallway = new RoomBoundaryDefinition
        {
            center = new Vector2(0f, 70f),
            width = 55f,
            height = 25f,
            doorGap = 4f,
            doorOnTopEdge = true,
            doorOnBottomEdge = true
        };
        scienceLab = new RoomBoundaryDefinition
        {
            center = new Vector2(0f, 110f),
            width = 55f,
            height = 35f,
            doorGap = 4f,
            doorOnTopEdge = false,
            doorOnBottomEdge = true
        };
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    private void Awake()
    {
        BuildWallBoundaries();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureWallBoundaryWhenMissingFromScene()
    {
        var found = Object.FindFirstObjectByType<WallBoundary>(FindObjectsInactive.Include);
        if (found != null)
        {
            return;
        }

        // Field defaults on this component match the old hardcoded layout; Awake runs Build once.
        new GameObject("__WallBoundaryAuthoring").AddComponent<WallBoundary>();
    }

    /// <summary>Rebuilds colliders from current Inspector values (optional call from other scripts).</summary>
    public void BuildWallBoundaries()
    {
        var existing = GameObject.Find("__WallBoundaries");
        if (existing != null)
        {
            Destroy(existing);
        }

        var root = new GameObject("__WallBoundaries");

        CreateRoomWalls(root.transform, "Lobby", lobby);
        CreateRoomWalls(root.transform, "Cafe", cafeteria);
        CreateRoomWalls(root.transform, "Hall", hallway);
        CreateRoomWalls(root.transform, "Lab", scienceLab);

        Debug.Log("WallBoundary: Built wall boundaries from scene settings.");
    }

    private static void CreateRoomWalls(Transform parent, string prefix, RoomBoundaryDefinition r)
    {
        if (r.width <= 0.01f || r.height <= 0.01f)
        {
            Debug.LogWarning($"WallBoundary: Skipping '{prefix}' — width or height is too small.");
            return;
        }

        float cx = r.center.x;
        float cy = r.center.y;
        float edgeYTop = cy + r.height / 2f;
        float edgeYBottom = cy - r.height / 2f;

        if (r.doorOnTopEdge)
        {
            CreateHorizontalSegmentsWithDoor(parent, $"{prefix}_Top", edgeYTop, r.width, r.doorGap, cx);
        }
        else
        {
            CreateWall(parent, $"{prefix}_Top", new Vector3(cx, edgeYTop, 0f), new Vector2(r.width, 2f));
        }

        if (r.doorOnBottomEdge)
        {
            CreateHorizontalSegmentsWithDoor(parent, $"{prefix}_Bottom", edgeYBottom, r.width, r.doorGap, cx);
        }
        else
        {
            CreateWall(parent, $"{prefix}_Bottom", new Vector3(cx, edgeYBottom, 0f), new Vector2(r.width, 2f));
        }

        CreateWall(parent, $"{prefix}_Left", new Vector3(cx - r.width / 2f, cy, 0f), new Vector2(2f, r.height));
        CreateWall(parent, $"{prefix}_Right", new Vector3(cx + r.width / 2f, cy, 0f), new Vector2(2f, r.height));
    }

    private static void CreateHorizontalSegmentsWithDoor(
        Transform parent,
        string prefix,
        float centerY,
        float roomWidth,
        float doorGap,
        float centerX)
    {
        float gap = Mathf.Max(0.1f, doorGap);
        float halfSegment = (roomWidth - gap) / 2f;
        CreateWall(parent, $"{prefix}_West",
            new Vector3(centerX - gap / 2f - halfSegment / 2f, centerY, 0f),
            new Vector2(halfSegment, 2f));
        CreateWall(parent, $"{prefix}_East",
            new Vector3(centerX + gap / 2f + halfSegment / 2f, centerY, 0f),
            new Vector2(halfSegment, 2f));
    }

    private static void CreateWall(Transform parent, string name, Vector3 position, Vector2 size)
    {
        var wall = new GameObject(name);
        wall.transform.SetParent(parent);
        wall.transform.position = position;

        var collider = wall.AddComponent<BoxCollider2D>();
        collider.size = size;
        collider.isTrigger = false;

        var rb = wall.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
        rb.gravityScale = 0f;
    }

#if UNITY_EDITOR
    [ContextMenu("Match hallway room to Maze-H sprite bounds")]
    private void EditorMatchHallwayToMazeH()
    {
        SpriteRenderer mazeSr = null;
        foreach (var sr in FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (sr == null || !sr.gameObject.scene.IsValid()) continue;
            if (sr.gameObject.name.Equals("Maze-H", System.StringComparison.OrdinalIgnoreCase))
            {
                mazeSr = sr;
                break;
            }
        }

        if (mazeSr == null)
        {
            Debug.LogWarning("WallBoundary: No SpriteRenderer on a GameObject named Maze-H found. Rename your maze object or adjust hallway manually.");
            return;
        }

        var b = mazeSr.bounds;
        bool top = hallway.doorOnTopEdge;
        bool bottom = hallway.doorOnBottomEdge;
        float gap = hallway.doorGap;
        hallway = new RoomBoundaryDefinition
        {
            center = new Vector2(b.center.x, b.center.y),
            width = Mathf.Max(4f, b.size.x),
            height = Mathf.Max(4f, b.size.y),
            doorGap = gap,
            doorOnTopEdge = top,
            doorOnBottomEdge = bottom
        };
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"WallBoundary: Hallway matched Maze-H bounds (center {hallway.center}, size {hallway.width}x{hallway.height}). Save the scene.");
    }
#endif
}
