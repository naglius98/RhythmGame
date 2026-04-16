using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;


public class GameManager : MonoBehaviour
{
    [Tooltip("Folder name inside StreamingAssets/Maps/")]
    public string songFolderName;

    public NoteSpawner noteSpawner;
    public AudioSource audioSource;

    [Tooltip("If > 0, consecutive taps on the same lane merge into holds when each step is at most this many beats apart (minimum span is MapLoader.MinHoldBeatSpan). 0 = off.")]
    [Min(0f)]
    public float synthHoldMaxBeatGapBeats;

    [Tooltip("Scene to load on game over")]
    public string gameOverSceneName = "GameOver";

    [Tooltip("After at least 8 judged notes, game over if accuracy is below this percent. Set to 0 to turn off accuracy-based game over (e.g. for testing).")]
    [Range(0f, 100f)]
    public float gameOverIfAccuracyBelowPercent = 60f;

    [Header("Runtime (read-only)")]
    [SerializeField] string loadedSongName;
    [SerializeField] float bpm;
    [SerializeField] int spawnCount;

    List<MapNoteSpawn> spawnList;
    float gameStartTime;
    bool gameStarted;
    bool gameOverTriggered;

    void Start()
    {
        ScoreManager.Reset();
        if (noteSpawner == null)
        {
            noteSpawner = FindObjectOfType<NoteSpawner>();
        }
        if (audioSource == null)
        {
            audioSource = FindObjectOfType<AudioSource>();
        }
        if (audioSource == null)
        {
            var go = new GameObject("MapAudioSource");
            audioSource = go.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        string folder = songFolderName;
        if (string.IsNullOrEmpty(folder))
        {
            folder = SongSelectBehaviour.SelectedSongFolder;
        }
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

        float travelTime = noteSpawner.GetTravelTimeSeconds();
        spawnList = MapLoader.LoadAndBuildSpawnList(folder, info._beatsPerMinute, travelTime, synthHoldMaxBeatGapBeats);
        if (spawnList == null || spawnList.Count == 0)
        {
            Debug.LogWarning("No notes in map. Using random spawner.");
            return;
        }

        loadedSongName = info._songName;
        bpm = info._beatsPerMinute;
        spawnCount = spawnList.Count;

        noteSpawner.UseMapSpawns(spawnList);
        ScoreManager.SetTotalNotes(spawnList.Count);
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
        {
            uri = "file:///" + uri.Replace("\\", "/");
        }
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

    public bool MapClockRunning => gameStarted;

    // Seconds since map start
    public float GetMapElapsedSeconds()
    {
        if (!gameStarted)
        {
            return 0f;
        }
        return Time.time - gameStartTime;
    }

    // Call if song is complete or accuracy drops below 60%
    public void TriggerGameOver()
    {
        Time.timeScale = 1f;
        if (string.IsNullOrEmpty(gameOverSceneName))
        {
            Debug.LogWarning("GameManager: No game over scene name set.");
            return;
        }
        SceneManager.LoadScene(gameOverSceneName);
    }

    void Update()
    {
        if (!gameStarted || spawnList == null || noteSpawner == null || !noteSpawner.IsUsingMap())
        {
            return;
        }

        float elapsed = Time.time - gameStartTime;
        noteSpawner.SpawnFromMapUpTo(elapsed);

        // Need to check if the song is over and not paused
        if (!gameOverTriggered && !PauseMenuBehaviour.IsPaused && audioSource != null && audioSource.clip != null && !audioSource.isPlaying)
        {
            gameOverTriggered = true;
            TriggerGameOver();
        }

        int totalJudged = ScoreManager.NotesJudged;
        if (!gameOverTriggered && !PauseMenuBehaviour.IsPaused
            && gameOverIfAccuracyBelowPercent > 0f
            && totalJudged >= 8
            && ScoreManager.GetAccuracyPercent() < gameOverIfAccuracyBelowPercent)
        {
            gameOverTriggered = true;
            TriggerGameOver();
        }
    }
}
