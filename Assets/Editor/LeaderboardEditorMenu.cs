using UnityEditor;
using UnityEngine;

public static class LeaderboardEditorMenu
{
    [MenuItem("Tools/Middle School Escape/Reveal Leaderboard JSON…")]
    private static void RevealLeaderboardFile()
    {
        var path = LeaderboardStorage.FilePath;
        if (!System.IO.File.Exists(path))
        {
            EditorUtility.DisplayDialog("Leaderboard", $"No file yet. It will be created at the first win:\n{path}", "OK");
        }
        else
        {
            EditorUtility.RevealInFinder(path);
        }
    }

    [MenuItem("Tools/Middle School Escape/Clear Leaderboard (delete JSON)")]
    private static void ClearLeaderboard()
    {
        if (!EditorUtility.DisplayDialog("Clear leaderboard", "Delete all saved times? This cannot be undone.", "Delete", "Cancel"))
        {
            return;
        }

        LeaderboardStorage.ClearAll();
        Debug.Log("Leaderboard JSON cleared.");
    }
}
