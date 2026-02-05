using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    public GameObject[] hitZones; // List of hit zones

    void Update()
    {
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
    }

    void CheckHit(int railIndex)
    {
        Debug.Log($"Rail {railIndex} pressed!");

        // Pulse effect for visual feedback
        if (hitZones[railIndex] != null)
        {
            HitZonePulse pulse = hitZones[railIndex].GetComponent<HitZonePulse>();
            if (pulse != null)
            {
                pulse.TriggerPulse();
            }
        }
    }
}

