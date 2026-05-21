using System.Collections;
using UnityEngine;

public class ShadowHallucinationSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;

    [SerializeField]
    private GameObject shadowEnemyPrefab;

    [Header("Spawn Area")]
    [SerializeField]
    private float spawnOffset = 12f;

    [Header("Timing")]
    [SerializeField]
    private float minSpawnDelay = 2f;

    [SerializeField]
    private float maxSpawnDelay = 5f;

    [Header("Condition")]
    [Range(0f, 1f)]
    [SerializeField]
    private float stabilityThresholdPercent = 0.7f;

    [Header("Movement")]
    [SerializeField]
    private float moveSpeed = 15f;

    public enum SpawnDirection
    {
        UpToDown,
        DownToUp,
        LeftToRight,
        RightToLeft
    }

    private void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            float waitTime =
                Random.Range(
                    minSpawnDelay,
                    maxSpawnDelay
                );

            yield return new WaitForSeconds(
                waitTime
            );

            if (!CanSpawn())
            {
                continue;
            }

            SpawnShadow();
        }
    }

    private bool CanSpawn()
    {
        if (PlayerStatus.Instance == null)
        {
            return false;
        }

        if (PlayerStatus.Instance.isDead)
        {
            return false;
        }

        float currentPercent =
            PlayerStatus.Instance.stability /
            PlayerStatus.Instance.maxStability;

        return currentPercent <=
               stabilityThresholdPercent;
    }

    private void SpawnShadow()
    {
        SpawnDirection dir =
            (SpawnDirection)
            Random.Range(0, 4);

        Vector2 spawnPos = Vector2.zero;
        Vector2 moveDir = Vector2.zero;

        Vector3 playerPos = player.position;

        switch (dir)
        {
            case SpawnDirection.UpToDown:

                spawnPos =
                    new Vector2(
                        playerPos.x,
                        spawnOffset
                    );

                moveDir = Vector2.down;

                break;

            case SpawnDirection.DownToUp:

                spawnPos =
                    new Vector2(
                        playerPos.x,
                        -spawnOffset
                    );

                moveDir = Vector2.up;

                break;

            case SpawnDirection.LeftToRight:

                spawnPos =
                    new Vector2(
                        -spawnOffset,
                        playerPos.y
                    );

                moveDir = Vector2.right;

                break;

            case SpawnDirection.RightToLeft:

                spawnPos =
                    new Vector2(
                        spawnOffset,
                        playerPos.y
                    );

                moveDir = Vector2.left;

                break;
        }

        GameObject shadow =
            Instantiate(
                shadowEnemyPrefab,
                spawnPos,
                Quaternion.identity
            );

        Rigidbody2D rb =
            shadow.GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.velocity =
                moveDir * moveSpeed;
        }

        Destroy(shadow, 5f);
    }
}