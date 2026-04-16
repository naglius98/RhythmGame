using UnityEngine;


// Long note: head at transform.position, body is a scaled child sprite
// root keeps BoxCollider2D so triggers are not stretched with note length

public class HoldNote : Note
{
    const string BodyChildName = "HoldBodyVisual";

    public float idealTailElapsed;

    float holdVisualWidthX;
    GameManager gameManager;
    Transform bodyVisual;
    float anchoredHeadY;
    bool hasAnchoredHeadY;

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

        Vector3 s0 = transform.localScale;
        holdVisualWidthX = s0.x * 0.25f;

        EnsureBodyVisualFromPrefabSprite();
        transform.localScale = new Vector3(holdVisualWidthX, 1f, 1f);
        ApplyBodyVisualLength(length);
        ConfigureHeadCollider();

        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }
        ApplyRailColor();
    }

    void EnsureBodyVisualFromPrefabSprite()
    {
        if (bodyVisual != null)
        {
            return;
        }

        Transform existing = transform.Find(BodyChildName);
        if (existing != null)
        {
            bodyVisual = existing;
            return;
        }

        SpriteRenderer rootSr = GetComponent<SpriteRenderer>();
        if (rootSr == null)
        {
            return;
        }

        GameObject childGo = new GameObject(BodyChildName);
        bodyVisual = childGo.transform;
        bodyVisual.SetParent(transform, false);
        bodyVisual.localRotation = Quaternion.identity;
        SpriteRenderer childSr = childGo.AddComponent<SpriteRenderer>();
        CopySpriteRendererForBody(rootSr, childSr);
        Destroy(rootSr);
    }

    static void CopySpriteRendererForBody(SpriteRenderer from, SpriteRenderer to)
    {
        to.sprite = from.sprite;
        to.color = from.color;
        to.sharedMaterial = from.sharedMaterial;
        to.sortingLayerID = from.sortingLayerID;
        to.sortingOrder = from.sortingOrder;
        to.flipX = from.flipX;
        to.flipY = from.flipY;
        to.drawMode = from.drawMode;
        to.size = from.size;
        to.maskInteraction = from.maskInteraction;
        to.spriteSortPoint = from.spriteSortPoint;
    }

    void ApplyBodyVisualLength(float lengthWorld)
    {
        if (bodyVisual == null)
        {
            return;
        }

        float len = Mathf.Max(0.0001f, lengthWorld);
        // Keep visual bottom exactly at parent origin regardless sprite pivot
        bodyVisual.localScale = new Vector3(1f, len, 1f);
        bodyVisual.localPosition = new Vector3(0f, GetBodyBottomPivotOffsetY() * len, 0f);
    }

    float GetBodyBottomPivotOffsetY()
    {
        SpriteRenderer sr = bodyVisual.GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null)
        {
            return 0.5f;
        }

        Sprite sprite = sr.sprite;
        float pixelsPerUnit = sprite.pixelsPerUnit;
        if (pixelsPerUnit <= 0.0001f)
        {
            return 0.5f;
        }

        // Local-space offset needed so sprite minY lands at parent origin
        return sprite.pivot.y / pixelsPerUnit;
    }

    void ConfigureHeadCollider()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box == null)
        {
            return;
        }

        float h = 0.55f;

        // Wider than the thin body sprite so head hits register reliably 
        box.size = new Vector2(1.8f, h);
        box.offset = new Vector2(0f, h * 0.5f);
    }

    public bool TryStartHead(float mapElapsedSeconds, float? caughtHeadY = null)
    {
        if (Phase != HoldPhase.Pending)
        {
            return false;
        }
        if (caughtHeadY.HasValue)
        {
            anchoredHeadY = caughtHeadY.Value;
            hasAnchoredHeadY = true;
        }
        Phase = HoldPhase.Holding;
        RefreshAnchoredHoldVisual(mapElapsedSeconds);
        return true;
    }

    void RefreshAnchoredHoldVisual(float mapElapsedSeconds)
    {
        NoteSpawner sp = NoteSpawner.Instance;
        if (sp == null)
        {
            return;
        }

        float remaining = Mathf.Max(0f, idealTailElapsed - mapElapsedSeconds);
        float length = speed * remaining;
        int r = Mathf.Clamp(railIndex, 0, sp.railPositions.Length - 1);
        float x = sp.railPositions[r];
        float y = hasAnchoredHeadY ? anchoredHeadY : sp.hitZoneY;
        transform.position = new Vector3(x, y, transform.position.z);
        transform.localScale = new Vector3(holdVisualWidthX, 1f, 1f);
        ApplyBodyVisualLength(length);
    }

    protected override void Update()
    {
        if (Phase == HoldPhase.Holding)
        {
            if (gameManager == null)
            {
                gameManager = FindObjectOfType<GameManager>();
            }
            if (gameManager != null && gameManager.MapClockRunning)
            {
                RefreshAnchoredHoldVisual(gameManager.GetMapElapsedSeconds());
            }
            return;
        }

        transform.Translate(Vector3.down * speed * Time.deltaTime);
        if (GetJudgeWorldY() < -6f)
        {
            ScoreManager.RecordMiss($"(passed line) rail{railIndex}", this);
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
