using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


// Load the map
public static class MapLoader
{
    const string MapsFolder = "Maps";
    const string HoldsSidecarFilename = "holds.json";

    // Minimum beat span (tb − b) for a burst slider to become a hold
    public const float MinHoldBeatSpan = 0.0625f;

    // Tap within this many seconds of a hold head is dropped 
    public const float HeadConflictEpsilonSeconds = 0.05f;

    // Full path to a song folder 
    public static string GetSongPath(string songFolderName)
    {
        return Path.Combine(Application.streamingAssetsPath, MapsFolder, songFolderName);
    }

    // List song folder names 
    public static string[] GetAvailableSongFolders()
    {
        string mapsPath = Path.Combine(Application.streamingAssetsPath, MapsFolder);
        if (!Directory.Exists(mapsPath))
        {
            return Array.Empty<string>();
        }

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
            Debug.LogWarning($"Info.dat not found at {path}");
            return null;
        }

        try
        {
            string json = File.ReadAllText(path);
            var info = JsonUtility.FromJson<InfoDat>(json);
            if (info != null && info._difficultyBeatmapSets != null && info._difficultyBeatmapSets.Length > 0)
            {
                return info;
            }
            Debug.LogWarning("Info.dat missing _difficultyBeatmapSets.");
            return info;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to parse Info.dat: {e.Message}");
            return null;
        }
    }

    // Load difficulty file (v2 or v3 format)
    public static DifficultyDat LoadDifficulty(string songFolderName, string fileName)
    {
        string path = Path.Combine(GetSongPath(songFolderName), fileName);
        if (!File.Exists(path))
        {
            Debug.LogWarning($"Difficulty file not found at {path}");
            return null;
        }

        try
        {
            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<DifficultyDat>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to parse difficulty file: {e.Message}");
            return null;
        }
    }

    // Full path to a beatmap .dat in the song folder
    public static string GetBeatmapDatPathInSongFolder(string songFolderName)
    {
        string folder = GetSongPath(songFolderName);
        if (!Directory.Exists(folder))
        {
            return null;
        }

        var found = new List<string>();
        foreach (string full in Directory.GetFiles(folder, "*.dat"))
        {
            if (string.Equals(Path.GetFileName(full), "Info.dat", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            found.Add(full);
        }
        if (found.Count == 0)
        {
            return null;
        }
        found.Sort(StringComparer.OrdinalIgnoreCase);
        if (found.Count > 1)
        {
            Debug.LogWarning(
                "[MapLoader] Multiple beatmap .dat files in \"" + songFolderName + "\"; using " +
                Path.GetFileName(found[0]) + ".");
        }
        return found[0];
    }

    // Load .dat file and build spawn list
    public static List<MapNoteSpawn> LoadAndBuildSpawnList(
        string songFolderName,
        float bpm,
        float travelTimeSeconds,
        float synthHoldMaxBeatGapBeats = 0f)
    {
        string path = GetBeatmapDatPathInSongFolder(songFolderName);
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning("No beatmap .dat in song folder (excluding Info.dat): " + GetSongPath(songFolderName));
            return new List<MapNoteSpawn>();
        }

        try
        {
            string json = File.ReadAllText(path);
            var v2 = JsonUtility.FromJson<DifficultyDat>(json);
            if (v2 != null && v2._notes != null && v2._notes.Length > 0)
            {
                List<MapNoteSpawn> taps = BuildSpawnList(bpm, travelTimeSeconds, v2);
                taps = SynthesizeHoldsFromAdjacentTaps(taps, bpm, travelTimeSeconds, synthHoldMaxBeatGapBeats);
                List<MapNoteSpawn> sidecar = LoadHoldsSidecar(songFolderName, bpm, travelTimeSeconds);
                return ReturnWithHoldLog(MergeTapsAndHolds(taps, sidecar, null), 0);
            }

            var v3 = JsonUtility.FromJson<DifficultyDatV3>(json);
            if (v3 != null)
            {
                // Merge burst sliders with raw-array parse
                BurstSliderV3[] burstSliders = CoalesceBurstSliders(v3.burstSliders, json);
                bool hasColor = v3.colorNotes != null && v3.colorNotes.Length > 0;
                bool hasBurst = burstSliders != null && burstSliders.Length > 0;
                if (hasColor || hasBurst)
                {
                    List<MapNoteSpawn> taps = hasColor
                        ? BuildSpawnListFromV3(bpm, travelTimeSeconds, v3.colorNotes)
                        : new List<MapNoteSpawn>();
                    taps = SynthesizeHoldsFromAdjacentTaps(taps, bpm, travelTimeSeconds, synthHoldMaxBeatGapBeats);
                    List<MapNoteSpawn> burstHolds = hasBurst
                        ? BuildHoldsFromBurstSliders(bpm, travelTimeSeconds, burstSliders)
                        : new List<MapNoteSpawn>();
                    List<MapNoteSpawn> sidecar = LoadHoldsSidecar(songFolderName, bpm, travelTimeSeconds);
                    return ReturnWithHoldLog(MergeTapsAndHolds(taps, sidecar, burstHolds), burstSliders?.Length ?? 0);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to parse difficulty file: {e.Message}");
        }

        return new List<MapNoteSpawn>();
    }

    static List<MapNoteSpawn> ReturnWithHoldLog(List<MapNoteSpawn> list, int burstSlidersParsed = 0)
    {
        if (list != null)
        {
            int hc = 0;
            foreach (MapNoteSpawn s in list)
            {
                if (s.kind == NoteKind.Hold && s.IsHold)
                {
                    hc++;
                }
            }
            Debug.Log($"[MapLoader] Hold notes in spawn list: {hc} (burst slider objects parsed from file: {burstSlidersParsed}).");
        }
        return list;
    }

    // Fall back to extracting the JSON array by bracket matching
    static BurstSliderV3[] CoalesceBurstSliders(BurstSliderV3[] fromUtility, string rawJson)
    {
        if (fromUtility != null && fromUtility.Length > 0)
        {
            return fromUtility;
        }
        BurstSliderV3[] extracted = TryExtractBurstSlidersArray(rawJson);
        return extracted != null ? extracted : Array.Empty<BurstSliderV3>();
    }

    [Serializable]
    class BurstSliderJsonArray
    {
        public BurstSliderV3[] items;
    }

    static BurstSliderV3[] TryExtractBurstSlidersArray(string json)
    {
        int keyIdx = json.IndexOf("\"burstSliders\"", StringComparison.Ordinal);
        if (keyIdx < 0)
        {
            return null;
        }
        int lb = json.IndexOf('[', keyIdx);
        if (lb < 0)
        {
            return null;
        }
        int depth = 0;
        int end = -1;
        for (int i = lb; i < json.Length; i++)
        {
            char c = json[i];
            if (c == '[')
            {
                depth++;
            }
            else if (c == ']')
            {
                depth--;
                if (depth == 0)
                {
                    end = i;
                    break;
                }
            }
        }
        if (end < 0)
        {
            return null;
        }
        string arrayJson = json.Substring(lb, end - lb + 1);
        if (arrayJson == "[]")
        {
            return Array.Empty<BurstSliderV3>();
        }
        string wrapped = "{\"items\":" + arrayJson + "}";
        try
        {
            BurstSliderJsonArray w = JsonUtility.FromJson<BurstSliderJsonArray>(wrapped);
            return w != null && w.items != null ? w.items : Array.Empty<BurstSliderV3>();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[MapLoader] Could not parse burstSliders array: {e.Message}");
            return null;
        }
    }

    const int BombType = 3;

    // Convert map notes to spawn list: beat -> spawn time (seconds), rail index (v2 format)
    public static List<MapNoteSpawn> BuildSpawnList(float bpm, float travelTimeSeconds, DifficultyDat difficulty)
    {
        var list = new List<MapNoteSpawn>();
        if (difficulty == null || difficulty._notes == null)
        {
            return list;
        }

        float secondsPerBeat = 60f / bpm;

        foreach (NoteData n in difficulty._notes)
        {
            if (n._type == BombType)
            {
                continue;
            }

            int rail = Mathf.Clamp(n._lineIndex, 0, 3);
            float hitTimeSeconds = n._time * secondsPerBeat;
            float spawnTime = hitTimeSeconds - travelTimeSeconds;

            list.Add(new MapNoteSpawn
            {
                kind = NoteKind.Tap,
                idealHeadElapsed = hitTimeSeconds,
                spawnTime = spawnTime,
                railIndex = rail,
                idealTailElapsed = 0f
            });
        }

        list.Sort((a, b) => a.spawnTime.CompareTo(b.spawnTime));
        return list;
    }
    
    // Build spawn list from v3 colorNotes (b=beat, x=line index)
    public static List<MapNoteSpawn> BuildSpawnListFromV3(float bpm, float travelTimeSeconds, ColorNoteV3[] colorNotes)
    {
        var list = new List<MapNoteSpawn>();
        if (colorNotes == null || colorNotes.Length == 0)
        {
            return list;
        }

        float secondsPerBeat = 60f / bpm;

        foreach (var n in colorNotes)
        {
            int rail = Mathf.Clamp(n.x, 0, 3);
            float hitTimeSeconds = n.b * secondsPerBeat;
            float spawnTime = hitTimeSeconds - travelTimeSeconds;
            list.Add(new MapNoteSpawn
            {
                kind = NoteKind.Tap,
                idealHeadElapsed = hitTimeSeconds,
                spawnTime = spawnTime,
                railIndex = rail,
                idealTailElapsed = 0f
            });
        }

        list.Sort((a, b) => a.spawnTime.CompareTo(b.spawnTime));
        return list;
    }

    // Merge consecutive taps on the same lane into one hold when each step gap is in [MinHoldBeatSpan, maxBeatGap]
    static List<MapNoteSpawn> SynthesizeHoldsFromAdjacentTaps(
        List<MapNoteSpawn> taps,
        float bpm,
        float travelTimeSeconds,
        float maxBeatGap)
    {
        if (maxBeatGap <= 0f || taps == null || taps.Count < 2)
        {
            return taps;
        }

        float secondsPerBeat = 60f / bpm;
        var byRail = new List<MapNoteSpawn>[4];
        for (int r = 0; r < 4; r++)
        {
            byRail[r] = new List<MapNoteSpawn>();
        }
        var nonTap = new List<MapNoteSpawn>();
        foreach (MapNoteSpawn s in taps)
        {
            if (s.kind != NoteKind.Tap)
            {
                nonTap.Add(s);
                continue;
            }
            int r = Mathf.Clamp(s.railIndex, 0, 3);
            byRail[r].Add(s);
        }
        for (int r = 0; r < 4; r++)
        {
            byRail[r].Sort((a, b) => a.idealHeadElapsed.CompareTo(b.idealHeadElapsed));
        }

        var merged = new List<MapNoteSpawn>(taps.Count);
        for (int r = 0; r < 4; r++)
        {
            List<MapNoteSpawn> list = byRail[r];
            int i = 0;
            while (i < list.Count)
            {
                int j = i;
                while (j + 1 < list.Count)
                {
                    float gapBeats = (list[j + 1].idealHeadElapsed - list[j].idealHeadElapsed) / secondsPerBeat;
                    if (gapBeats >= MinHoldBeatSpan - 0.0001f && gapBeats <= maxBeatGap + 0.0001f)
                    {
                        j++;
                    }
                    else
                    {
                        break;
                    }
                }

                if (j > i)
                {
                    MapNoteSpawn head = list[i];
                    MapNoteSpawn tail = list[j];
                    float spawnTime = head.idealHeadElapsed - travelTimeSeconds;
                    merged.Add(new MapNoteSpawn
                    {
                        kind = NoteKind.Hold,
                        idealHeadElapsed = head.idealHeadElapsed,
                        idealTailElapsed = tail.idealHeadElapsed,
                        spawnTime = spawnTime,
                        railIndex = r
                    });
                    i = j + 1;
                }
                else
                {
                    merged.Add(list[i]);
                    i++;
                }
            }
        }
        merged.AddRange(nonTap);
        merged.Sort((a, b) => a.spawnTime.CompareTo(b.spawnTime));
        return merged;
    }

    static List<MapNoteSpawn> BuildHoldsFromBurstSliders(float bpm, float travelTimeSeconds, BurstSliderV3[] sliders)
    {
        var list = new List<MapNoteSpawn>();
        if (sliders == null || sliders.Length == 0)
        {
            return list;
        }

        float secondsPerBeat = 60f / bpm;

        foreach (BurstSliderV3 s in sliders)
        {
            if (s.tb <= s.b + MinHoldBeatSpan - 0.0001f)
            {
                continue;
            }

            int rail = Mathf.Clamp(s.x, 0, 3);
            float headSec = s.b * secondsPerBeat;
            float tailSec = s.tb * secondsPerBeat;
            if (tailSec <= headSec)
            {
                continue;
            }

            float spawnTime = headSec - travelTimeSeconds;
            list.Add(new MapNoteSpawn
            {
                kind = NoteKind.Hold,
                idealHeadElapsed = headSec,
                idealTailElapsed = tailSec,
                spawnTime = spawnTime,
                railIndex = rail
            });
        }

        list.Sort((a, b) => a.spawnTime.CompareTo(b.spawnTime));
        return list;
    }

    static List<MapNoteSpawn> LoadHoldsSidecar(string songFolderName, float bpm, float travelTimeSeconds)
    {
        var list = new List<MapNoteSpawn>();
        string path = Path.Combine(GetSongPath(songFolderName), HoldsSidecarFilename);
        if (!File.Exists(path))
        {
            return list;
        }

        try
        {
            string json = File.ReadAllText(path);
            var data = JsonUtility.FromJson<HoldsSidecarFile>(json);
            if (data == null || data.holds == null || data.holds.Length == 0)
            {
                return list;
            }

            float secondsPerBeat = 60f / bpm;

            foreach (HoldSidecarEntry e in data.holds)
            {
                if (e == null || e.tailBeat <= e.headBeat + MinHoldBeatSpan - 0.0001f)
                {
                    continue;
                }

                int rail = Mathf.Clamp(e.rail, 0, 3);
                float headSec = e.headBeat * secondsPerBeat;
                float tailSec = e.tailBeat * secondsPerBeat;
                if (tailSec <= headSec)
                {
                    continue;
                }

                float spawnTime = headSec - travelTimeSeconds;
                list.Add(new MapNoteSpawn
                {
                    kind = NoteKind.Hold,
                    idealHeadElapsed = headSec,
                    idealTailElapsed = tailSec,
                    spawnTime = spawnTime,
                    railIndex = rail
                });
            }

            list.Sort((a, b) => a.spawnTime.CompareTo(b.spawnTime));
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load holds.json: {e.Message}");
        }

        return list;
    }

    
    // Sidecar holds first
    // Burst holds are not duplicates
    // Taps whose head conflicts with any hold head on the same rail are removed
    static List<MapNoteSpawn> MergeTapsAndHolds(
        List<MapNoteSpawn> taps,
        List<MapNoteSpawn> sidecarHolds,
        List<MapNoteSpawn> burstHolds)
    {
        var holds = new List<MapNoteSpawn>();
        if (sidecarHolds != null)
        {
            foreach (MapNoteSpawn h in sidecarHolds)
            {
                holds.Add(h);
            }
        }

        if (burstHolds != null)
        {
            foreach (MapNoteSpawn h in burstHolds)
            {
                if (HoldDuplicateOfAny(h, holds))
                {
                    continue;
                }
                holds.Add(h);
            }
        }

        var filteredTaps = new List<MapNoteSpawn>(taps != null ? taps.Count : 0);
        if (taps != null)
        {
            foreach (MapNoteSpawn t in taps)
            {
                if (t.kind != NoteKind.Tap)
                {
                    filteredTaps.Add(t);
                    continue;
                }

                if (TapConflictsWithHoldHead(t, holds))
                {
                    continue;
                }

                filteredTaps.Add(t);
            }
        }

        var merged = new List<MapNoteSpawn>(holds.Count + filteredTaps.Count);
        foreach (MapNoteSpawn h in holds)
        {
            merged.Add(h);
        }

        foreach (MapNoteSpawn t in filteredTaps)
        {
            merged.Add(t);
        }

        merged.Sort((a, b) => a.spawnTime.CompareTo(b.spawnTime));
        return merged;
    }

    /// True if a hold with the same rail and head time already exists
    static bool HoldDuplicateOfAny(MapNoteSpawn candidate, List<MapNoteSpawn> existing)
    {
        foreach (MapNoteSpawn h in existing)
        {
            if (h.kind != NoteKind.Hold)
            {
                continue;
            }

            if (h.railIndex != candidate.railIndex)
            {
                continue;
            }

            if (Mathf.Abs(h.idealHeadElapsed - candidate.idealHeadElapsed) < HeadConflictEpsilonSeconds)
            {
                return true;
            }
        }

        return false;
    }

    static bool TapConflictsWithHoldHead(MapNoteSpawn tap, List<MapNoteSpawn> holds)
    {
        foreach (MapNoteSpawn h in holds)
        {
            if (h.kind != NoteKind.Hold || !h.IsHold)
            {
                continue;
            }

            if (h.railIndex != tap.railIndex)
            {
                continue;
            }

            if (Mathf.Abs(h.idealHeadElapsed - tap.idealHeadElapsed) < HeadConflictEpsilonSeconds)
            {
                return true;
            }
        }

        return false;
    }
}
