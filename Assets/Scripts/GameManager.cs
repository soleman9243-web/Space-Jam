using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Player Stats")]
    public int totalFragmentsCollected = 0;
    
    [Range(0f, 100f)]
    [Tooltip("Player's mental stability. 100 is stable, 0 is fully unhinged.")]
    public float currentStability = 100f;

    [Header("Events")]
    public UnityEvent<int> OnMemoryCollected;
    public UnityEvent<float> OnStabilityChanged;

    private void Awake()
    {
        // Basic Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // Since this is a single scene game, DontDestroyOnLoad is optional, 
            // but left here in case it's needed later.
            // DontDestroyOnLoad(gameObject);
        }
    }

    /// <summary>
    /// Adds a collected fragment and increases stability.
    /// </summary>
    public void AddMemory(int amount, float stabilityIncrease)
    {
        totalFragmentsCollected += amount;
        OnMemoryCollected?.Invoke(totalFragmentsCollected);
        
        IncreaseStability(stabilityIncrease);
    }

    public void IncreaseStability(float amount)
    {
        currentStability = Mathf.Clamp(currentStability + amount, 0f, 100f);
        OnStabilityChanged?.Invoke(currentStability);
    }

    public void DecreaseStability(float amount)
    {
        currentStability = Mathf.Clamp(currentStability - amount, 0f, 100f);
        OnStabilityChanged?.Invoke(currentStability);
    }
}
