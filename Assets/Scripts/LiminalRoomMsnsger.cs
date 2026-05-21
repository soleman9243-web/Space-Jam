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
/// - Saat enter: pilih tantangan random (Shadow / Enemy / Bullet)
/// - Setiap challengeDuration detik, tantangan berganti ke tipe baru yang
///   random (tidak langsung repeat tipe yang sama)
/// - Loop terus sampai stability >= 100 (player keluar sendiri)
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

    [Header("Challenge Settings")]
    [Tooltip("Berapa detik setiap tantangan berlangsung sebelum berganti.")]
    [SerializeField] private float challengeDuration = 15f;

    // ?? State ??????????????????????????????????????????????????????????????
    private bool active;
    private bool transitioning;
    private LiminalChallengeType currentChallenge;
    private GameObject currentObstacle;
    private Coroutine challengeLoopCoroutine;

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

        // Mulai loop tantangan
        challengeLoopCoroutine = StartCoroutine(ChallengeLoop());

        transitioning = false;
    }

    public void ExitLiminal()
    {
        if (transitioning) return;
        transitioning = true;
        active = false;

        // Hentikan loop
        if (challengeLoopCoroutine != null)
        {
            StopCoroutine(challengeLoopCoroutine);
            challengeLoopCoroutine = null;
        }

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

    // ?? Challenge Loop ?????????????????????????????????????????????????????

    /// <summary>
    /// Terus memutar tantangan random selama fase Liminal aktif.
    /// Setiap challengeDuration detik, tipe tantangan berganti.
    /// </summary>
    private IEnumerator ChallengeLoop()
    {
        // Pilih tantangan pertama benar-benar random
        currentChallenge = PickRandomChallenge(null);

        while (active)
        {
            StartChallenge(currentChallenge);

            yield return new WaitForSeconds(challengeDuration);

            if (!active) break;

            StopChallenge(currentChallenge);

            // Pilih tantangan berikutnya — hindari tipe yang sama
            LiminalChallengeType next = PickRandomChallenge(currentChallenge);
            currentChallenge = next;
        }
    }

    /// <summary>
    /// Pilih tipe tantangan random. Jika exclude != null, tidak akan
    /// memilih tipe yang sama dengan yang sedang berjalan.
    /// </summary>
    private LiminalChallengeType PickRandomChallenge(
        LiminalChallengeType? exclude
    )
    {
        var all = new List<LiminalChallengeType>
        {
            LiminalChallengeType.Shadow,
            LiminalChallengeType.Enemy,
            LiminalChallengeType.Bullet
        };

        if (exclude.HasValue && all.Count > 1)
        {
            all.Remove(exclude.Value);
        }

        return all[Random.Range(0, all.Count)];
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