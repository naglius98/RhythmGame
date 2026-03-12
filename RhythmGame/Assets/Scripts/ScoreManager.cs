using UnityEngine;

public static class ScoreManager
{
    static int hits;
    static int misses;
    static int totalNotes; 

    public static void Reset()
    {
        hits = 0;
        misses = 0;
        totalNotes = 0;
    }

    // Set the total number of notes
    public static void SetTotalNotes(int total)
    {
        totalNotes = total > 0 ? total : 0;
    }

    public static void RecordHit()
    {
        hits++;
    }

    public static void RecordMiss()
    {
        misses++;
    }

    // Start at 100%
    public static float GetAccuracyPercent()
    {
        if (totalNotes > 0)
            return Mathf.Max(0f, 100f - (misses * (100f / totalNotes)));
        int total = hits + misses;
        if (total == 0)
            return 100f;
        return (hits / (float)total) * 100f;
    }

    public static int Hits => hits;
    public static int Misses => misses;
}
