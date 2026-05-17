using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyDashSpawner : MonoBehaviour
{
    public enum DashDirection
    {
        UpToDown,
        DownToUp,
        LeftToRight,
        RightToLeft
    }

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private GameObject warningTrackPrefab;
    [SerializeField] private GameObject dashEnemyPrefab;

    [Header("Runtime Settings (controlled by Difficulty)")]
    [SerializeField] private int trackCount = 1;
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float spawnCooldown = 2f;
    [SerializeField] private float followDuration = 2f;
    [SerializeField] private float lockDuration = 0.5f;

    [Header("Pattern")]
    [SerializeField] private float spacing = 2f;
    [SerializeField] private float spawnOffset = 12f;

    private List<GameObject> currentTracks = new List<GameObject>();
    private DashDirection currentDirection;

    private void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            if (!canSpawn)
            {
                yield return null;
                continue;
            }

            yield return SpawnAttack();
            yield return new WaitForSeconds(spawnCooldown);
        }
    }

    private IEnumerator SpawnAttack()
    {
        currentDirection = (DashDirection)Random.Range(0, 4);

        currentTracks.Clear();

        for (int i = 0; i < trackCount; i++)
        {
            GameObject track = Instantiate(warningTrackPrefab);
            SetupTrackRotation(track);
            currentTracks.Add(track);
        }

        float timer = 0f;

        while (timer < followDuration)
        {
            FollowPlayer();
            timer += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(lockDuration);

        SpawnEnemies();

        FindObjectOfType<EnemyWaveController>()?.RegisterWaveComplete();

        foreach (var t in currentTracks)
        {
            Destroy(t);
        }
    }

    private void FollowPlayer()
    {
        Vector3 p = player.position;

        for (int i = 0; i < currentTracks.Count; i++)
        {
            float offset = (i - (trackCount - 1) / 2f) * spacing;

            switch (currentDirection)
            {
                case DashDirection.UpToDown:
                case DashDirection.DownToUp:

                    currentTracks[i].transform.position = new Vector3(p.x + offset, 0f, 0f);
                    break;

                case DashDirection.LeftToRight:
                case DashDirection.RightToLeft:

                    currentTracks[i].transform.position = new Vector3(0f, p.y + offset, 0f);
                    break;
            }
        }
    }

    private void SetupTrackRotation(GameObject track)
    {
        if (currentDirection == DashDirection.UpToDown ||
            currentDirection == DashDirection.DownToUp)
        {
            track.transform.rotation = Quaternion.Euler(0, 0, 90);
        }
        else
        {
            track.transform.rotation = Quaternion.identity;
        }
    }

    private void SpawnEnemies()
    {
        foreach (var track in currentTracks)
        {
            Vector2 spawnPos = Vector2.zero;
            Vector2 dir = Vector2.zero;

            switch (currentDirection)
            {
                case DashDirection.UpToDown:
                    spawnPos = new Vector2(track.transform.position.x, spawnOffset);
                    dir = Vector2.down;
                    break;

                case DashDirection.DownToUp:
                    spawnPos = new Vector2(track.transform.position.x, -spawnOffset);
                    dir = Vector2.up;
                    break;

                case DashDirection.LeftToRight:
                    spawnPos = new Vector2(-spawnOffset, track.transform.position.y);
                    dir = Vector2.right;
                    break;

                case DashDirection.RightToLeft:
                    spawnPos = new Vector2(spawnOffset, track.transform.position.y);
                    dir = Vector2.left;
                    break;
            }

            GameObject enemy = Instantiate(dashEnemyPrefab, spawnPos, Quaternion.identity);

            enemy.GetComponent<DashEnemy>().Initialize(dir, dashSpeed);
        }
    }
    private bool canSpawn = false;

    public void StartSpawning()
    {
        canSpawn = true;
    }

    public void StopSpawning()
    {
        canSpawn = false;
    }

    // ===== Difficulty API =====
    public void SetTrackCount(int v) => trackCount = v;
    public void SetDashSpeed(float v) => dashSpeed = v;
    public void SetSpawnCooldown(float v) => spawnCooldown = v;
    public void SetFollowDuration(float v) => followDuration = v;
}