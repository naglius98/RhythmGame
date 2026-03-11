using System;
using UnityEngine;

// Beat Saber Info.dat 
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

// Beat Saber difficulty file (v2) 
[Serializable]
public class DifficultyDat
{
    public string _version;
    public NoteData[] _notes;
}

[Serializable]
public class NoteData
{
    public float _time;        // Beat time
    public int _lineIndex;    // Lines on the track
    public int _lineLayer;    // Ignored for now
    public int _type;         //  Left, Right, Bomb
    public int _cutDirection;
}

// Beat Saber difficulty file (v3) - uses "colorNotes" instead of "_notes"
[Serializable]
public class DifficultyDatV3
{
    public string version;
    public ColorNoteV3[] colorNotes;
}

[Serializable]
public class ColorNoteV3
{
    public float b;  // beat
    public int x;    // line index 0-3
    public int y;    // layer
    public int c;    // color (0 left, 1 right; bombs are in bombNotes)
    public int d;    // cut direction
    public int a;    // angle offset
}

// One spawn to perform at a given time
[Serializable]
public struct MapNoteSpawn
{
    public float spawnTime;
    public int railIndex;
}
