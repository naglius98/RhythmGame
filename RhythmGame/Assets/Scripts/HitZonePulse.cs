using UnityEngine;

public class HitZonePulse : MonoBehaviour
{
    public float pulseDuration = 0.2f; // Pulse length
    public float pulseScale = 1.5f; // Pulse size
    
    private Vector3 originalScale;
    private float pulseTimer = 0f;
    private bool isPulsing = false;

    void Start()
    {
        originalScale = transform.localScale;
    }

    void Update()
    {
        if (isPulsing)
        {
            pulseTimer += Time.deltaTime;
            
            // Calculate scale 
            float progress = pulseTimer / pulseDuration;
            float scale = Mathf.Lerp(pulseScale, 1f, progress);
            
            transform.localScale = originalScale * scale;
            
            // End pulse when done
            if (pulseTimer >= pulseDuration)
            {
                isPulsing = false;
                transform.localScale = originalScale;
            }
        }
    }

    public void TriggerPulse()
    {
        pulseTimer = 0f;
        isPulsing = true;
    }
}
