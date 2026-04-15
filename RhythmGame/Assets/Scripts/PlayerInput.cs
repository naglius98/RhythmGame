using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerInput : MonoBehaviour
{
    public GameObject[] hitZones; // List of hit zones

    GameManager gameManager;

    // Track all active notes per rail
    private List<Note>[] notesPerRail = new List<Note>[4];

    void Awake()
    {
        gameManager = FindObjectOfType<GameManager>();
    }

    void Start()
    {
        for (int i = 0; i < 4; i++)
            notesPerRail[i] = new List<Note>();
    }

    void Update()
    {
        if (PauseMenuBehaviour.IsPaused)
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

        // Quit 
        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            QuitGame();
        }
    }

    void CheckHit(int railIndex)
    {
        // Pulse effect for visual feedback
        if (hitZones[railIndex] != null)
        {
            HitZonePulse pulse = hitZones[railIndex].GetComponent<HitZonePulse>();
            if (pulse != null)
            {
                pulse.TriggerPulse();
            }
        }

        // Check for notes in the zone
        HitZone zone = hitZones[railIndex].GetComponent<HitZone>();
        if (zone != null)
        {
            Note hitNote = zone.GetClosestNote();
            
            if (hitNote != null)
            {
                float elapsed = gameManager != null && gameManager.MapClockRunning
                    ? gameManager.GetMapElapsedSeconds()
                    : Time.time;
                float errorSec = elapsed - hitNote.idealHitElapsed;
                ScoreManager.RecordTimedHit(errorSec, railIndex);
                RemoveNote(hitNote);
                Destroy(hitNote.gameObject);
            }
            else // No note in hit zone
            { 
                Note earliestNote = GetEarliestNoteOnRail(railIndex);
                if (earliestNote != null)
                {
                    ScoreManager.RecordMiss($"(early destroy) rail{railIndex}");
                    RemoveNote(earliestNote);
                    Destroy(earliestNote.gameObject);
                }
                else // No note in rail at all
                {
                    // Do nothing
                }
            }
        }
    }
    
    Note GetEarliestNoteOnRail(int railIndex)
    {
        // Clean up null references
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
    
    // Add the note to the list
    public void RegisterNote(Note note)
    {
        if (note.railIndex >= 0 && note.railIndex < 4)
        {
            notesPerRail[note.railIndex].Add(note);
        }
    }
    
    // Destroy the note and remove it from the list
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

