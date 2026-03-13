using System.Collections.Generic;
using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    public GameObject notePrefab;
    public float[] railPositions = {-3f, -1f, 1f, 3f}; // X positions
    public float spawnHeight = 6f;
    [Tooltip("Y position of the hit zone center")]
    public float hitZoneY = 0f;
    public float minInterval = 0.5f;
    public float maxInterval = 2f;

    private float nextSpawnTime;
    private List<MapNoteSpawn> mapSpawns;
    private int mapSpawnIndex;
    private bool useMap;

    void Start()
    {
        if (!useMap)
            ScheduleNextSpawn();
    }

    void Update()
    {
        if (useMap)
            return; 
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
        var note = notePrefab != null ? notePrefab.GetComponent<Note>() : null;
        if (note != null)
            speed = note.speed;
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
            return;
        while (mapSpawnIndex < mapSpawns.Count && mapSpawns[mapSpawnIndex].spawnTime <= gameTime)
        {
            var s = mapSpawns[mapSpawnIndex];
            SpawnNoteOnRail(s.railIndex);
            mapSpawnIndex++;
        }
    }

    void SpawnNoteOnRail(int railIndex)
    {
        railIndex = Mathf.Clamp(railIndex, 0, railPositions.Length - 1);
        Vector3 spawnPos = new Vector3(railPositions[railIndex], spawnHeight, 0);
        GameObject note = Instantiate(notePrefab, spawnPos, Quaternion.identity);
        note.GetComponent<Note>().Initialize(railIndex);
    }

    void SpawnRandomNote()
    {
        int randomRail = Random.Range(0, 4);
        SpawnNoteOnRail(randomRail);
    }

    void ScheduleNextSpawn()
    {
        nextSpawnTime = Time.time + Random.Range(minInterval, maxInterval);
    }
}
