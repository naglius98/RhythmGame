using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuBehaviour : MonoBehaviour
{
    [Tooltip("Scene to load when Song Select is pressed (e.g. SampleScene or SongSelect).")]
    public string songSelectSceneName = "SampleScene";

    
    public void OnSongSelect()
    {
        if (string.IsNullOrEmpty(songSelectSceneName))
        {
            Debug.LogWarning("MainMenuBehaviour: No scene name set for Song Select.");
            return;
        }
        SceneManager.LoadScene(songSelectSceneName);
    }

    public void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
