using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ScienceLabTileSetupTool
{
    private const string CrackSheetGuid = "498f8ed18b24195489c45c2d04d7de75";
    private const string HoleSpriteGuid = "c8f1eaa5b8660624ca4705802783ca7d";
    private const string OutputPrefix = "a820050d-407f-4085-b982-1705ff566f06_phase_";

    [MenuItem("Tools/Science Lab/Setup Cracking Tiles (Extract + Assign)")]
    public static void SetupCrackingTiles()
    {
        string crackSheetPath = AssetDatabase.GUIDToAssetPath(CrackSheetGuid);
        if (string.IsNullOrEmpty(crackSheetPath))
        {
            Debug.LogError("ScienceLab setup failed: crack sprite sheet not found by GUID.");
            return;
        }

        string outputFolder = Path.GetDirectoryName(crackSheetPath)?.Replace("\\", "/");
        if (string.IsNullOrEmpty(outputFolder))
        {
            Debug.LogError("ScienceLab setup failed: could not resolve output folder.");
            return;
        }

        List<Sprite> extractedPhases = ExtractPhaseSprites(crackSheetPath, outputFolder);
        if (extractedPhases.Count == 0)
        {
            Debug.LogError("ScienceLab setup failed: no phase sprites were extracted.");
            return;
        }

        Sprite holeSprite = LoadHoleSprite();
        int configuredCount = ConfigureSceneCrackingTiles(extractedPhases, holeSprite);

        Debug.Log($"ScienceLab setup complete. Extracted {extractedPhases.Count} phase sprites and configured {configuredCount} CrackingTile objects.");
    }

    private static List<Sprite> ExtractPhaseSprites(string crackSheetPath, string outputFolder)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(crackSheetPath);
        List<Sprite> sourceSprites = assets
            .OfType<Sprite>()
            .OrderBy(ParsePhaseIndex)
            .Where(s => ParsePhaseIndex(s) >= 0 && ParsePhaseIndex(s) <= 5)
            .ToList();

        if (sourceSprites.Count == 0)
            return new List<Sprite>();

        Texture2D readableSheet = GetReadableTexture(sourceSprites[0].texture);
        List<string> generatedPaths = new List<string>();

        for (int i = 0; i < sourceSprites.Count; i++)
        {
            Sprite sprite = sourceSprites[i];
            Rect r = sprite.rect;

            Texture2D frame = new Texture2D((int)r.width, (int)r.height, TextureFormat.RGBA32, false);
            Color[] pixels = readableSheet.GetPixels((int)r.x, (int)r.y, (int)r.width, (int)r.height);
            frame.SetPixels(pixels);
            frame.Apply();

            string filePath = $"{outputFolder}/{OutputPrefix}{(i + 1):00}.png";
            File.WriteAllBytes(filePath, frame.EncodeToPNG());
            Object.DestroyImmediate(frame);
            generatedPaths.Add(filePath);
        }

        AssetDatabase.Refresh();

        List<Sprite> extracted = new List<Sprite>();
        foreach (string path in generatedPaths)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            Sprite loaded = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (loaded != null)
                extracted.Add(loaded);
        }

        return extracted;
    }

    private static int ParsePhaseIndex(Sprite sprite)
    {
        if (sprite == null || string.IsNullOrEmpty(sprite.name))
            return int.MaxValue;

        int underscore = sprite.name.LastIndexOf('_');
        if (underscore < 0 || underscore == sprite.name.Length - 1)
            return int.MaxValue;

        string suffix = sprite.name.Substring(underscore + 1);
        return int.TryParse(suffix, out int index) ? index : int.MaxValue;
    }

    private static Texture2D GetReadableTexture(Texture2D source)
    {
        RenderTexture rt = RenderTexture.GetTemporary(
            source.width,
            source.height,
            0,
            RenderTextureFormat.Default,
            RenderTextureReadWrite.Linear
        );

        Graphics.Blit(source, rt);
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        readable.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        readable.Apply();

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        return readable;
    }

    private static Sprite LoadHoleSprite()
    {
        string holePath = AssetDatabase.GUIDToAssetPath(HoleSpriteGuid);
        if (string.IsNullOrEmpty(holePath))
            return null;

        Sprite direct = AssetDatabase.LoadAssetAtPath<Sprite>(holePath);
        if (direct != null)
            return direct;

        Object[] subs = AssetDatabase.LoadAllAssetsAtPath(holePath);
        return subs.OfType<Sprite>().FirstOrDefault();
    }

    private static int ConfigureSceneCrackingTiles(List<Sprite> phaseSprites, Sprite holeSprite)
    {
        CrackingTile[] tiles = Object.FindObjectsByType<CrackingTile>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (tiles == null || tiles.Length == 0)
            return 0;

        GameObject[] holes = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(go => go.name.Contains("Holetile-S"))
            .ToArray();

        int count = 0;
        foreach (CrackingTile tile in tiles)
        {
            tile.phaseSprites = phaseSprites.ToArray();
            tile.holeSprite = holeSprite;

            GameObject nearest = FindNearestHole(tile.transform.position, holes, 0.6f);
            if (nearest != null)
                tile.holeTileObject = nearest;

            EditorUtility.SetDirty(tile);
            count++;
        }

        if (tiles.Length > 0)
            EditorSceneManager.MarkSceneDirty(tiles[0].gameObject.scene);

        return count;
    }

    private static GameObject FindNearestHole(Vector3 position, GameObject[] holes, float maxDistance)
    {
        if (holes == null || holes.Length == 0)
            return null;

        float maxDistanceSqr = maxDistance * maxDistance;
        GameObject best = null;
        float bestDist = float.MaxValue;

        foreach (GameObject hole in holes)
        {
            float dist = (hole.transform.position - position).sqrMagnitude;
            if (dist <= maxDistanceSqr && dist < bestDist)
            {
                bestDist = dist;
                best = hole;
            }
        }

        return best;
    }
}
