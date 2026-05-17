using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyDifficultyController : MonoBehaviour
{
    [SerializeField] private EnemyDashSpawner spawner;

    [SerializeField] private int difficultyLevel = 1;

    private void Update()
    {
        Apply();
    }

    private void Apply()
    {
        switch (difficultyLevel)
        {
            case 1:
                spawner.SetTrackCount(1);
                spawner.SetDashSpeed(12f);
                spawner.SetSpawnCooldown(2.5f);
                spawner.SetFollowDuration(2.5f);
                break;

            case 2:
                spawner.SetTrackCount(2);
                spawner.SetDashSpeed(15f);
                spawner.SetSpawnCooldown(2f);
                spawner.SetFollowDuration(2f);
                break;

            case 3:
                spawner.SetTrackCount(3);
                spawner.SetDashSpeed(18f);
                spawner.SetSpawnCooldown(1.5f);
                spawner.SetFollowDuration(1.8f);
                break;

            case 4:
                spawner.SetTrackCount(4);
                spawner.SetDashSpeed(22f);
                spawner.SetSpawnCooldown(1.2f);
                spawner.SetFollowDuration(1.5f);
                break;

            default:
                spawner.SetTrackCount(5);
                spawner.SetDashSpeed(28f);
                spawner.SetSpawnCooldown(0.8f);
                spawner.SetFollowDuration(1f);
                break;
        }
    }

    public void SetDifficulty(int level)
    {
        difficultyLevel = level;
    }
}