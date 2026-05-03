using UnityEngine;

public static class PlayerSessionData
{
    public static string PlayerName { get; private set; } = string.Empty;
    public static Sprite SelectedSprite { get; private set; }
    public static CharacterSprites SelectedCharacterSprites { get; private set; }
    public static bool IsConfigured => !string.IsNullOrWhiteSpace(PlayerName) && SelectedSprite != null;

    public static void Set(string playerName, Sprite sprite)
    {
        PlayerName = playerName == null ? string.Empty : playerName.Trim();
        SelectedSprite = sprite;
    }

    public static void Set(string playerName, CharacterSprites characterSprites)
    {
        PlayerName = playerName == null ? string.Empty : playerName.Trim();
        SelectedCharacterSprites = characterSprites;
        SelectedSprite = characterSprites?.Front;
    }
}

public class CharacterSprites
{
    public Sprite Front { get; set; }
    public Sprite Right { get; set; }
    public Sprite Back { get; set; }
    public Sprite Left { get; set; }
    public string Name { get; set; }
}
