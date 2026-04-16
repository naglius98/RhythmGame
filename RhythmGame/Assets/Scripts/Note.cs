using UnityEngine;
using UnityEngine.UI;

public class Note : MonoBehaviour
{
    public float speed = 5f;
    public int railIndex;

    // Timeline seconds when this note should be hit
    public float idealHitElapsed;

    // World Y of the leading edge when the note falls downward 
    public float GetJudgeWorldY()
    {
        Collider2D c = GetComponent<Collider2D>();
        return c != null ? c.bounds.min.y : transform.position.y;
    }

    protected virtual void Update()
    {
        transform.Translate(Vector3.down * speed * Time.deltaTime);

        if (transform.position.y < -6f)
        {
            ScoreManager.RecordMiss($"(passed line) rail{railIndex}", this);
            Destroy(gameObject);
        }
    }

    public void Initialize(int rail, float idealHitElapsedSeconds)
    {
        railIndex = rail;
        idealHitElapsed = idealHitElapsedSeconds;
        ApplyRailColor();

        PlayerInput playerInput = FindObjectOfType<PlayerInput>();
        if (playerInput != null)
        {
            playerInput.RegisterNote(this);
        }
    }

    protected void ApplyRailColor()
    {
        NoteSpawner spawner = NoteSpawner.Instance;
        if (spawner == null)
        {
            return;
        }
        Color c = spawner.GetRailNoteColor(railIndex);
        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>(true))
        {
            sr.color = c;
        }
        foreach (Graphic g in GetComponentsInChildren<Graphic>(true))
        {
            g.color = c;
        }
    }
}
