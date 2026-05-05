using UnityEngine;

/// <summary>Loads cafeteria / lab clips from Assets/Resources/Audio/*.mp3 (no extension).</summary>
public static class GameplaySfx
{
    private const string LunchTrayHit = "Audio/lunchtrayhit";
    private const string LunchTableThrow = "Audio/lunchtablethrow";

    private static AudioClip trayHitCached;
    private static AudioClip tableThrowCached;

    public static void PlayTrayHitAt(Vector3 worldPosition, float volume = 0.95f)
    {
        trayHitCached ??= Resources.Load<AudioClip>(LunchTrayHit);
        if (trayHitCached == null)
        {
            Debug.LogWarning($"GameplaySfx: Missing Resources/{LunchTrayHit}.mp3");
            return;
        }

        AudioSource.PlayClipAtPoint(trayHitCached, worldPosition, volume);
    }

    public static void PlayTableThrowAt(Vector3 worldPosition, float volume = 0.9f)
    {
        tableThrowCached ??= Resources.Load<AudioClip>(LunchTableThrow);
        if (tableThrowCached == null)
        {
            Debug.LogWarning($"GameplaySfx: Missing Resources/{LunchTableThrow}.mp3");
            return;
        }

        AudioSource.PlayClipAtPoint(tableThrowCached, worldPosition, volume);
    }
}
