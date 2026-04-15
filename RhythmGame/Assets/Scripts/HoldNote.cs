using UnityEngine;


// Long note: head timing uses idealHeadElapsed; tail at idealTailElapsed   
// Sprite pivot at bottom so transform.position.y is the head; scale Y is body length

public class HoldNote : Note
{
    public float idealTailElapsed;

    public enum HoldPhase
    {
        Pending,
        Holding
    }

    public HoldPhase Phase { get; private set; } = HoldPhase.Pending;

    public void InitializeHold(int rail, float idealHeadElapsedSeconds, float idealTailElapsedSeconds)
    {
        idealTailElapsed = idealTailElapsedSeconds;
        Initialize(rail, idealHeadElapsedSeconds);
        float duration = Mathf.Max(0.0001f, idealTailElapsedSeconds - idealHeadElapsedSeconds);
        float length = speed * duration;
        Vector3 s = transform.localScale;
        s.y = length;
        transform.localScale = s;
        // Same rail tint as tap notes
        ApplyRailColor();
    }

    //Start the hold if the head is within the good timing window
    public bool TryStartHead(float mapElapsedSeconds)
    {
        if (Phase != HoldPhase.Pending)
        {
            return false;
        }
        float errSec = mapElapsedSeconds - idealHitElapsed;
        if (Mathf.Abs(errSec * 1000f) > ScoreManager.GoodWindowMs)
        {
            return false;
        }
        Phase = HoldPhase.Holding;
        return true;
    }

    protected override void Update()
    {
        transform.Translate(Vector3.down * speed * Time.deltaTime);
        if (transform.position.y < -6f)
        {
            if (Phase == HoldPhase.Pending)
            {
                ScoreManager.RecordMiss($"(passed line) rail{railIndex}");
            }
            else
            {
                ScoreManager.RecordMiss($"(hold tail) rail{railIndex}");
            }
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        PlayerInput input = FindObjectOfType<PlayerInput>();
        if (input != null)
        {
            input.NotifyHoldDestroyed(this);
        }
    }
}
