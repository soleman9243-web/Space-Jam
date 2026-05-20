using UnityEngine;

public class EnemyWaveController : MonoBehaviour
{
    [SerializeField] private EnemyDashSpawner spawner;

    [Header("Difficulty")]
    [SerializeField] private int difficulty = 1;
    [SerializeField] private int maxDifficulty = 10;

    private bool liminalMode;
    private float timer;

    public int GetDifficulty()
    {
        return difficulty;
    }

    public void SetLiminalMode(bool v)
    {
        liminalMode = v;
        timer = 0f;
    }

    private void Update()
    {
        if (!liminalMode)
        {
            return;
        }

        timer += Time.deltaTime;

        if (timer >= 10f)
        {
            timer = 0f;
            difficulty = Mathf.Clamp(difficulty + 1, 1, maxDifficulty);

            ApplyDifficulty();
        }
    }

    private void ApplyDifficulty()
    {
        spawner.SetTrackCount(Mathf.Clamp(difficulty, 1, 5));
        spawner.SetDashSpeed(12f + difficulty * 3f);
        spawner.SetSpawnCooldown(Mathf.Max(0.5f, 2.5f - difficulty * 0.2f));
        spawner.SetFollowDuration(Mathf.Max(0.8f, 2.5f - difficulty * 0.15f));
    }
}