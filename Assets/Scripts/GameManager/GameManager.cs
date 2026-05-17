using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private GameOver gameOver;

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
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Update()
    {
        // Keep currentStability synced with PlayerStatus if PlayerStatus exists in the scene
        if (PlayerStatus.Instance != null)
        {
            currentStability = PlayerStatus.Instance.stability;
        }
    }

    public void AddMemory(int amount, float stabilityRestore)
    {
        totalFragmentsCollected += amount;
        OnMemoryCollected?.Invoke(totalFragmentsCollected);

        ModifyStability(stabilityRestore);
    }

    public void ModifyStability(float amount)
    {
        currentStability = Mathf.Clamp(currentStability + amount, 0f, 100f);
        if (PlayerStatus.Instance != null)
        {
            PlayerStatus.Instance.stability = currentStability;
        }
        OnStabilityChanged?.Invoke(currentStability);
        
        if (currentStability <= 0f)
        {
            GameOver();
        }
    }

    public void GameOver()
    {
        if (gameOver != null)
        {
            gameOver.Show();
        }
        else if (global::GameOver.Instance != null)
        {
            global::GameOver.Instance.Show();
        }
    }
}