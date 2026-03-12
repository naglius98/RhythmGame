using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;


public class GameManager : MonoBehaviour
{
    [Tooltip("Folder name inside StreamingAssets/Maps/")]
    public string songFolderName;

    [Tooltip("Preferred difficulty")]
    public string preferredDifficulty = "Normal";

    public NoteSpawner noteSpawner;
    public AudioSource audioSource;

    [Header("Runtime (read-only)")]
    [SerializeField] string loadedSongName;
    [SerializeField] float bpm;
    [SerializeField] int spawnCount;

    List<MapNoteSpawn> spawnList;
    float gameStartTime;
    bool gameStarted;

    void Start()
    {
        ScoreManager.Reset();
        if (noteSpawner == null) noteSpawner = FindObjectOfType<NoteSpawner>();
        if (audioSource == null) audioSource = FindObjectOfType<AudioSource>();
        if (audioSource == null)
        {
            var go = new GameObject("MapAudioSource");
            audioSource = go.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        string folder = songFolderName;
        if (string.IsNullOrEmpty(folder))
            folder = SongSelectBehaviour.SelectedSongFolder;
        if (string.IsNullOrEmpty(folder))
        {
            string[] available = MapLoader.GetAvailableSongFolders();
            if (available == null || available.Length == 0)
            {
                Debug.LogWarning("No song folders in StreamingAssets/Maps. Using random spawner.");
                return;
            }
            folder = available[0];
            Debug.Log($"No song set, using first folder: {folder}");
        }

        InfoDat info = MapLoader.LoadInfo(folder);
        if (info == null)
        {
            Debug.LogWarning("Could not load map. Using random spawner.");
            return;
        }

        string diffFile = MapLoader.GetFirstDifficultyFilename(info, preferredDifficulty);
        if (string.IsNullOrEmpty(diffFile))
        {
            Debug.LogWarning("No difficulty file in map. Using random spawner.");
            return;
        }

        float travelTime = noteSpawner.GetTravelTimeSeconds();
        spawnList = MapLoader.LoadAndBuildSpawnList(folder, diffFile, info._beatsPerMinute, travelTime);
        if (spawnList == null || spawnList.Count == 0)
        {
            Debug.LogWarning("No notes in map. Using random spawner.");
            return;
        }

        loadedSongName = info._songName;
        bpm = info._beatsPerMinute;
        spawnCount = spawnList.Count;

        noteSpawner.UseMapSpawns(spawnList);
        gameStartTime = 0f;
        gameStarted = false;

        StartCoroutine(LoadAndPlayMusic(folder, info._songFilename));
    }

    IEnumerator LoadAndPlayMusic(string songFolder, string audioFileName)
    {
        if (string.IsNullOrEmpty(audioFileName))
        {
            Debug.LogWarning("No _songFilename in Info.dat.");
            StartMapPlaybackWithOffset();
            yield break;
        }

        string fullPath = Path.Combine(MapLoader.GetSongPath(songFolder), audioFileName);
        string uri = fullPath;
#if UNITY_EDITOR || UNITY_STANDALONE
        if (!uri.StartsWith("file://"))
            uri = "file:///" + uri.Replace("\\", "/");
#endif
        AudioType audioType = audioFileName.EndsWith(".mp3", System.StringComparison.OrdinalIgnoreCase)
            ? AudioType.MPEG
            : AudioType.OGGVORBIS;

        using (var req = UnityWebRequestMultimedia.GetAudioClip(uri, audioType))
        {
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success && req.downloadHandler is DownloadHandlerAudioClip downloadHandler)
            {
                audioSource.clip = downloadHandler.audioClip;
                audioSource.Play();
                StartMapPlaybackWithOffset();
            }
            else
            {
                Debug.LogWarning($"Could not load audio ({req.error}). Starting map without music.");
                StartMapPlaybackWithOffset();
            }
        }
    }

    // Start map playback so that notes hit the zone exactly on the beat
    void StartMapPlaybackWithOffset()
    {
        float travelTime = noteSpawner != null ? noteSpawner.GetTravelTimeSeconds() : 0f;
        gameStartTime = Time.time + travelTime;
        gameStarted = true;
    }

    void Update()
    {
        if (!gameStarted || spawnList == null || noteSpawner == null || !noteSpawner.IsUsingMap())
            return;

        float elapsed = Time.time - gameStartTime;
        noteSpawner.SpawnFromMapUpTo(elapsed);
    }
}
