using System;
using UnityEngine;

// Beat Saber Info.dat (root)
[Serializable]
public class InfoDat
{
    public string _version;
    public string _songName;
    public string _songSubName;
    public string _songAuthorName;
    public string _levelAuthorName;
    public float _beatsPerMinute;
    public float _songTimeOffset;
    public string _songFilename;
    public string _coverImageFilename;
    public DifficultyBeatmapSet[] _difficultyBeatmapSets;
}

[Serializable]
public class DifficultyBeatmapSet
{
    public string _beatmapCharacteristicName;
    public DifficultyBeatmap[] _difficultyBeatmaps;
}

[Serializable]
public class DifficultyBeatmap
{
    public string _difficulty;
    public int _difficultyRank;
    public string _beatmapFilename;
    public float _noteJumpMovementSpeed;
    public float _noteJumpStartBeatOffset;
}

// Beat Saber difficulty file (v2) - e.g. Normal.dat, Hard.dat
[Serializable]
public class DifficultyDat
{
    public string _version;
    public NoteData[] _notes;
    // _obstacles, _events omitted for now
}

[Serializable]
public class NoteData
{
    public float _time;        // Beat when note reaches the player
    public int _lineIndex;    // 0-3, maps to our rail
    public int _lineLayer;    // 0-2, we ignore (or use for future 4x3)
    public int _type;         // 0 = left, 1 = right, 3 = bomb (skip)
    public int _cutDirection;
}

// One spawn to perform at a given time (rail + spawn time in seconds)
[Serializable]
public struct MapNoteSpawn
{
    public float spawnTime;
    public int railIndex;
}
