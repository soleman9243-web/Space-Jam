using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyWaveController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyDashSpawner spawner;
    [SerializeField] private PlayerStatus player;

    [Header("Stability Rule")]
    [SerializeField] private float activateThreshold = 30f;

    [Header("Difficulty")]
    [SerializeField] private int difficulty = 1;
    [SerializeField] private int wavesCleared = 0;

    [Header("Wave Scaling")]
    [SerializeField] private int wavesPerDifficulty = 5;

    private bool isActive = false;

    private void Update()
    {
        HandleSpawnState();
    }

    private void HandleSpawnState()
    {
        float stability = player.stability;

        if (stability < activateThreshold)
        {
            if (!isActive)
            {
                isActive = true;
                spawner.StartSpawning();
            }
        }
        else
        {
            if (isActive)
            {
                isActive = false;
                spawner.StopSpawning();
            }
        }
    }

    public void RegisterWaveComplete()
    {
        wavesCleared++;

        if (wavesCleared >= wavesPerDifficulty)
        {
            wavesCleared = 0;
            difficulty++;

            ApplyDifficulty();
        }
    }

    private void ApplyDifficulty()
    {
        spawner.SetTrackCount(Mathf.Clamp(difficulty, 1, 5));
        spawner.SetDashSpeed(12f + difficulty * 3f);
        spawner.SetSpawnCooldown(Mathf.Max(0.8f, 2.5f - difficulty * 0.2f));
        spawner.SetFollowDuration(Mathf.Max(1f, 2.5f - difficulty * 0.15f));
    }
}