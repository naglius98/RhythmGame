using UnityEngine;
using System.Collections.Generic;

public class HitZone : MonoBehaviour
{
    public int railIndex; // Which rail this hit zone is for
    public float hitTolerance = 0.5f; // How close the note needs to be 
    
    private List<Note> notesInZone = new List<Note>();

    void OnTriggerEnter2D(Collider2D other)
    {
        Note note = other.GetComponent<Note>();
        if (note != null && note.railIndex == railIndex)
        {
            notesInZone.Add(note);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        Note note = other.GetComponent<Note>();
        if (note != null)
        {
            notesInZone.Remove(note);
        }
    }

    public Note GetClosestNote()
    {
        if (notesInZone.Count == 0)
        {
            return null;
        }
        
        // Return the note closest to the center of the hit zone
        Note closest = null;
        float closestDistance = float.MaxValue;
        
        foreach (Note note in notesInZone)
        {
            if (note == null)
            {
                continue;
            }
            
            float distance = Mathf.Abs(note.transform.position.y - transform.position.y);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = note;
            }
        }
        
        return closest;
    }
    
    // Clean up null references
    void Update()
    {
        notesInZone.RemoveAll(note => note == null);
    }
}
