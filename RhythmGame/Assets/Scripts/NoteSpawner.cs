using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    public GameObject notePrefab;
    public float[] railPositions = {-3f, -1f, 1f, 3f}; // X positions
    public float spawnHeight = 6f;
    public float minInterval = 0.5f;
    public float maxInterval = 2f;
    
    private float nextSpawnTime;

    void Start()
    {
        ScheduleNextSpawn();
    }

    void Update()
    {
        if (Time.time >= nextSpawnTime)
        {
            SpawnRandomNote();
            ScheduleNextSpawn();
        }
    }

    void SpawnRandomNote()
    {
        int randomRail = Random.Range(0, 4);
        Vector3 spawnPos = new Vector3(railPositions[randomRail], spawnHeight, 0);
        GameObject note = Instantiate(notePrefab, spawnPos, Quaternion.identity);
        note.GetComponent<Note>().Initialize(randomRail);
    }

    void ScheduleNextSpawn()
    {
        nextSpawnTime = Time.time + Random.Range(minInterval, maxInterval);
    }
}
