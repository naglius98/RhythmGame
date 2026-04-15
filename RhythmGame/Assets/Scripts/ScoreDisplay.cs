using UnityEngine;

public class ScoreDisplay : MonoBehaviour
{
    [Tooltip("TMP_Text to show score, combo, and timing accuracy.")]
    public TMPro.TMP_Text tmpText;

    void Update()
    {
        if (tmpText == null)
        {
            return;
        }
        long sc = ScoreManager.Score;
        int cb = ScoreManager.Combo;
        float acc = ScoreManager.GetAccuracyPercent();
        tmpText.text = $"Score {sc}  |  x{cb}  |  {acc:F1}%";
    }
}
