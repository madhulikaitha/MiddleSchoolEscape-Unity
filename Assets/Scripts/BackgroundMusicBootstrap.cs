using UnityEngine;

/// <summary>
/// Plays looping background music from Resources/Audio/gamebackgroundmusic (copy of Assets/Audio/gamebackgroundmusic.mp3).
/// Persists across scene loads while in Play mode.
/// </summary>
public static class BackgroundMusicBootstrap
{
    private const string ResourcePath = "Audio/gamebackgroundmusic";
    private const float Volume = 0.55f;

    private static bool started;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsurePlaying()
    {
        if (started)
        {
            return;
        }

        var clip = Resources.Load<AudioClip>(ResourcePath);
        if (clip == null)
        {
            Debug.LogWarning(
                $"BackgroundMusicBootstrap: No clip at Resources/{ResourcePath}. Ensure Assets/Resources/Audio/gamebackgroundmusic.mp3 exists and Unity has imported it.");
            return;
        }

        started = true;

        var go = new GameObject("__BackgroundMusic");
        Object.DontDestroyOnLoad(go);

        var src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.loop = true;
        src.playOnAwake = false;
        src.volume = Volume;
        src.spatialBlend = 0f;
        src.bypassListenerEffects = false;
        src.Play();
    }
}
