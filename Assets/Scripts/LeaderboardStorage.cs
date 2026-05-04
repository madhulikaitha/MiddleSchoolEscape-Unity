using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Local JSON leaderboard (fastest times first).
/// In the Unity Editor the file is stored under &lt;project root&gt;/LeaderboardData/ so it stays with the repo.
/// In builds it uses Application.persistentDataPath (writable on device).
/// </summary>
public static class LeaderboardStorage
{
    private const string FileName = "hall_escape_leaderboard.json";
    private const string EditorSubfolder = "LeaderboardData";

    public static string FilePath => GetFilePath();

    private static string GetFilePath()
    {
#if UNITY_EDITOR
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
        return Path.Combine(projectRoot, EditorSubfolder, FileName);
#else
        return Path.Combine(Application.persistentDataPath, FileName);
#endif
    }

    [Serializable]
    private class FileDto
    {
        public LeaderboardEntryData[] entries = Array.Empty<LeaderboardEntryData>();
    }

    [Serializable]
    public class LeaderboardEntryData
    {
        public string playerName;
        public float timeSeconds;
        /// <summary>Optional; used to highlight the row just saved when times/names tie.</summary>
        public string runId;
    }

    public static List<LeaderboardEntryData> LoadAllSorted()
    {
        var list = new List<LeaderboardEntryData>();
        try
        {
            if (!File.Exists(FilePath))
            {
                return list;
            }

            var json = File.ReadAllText(FilePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return list;
            }

            var dto = JsonUtility.FromJson<FileDto>(json);
            if (dto?.entries == null)
            {
                return list;
            }

            list.AddRange(dto.entries);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"LeaderboardStorage: Could not load ({FilePath}): {ex.Message}");
        }

        list.Sort(CompareByTimeThenName);
        return list;
    }

    private static int CompareByTimeThenName(LeaderboardEntryData a, LeaderboardEntryData b)
    {
        int c = a.timeSeconds.CompareTo(b.timeSeconds);
        if (c != 0)
        {
            return c;
        }

        return string.Compare(a.playerName, b.playerName, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Adds one finish time, saves to disk, returns 1-based rank among all stored rows (after insert).</summary>
    public static int AddEntry(string playerName, float timeSeconds, out string runId)
    {
        var name = string.IsNullOrWhiteSpace(playerName) ? "PLAYER" : playerName.Trim();
        timeSeconds = Mathf.Max(0f, timeSeconds);
        string newRunId = Guid.NewGuid().ToString("N");
        runId = newRunId;

        var list = LoadAllSorted();
        list.Add(new LeaderboardEntryData { playerName = name, timeSeconds = timeSeconds, runId = newRunId });
        list.Sort(CompareByTimeThenName);

        int rank = list.FindIndex(e => e.runId == newRunId);
        SaveList(list);
        return rank >= 0 ? rank + 1 : list.Count;
    }

    private static void SaveList(List<LeaderboardEntryData> list)
    {
        try
        {
            string path = FilePath;
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var dto = new FileDto { entries = list.ToArray() };
            var json = JsonUtility.ToJson(dto, true);
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"LeaderboardStorage: Save failed ({FilePath}): {ex.Message}");
        }
    }

    /// <summary>Removes one row from the sorted list (1-based rank index).</summary>
    public static bool TryRemoveAtSortedRank(int oneBasedRank)
    {
        if (oneBasedRank < 1)
        {
            return false;
        }

        var list = LoadAllSorted();
        int i = oneBasedRank - 1;
        if (i >= list.Count)
        {
            return false;
        }

        list.RemoveAt(i);
        SaveList(list);
        return true;
    }

    public static void ClearAll()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                File.Delete(FilePath);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"LeaderboardStorage: Clear failed: {ex.Message}");
        }
    }
}
