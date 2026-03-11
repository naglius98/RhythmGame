using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


// Load the map
public static class MapLoader
{
    const string MapsFolder = "Maps";

    /// Full path to a song folder 
    public static string GetSongPath(string songFolderName)
    {
        return Path.Combine(Application.streamingAssetsPath, MapsFolder, songFolderName);
    }

    // List song folder names 
    public static string[] GetAvailableSongFolders()
    {
        string mapsPath = Path.Combine(Application.streamingAssetsPath, MapsFolder);
        if (!Directory.Exists(mapsPath))
            return Array.Empty<string>();

        string[] dirs = Directory.GetDirectories(mapsPath);
        var names = new string[dirs.Length];
        for (int i = 0; i < dirs.Length; i++)
            names[i] = Path.GetFileName(dirs[i]);
        return names;
    }

    // Load Info.dat from a song folder
    public static InfoDat LoadInfo(string songFolderName)
    {
        string path = Path.Combine(GetSongPath(songFolderName), "Info.dat");
        if (!File.Exists(path))
        {
            Debug.LogWarning($"MapLoader: Info.dat not found at {path}");
            return null;
        }

        try
        {
            string json = File.ReadAllText(path);
            var info = JsonUtility.FromJson<InfoDat>(json);
            if (info != null && info._difficultyBeatmapSets != null && info._difficultyBeatmapSets.Length > 0)
                return info;
            Debug.LogWarning("MapLoader: Info.dat missing _difficultyBeatmapSets.");
            return info;
        }
        catch (Exception e)
        {
            Debug.LogError($"MapLoader: Failed to parse Info.dat: {e.Message}");
            return null;
        }
    }

    // Load difficulty file as v2 format
    public static DifficultyDat LoadDifficulty(string songFolderName, string fileName)
    {
        string path = Path.Combine(GetSongPath(songFolderName), fileName);
        if (!File.Exists(path))
        {
            Debug.LogWarning($"MapLoader: Difficulty file not found at {path}");
            return null;
        }

        try
        {
            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<DifficultyDat>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"MapLoader: Failed to parse difficulty file: {e.Message}");
            return null;
        }
    }

    const int BombType = 3;

    /// <summary>
    /// Convert map notes to spawn list: beat -> spawn time (seconds), rail index
    /// Uses BPM and note travel time so the note reaches the hit zone at the correct beat
    /// </summary>
    /// <param name="bpm">Beats per minute from Info.dat.</param>
    /// <param name="travelTimeSeconds">Time for a note to go from spawn Y to hit zone Y at current speed</param>
    /// <param name="difficulty">Parsed difficulty (notes array)</param>
    /// <returns>Sorted list of spawns (by spawnTime)</returns>
    public static List<MapNoteSpawn> BuildSpawnList(float bpm, float travelTimeSeconds, DifficultyDat difficulty)
    {
        var list = new List<MapNoteSpawn>();
        if (difficulty == null || difficulty._notes == null)
            return list;

        float secondsPerBeat = 60f / bpm;

        foreach (NoteData n in difficulty._notes)
        {
            if (n._type == BombType)
                continue;

            int rail = Mathf.Clamp(n._lineIndex, 0, 3);
            float hitTimeSeconds = n._time * secondsPerBeat;
            float spawnTime = hitTimeSeconds - travelTimeSeconds;

            list.Add(new MapNoteSpawn { spawnTime = spawnTime, railIndex = rail });
        }

        list.Sort((a, b) => a.spawnTime.CompareTo(b.spawnTime));
        return list;
    }

    
    // Get the first difficulty beatmap filename 
    public static string GetFirstDifficultyFilename(InfoDat info, string preferredDifficulty = "Normal")
    {
        if (info?._difficultyBeatmapSets == null || info._difficultyBeatmapSets.Length == 0)
            return null;

        foreach (var set in info._difficultyBeatmapSets)
        {
            if (set._difficultyBeatmaps == null || set._difficultyBeatmaps.Length == 0)
                continue;

            foreach (var d in set._difficultyBeatmaps)
            {
                if (string.Equals(d._difficulty, preferredDifficulty, StringComparison.OrdinalIgnoreCase))
                    return d._beatmapFilename;
            }

            return set._difficultyBeatmaps[0]._beatmapFilename;
        }

        return null;
    }
}
