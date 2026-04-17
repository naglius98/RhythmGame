using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerInput : MonoBehaviour
{
    public GameObject[] hitZones; // List of hit zones
    [Tooltip("If > 0, only destroy a note on early press when it's within this many seconds ahead. Set <= 0 for no lead limit.")]
    public float earlyPressDestroyLeadSeconds = 0f;

    GameManager gameManager;

    // Track all active notes per rail
    private List<Note>[] notesPerRail = new List<Note>[4];

    // Hold in progress per rail 
    readonly HoldNote[] activeHoldByRail = new HoldNote[4];

    void Awake()
    {
        gameManager = FindObjectOfType<GameManager>();
    }

    void Start()
    {
        for (int i = 0; i < 4; i++)
            notesPerRail[i] = new List<Note>();
    }

    float GetMapElapsed()
    {
        return gameManager != null && gameManager.MapClockRunning
            ? gameManager.GetMapElapsedSeconds()
            : Time.time;
    }

    void Update()
    {
        if (PauseMenuBehaviour.IsPaused)
        {
            return;
        }

        ProcessActiveHolds();

        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.dKey.wasPressedThisFrame)
        {
            CheckHit(0);
        }
        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            CheckHit(1);
        }
        if (Keyboard.current.jKey.wasPressedThisFrame)
        {
            CheckHit(2);
        }
        if (Keyboard.current.kKey.wasPressedThisFrame)
        {
            CheckHit(3);
        }

        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            QuitGame();
        }
    }

    static float TailWindowSeconds => ScoreManager.GoodWindowMs / 1000f;

    void ProcessActiveHolds()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        for (int rail = 0; rail < 4; rail++)
        {
            HoldNote h = activeHoldByRail[rail];
            if (h == null)
            {
                continue;
            }

            float elapsed = GetMapElapsed();

            if (WasReleasedThisFrame(rail))
            {
                float tailErr = elapsed - h.idealTailElapsed;
                if (elapsed < h.idealTailElapsed - TailWindowSeconds)
                {
                    ScoreManager.RecordMiss($"(hold early release) rail{rail}", h);
                }
                else
                {
                    ScoreManager.RecordTimedHit(tailErr, rail, h);
                }
                ClearActiveHold(rail, h);
                continue;
            }

            if (elapsed > h.idealTailElapsed + TailWindowSeconds)
            {
                ScoreManager.RecordMiss($"(hold late tail) rail{rail}", h);
                ClearActiveHold(rail, h);
            }
        }
    }

    static bool WasReleasedThisFrame(int rail)
    {
        switch (rail)
        {
            case 0: return Keyboard.current.dKey.wasReleasedThisFrame;
            case 1: return Keyboard.current.fKey.wasReleasedThisFrame;
            case 2: return Keyboard.current.jKey.wasReleasedThisFrame;
            case 3: return Keyboard.current.kKey.wasReleasedThisFrame;
            default: return false;
        }
    }

    void ClearActiveHold(int rail, HoldNote h)
    {
        activeHoldByRail[rail] = null;
        RemoveNote(h);
        if (h != null)
        {
            Destroy(h.gameObject);
        }
    }

    void CheckHit(int railIndex)
    {
        if (activeHoldByRail[railIndex] != null)
        {
            return;
        }

        if (hitZones[railIndex] != null)
        {
            HitZonePulse pulse = hitZones[railIndex].GetComponent<HitZonePulse>();
            if (pulse != null)
            {
                pulse.TriggerPulse();
            }
        }

        HitZone zone = hitZones[railIndex] != null ? hitZones[railIndex].GetComponent<HitZone>() : null;
        float elapsed = GetMapElapsed();
        LogNearestTimingDebug(railIndex, elapsed);
        if (zone != null)
        {
            HoldNote zoneHold = zone.GetClosestPendingHold();
            if (zoneHold != null && zone.Contains(zoneHold))
            {
                float holdErrSec = elapsed - zoneHold.idealHitElapsed;
                float holdErrMs = Mathf.Abs(holdErrSec * 1000f);
                if (holdErrMs <= ScoreManager.GoodWindowMs && zoneHold.TryStartHead(elapsed, zoneHold.GetJudgeWorldY()))
                {
                    ScoreManager.RecordTimedHit(holdErrSec, railIndex, zoneHold);
                    activeHoldByRail[railIndex] = zoneHold;
                    return;
                }
            }

            Note zoneNote = zone.GetClosestNote();
            if (zoneNote != null && !(zoneNote is HoldNote))
            {
                float tapErrSec = elapsed - zoneNote.idealHitElapsed;
                if (Mathf.Abs(tapErrSec * 1000f) <= ScoreManager.GoodWindowMs)
                {
                    ScoreManager.RecordTimedHit(tapErrSec, railIndex, zoneNote);
                    ConsumeNote(zoneNote);
                    return;
                }
            }
        }

        Note hitNote = GetClosestTapOnRailByTiming(railIndex, elapsed);
        if (hitNote != null)
        {
            float errorSec = elapsed - hitNote.idealHitElapsed;
            if (Mathf.Abs(errorSec * 1000f) <= ScoreManager.GoodWindowMs)
            {
                ScoreManager.RecordTimedHit(errorSec, railIndex, hitNote);
                ConsumeNote(hitNote);
                return;
            }
        }

        // Destroy the earliest pending note on the rail
        Note earliest = GetEarliestPendingNoteOnRailByTime(railIndex);
        if (earliest != null)
        {
            float leadSec = earliest.idealHitElapsed - elapsed;
            string kind = earliest is HoldNote ? "hold" : "tap";
            bool inAllowedEarlyRange = earlyPressDestroyLeadSeconds <= 0f || leadSec <= earlyPressDestroyLeadSeconds;

            if (leadSec > TailWindowSeconds && inAllowedEarlyRange)
            {
                ScoreManager.RecordMiss($"({kind} early press destroy) rail{railIndex}", earliest);
                ConsumeNote(earliest);
                return;
            }

            if (leadSec < -TailWindowSeconds)
            {
                ScoreManager.RecordMiss($"({kind} late press destroy) rail{railIndex}", earliest);
                ConsumeNote(earliest);
                return;
            }
        }

        ScoreManager.RecordMiss($"(bad press) rail{railIndex}");
    }

    Note GetEarliestPendingNoteOnRailByTime(int railIndex)
    {
        notesPerRail[railIndex].RemoveAll(note => note == null);
        Note earliest = null;
        float bestTime = float.MaxValue;
        foreach (Note note in notesPerRail[railIndex])
        {
            if (note is HoldNote h && h.Phase != HoldNote.HoldPhase.Pending)
            {
                continue;
            }
            if (note.idealHitElapsed < bestTime)
            {
                bestTime = note.idealHitElapsed;
                earliest = note;
            }
        }
        return earliest;
    }

    Note GetEarliestNoteOnRail(int railIndex)
    {
        notesPerRail[railIndex].RemoveAll(note => note == null);

        if (notesPerRail[railIndex].Count == 0)
        {
            return null;
        }

        Note earliest = null;
        float lowestY = float.MaxValue;

        foreach (Note note in notesPerRail[railIndex])
        {
            float judgeY = note.GetJudgeWorldY();
            if (judgeY < lowestY)
            {
                lowestY = judgeY;
                earliest = note;
            }
        }

        return earliest;
    }

    Note GetClosestTapOnRailByTiming(int railIndex, float elapsed)
    {
        notesPerRail[railIndex].RemoveAll(note => note == null);
        if (notesPerRail[railIndex].Count == 0)
        {
            return null;
        }

        float timingWindowSec = ScoreManager.GoodWindowMs / 1000f;
        Note closest = null;
        float closestError = float.MaxValue;

        foreach (Note note in notesPerRail[railIndex])
        {
            if (note is HoldNote)
            {
                continue;
            }

            float err = Mathf.Abs(elapsed - note.idealHitElapsed);
            if (err <= timingWindowSec && err < closestError)
            {
                closestError = err;
                closest = note;
            }
        }

        return closest;
    }

    HoldNote GetNearestPendingHoldOnRail(int railIndex)
    {
        notesPerRail[railIndex].RemoveAll(note => note == null);
        HoldNote nearest = null;
        float bestHead = float.MaxValue;
        foreach (Note note in notesPerRail[railIndex])
        {
            if (note is HoldNote h && h.Phase == HoldNote.HoldPhase.Pending && h.idealHitElapsed < bestHead)
            {
                bestHead = h.idealHitElapsed;
                nearest = h;
            }
        }
        return nearest;
    }

    void LogNearestTimingDebug(int railIndex, float elapsed)
    {
        notesPerRail[railIndex].RemoveAll(note => note == null);
        if (notesPerRail[railIndex].Count == 0)
        {
            Debug.Log($"[InputDebug] rail{railIndex} press t={elapsed:F3}s nearest=none");
            return;
        }

        Note nearest = null;
        float nearestAbsErr = float.MaxValue;
        foreach (Note note in notesPerRail[railIndex])
        {
            if (note is HoldNote h && h.Phase != HoldNote.HoldPhase.Pending)
            {
                continue;
            }

            float absErr = Mathf.Abs(elapsed - note.idealHitElapsed);
            if (absErr < nearestAbsErr)
            {
                nearestAbsErr = absErr;
                nearest = note;
            }
        }

        if (nearest == null)
        {
            Debug.Log($"[InputDebug] rail{railIndex} press t={elapsed:F3}s nearest=none-pending");
            return;
        }

        float signedMs = (elapsed - nearest.idealHitElapsed) * 1000f;
        float goodMs = ScoreManager.GoodWindowMs;
        bool inGood = Mathf.Abs(signedMs) <= goodMs;
        string kind = nearest is HoldNote hn
            ? $"Hold phase={hn.Phase} head={hn.idealHitElapsed:F3}s tail={hn.idealTailElapsed:F3}s"
            : $"Tap ideal={nearest.idealHitElapsed:F3}s";

        Debug.Log(
            $"[InputDebug] rail{railIndex} press t={elapsed:F3}s nearest={kind} err={signedMs:F1}ms inGood={inGood} (Good<= {goodMs:F1}ms)");
    }

    public void RegisterNote(Note note)
    {
        if (note.railIndex >= 0 && note.railIndex < 4)
        {
            notesPerRail[note.railIndex].Add(note);
        }
    }

    public void NotifyHoldDestroyed(HoldNote hold)
    {
        for (int i = 0; i < 4; i++)
        {
            if (activeHoldByRail[i] == hold)
            {
                activeHoldByRail[i] = null;
            }
        }
    }

    void RemoveNote(Note note)
    {
        if (note.railIndex >= 0 && note.railIndex < 4)
        {
            notesPerRail[note.railIndex].Remove(note);
        }
    }

    void ConsumeNote(Note note)
    {
        if (note == null)
        {
            return;
        }
        RemoveNote(note);
        note.gameObject.SetActive(false);
        Destroy(note.gameObject);
    }

    void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
