using UnityEngine;

public class Note : MonoBehaviour
{
    public float speed = 5f;
    public int railIndex; 
    
    void Update()
    {
        // Move the note down
        transform.Translate(Vector3.down * speed * Time.deltaTime);
        
        // Destroy once it's off screen
        if (transform.position.y < -6f)
        {
            Destroy(gameObject);
        }
    }
}
