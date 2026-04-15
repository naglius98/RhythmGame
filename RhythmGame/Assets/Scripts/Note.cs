using UnityEngine;

public class Note : MonoBehaviour
{
    public float speed = 5f;
    public int railIndex;

    // Timeline seconds when this note should be hit
    public float idealHitElapsed;

    void Update()
    {
        transform.Translate(Vector3.down * speed * Time.deltaTime);

        if (transform.position.y < -6f)
        {
            ScoreManager.RecordMiss($"(passed line) rail{railIndex}");
            Destroy(gameObject);
        }
    }

    public void Initialize(int rail, float idealHitElapsedSeconds)
    {
        railIndex = rail;
        idealHitElapsed = idealHitElapsedSeconds;

        PlayerInput playerInput = FindObjectOfType<PlayerInput>();
        if (playerInput != null)
        {
            playerInput.RegisterNote(this);
        }
    }
}
