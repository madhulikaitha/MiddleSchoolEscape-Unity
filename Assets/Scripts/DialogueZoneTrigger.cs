using UnityEngine;

/// <summary>
/// Narrative volumes. Room exits stop timed interior dialogue. <see cref="NarrativeTriggerKind.LobbyExit"/> stops lobby autoplay;
/// <see cref="NarrativeTriggerKind.LobbyDoor"/> also stops autoplay and plays the door-forward line once.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class DialogueZoneTrigger : MonoBehaviour
{
    public enum NarrativeTriggerKind
    {
        LobbyDoor,
        CafeteriaEntry,
        CafeteriaFlippedTables,
        CafeteriaNearExit,
        CafeteriaExit,
        HallwayMazeEntry,
        HallwayExit,
        ScienceLabEntry,
        ScienceLabNearVictory,
        LobbyInterior,
        LobbyCorridor,
        HallwayMid,
        ScienceLabMidFloor,
        LobbyExit,
        ScienceLabExit
    }

    [SerializeField] private NarrativeTriggerKind triggerKind = NarrativeTriggerKind.LobbyDoor;

    [Tooltip("Fire every enter vs once ever this session")]
    [SerializeField] private bool fireOnce = true;

    private bool _fired;

    private void Reset()
    {
        var c = GetComponent<Collider2D>();
        if (c != null)
            c.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (fireOnce && _fired)
            return;

        NarrativeDialogueController.Instance?.FireZoneTrigger(triggerKind);
        if (fireOnce)
            _fired = true;
    }
}
