using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    [Tooltip("Final precision score")]
    public TMPro.TMP_Text precisionText;

    [Tooltip("Scene to load when Restart is pressed")]
    public string gameSceneName = "SampleScene";

    [Tooltip("Scene to load when Main Menu is pressed")]
    public string mainMenuSceneName = "MainMenu";

    void Start()
    {
        if (precisionText != null)
        {
            precisionText.text =
                "FINAL SCORE: " + ScoreManager.Score +
                "\nTiming: " + ScoreManager.GetAccuracyPercent().ToString("F1") + "%" +
                "\nMax combo: " + ScoreManager.MaxCombo;
        }
    }

    public void OnRestart()
    {
        if (string.IsNullOrEmpty(gameSceneName))
        {
            Debug.LogWarning("No game scene name set");
            return;
        }
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnMainMenu()
    {
        if (string.IsNullOrEmpty(mainMenuSceneName))
        {
            Debug.LogWarning("No main menu scene name set");
            return;
        }
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
