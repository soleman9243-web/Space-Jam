using UnityEngine;

public class EnemyDifficultyController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyDashSpawner spawner;

    [Header("Difficulty")]
    [SerializeField] private int difficulty = 1;
    [SerializeField] private int maxDifficulty = 10;

    [Header("Pattern Scaling")]
    [SerializeField] private int basePattern = 1;
    [SerializeField] private int maxPattern = 8;

    [Header("Liminal Mode")]
    [SerializeField] private bool liminalMode;
    [SerializeField] private float timer;

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
            Apply();
        }
    }

    private void Apply()
    {
        if (spawner == null)
        {
            return;
        }

        spawner.SetTrackCount(Mathf.Clamp(difficulty, 1, 5));
        spawner.SetDashSpeed(12f + difficulty * 3f);
        spawner.SetSpawnCooldown(Mathf.Max(0.5f, 2.5f - difficulty * 0.2f));
        spawner.SetFollowDuration(Mathf.Max(0.8f, 2.5f - difficulty * 0.15f));
    }

    public int GetDifficulty()
    {
        return difficulty;
    }

    public int GetPatternCount()
    {
        return Mathf.Clamp(basePattern + difficulty / 2, 1, maxPattern);
    }

    public void SetLiminalMode(bool value)
    {
        liminalMode = value;
        timer = 0f;
    }

    public void SetDifficulty(int value)
    {
        difficulty = Mathf.Clamp(value, 1, maxDifficulty);
        Apply();
    }
}