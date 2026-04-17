using UnityEngine;


// Long note: head at transform.position, body is a scaled child sprite
// root keeps BoxCollider2D so triggers are not stretched with note length

public class HoldNote : Note
{
    const string BodyChildName = "HoldBodyVisual";
    const string GlowChildName = "HoldGlowVisual";

    public float idealTailElapsed;
    [Header("Hold Glow")]
    [Range(0f, 1f)] public float glowBaseAlpha = 0.35f;
    [Range(0f, 1f)] public float glowPulseAmount = 0.15f;
    [Min(0f)] public float glowPulseSpeed = 7f;
    [Min(1f)] public float glowWidthMultiplier = 1.2f;
    [Min(1f)] public float glowLengthMultiplier = 1.05f;
    [Range(0f, 1f)] public float glowTintToWhite = 0.9f;
    [Min(0f)] public float glowAlphaMultiplier = 2f;

    float holdVisualWidthX;
    GameManager gameManager;
    Transform bodyVisual;
    Transform glowVisual;
    SpriteRenderer glowRenderer;
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
        EnsureGlowVisual();
        transform.localScale = new Vector3(holdVisualWidthX, 1f, 1f);
        ApplyBodyVisualLength(length);
        ApplyGlowVisualLength(length);
        SetGlowAlpha(0f);
        ConfigureHeadCollider();

        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }
        ApplyRailColor();
        RefreshGlowTint();
    }

    void EnsureGlowVisual()
    {
        if (glowVisual != null && glowRenderer != null)
        {
            return;
        }

        Transform existing = transform.Find(GlowChildName);
        if (existing != null)
        {
            glowVisual = existing;
            glowRenderer = existing.GetComponent<SpriteRenderer>();
            return;
        }

        if (bodyVisual == null)
        {
            return;
        }

        SpriteRenderer bodySr = bodyVisual.GetComponent<SpriteRenderer>();
        if (bodySr == null)
        {
            return;
        }

        GameObject glowGo = new GameObject(GlowChildName);
        glowVisual = glowGo.transform;
        glowVisual.SetParent(transform, false);
        glowVisual.localRotation = Quaternion.identity;
        glowRenderer = glowGo.AddComponent<SpriteRenderer>();
        CopySpriteRendererForBody(bodySr, glowRenderer);
        glowRenderer.sortingOrder = bodySr.sortingOrder + 2;
        RefreshGlowTint();
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

    void ApplyGlowVisualLength(float lengthWorld)
    {
        if (glowVisual == null)
        {
            return;
        }

        float len = Mathf.Max(0.0001f, lengthWorld) * glowLengthMultiplier;
        glowVisual.localScale = new Vector3(glowWidthMultiplier, len, 1f);
        glowVisual.localPosition = new Vector3(0f, GetBodyBottomPivotOffsetY() * len, 0f);
    }

    void SetGlowAlpha(float alpha)
    {
        if (glowRenderer == null)
        {
            return;
        }
        Color c = glowRenderer.color;
        c.a = Mathf.Clamp01(alpha * glowAlphaMultiplier);
        glowRenderer.color = c;
    }

    void RefreshGlowTint()
    {
        if (glowRenderer == null)
        {
            return;
        }

        Color baseColor = Color.white;
        if (bodyVisual != null)
        {
            SpriteRenderer bodySr = bodyVisual.GetComponent<SpriteRenderer>();
            if (bodySr != null)
            {
                baseColor = bodySr.color;
            }
        }
        Color tinted = Color.Lerp(baseColor, Color.white, glowTintToWhite);
        tinted.a = glowRenderer.color.a;
        glowRenderer.color = tinted;
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
        ApplyGlowVisualLength(length);
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
            float pulse = glowBaseAlpha + Mathf.Sin(Time.time * glowPulseSpeed) * glowPulseAmount;
            SetGlowAlpha(pulse);
            return;
        }

        SetGlowAlpha(0f);

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
