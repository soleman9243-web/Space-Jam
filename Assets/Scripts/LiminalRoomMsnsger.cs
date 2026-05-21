using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tiga jenis tantangan yang bisa muncul di fase Liminal.
/// </summary>
public enum LiminalChallengeType
{
    Shadow,
    Enemy,
    Bullet
}

/// <summary>
/// Mengelola fase Liminal:
/// - Saat enter: pilih 1 tantangan random (Shadow / Enemy / Bullet)
/// - Tantangan itu jalan terus sampai player keluar sendiri (stability >= 100)
/// - Setiap masuk Liminal baru, tipe tantangan di-random ulang
/// </summary>
public class LiminalRoomManager : MonoBehaviour
{
    [Header("Worlds")]
    [SerializeField] private GameObject normalWorld;
    [SerializeField] private GameObject liminalWorld;
    [SerializeField] private Transform liminalSpawnPoint;

    [Header("Enemy Challenge")]
    [SerializeField] private EnemyDashSpawner enemySpawner;
    [SerializeField] private EnemyDifficultyController difficultyController;

    [Header("Bullet Challenge")]
    [SerializeField] private BulletSpawner bulletSpawner;

    [Header("Shadow Challenge")]
    [SerializeField] private ShadowSpawner shadowSpawner;

    [Header("Obstacles (opsional)")]
    [SerializeField] private List<GameObject> liminalObstacles;

    // ?? State ??????????????????????????????????????????????????????????????
    private bool active;
    private bool transitioning;
    private LiminalChallengeType currentChallenge;
    private GameObject currentObstacle;
    private Vector3 previousPlayerPosition;

    private static List<LiminalChallengeType> challengeBag = new List<LiminalChallengeType>();

    // ?? Unity ??????????????????????????????????????????????????????????????

    private void Update()
    {
        if (!active || PlayerStatus.Instance == null)
        {
            return;
        }

        if (PlayerStatus.Instance.stability >= 100f)
        {
            ExitLiminal();
        }
    }

    // ?? Public API ?????????????????????????????????????????????????????????

    public void EnterLiminal()
    {
        if (transitioning) return;
        transitioning = true;
        active = true;

        normalWorld.SetActive(false);
        liminalWorld.SetActive(true);

        // --- TELEPORT PLAYER ---
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            previousPlayerPosition = player.transform.position;
            if (liminalSpawnPoint != null)
            {
                player.transform.position = liminalSpawnPoint.position;
            }
            else
            {
                player.transform.position = liminalWorld.transform.position;
            }

            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.velocity = Vector2.zero;
        }

        SpawnRandomObstacle();

        // Sync loop saat ini ke ShadowSpawner untuk difficulty yang tepat
        PhaseLoopManager phaseManager = FindObjectOfType<PhaseLoopManager>();
        if (phaseManager != null && shadowSpawner != null)
        {
            shadowSpawner.SetLoop(phaseManager.currentLoop);
        }

        if (difficultyController != null)
        {
            difficultyController.SetLiminalMode(true);
        }

        if (challengeBag.Count == 0)
        {
            challengeBag.Add(LiminalChallengeType.Shadow);
            challengeBag.Add(LiminalChallengeType.Enemy);
            challengeBag.Add(LiminalChallengeType.Bullet);

            // Shuffle
            for (int i = 0; i < challengeBag.Count; i++)
            {
                int r = Random.Range(i, challengeBag.Count);
                var temp = challengeBag[i];
                challengeBag[i] = challengeBag[r];
                challengeBag[r] = temp;
            }
        }

        currentChallenge = challengeBag[0];
        challengeBag.RemoveAt(0);

        Debug.Log($"[LiminalRoomManager] Tantangan sesi ini: {currentChallenge}");

        StartChallenge(currentChallenge);

        transitioning = false;
    }

    public void ExitLiminal()
    {
        if (transitioning) return;
        transitioning = true;
        active = false;

        // Bersihkan tantangan yang sedang berjalan
        StopChallenge(currentChallenge);

        // Bersihkan obstacle
        if (currentObstacle != null)
        {
            Destroy(currentObstacle);
            currentObstacle = null;
        }

        if (difficultyController != null)
        {
            difficultyController.SetLiminalMode(false);
        }

        liminalWorld.SetActive(false);
        normalWorld.SetActive(true);

        // --- KEMBALIKAN PLAYER ---
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.transform.position = previousPlayerPosition;
            player.transform.rotation = Quaternion.identity; // FIX: Reset rotation
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.velocity = Vector2.zero;
        }

        // Kembalikan ke state sebelum masuk Liminal
        PhaseLoopManager manager = FindObjectOfType<PhaseLoopManager>();
        if (manager != null)
        {
            manager.StartManualTransition(manager.PreviousState);
        }

        transitioning = false;
    }

    // ?? Start / Stop per challenge ?????????????????????????????????????????

    private void StartChallenge(LiminalChallengeType type)
    {
        Debug.Log($"[LiminalRoomManager] Mulai tantangan: {type}");

        switch (type)
        {
            case LiminalChallengeType.Shadow:
                if (shadowSpawner != null)
                {
                    shadowSpawner.SpawnOnPlayer();
                }
                break;

            case LiminalChallengeType.Enemy:
                if (enemySpawner != null)
                {
                    enemySpawner.SetInfiniteMode(true);
                    enemySpawner.StartSpawning();
                }
                break;

            case LiminalChallengeType.Bullet:
                if (bulletSpawner != null)
                {
                    bulletSpawner.StartSpawning();
                }
                break;
        }
    }

    private void StopChallenge(LiminalChallengeType type)
    {
        Debug.Log($"[LiminalRoomManager] Stop tantangan: {type}");

        switch (type)
        {
            case LiminalChallengeType.Shadow:
                if (shadowSpawner != null)
                {
                    shadowSpawner.RemoveShadow();
                }
                break;

            case LiminalChallengeType.Enemy:
                if (enemySpawner != null)
                {
                    enemySpawner.SetInfiniteMode(false);
                    enemySpawner.StopSpawning();
                }
                break;

            case LiminalChallengeType.Bullet:
                if (bulletSpawner != null)
                {
                    bulletSpawner.StopSpawning();
                }
                break;
        }
    }

    // ?? Obstacle ???????????????????????????????????????????????????????????

    private void SpawnRandomObstacle()
    {
        if (currentObstacle != null)
        {
            Destroy(currentObstacle);
        }

        if (liminalObstacles == null || liminalObstacles.Count == 0) return;

        int rand = Random.Range(0, liminalObstacles.Count);
        currentObstacle = Instantiate(liminalObstacles[rand]);
    }
}