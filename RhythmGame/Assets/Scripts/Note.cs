using UnityEngine;

public class Note : MonoBehaviour
{
    public float speed = 5f;
    public int railIndex; // Which rail the note is on
    
    void Update()
    {
        // Move the note down
        transform.Translate(Vector3.down * speed * Time.deltaTime);
        
        if (transform.position.y < -6f)
        {
            ScoreManager.RecordMiss();
            Destroy(gameObject);
        }
    }
    
    public void Initialize(int rail)
    {
        railIndex = rail;
        
        // Register after setting the rail index
        PlayerInput playerInput = FindObjectOfType<PlayerInput>();
        if (playerInput != null)
        {
            playerInput.RegisterNote(this);
            Debug.Log($"Note registered on rail {railIndex} at position {transform.position}");
        }
    }
}
