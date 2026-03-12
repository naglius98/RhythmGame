using UnityEngine;

public class ScoreDisplay : MonoBehaviour
{
    [Tooltip("TMP_Text to show accuracy.")]
    public TMPro.TMP_Text tmpText;

    void Update()
    {
        if (tmpText == null)
            return;
        float accuracy = ScoreManager.GetAccuracyPercent();
        tmpText.text = accuracy.ToString("F1") + "%";
    }
}
