// ==============================
// TrashCleanupTask
// ==============================

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

    [Header("Quest Scaling")]
    [SerializeField] private int baseTargetTrash = 5;
    [SerializeField] private int trashPerLoop = 2;
    [SerializeField] private int maxTargetTrash = 20;

    [Header("Stability Reward")]
    [SerializeField] private float stabilityPerTrash = 2f;
    [SerializeField] private float stabilityCompleteBonus = 15f;

    private int targetTrash;
    private int cleanedTrash;
    private TrashObject currentTrash;

    private bool initialized;

    private int GetCurrentLoop()
    {
        PhaseLoopManager pm = FindObjectOfType<PhaseLoopManager>();

        return pm != null ? pm.currentLoop : 1;
    }

    public override void ActivateTask()
    {
        base.ActivateTask();

        if (initialized)
        {
            return;
        }

        initialized = true;

        int loop = GetCurrentLoop();

        targetTrash = Mathf.Min(
            baseTargetTrash + (loop - 1) * trashPerLoop,
            maxTargetTrash
        );

        cleanedTrash = 0;

        UpdateTaskText();

        SpawnTrash();

        Debug.Log($"[TrashTask] Loop {loop} -> Target: {targetTrash}");
    }

    public override void ResetTask()
    {
        base.ResetTask();

        initialized = false;

        cleanedTrash = 0;

        if (currentTrash != null)
        {
            Destroy(currentTrash.gameObject);

            currentTrash = null;
        }
    }

    public override void ForceStopTask()
    {
        if (currentTrash != null)
        {
            Destroy(currentTrash.gameObject);

            currentTrash = null;
        }

        initialized = false;

        DeactivateTask();
    }

    public void CleanTrash(TrashObject trash)
    {
        if (!IsActive || trash != currentTrash)
        {
            return;
        }

        cleanedTrash++;

        Destroy(trash.gameObject);

        currentTrash = null;

        if (PlayerStatus.Instance != null)
        {
            PlayerStatus.Instance.IncreaseStability(stabilityPerTrash);
        }

        UpdateTaskText();

        if (cleanedTrash >= targetTrash)
        {
            if (PlayerStatus.Instance != null)
            {
                PlayerStatus.Instance.IncreaseStability(stabilityCompleteBonus);
            }

            CompleteTask();

            return;
        }

        SpawnTrash();
    }

    private void SpawnTrash()
    {
        if (currentTrash != null)
        {
            Destroy(currentTrash.gameObject);

            currentTrash = null;
        }

        if (!TryGetSpawnPosition(out Vector2 spawnPos))
        {
            Debug.LogWarning("Gagal spawn trash");

            return;
        }

        currentTrash =
            Instantiate(trashPrefab, spawnPos, Quaternion.identity);

        currentTrash.Setup(this);
    }

    private bool TryGetSpawnPosition(out Vector2 position)
    {
        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            Vector2 rand = new Vector2(
                Random.Range(minSpawnPosition.x, maxSpawnPosition.x),
                Random.Range(minSpawnPosition.y, maxSpawnPosition.y)
            );

            if (Physics2D.OverlapCircle(rand, checkRadius, blockedLayer))
            {
                continue;
            }

            if (Vector2.Distance(rand, player.position) < minDistanceFromPlayer)
            {
                continue;
            }

            position = rand;

            return true;
        }

        position = Vector2.zero;

        return false;
    }

    private void UpdateTaskText()
    {
        taskText =
            $"Clean Trash ({cleanedTrash:0}/{targetTrash:0})";

        PhaseTaskManager tm =
            FindObjectOfType<PhaseTaskManager>();

        if (tm != null)
        {
            tm.UpdateObjectiveUI();
        }
    }
}