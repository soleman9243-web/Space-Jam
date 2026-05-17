using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MemoryFragment : MonoBehaviour
{
    [Header("Settings")]
    public int memoryValue = 1;
    public float stabilityIncrease = 15f;
    
    [Header("Audio")]
    public AudioClip collectionSound;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if the overlapping collider belongs to the Player
        if (collision.CompareTag("Player"))
        {
            // Report to GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddMemory(memoryValue, stabilityIncrease);
            }

            // Play sound at this position (useful because the object will be destroyed)
            if (collectionSound != null)
            {
                AudioSource.PlayClipAtPoint(collectionSound, transform.position);
            }

            // Remove fragment from the scene
            Destroy(gameObject);
        }
    }
}
