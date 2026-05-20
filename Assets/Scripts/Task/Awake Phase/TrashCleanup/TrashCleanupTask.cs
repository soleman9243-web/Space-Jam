using UnityEngine;

public class TrashCleanupTask : BaseTask
{
    [Header("Trash")]
    [SerializeField] private TrashObject trashPrefab;

    [Header("Spawn Area")]
    [SerializeField] private Vector2 minSpawnPosition;

    [SerializeField] private Vector2 maxSpawnPosition;

    [Header("Spawn Check")]
    [SerializeField] private LayerMask blockedLayer;

    [SerializeField] private float checkRadius = 0.5f;

    [SerializeField] private int maxSpawnAttempts = 50;

    [Header("Player")]
    [SerializeField] private Transform player;

    [SerializeField] private float minDistanceFromPlayer = 2f;

    [Header("Quest")]
    [SerializeField] private int targetTrash = 5;

    [Header("Stability Reward")]
    [SerializeField] private float stabilityPerTrash = 2f;

    [SerializeField] private float stabilityCompleteBonus = 15f;

    private int cleanedTrash;

    private TrashObject currentTrash;

    public override void ActivateTask()
    {
        base.ActivateTask();

        cleanedTrash = 0;

        UpdateTaskText();

        SpawnTrash();
    }

    public override void ResetTask()
    {
        base.ResetTask();

        cleanedTrash = 0;

        if (currentTrash != null)
        {
            Destroy(currentTrash.gameObject);
        }
    }

    public void CleanTrash(TrashObject trash)
    {
        if (!IsActive)
        {
            return;
        }

        if (trash != currentTrash)
        {
            return;
        }

        cleanedTrash++;

        Destroy(trash.gameObject);

        // reward kecil tiap sampah
        if (PlayerStatus.Instance != null)
        {
            PlayerStatus.Instance.IncreaseStability(
                stabilityPerTrash
            );
        }

        UpdateTaskText();

        if (cleanedTrash >= targetTrash)
        {
            // bonus besar saat quest selesai
            if (PlayerStatus.Instance != null)
            {
                PlayerStatus.Instance.IncreaseStability(
                    stabilityCompleteBonus
                );
            }

            CompleteTask();

            return;
        }

        SpawnTrash();
    }

    private void SpawnTrash()
    {
        Vector2 spawnPosition;

        bool found =
            TryGetSpawnPosition(out spawnPosition);

        if (!found)
        {
            Debug.LogWarning("Gagal menemukan posisi sampah");

            return;
        }

        currentTrash =
            Instantiate(
                trashPrefab,
                spawnPosition,
                Quaternion.identity
            );

        currentTrash.Setup(this);
    }

    private bool TryGetSpawnPosition(out Vector2 position)
    {
        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            Vector2 randomPosition =
                new Vector2(
                    Random.Range(
                        minSpawnPosition.x,
                        maxSpawnPosition.x
                    ),
                    Random.Range(
                        minSpawnPosition.y,
                        maxSpawnPosition.y
                    )
                );

            bool blocked =
                Physics2D.OverlapCircle(
                    randomPosition,
                    checkRadius,
                    blockedLayer
                );

            if (blocked)
            {
                continue;
            }

            float distance =
                Vector2.Distance(
                    randomPosition,
                    player.position
                );

            if (distance < minDistanceFromPlayer)
            {
                continue;
            }

            position = randomPosition;

            return true;
        }

        position = Vector2.zero;

        return false;
    }

    private void UpdateTaskText()
    {
        taskText =
            "Clean Trash (" +
            cleanedTrash +
            "/" +
            targetTrash +
            ")";

        PhaseTaskManager taskManager =
            FindObjectOfType<PhaseTaskManager>();

        if (taskManager != null)
        {
            taskManager.UpdateObjectiveUI();
        }
    }

    private void OnDrawGizmosSelected()
    {
        // area spawn
        Gizmos.color = Color.green;

        Vector2 center =
            (minSpawnPosition + maxSpawnPosition) / 2f;

        Vector2 size =
            maxSpawnPosition - minSpawnPosition;

        Gizmos.DrawWireCube(center, size);

        // radius check preview
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(minSpawnPosition, checkRadius);
        Gizmos.DrawWireSphere(maxSpawnPosition, checkRadius);

        // player distance preview
        if (player != null)
        {
            Gizmos.color = Color.red;

            Gizmos.DrawWireSphere(
                player.position,
                minDistanceFromPlayer
            );
        }
    }
}