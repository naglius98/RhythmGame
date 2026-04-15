using System.Collections.Generic;
using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    public static NoteSpawner Instance { get; private set; }

    public GameObject notePrefab;
    public GameObject holdNotePrefab;
    public float[] railPositions = {-3f, -1f, 1f, 3f}; // X positions

    [Tooltip("Index 0-3: left to right in the track")]
    public Color[] railNoteColors =
    {
        new Color(0.95f, 0.35f, 0.35f),
        new Color(0.35f, 0.75f, 0.95f),
        new Color(0.45f, 0.95f, 0.45f),
        new Color(0.95f, 0.85f, 0.35f)
    };
    public float spawnHeight = 6f;
    [Tooltip("Y position of the hit zone center")]
    public float hitZoneY = 0f;
    public float minInterval = 0.5f;
    public float maxInterval = 2f;

    private float nextSpawnTime;
    private List<MapNoteSpawn> mapSpawns;
    private int mapSpawnIndex;
    private bool useMap;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Start()
    {
        if (!useMap)
        {
            ScheduleNextSpawn();
        }
    }

    public Color GetRailNoteColor(int railIndex)
    {
        if (railNoteColors == null || railNoteColors.Length == 0)
        {
            return Color.white;
        }
        return railNoteColors[Mathf.Clamp(railIndex, 0, railNoteColors.Length - 1)];
    }

    void Update()
    {
        if (useMap)
        {
            return;
        }
        if (Time.time >= nextSpawnTime)
        {
            SpawnRandomNote();
            ScheduleNextSpawn();
        }
    }

    // Time in seconds for a note to travel from spawn height to hit zone
    public float GetTravelTimeSeconds()
    {
        float speed = 5f;
        if (notePrefab != null)
        {
            var n = notePrefab.GetComponent<Note>();
            if (n != null)
            {
                speed = n.speed;
            }
        }
        else if (holdNotePrefab != null)
        {
            var h = holdNotePrefab.GetComponent<HoldNote>();
            if (h != null)
            {
                speed = h.speed;
            }
        }
        float distance = spawnHeight - hitZoneY;
        return distance > 0 && speed > 0 ? distance / speed : 1f;
    }

    public void UseMapSpawns(List<MapNoteSpawn> spawns)
    {
        mapSpawns = spawns;
        mapSpawnIndex = 0;
        useMap = spawns != null && spawns.Count > 0;
    }

    public bool IsUsingMap()
    {
        return useMap;
    }

    // Spawn all map notes whose spawnTime is <= gameTime
    public void SpawnFromMapUpTo(float gameTime)
    {
        if (mapSpawns == null || notePrefab == null)
        {
            return;
        }
        while (mapSpawnIndex < mapSpawns.Count && mapSpawns[mapSpawnIndex].spawnTime <= gameTime)
        {
            var s = mapSpawns[mapSpawnIndex];
            if (s.kind == NoteKind.Hold && s.IsHold)
            {
                SpawnHoldOnRail(s.railIndex, s.idealHeadElapsed, s.idealTailElapsed);
            }
            else
            {
                SpawnNoteOnRail(s.railIndex, s.idealHeadElapsed);
            }
            mapSpawnIndex++;
        }
    }

    void SpawnHoldOnRail(int railIndex, float idealHeadElapsed, float idealTailElapsed)
    {
        if (holdNotePrefab == null)
        {
            Debug.LogError("Map has hold notes but holdNotePrefab is not assigned.");
            return;
        }
        railIndex = Mathf.Clamp(railIndex, 0, railPositions.Length - 1);
        Vector3 spawnPos = new Vector3(railPositions[railIndex], spawnHeight, 0);
        GameObject go = Instantiate(holdNotePrefab, spawnPos, Quaternion.identity);
        HoldNote hold = go.GetComponent<HoldNote>();
        if (hold == null)
        {
            Debug.LogError("holdNotePrefab must have a HoldNote component.");
            Destroy(go);
            return;
        }
        hold.InitializeHold(railIndex, idealHeadElapsed, idealTailElapsed);
    }

    void SpawnNoteOnRail(int railIndex, float idealHitElapsed)
    {
        railIndex = Mathf.Clamp(railIndex, 0, railPositions.Length - 1);
        Vector3 spawnPos = new Vector3(railPositions[railIndex], spawnHeight, 0);
        GameObject note = Instantiate(notePrefab, spawnPos, Quaternion.identity);
        note.GetComponent<Note>().Initialize(railIndex, idealHitElapsed);
    }

    void SpawnRandomNote()
    {
        int randomRail = Random.Range(0, 4);
        var gm = FindObjectOfType<GameManager>();
        float elapsed = gm != null && gm.MapClockRunning ? gm.GetMapElapsedSeconds() : Time.time;
        float ideal = elapsed + GetTravelTimeSeconds();
        SpawnNoteOnRail(randomRail, ideal);
    }

    void ScheduleNextSpawn()
    {
        nextSpawnTime = Time.time + Random.Range(minInterval, maxInterval);
    }
}
