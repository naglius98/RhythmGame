using UnityEngine;

public enum HitJudgment
{
    Perfect,
    Great,
    Good,
    Miss
}

public static class ScoreManager
{
    // Turn the logs off in editor
    public static bool LogScoring = true;

    static int perfects;
    static int greats;
    static int goods;
    static int misses;
    static int totalNotes;
    static long score;
    static int combo;
    static int maxCombo;

    public static float PerfectWindowMs = 45f;
    public static float GreatWindowMs = 90f;
    public static float GoodWindowMs = 135f;

    static int BasePoints(HitJudgment j)
    {
        switch (j)
        {
            case HitJudgment.Perfect: return 300;
            case HitJudgment.Great: return 200;
            case HitJudgment.Good: return 100;
            default: return 0;
        }
    }

    public static void Reset()
    {
        perfects = greats = goods = misses = 0;
        totalNotes = 0;
        score = 0;
        combo = maxCombo = 0;
        if (LogScoring)
        {
            Debug.Log("[Scoring] Reset (new song / scene)");
        }
    }

    public static void SetTotalNotes(int total)
    {
        totalNotes = total > 0 ? total : 0;
        if (LogScoring && totalNotes > 0)
        {
            Debug.Log($"[Scoring] Map note count = {totalNotes} (windows: P≤{PerfectWindowMs} G≤{GreatWindowMs} Go≤{GoodWindowMs} ms)");
        }
    }

    // Absolute timing error
    public static HitJudgment ClassifyTiming(float absErrorMs)
    {
        if (absErrorMs <= PerfectWindowMs)
        {
            return HitJudgment.Perfect;
        }
        if (absErrorMs <= GreatWindowMs)
        {
            return HitJudgment.Great;
        }
        if (absErrorMs <= GoodWindowMs)
        {
            return HitJudgment.Good;
        }
        return HitJudgment.Miss;
    }

    public static void RecordTimedHit(float errorSeconds, int railIndex = -1, Note hitNote = null)
    {
        float absMs = Mathf.Abs(errorSeconds * 1000f);
        float signedMs = errorSeconds * 1000f;
        HitJudgment j = ClassifyTiming(absMs);
        switch (j)
        {
            case HitJudgment.Perfect: perfects++; break;
            case HitJudgment.Great: greats++; break;
            case HitJudgment.Good: goods++; break;
            case HitJudgment.Miss:
                misses++;
                combo = 0;
                break;
        }

        if (j != HitJudgment.Miss)
        {
            combo++;
            if (combo > maxCombo)
            {
                maxCombo = combo;
            }

            float comboMult = 1f + 0.01f * Mathf.Min(combo - 1, 100);
            int gained = Mathf.RoundToInt(BasePoints(j) * comboMult);
            score += gained;

            if (LogScoring)
            {
                string rail = railIndex >= 0 ? $"rail{railIndex} " : "";
                Debug.Log(
                    $"[Scoring] {rail}{j}  signedErr={signedMs:F1}ms  abs={absMs:F1}ms  " +
                    $"combo={combo}  +{gained}  totalScore={score}  timingAcc={GetAccuracyPercent():F1}%{FormatNoteKindLine(hitNote)}");
            }
            return;
        }

        if (LogScoring)
        {
            string rail = railIndex >= 0 ? $"rail{railIndex} " : "";
            Debug.Log(
                $"[Scoring] {rail}{j}  signedErr={signedMs:F1}ms  abs={absMs:F1}ms  " +
                $"combo=0  +0  totalScore={score}  timingAcc={GetAccuracyPercent():F1}%{FormatNoteKindLine(hitNote)}");
        }
    }

    static string FormatNoteKindLine(Note n)
    {
        if (n == null)
        {
            return "";
        }
        if (n is HoldNote hn)
        {
            return $"  noteKind=Hold phase={hn.Phase} idealHead={hn.idealHitElapsed:F3}s tail={hn.idealTailElapsed:F3}s judgeY={hn.GetJudgeWorldY():F2}";
        }
        return $"  noteKind=Tap ideal={n.idealHitElapsed:F3}s judgeY={n.GetJudgeWorldY():F2}";
    }

    public static void RecordMiss(string debugContext = "", Note missedNote = null)
    {
        misses++;
        combo = 0;
        if (LogScoring)
        {
            Debug.Log(
                $"[Scoring] Miss{(string.IsNullOrEmpty(debugContext) ? "" : " " + debugContext)}{FormatNoteKindLine(missedNote)}  misses={misses}  combo=0");
        }
    }

    public static float GetAccuracyPercent()
    {
        int judged = perfects + greats + goods + misses;
        if (judged == 0)
        {
            return 100f;
        }
        // Weights vs 100 = "perfect note". Good used to be 50 → all-Good runs capped at 50% and failed the 60% game-over rule
        float w = perfects * 100f + greats * 94f + goods * 88f;
        return Mathf.Clamp01(w / (judged * 100f)) * 100f;
    }

    public static long Score => score;
    public static int Combo => combo;
    public static int MaxCombo => maxCombo;
    public static int Perfects => perfects;
    public static int Greats => greats;
    public static int Goods => goods;
    public static int Misses => misses;
    public static int Hits => perfects + greats + goods;
    public static int NotesJudged => perfects + greats + goods + misses;

    public static long GetMaxPossibleScore()
    {
        if (totalNotes <= 0)
        {
            return 0;
        }
        float maxComboMult = 1f + 0.01f * Mathf.Min(totalNotes - 1, 100);
        return Mathf.RoundToInt(300f * maxComboMult * totalNotes);
    }

    public static float GetScorePercent()
    {
        long max = GetMaxPossibleScore();
        if (max <= 0)
        {
            return 0f;
        }
        return Mathf.Clamp01(score / (float)max) * 100f;
    }
}
