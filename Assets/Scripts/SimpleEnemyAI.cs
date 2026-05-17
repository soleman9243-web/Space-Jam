using UnityEngine;

public enum EnemyMovementType
{
    Patrol,
    Chase,
    Teleport
}

[RequireComponent(typeof(Rigidbody2D))]
public class SimpleEnemyAI : MonoBehaviour
{
    [Header("AI Settings")]
    public EnemyMovementType movementType;
    public float moveSpeed = 3f;

    [Header("Patrol Settings")]
    [Tooltip("Drop Transforms here for the AI to move between.")]
    public Transform[] patrolWaypoints;
    private int currentWaypointIndex = 0;

    [Header("Chase Settings")]
    public string playerTag = "Player";
    public float detectionRadius = 5f;
    private Transform playerTransform;

    [Header("Teleport Settings")]
    public float teleportInterval = 3f;
    public float teleportRadius = 4f;
    private float teleportTimer = 0f;

    private Rigidbody2D rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Ensure the Rigidbody is set up nicely for a top down 2D game
        rb.gravityScale = 0f;
        rb.freezeRotation = true; 

        // Find the player automatically based on tag
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
    }

    private void FixedUpdate()
    {
        // Use FixedUpdate for physics-based movement
        switch (movementType)
        {
            case EnemyMovementType.Patrol:
                HandlePatrol();
                break;
            case EnemyMovementType.Chase:
                HandleChase();
                break;
            case EnemyMovementType.Teleport:
                HandleTeleport();
                break;
        }
    }

    private void HandlePatrol()
    {
        if (patrolWaypoints == null || patrolWaypoints.Length == 0) return;

        Transform targetWaypoint = patrolWaypoints[currentWaypointIndex];
        Vector2 newPosition = Vector2.MoveTowards(rb.position, targetWaypoint.position, moveSpeed * Time.fixedDeltaTime);
        rb.MovePosition(newPosition);

        // Check if close enough to waypoint to swap to the next one
        if (Vector2.Distance(rb.position, targetWaypoint.position) < 0.1f)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % patrolWaypoints.Length;
        }
    }

    private void HandleChase()
    {
        if (playerTransform == null) return;

        float distanceToPlayer = Vector2.Distance(rb.position, playerTransform.position);
        
        if (distanceToPlayer <= detectionRadius)
        {
            Vector2 newPosition = Vector2.MoveTowards(rb.position, playerTransform.position, moveSpeed * Time.fixedDeltaTime);
            rb.MovePosition(newPosition);
        }
    }

    private void HandleTeleport()
    {
        teleportTimer += Time.fixedDeltaTime;

        if (teleportTimer >= teleportInterval)
        {
            teleportTimer = 0f;
            TeleportToRandomPosition();
        }
    }

    private void TeleportToRandomPosition()
    {
        // Get a random position within a circle
        Vector2 randomDirection = Random.insideUnitCircle * teleportRadius;
        Vector2 newPosition = rb.position + randomDirection;
        
        // Simply move to the new position. 
        // Note: In a robust game, you'd want to use Physics2D.OverlapPoint or similar 
        // to make sure it doesn't teleport inside a wall collider.
        rb.position = newPosition;
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize the radiuses in the editor
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, teleportRadius);
    }
}
