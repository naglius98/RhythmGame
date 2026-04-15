using UnityEngine;


[RequireComponent(typeof(UnityEngine.UI.Button))]
public class SongButtonHelper : MonoBehaviour
{
    [Tooltip("Map folder name from StreamingAssets/Maps/")]
    public string folderName;

    public void OnSongClicked()
    {
        var songSelect = FindObjectOfType<SongSelectBehaviour>();
        if (songSelect != null)
        {
            songSelect.OnSongSelected(folderName);
        }
        else
        {
            Debug.LogWarning("SongButtonHelper: No SongSelectBehaviour in scene.");
        }
    }
}
