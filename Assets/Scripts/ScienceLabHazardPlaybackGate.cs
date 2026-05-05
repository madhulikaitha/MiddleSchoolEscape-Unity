using UnityEngine;

/// <summary>Lets science-lab hazard audio/scheduling run only after the player crosses the scripted lab entry dialogue trigger.</summary>
public static class ScienceLabHazardPlaybackGate
{
    public static bool LabHazardAndAudioAllowed { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Bootstrap()
    {
        LabHazardAndAudioAllowed = false;
    }

    public static void Reset() => LabHazardAndAudioAllowed = false;

    public static void AllowFromScienceLabEntryTrigger()
    {
        LabHazardAndAudioAllowed = true;
    }
}
