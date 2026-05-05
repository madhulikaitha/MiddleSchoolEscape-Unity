using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plays bursts in waves of two (up to three waves per cycle after a rested pause), with per-cycle randomized pairings.
/// </summary>
[DefaultExecutionOrder(-400)]
public class ScienceLabGasOrchestrator : MonoBehaviour
{
    [Header("Coordinated bursts")]
    public float bubblyLeadSeconds = 0.48f;

    [Tooltip("Extra wait after the animator finishes before shutting hazard damage off.")]
    public float trailingHazardShutoffBuffer = 0.08f;

    public float interWavePauseMin = 0.85f;
    public float interWavePauseMax = 2.05f;

    public float cycleRestMin = 2.6f;
    public float cycleRestMax = 5.75f;

    private AudioClip _bubbly;
    private AudioClip _ambientSpread;

    private void Awake()
    {
        _bubbly = Resources.Load<AudioClip>("Audio/beforegasexplosion-flaskbubbling");
        _ambientSpread = Resources.Load<AudioClip>("Audio/gasspreadingsound");

        if (_bubbly == null)
            Debug.LogWarning("ScienceLabGasOrchestrator: Missing Resources/Audio/beforegasexplosion-flaskbubbling.mp3");
        if (_ambientSpread == null)
            Debug.LogWarning("ScienceLabGasOrchestrator: Missing Resources/Audio/gasspreadingsound.mp3");
    }

    private void Start()
    {
        StartCoroutine(HostRoutine());
    }

    private IEnumerator HostRoutine()
    {
        yield return null;
        yield return null;

        var members = GatherMembers();
        if (members.Count < 4)
        {
            Destroy(gameObject);
            yield break;
        }

        foreach (var s in members)
            s.AbortStandaloneGasLoop();

        StartCoroutine(RunCoordinatedLoops(members));
    }

    private static List<ScienceLabRandomGasSpread> GatherMembers()
    {
        var list = new List<ScienceLabRandomGasSpread>();
        foreach (var s in FindObjectsByType<ScienceLabRandomGasSpread>(FindObjectsInactive.Exclude,
                     FindObjectsSortMode.InstanceID))
        {
            if (s != null && s.coordinateLabBurstSchedule)
                list.Add(s);
        }

        return list;
    }

    private IEnumerator RunCoordinatedLoops(List<ScienceLabRandomGasSpread> members)
    {
        bool debutedNarrative = false;

        while (enabled)
        {
            while (!ScienceLabHazardPlaybackGate.LabHazardAndAudioAllowed)
                yield return null;

            yield return new WaitForSeconds(Random.Range(cycleRestMin, cycleRestMax));
            Shuffle(members);

            int waveCount = Mathf.Min(3, members.Count / 2);
            if (waveCount <= 0)
                continue;

            for (int wave = 0; wave < waveCount; wave++)
            {
                ScienceLabRandomGasSpread a = members[wave * 2];
                ScienceLabRandomGasSpread b = members[wave * 2 + 1];
                bool debutThisWave = !debutedNarrative;

                LaunchPairBurst(a, b, _bubbly, bubblyLeadSeconds, _ambientSpread, debutThisWave);

                if (debutThisWave)
                    debutedNarrative = true;

                float duration = Mathf.Max(a.EstimatedBurstSeconds, b.EstimatedBurstSeconds);
                yield return new WaitForSeconds(duration + trailingHazardShutoffBuffer);

                if (wave + 1 < waveCount)
                    yield return new WaitForSeconds(Random.Range(interWavePauseMin, interWavePauseMax));
            }
        }
    }

    private static void LaunchPairBurst(ScienceLabRandomGasSpread left, ScienceLabRandomGasSpread right,
        AudioClip bubbly, float bubblyLead, AudioClip ambient, bool debutNarrativeLeftOnly)
    {
        if (left != null)
            left.StartCoroutine(left.CoordinatedBurst(bubbly, bubblyLead, ambient, debutNarrativeLeftOnly));
        if (right != null)
            right.StartCoroutine(right.CoordinatedBurst(bubbly, bubblyLead, ambient, false));
    }

    private static void Shuffle(List<ScienceLabRandomGasSpread> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
