using UnityEngine;

/// <summary>
/// Place on an empty object at the final door with a <see cref="BoxCollider2D"/> set as trigger.
/// When the player touches it, the run ends, the time is saved to the local leaderboard file, and the leaderboard UI appears.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class GameVictoryTrigger : MonoBehaviour
{
    private bool fired;

    private void Reset()
    {
        var c = GetComponent<Collider2D>();
        c.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (fired)
        {
            return;
        }

        if (!TryResolvePlayerRoot(other, out GameObject playerRoot))
        {
            return;
        }

        if (!PlayerSessionData.IsRunActive)
        {
            Debug.LogWarning("GameVictoryTrigger: Player entered exit but the run timer is not active (IsRunActive is false). Leaderboard will not show.");
            return;
        }

        fired = true;
        GameVictoryFlow.RunVictorySequence(playerRoot);
    }

    private static bool TryResolvePlayerRoot(Collider2D other, out GameObject playerRoot)
    {
        if (other.CompareTag("Player"))
        {
            playerRoot = other.gameObject;
            return true;
        }

        var t = other.transform;
        while (t != null)
        {
            if (t.CompareTag("Player"))
            {
                playerRoot = t.gameObject;
                return true;
            }

            if (t.GetComponent<PlayerMovement2D>() != null || t.GetComponent<PlayerHealth>() != null)
            {
                playerRoot = t.gameObject;
                return true;
            }

            t = t.parent;
        }

        playerRoot = null;
        return false;
    }
}
