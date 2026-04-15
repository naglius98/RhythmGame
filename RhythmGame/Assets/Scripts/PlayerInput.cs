using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerInput : MonoBehaviour
{
    public GameObject[] hitZones; // List of hit zones

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
                    ScoreManager.RecordMiss($"(hold early release) rail{rail}");
                }
                else
                {
                    ScoreManager.RecordTimedHit(tailErr, rail);
                }
                ClearActiveHold(rail, h);
                continue;
            }

            if (elapsed > h.idealTailElapsed + TailWindowSeconds)
            {
                ScoreManager.RecordMiss($"(hold late tail) rail{rail}");
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

        HitZone zone = hitZones[railIndex].GetComponent<HitZone>();
        if (zone == null)
        {
            return;
        }

        float elapsed = GetMapElapsed();

        Note hitNote = zone.GetClosestNote();
        if (hitNote is HoldNote hn && hn.Phase == HoldNote.HoldPhase.Pending)
        {
            if (hn.TryStartHead(elapsed))
            {
                float errorSec = elapsed - hn.idealHitElapsed;
                ScoreManager.RecordTimedHit(errorSec, railIndex);
                activeHoldByRail[railIndex] = hn;
            }
            return;
        }

        if (hitNote != null)
        {
            float errorSec = elapsed - hitNote.idealHitElapsed;
            ScoreManager.RecordTimedHit(errorSec, railIndex);
            RemoveNote(hitNote);
            Destroy(hitNote.gameObject);
        }
        else
        {
            Note earliestNote = GetEarliestNoteOnRail(railIndex);
            if (earliestNote != null)
            {
                ScoreManager.RecordMiss($"(early destroy) rail{railIndex}");
                RemoveNote(earliestNote);
                Destroy(earliestNote.gameObject);
            }
        }
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
            if (note.transform.position.y < lowestY)
            {
                lowestY = note.transform.position.y;
                earliest = note;
            }
        }

        return earliest;
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

    void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
