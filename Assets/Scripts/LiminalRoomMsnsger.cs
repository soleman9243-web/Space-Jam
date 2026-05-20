using System.Collections.Generic;
using UnityEngine;

public class LiminalRoomManager : MonoBehaviour
{
    [Header("Rooms")]
    [SerializeField] private GameObject normalWorld;
    [SerializeField] private GameObject liminalWorld;

    [Header("Systems")]
    [SerializeField] private EnemyDashSpawner spawner;
    [SerializeField] private EnemyDifficultyController difficultyController;

    [Header("Obstacles")]
    [SerializeField] private List<GameObject> liminalObstacles;

    private GameObject currentObstacle;
    private bool active;
    private bool transitioning;

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

    public void EnterLiminal()
    {
        if (transitioning)
        {
            return;
        }

        transitioning = true;
        active = true;

        normalWorld.SetActive(false);
        liminalWorld.SetActive(true);

        SpawnRandomObstacle();

        if (spawner != null)
        {
            spawner.SetInfiniteMode(true);
            spawner.StartSpawning();
        }

        if (difficultyController != null)
        {
            difficultyController.SetLiminalMode(true);
        }

        transitioning = false;
    }

    public void ExitLiminal()
    {
        if (transitioning)
        {
            return;
        }

        transitioning = true;
        active = false;

        if (currentObstacle != null)
        {
            Destroy(currentObstacle);
        }

        if (spawner != null)
        {
            spawner.SetInfiniteMode(false);
            spawner.StopSpawning();
        }

        if (difficultyController != null)
        {
            difficultyController.SetLiminalMode(false);
        }

        liminalWorld.SetActive(false);
        normalWorld.SetActive(true);

        PhaseLoopManager manager = FindObjectOfType<PhaseLoopManager>();

        if (manager != null)
        {
            manager.StartManualTransition(manager.PreviousState);
        }

        transitioning = false;
    }

    private void SpawnRandomObstacle()
    {
        if (currentObstacle != null)
        {
            Destroy(currentObstacle);
        }

        if (liminalObstacles == null || liminalObstacles.Count == 0)
        {
            return;
        }

        int rand = Random.Range(0, liminalObstacles.Count);
        currentObstacle = Instantiate(liminalObstacles[rand]);
    }
}