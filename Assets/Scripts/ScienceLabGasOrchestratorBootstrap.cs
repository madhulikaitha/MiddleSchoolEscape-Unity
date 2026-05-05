using UnityEngine;

/// <summary>Creates <see cref="ScienceLabGasOrchestrator"/> at runtime whenever enough lab gas spreads opt into coordination.</summary>
public static class ScienceLabGasOrchestratorBootstrap
{
    private const int MinCoordinatedSpreads = 4;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureOrchestrator()
    {
        var spreads = Object.FindObjectsByType<ScienceLabRandomGasSpread>(FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);
        int coordinating = 0;
        foreach (var s in spreads)
        {
            if (s != null && s.coordinateLabBurstSchedule)
                coordinating++;
        }

        if (coordinating < MinCoordinatedSpreads)
            return;

        if (Object.FindFirstObjectByType<ScienceLabGasOrchestrator>() != null)
            return;

        var go = new GameObject("ScienceLabGasOrchestrator");
        go.AddComponent<ScienceLabGasOrchestrator>();
    }
}
