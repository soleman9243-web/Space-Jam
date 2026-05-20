using UnityEngine;

namespace SpaceJam.Minigames
{
    [RequireComponent(typeof(Collider2D))]
    public class CrossyRoadObstacle : MonoBehaviour
    {
        private Vector3 direction;
        private float speed;
        private Vector3 targetPos;

        public void Setup(Vector3 start, Vector3 end, float obstacleSpeed)
        {
            transform.position = start;
            targetPos = end;
            speed = obstacleSpeed;
            direction = (end - start).normalized;

            // Auto-flip sprite based on horizontal direction
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                // If moving right (direction.x > 0), flip the sprite. Otherwise, don't.
                sr.flipX = direction.x > 0f;
            }
        }

        private void Update()
        {
            // Move horizontal
            transform.position += direction * speed * Time.deltaTime;

            // Destroy if close to target position
            if (Vector3.Distance(transform.position, targetPos) < 0.2f)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                Debug.Log("[CrossyRoadObstacle] Player was hit by an enemy!");
                
                PlayerStatus playerStatus = collision.GetComponent<PlayerStatus>();
                if (playerStatus != null)
                {
                    playerStatus.Die();
                }
            }
        }
    }
}
