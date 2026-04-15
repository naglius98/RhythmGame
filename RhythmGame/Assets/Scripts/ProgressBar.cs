using UnityEngine;
using UnityEngine.UI;

public class ProgressBar : MonoBehaviour
{
    [SerializeField] GameManager gameManager;
    [SerializeField] Slider progressSlider;
    [SerializeField] TMPro.TMP_Text timeLabel;

    void Awake()
    {
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }
    }

    void Start()
    {
        if (progressSlider != null)
        {
            progressSlider.minValue = 0f;
            progressSlider.maxValue = 1f;
            progressSlider.wholeNumbers = false;
            progressSlider.value = 0f;
        }
    }

    void Update()
    {
        if (progressSlider == null)
        {
            return;
        }

        AudioSource src = gameManager != null ? gameManager.audioSource : null;
        if (src == null || src.clip == null)
        {
            progressSlider.value = 0f;
            if (timeLabel != null)
            {
                timeLabel.text = "";
            }
            return;
        }

        float len = src.clip.length;
        float t = Mathf.Clamp(src.time, 0f, len);
        progressSlider.value = len > 0.001f ? t / len : 0f;

        if (timeLabel != null)
        {
            timeLabel.text = $"{FormatTime(t)} / {FormatTime(len)}";
        }
    }

    static string FormatTime(float seconds)
    {
        int total = Mathf.FloorToInt(seconds);
        int m = total / 60;
        int s = total % 60;
        return $"{m}:{s:00}";
    }
}
