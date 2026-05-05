using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class MazeColliderGenerator : MonoBehaviour
{
    public enum WallLuminanceMode
    {
        [Tooltip("Guess from corner pixels: yellow/light tile mazes vs dark-line mazes.")]
        Auto,
        [Tooltip("Walls are darker than paths (black ink, dark grout).")]
        DarkPixelsAreWalls,
        [Tooltip("Walls are brighter than paths (yellow bricks on blue floor).")]
        BrightPixelsAreWalls
    }

    [Tooltip("How to decide which pixels are walls from sprite color.")]
    public WallLuminanceMode wallMode = WallLuminanceMode.Auto;

    [Tooltip("Dark mode: wall if luminance is below this. Bright mode: wall if luminance is above this.")]
    [Range(0f, 1f)]
    public float wallLuminanceThreshold = 0.38f;

    [Tooltip("How many pixels each collision cell covers (smaller = thinner walls in world units, more collider objects). Default 7 vs legacy 8 gives slightly narrower walls for the same art.")]
    public int cellPixelSize = 6;

    [Tooltip("Skip this many cells from each edge when sampling (0 = full image; use 1–2 only if the art has a solid border frame).")]
    public int borderCellsToSkip = 0;

    [Tooltip("Interior cells need this many wall neighbours (filters noise). Edge cells always keep walls.")]
    public int minWallNeighbours = 2;

    private void Start()
    {
        // If colliders were baked in the editor and saved with the scene, skip generation.
        if (transform.childCount > 0)
        {
            Debug.Log($"MazeColliderGenerator on '{name}': using {transform.childCount} baked colliders from scene.");
            return;
        }

        GenerateColliders();
    }

    private void GenerateColliders()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return;

        var sprite = sr.sprite;
        var texture = sprite.texture;

        if (!texture.isReadable)
        {
            Debug.LogError($"MazeColliderGenerator on '{name}': texture is not readable. Enable Read/Write in texture import settings.");
            return;
        }

        // Clear any existing baked children before regenerating.
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
#if UNITY_EDITOR
            DestroyImmediate(transform.GetChild(i).gameObject);
#else
            Destroy(transform.GetChild(i).gameObject);
#endif
        }

        var rect = sprite.rect;
        int texLeft = (int)rect.x;
        int texBottom = (int)rect.y;
        int sw = (int)rect.width;
        int sh = (int)rect.height;

        float ppu = sprite.pixelsPerUnit;
        int cols = Mathf.CeilToInt((float)sw / cellPixelSize);
        int rows = Mathf.CeilToInt((float)sh / cellPixelSize);

        bool brightWalls = wallMode switch
        {
            WallLuminanceMode.BrightPixelsAreWalls => true,
            WallLuminanceMode.DarkPixelsAreWalls => false,
            _ => DetectBrightWallCorners(texture, texLeft, texBottom, sw, sh)
        };

        int maxSkip = Mathf.Max(0, Mathf.Min(cols, rows) / 2 - 1);
        int borderSkip = Mathf.Clamp(borderCellsToSkip, 0, maxSkip);

        bool[,] isWall = new bool[cols, rows];

        for (int col = 0; col < cols; col++)
        {
            for (int row = 0; row < rows; row++)
            {
                if (col < borderSkip || col >= cols - borderSkip) continue;
                if (row < borderSkip || row >= rows - borderSkip) continue;

                int px = Mathf.Clamp(texLeft + col * cellPixelSize + cellPixelSize / 2, texLeft, texLeft + sw - 1);
                int py = Mathf.Clamp(texBottom + row * cellPixelSize + cellPixelSize / 2, texBottom, texBottom + sh - 1);

                Color c = texture.GetPixel(px, py);
                float lum = 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
                bool wallSample = brightWalls
                    ? c.a > 0.1f && lum > wallLuminanceThreshold
                    : c.a > 0.1f && lum < wallLuminanceThreshold;
                isWall[col, row] = wallSample;
            }
        }

        bool[,] filtered = new bool[cols, rows];
        for (int col = 0; col < cols; col++)
        {
            for (int row = 0; row < rows; row++)
            {
                if (!isWall[col, row])
                    continue;

                bool onCellGridEdge = col == 0 || col == cols - 1 || row == 0 || row == rows - 1;
                if (onCellGridEdge)
                {
                    filtered[col, row] = true;
                    continue;
                }

                int neighbours = 0;
                if (isWall[col - 1, row]) neighbours++;
                if (isWall[col + 1, row]) neighbours++;
                if (isWall[col, row - 1]) neighbours++;
                if (isWall[col, row + 1]) neighbours++;
                filtered[col, row] = neighbours >= minWallNeighbours;
            }
        }

        float originX = -sprite.pivot.x / ppu;
        float originY = -sprite.pivot.y / ppu;
        float cellSize = cellPixelSize / ppu;

        bool[,] done = new bool[cols, rows];
        int count = 0;

        for (int row = 0; row < rows; row++)
        {
            int col = 0;
            while (col < cols)
            {
                if (!filtered[col, row] || done[col, row])
                {
                    col++;
                    continue;
                }

                int end = col;
                while (end + 1 < cols && filtered[end + 1, row] && !done[end + 1, row])
                    end++;

                int span = end - col + 1;
                for (int c2 = col; c2 <= end; c2++) done[c2, row] = true;

                float lx = originX + (col + span * 0.5f) * cellSize;
                float ly = originY + (row + 0.5f) * cellSize;

                var cell = new GameObject($"W_{col}_{row}");
                cell.transform.SetParent(transform, false);
                cell.transform.localPosition = new Vector3(lx, ly, 0f);

                var box = cell.AddComponent<BoxCollider2D>();
                box.size = new Vector2(span * cellSize, cellSize);

                count++;
                col = end + 1;
            }
        }

        Debug.Log($"MazeColliderGenerator on '{name}': built {count} strips (brightWalls={brightWalls}, borderSkip={borderSkip}).");
    }

    /// <summary>Most school-tile mazes are light walls at the image corners; ink mazes are dark there.</summary>
    private static bool DetectBrightWallCorners(Texture2D texture, int texLeft, int texBottom, int sw, int sh)
    {
        int brightCorners = 0;
        int samples = 0;
        int inset = Mathf.Max(1, Mathf.Min(sw, sh) / 32);
        var points = new[]
        {
            (texLeft + inset, texBottom + inset),
            (texLeft + sw - 1 - inset, texBottom + inset),
            (texLeft + inset, texBottom + sh - 1 - inset),
            (texLeft + sw - 1 - inset, texBottom + sh - 1 - inset)
        };
        foreach (var (px, py) in points)
        {
            Color c = texture.GetPixel(px, py);
            float lum = 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
            if (c.a > 0.1f && lum > 0.42f)
                brightCorners++;
            samples++;
        }

        return brightCorners >= 3;
    }

#if UNITY_EDITOR
    [ContextMenu("Bake Colliders to Scene")]
    private void EditorBakeColliders()
    {
        GenerateColliders();
        UnityEditor.EditorUtility.SetDirty(gameObject);
        Debug.Log($"MazeColliderGenerator: Baked colliders on '{name}'. Save the scene to persist them.");
    }

    [ContextMenu("Clear Baked Colliders")]
    private void EditorClearColliders()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);
        UnityEditor.EditorUtility.SetDirty(gameObject);
        Debug.Log($"MazeColliderGenerator: Cleared baked colliders on '{name}'.");
    }
#endif
}
