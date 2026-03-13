using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SongSelectBehaviour : MonoBehaviour
{
    [Tooltip("Scene to load when a song is chosen")]
    public string gameSceneName = "SampleScene";

    [Tooltip("Scene to load when Back is pressed.")]
    public string mainMenuSceneName = "MainMenu";

    [Header("Song list")]
    [Tooltip("Parent transform where song buttons are spawned")]
    public RectTransform listContent;

    [Tooltip("Button prefab to instantiate for each song")]
    public GameObject buttonPrefab;

    public static string SelectedSongFolder { get; private set; }

    void Start()
    {
        PopulateSongList();
    }

    void PopulateSongList()
    {
        if (listContent == null || buttonPrefab == null)
        {
            Debug.LogWarning("Assign List Content and Button Prefab in the inspector.");
            return;
        }

        string[] folders = MapLoader.GetAvailableSongFolders();
        if (folders == null || folders.Length == 0)
            return;

        for (int i = 0; i < listContent.childCount; i++)
            Destroy(listContent.GetChild(i).gameObject);

        foreach (string folder in folders)
        {
            GameObject buttonObj = Instantiate(buttonPrefab, listContent, false);
            buttonObj.name = folder;

            var text = buttonObj.GetComponentInChildren<Text>();
            if (text != null)
                text.text = folder;
            else
            {
                var tmpText = buttonObj.GetComponentInChildren<TMPro.TMP_Text>();
                if (tmpText != null)
                    tmpText.text = folder;
            }

            var helper = buttonObj.GetComponent<SongButtonHelper>();
            if (helper != null)
                helper.folderName = folder;
            else
            {
                helper = buttonObj.AddComponent<SongButtonHelper>();
                helper.folderName = folder;
            }

            var button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                string folderCopy = folder;
                button.onClick.AddListener(() => OnSongSelected(folderCopy));
            }
        }
    }

    public void OnBackToMainMenu()
    {
        if (string.IsNullOrEmpty(mainMenuSceneName))
        {
            Debug.LogWarning("No main menu scene name set.");
            return;
        }
        SceneManager.LoadScene(mainMenuSceneName);
    }

    
    public void OnSongSelected(string folderName)
    {
        if (string.IsNullOrEmpty(folderName))
        {
            Debug.LogWarning("No folder name passed.");
            return;
        }
        SelectedSongFolder = folderName;
        if (string.IsNullOrEmpty(gameSceneName))
        {
            Debug.LogWarning("No game scene name set.");
            return;
        }
        SceneManager.LoadScene(gameSceneName);
    }
}
