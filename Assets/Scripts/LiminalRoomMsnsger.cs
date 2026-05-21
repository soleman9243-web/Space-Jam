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

        // Pilih 1 tipe tantangan random untuk seluruh sesi Liminal ini
        var all = new List<LiminalChallengeType>
        {
            LiminalChallengeType.Shadow,
            LiminalChallengeType.Enemy,
            LiminalChallengeType.Bullet
        };

        currentChallenge = all[Random.Range(0, all.Count)];
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