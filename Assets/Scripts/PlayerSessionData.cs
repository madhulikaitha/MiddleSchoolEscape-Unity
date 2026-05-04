using System;
using UnityEngine;

public static class PlayerSessionData
{
    /// <summary>Where the player stands in the lobby / hall center (matches spawn in PlayerSetupFlow).</summary>
    public static Vector3 LobbyWorldPosition { get; set; } = Vector3.zero;

    /// <summary>Fired when a run timer should reset to zero while staying in play (e.g. respawn after losing all lives).</summary>
    public static event Action OnRunTimerReset;

    public static string PlayerName { get; private set; } = string.Empty;
    public static Sprite SelectedSprite { get; private set; }
    public static CharacterSprites SelectedCharacterSprites { get; private set; }
    public static string SelectedCharacterName { get; private set; } = string.Empty;
    public static float CurrentRunElapsedTime { get; private set; }
    public static bool IsRunActive { get; private set; }
    public static bool IsConfigured => !string.IsNullOrWhiteSpace(PlayerName) && SelectedSprite != null;
    public static bool HasCharacterSelection => SelectedCharacterSprites != null || !string.IsNullOrWhiteSpace(SelectedCharacterName);

    private static readonly System.Collections.Generic.List<PlayerRunRecord> runHistory = new System.Collections.Generic.List<PlayerRunRecord>();
    public static System.Collections.Generic.IReadOnlyList<PlayerRunRecord> RunHistory => runHistory;

    public static void Set(string playerName, Sprite sprite)
    {
        PlayerName = playerName == null ? string.Empty : playerName.Trim();
        SelectedSprite = sprite;
        SelectedCharacterSprites = null;
        SelectedCharacterName = string.Empty;
        IsRunActive = false;
        CurrentRunElapsedTime = 0f;
    }

    public static void Set(string playerName, CharacterSprites characterSprites)
    {
        PlayerName = playerName == null ? string.Empty : playerName.Trim();
        SelectedCharacterSprites = characterSprites;
        SelectedSprite = characterSprites?.Front;
        SelectedCharacterName = characterSprites?.Name == null ? string.Empty : characterSprites.Name.Trim();
        IsRunActive = false;
        CurrentRunElapsedTime = 0f;
    }

    public static void BeginRun()
    {
        if (!IsConfigured)
        {
            return;
        }

        IsRunActive = true;
        CurrentRunElapsedTime = 0f;
    }

    public static void UpdateRunTime(float elapsedSeconds)
    {
        if (!IsRunActive)
        {
            return;
        }

        CurrentRunElapsedTime = Mathf.Max(0f, elapsedSeconds);
    }

    /// <summary>
    /// Keeps name and character, resets elapsed time and notifies HUD. Used when respawning in the lobby after game over (no scene reload).
    /// </summary>
    public static void ResetRunAtLobby()
    {
        CurrentRunElapsedTime = 0f;
        if (IsConfigured)
        {
            IsRunActive = true;
        }

        OnRunTimerReset?.Invoke();
    }

    /// <summary>Full reset for Play Again — clears name/character so the next load shows name entry + character pick.</summary>
    public static void ClearForRestart()
    {
        PlayerName = string.Empty;
        SelectedCharacterName = string.Empty;
        SelectedSprite = null;
        SelectedCharacterSprites = null;
        IsRunActive = false;
        CurrentRunElapsedTime = 0f;
        runHistory.Clear();
    }

    public static void CompleteCurrentRun()
    {
        if (!IsRunActive)
        {
            return;
        }

        runHistory.Add(new PlayerRunRecord
        {
            PlayerName = PlayerName,
            CharacterName = SelectedCharacterName,
            ElapsedTimeSeconds = CurrentRunElapsedTime
        });

        IsRunActive = false;
    }

    public static bool TryGetLastSelection(out string playerName, out string characterName)
    {
        playerName = PlayerName;
        characterName = SelectedCharacterName;
        return !string.IsNullOrWhiteSpace(playerName) || !string.IsNullOrWhiteSpace(characterName);
    }
}

public class PlayerRunRecord
{
    public string PlayerName { get; set; }
    public string CharacterName { get; set; }
    public float ElapsedTimeSeconds { get; set; }
}

public class CharacterSprites
{
    public Sprite Front { get; set; }
    public Sprite Right { get; set; }
    public Sprite Back { get; set; }
    public Sprite Left { get; set; }
    public string Name { get; set; }
}
