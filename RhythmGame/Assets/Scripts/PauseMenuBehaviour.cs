using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenuBehaviour : MonoBehaviour
{
    [Tooltip("Pause menu panel")]
    public GameObject pausePanel;

    [Tooltip("Countdown display")]
    public GameObject countdownDisplay;

    [Tooltip("Scene to load when Back to Main Menu is pressed")]
    public string mainMenuSceneName = "MainMenu";

    bool isCountdownActive;

    public static bool IsPaused { get; private set; }

    void Start()
    {
        IsPaused = false;
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
        if (countdownDisplay != null)
        {
            countdownDisplay.SetActive(false);
        }
    }

    void Update()
    {
        if (isCountdownActive)
        {
            return;
        }
        if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    void TogglePause()
    {
        if (IsPaused)
        {
            StartCountdownThenUnpause();
        }
        else
        {
            Pause();
        }
    }

    void Pause()
    {
        IsPaused = true;
        Time.timeScale = 0f;
        PauseMusic(true);
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }
        if (countdownDisplay != null)
        {
            countdownDisplay.SetActive(false);
        }
    }

    void Unpause()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        PauseMusic(false);
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
        if (countdownDisplay != null)
        {
            countdownDisplay.SetActive(false);
        }
    }

    void PauseMusic(bool pause)
    {
        var gm = FindObjectOfType<GameManager>();
        if (gm == null || gm.audioSource == null)
        {
            return;
        }
        if (pause)
        {
            gm.audioSource.Pause();
        }
        else
        {
            gm.audioSource.UnPause();
        }
    }

    public void OnContinue()
    {
        if (!IsPaused)
        {
            return;
        }
        StartCountdownThenUnpause();
    }

    void StartCountdownThenUnpause()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
        StartCoroutine(CountdownThenUnpause());
    }

    public void OnBackToMainMenu()
    {
        Time.timeScale = 1f;
        if (string.IsNullOrEmpty(mainMenuSceneName))
        {
            Debug.LogWarning("PauseMenuBehaviour: No main menu scene name set.");
            return;
        }
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void OnRestart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    IEnumerator CountdownThenUnpause()
    {
        isCountdownActive = true;
        if (countdownDisplay != null)
        {
            countdownDisplay.SetActive(true);
        }

        var text = countdownDisplay != null ? countdownDisplay.GetComponentInChildren<UnityEngine.UI.Text>() : null;
        var tmpText = countdownDisplay != null ? countdownDisplay.GetComponentInChildren<TMPro.TMP_Text>() : null;

        for (int i = 3; i >= 1; i--)
        {
            string label = i.ToString();
            if (text != null)
            {
                text.text = label;
            }
            if (tmpText != null)
            {
                tmpText.text = label;
            }
            yield return new WaitForSecondsRealtime(1f);
        }

        if (countdownDisplay != null)
        {
            countdownDisplay.SetActive(false);
        }
        Unpause(); // Unpause after the countdown
        isCountdownActive = false;
    }
}
