using UnityEngine;

public static class ScoreManager
{
    static int hits;
    static int misses;

    public static void Reset()
    {
        hits = 0;
        misses = 0;
    }

    public static void RecordHit()
    {
        hits++;
    }

    public static void RecordMiss()
    {
        misses++;
    }

   
    public static float GetAccuracyPercent()
    {
        int total = hits + misses;
        if (total == 0)
            return 100f;
        return (hits / (float)total) * 100f;
    }

    public static int Hits => hits;
    public static int Misses => misses;
}
