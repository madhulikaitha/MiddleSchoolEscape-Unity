using UnityEngine;

/// <summary>
/// Ends the run, writes the leaderboard file, shows the UI, and freezes gameplay.
/// </summary>
public static class GameVictoryFlow
{
    private static bool sequenceActive;

    public static void RunVictorySequence(GameObject playerObject)
    {
        if (sequenceActive)
        {
            return;
        }

        sequenceActive = true;

        PlayerSessionData.CompleteCurrentRun();
        if (PlayerSessionData.RunHistory.Count == 0)
        {
            sequenceActive = false;
            return;
        }

        var last = PlayerSessionData.RunHistory[PlayerSessionData.RunHistory.Count - 1];
        int rank = LeaderboardStorage.AddEntry(last.PlayerName, last.ElapsedTimeSeconds, out string runId);
        Debug.Log($"GameVictoryFlow: Leaderboard saved to {LeaderboardStorage.FilePath}");

        Time.timeScale = 0f;
        SetPlayerGameplayEnabled(playerObject, false);
        var hud = Object.FindFirstObjectByType<GameHUD>();
        if (hud != null)
        {
            hud.gameObject.SetActive(false);
        }

        LeaderboardEndScreen.Show(rank, runId, last.PlayerName, last.ElapsedTimeSeconds);
    }

    private static void SetPlayerGameplayEnabled(GameObject player, bool enabled)
    {
        if (player == null)
        {
            return;
        }

        var move = player.GetComponent<PlayerMovement2D>();
        if (move != null)
        {
            move.enabled = enabled;
        }

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }
}
