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
    [SerializeField] private EnemyDifficultyController difficultyController;

    [Header("Settings")]
    [SerializeField] private int trackCount = 5;
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float spawnCooldown = 2f;
    [SerializeField] private float followDuration = 2f;
    [SerializeField] private float lockDuration = 0.5f;

    [Header("Pattern")]
    [SerializeField] private float spacing = 3.5f;
    [SerializeField] private float spawnOffset = 12f;

    private readonly List<GameObject> currentTracks = new();

    private bool canSpawn;
    private bool infiniteMode;
    private float currentOffset = 0f;

    private bool isPatternActive;

    private void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            if (!canSpawn && !infiniteMode)
            {
                yield return null;
                continue;
            }

            yield return SpawnAttackCycle();
            yield return new WaitForSeconds(spawnCooldown);
        }
    }

    private IEnumerator SpawnAttackCycle()
    {
        if (isPatternActive)
        {
            yield break;
        }

        isPatternActive = true;

        int patternCount = difficultyController != null
            ? difficultyController.GetPatternCount()
            : 1;

        for (int i = 0; i < patternCount; i++)
        {
            DashDirection dir = (DashDirection)Random.Range(0, 4);
            currentOffset = Random.Range(-5f, 5f);
            yield return SpawnSinglePattern(dir);
            yield return new WaitForSeconds(0.1f);
        }

        isPatternActive = false;
    }

    private IEnumerator SpawnSinglePattern(DashDirection dir)
    {
        ClearTracks();

        for (int i = 0; i < trackCount; i++)
        {
            GameObject track = Instantiate(warningTrackPrefab);
            SetupTrackRotation(track, dir);
            currentTracks.Add(track);
        }

        float timer = 0f;

        while (timer < followDuration)
        {
            FollowPlayer(dir);
            timer += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(lockDuration);

        SpawnEnemies(dir);
        ClearTracks();
    }

    private void FollowPlayer(DashDirection dir)
    {
        Vector3 p = player.position;

        for (int i = 0; i < currentTracks.Count; i++)
        {
            float offset = (i - (trackCount - 1) / 2f) * spacing;

            if (dir == DashDirection.UpToDown ||
                dir == DashDirection.DownToUp)
            {
                currentTracks[i].transform.position =
                    new Vector3(p.x + offset + currentOffset, 0f, 0f);
            }
            else
            {
                currentTracks[i].transform.position =
                    new Vector3(0f, p.y + offset + currentOffset, 0f);
            }
        }
    }

    private void SetupTrackRotation(GameObject track, DashDirection dir)
    {
        track.transform.rotation =
            (dir == DashDirection.UpToDown ||
             dir == DashDirection.DownToUp)
            ? Quaternion.Euler(0, 0, 90)
            : Quaternion.identity;
    }

    private void SpawnEnemies(DashDirection dir)
    {
        foreach (var track in currentTracks)
        {
            if (track == null)
            {
                continue;
            }

            Vector2 spawnPos = Vector2.zero;
            Vector2 spawnDir = Vector2.zero;

            switch (dir)
            {
                case DashDirection.UpToDown:
                    spawnPos = new Vector2(track.transform.position.x, spawnOffset);
                    spawnDir = Vector2.down;
                    break;

                case DashDirection.DownToUp:
                    spawnPos = new Vector2(track.transform.position.x, -spawnOffset);
                    spawnDir = Vector2.up;
                    break;

                case DashDirection.LeftToRight:
                    spawnPos = new Vector2(-spawnOffset, track.transform.position.y);
                    spawnDir = Vector2.right;
                    break;

                case DashDirection.RightToLeft:
                    spawnPos = new Vector2(spawnOffset, track.transform.position.y);
                    spawnDir = Vector2.left;
                    break;
            }

            GameObject enemy = Instantiate(dashEnemyPrefab, spawnPos, Quaternion.identity);
            enemy.GetComponent<DashEnemy>().Initialize(spawnDir, dashSpeed);
        }
    }

    private void ClearTracks()
    {
        for (int i = 0; i < currentTracks.Count; i++)
        {
            if (currentTracks[i] != null)
            {
                Destroy(currentTracks[i]);
            }
        }

        currentTracks.Clear();
    }

    public void StartSpawning() => canSpawn = true;

    public void StopSpawning()
    {
        canSpawn = false;
        infiniteMode = false;
        isPatternActive = false;
        ClearTracks();
    }

    public void SetInfiniteMode(bool v) => infiniteMode = v;

    public void SetTrackCount(int v) => trackCount = v;
    public void SetDashSpeed(float v) => dashSpeed = v;
    public void SetSpawnCooldown(float v) => spawnCooldown = v;
    public void SetFollowDuration(float v) => followDuration = v;
}   