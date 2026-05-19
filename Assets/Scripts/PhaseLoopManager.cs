using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public enum GameState
{
    Awake,
    Dream,
    Confusion
}

public class PhaseLoopManager : MonoBehaviour
{
    public bool autoLoopPhases = false;

    public int currentLoop = 1;

    public string phase3SceneName = "ConfusionScene";

    public float awakeDuration = 10f;
    public float dreamDuration = 20f;
    public float confusionDuration = 15f;

    public CanvasGroup screenFader;
    public float fadeDuration = 1f;

    public UnityEvent<GameState> OnPhaseChanged;

    public GameState inspectorCurrentState;

    public GameState CurrentState { get; private set; }

    public static GameState GlobalState = GameState.Awake;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        CurrentState = GlobalState;
        inspectorCurrentState = CurrentState;

        Debug.Log("START STATE = " + CurrentState);

        OnPhaseChanged?.Invoke(CurrentState);

        if (autoLoopPhases)
        {
            StartCoroutine(PhaseLoopRoutine());
        }
    }

    private void Update()
    {
        inspectorCurrentState = CurrentState;

        // DEBUG REAL STATE
        Debug.Log("STATE NOW = " + CurrentState);

        if (CurrentState == GameState.Dream)
        {
            float stability = PlayerStatus.Instance != null
                ? PlayerStatus.Instance.stability
                : 100f;

            if (stability <= 30f)
            {
                StartManualTransition(GameState.Confusion);
            }
        }
    }

    // =======================
    // MAIN TRANSITION (FIXED)
    // =======================
    public void StartManualTransition(GameState targetState, Transform wakeUpPosition = null)
    {
        Debug.Log(">>> TRANSITION REQUEST: " + targetState);

        GlobalState = targetState;
        CurrentState = targetState;
        inspectorCurrentState = targetState;

        Debug.Log(">>> STATE SET TO: " + CurrentState);

        OnPhaseChanged?.Invoke(CurrentState);

        if (targetState == GameState.Confusion)
        {
            Debug.Log("LOADING SCENE: " + phase3SceneName);
            SceneManager.LoadScene(phase3SceneName);
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            Debug.Log("PLAYER FOUND - TELEPORT");

            if (wakeUpPosition != null)
            {
                player.transform.position = wakeUpPosition.position;
                player.transform.rotation = wakeUpPosition.rotation;
            }

            PlayerMovement move = player.GetComponent<PlayerMovement>();
            if (move != null) move.enabled = true;

            Collider2D col = player.GetComponent<Collider2D>();
            if (col != null) col.enabled = true;
        }
        else
        {
            Debug.LogWarning("PLAYER NOT FOUND");
        }
    }

    private IEnumerator PhaseLoopRoutine()
    {
        while (autoLoopPhases)
        {
            yield return new WaitForSeconds(GetDuration(CurrentState));

            TransitionToNext();

            GlobalState = CurrentState;

            OnPhaseChanged?.Invoke(CurrentState);
        }
    }

    private float GetDuration(GameState state)
    {
        switch (state)
        {
            case GameState.Awake: return awakeDuration;
            case GameState.Dream: return dreamDuration;
            case GameState.Confusion: return confusionDuration;
            default: return 10f;
        }
    }

    private void TransitionToNext()
    {
        if (CurrentState == GameState.Awake)
        {
            CurrentState = GameState.Dream;
        }
        else if (CurrentState == GameState.Dream)
        {
            CurrentState = GameState.Awake;
            currentLoop++;
        }
        else if (CurrentState == GameState.Confusion)
        {
            CurrentState = GameState.Awake;
        }

        Debug.Log("AUTO TRANSITION -> " + CurrentState);
    }
}