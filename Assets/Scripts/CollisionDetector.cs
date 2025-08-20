using UnityEngine;

public class CollisionDetector : MonoBehaviour
{
    void OnCollisionEnter2D(Collision2D collision)
    {
 //       Debug.Log("Collision with: " + collision.gameObject.name + " | Tag: " + collision.gameObject.tag);
        if (collision.gameObject.CompareTag("Wall"))
        {
            
            Destroy(transform.parent.gameObject); // Destroy the bullet
        }
    }
}