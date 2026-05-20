using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using SpaceJam.Environment;

public enum GameState
{
    Awake,
    Dream,
    Liminal
}
public class PhaseLoopManager : MonoBehaviour
{
    [Header("Liminal")]
    public bool useInSceneLiminal = true;

    [SerializeField]
    private LiminalRoomManager liminalRoomManager;

    public int currentLoop = 1;

    public string liminalSceneName = "LiminalScene";

    [Header("Fade")]
    public CanvasGroup screenFader;

    public float fadeDuration = 1f;

    [Header("Day Night Cycle")]
    public DayNightCycle2D dayNightCycle;

    public UnityEvent<GameState> OnPhaseChanged;

    public GameState inspectorCurrentState;

    public GameState CurrentState
    {
        get;
        private set;
    }

    public GameState PreviousState
    {
        get;
        private set;
    }

    public static GameState GlobalState =
        GameState.Awake;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        CurrentState = GlobalState;

        inspectorCurrentState = CurrentState;

        if (dayNightCycle != null)
        {
            dayNightCycle.OnPhaseChanged
                .AddListener(OnDayPhaseChanged);
        }
        else
        {
            Debug.LogWarning(
                "[PhaseLoopManager] DayNightCycle2D not assigned!"
            );
        }

        StartCoroutine(
            InvokePhaseChangedNextFrame(CurrentState)
        );
    }

    private void Update()
    {
        inspectorCurrentState = CurrentState;

        if (CurrentState == GameState.Liminal)
        {
            return;
        }

        float stability =
            PlayerStatus.Instance != null
            ? PlayerStatus.Instance.stability
            : 100f;

        if (stability <= 30f)
        {
            PreviousState = CurrentState;

            StartManualTransition(GameState.Liminal);
        }
    }

    // =============================================
    // DAY NIGHT CYCLE
    // =============================================

    private void OnDayPhaseChanged(DayPhase dayPhase)
    {
        if (CurrentState == GameState.Liminal)
        {
            return;
        }

        GameState targetState =
            DayPhaseToGameState(dayPhase);

        if (targetState == CurrentState)
        {
            return;
        }

        ExecuteTransition(targetState);
    }

    private GameState DayPhaseToGameState(
        DayPhase dayPhase
    )
    {
        return dayPhase == DayPhase.Night
            ? GameState.Dream
            : GameState.Awake;
    }

    // =============================================
    // TRANSITION
    // =============================================

    private void ExecuteTransition(GameState targetState)
    {
        if (CurrentState == GameState.Dream &&
            targetState == GameState.Awake)
        {
            currentLoop++;

            Debug.Log(
                $"[PhaseLoopManager] Loop ke-{currentLoop}"
            );
        }

        GlobalState = targetState;

        CurrentState = targetState;

        inspectorCurrentState = targetState;

        StartCoroutine(
            InvokePhaseChangeNextFrame(targetState)
        );
    }

    public void StartManualTransition(
        GameState targetState,
        Transform wakeUpPosition = null
    )
    {
        Debug.Log(
            ">>> MANUAL TRANSITION REQUEST: " +
            targetState
        );

        GlobalState = targetState;

        CurrentState = targetState;

        inspectorCurrentState = targetState;

        StartCoroutine(
            InvokePhaseChangeNextFrame(
                targetState,
                wakeUpPosition
            )
        );
    }

    private IEnumerator InvokePhaseChangeNextFrame(
        GameState targetState,
        Transform wakeUpPosition = null
    )
    {
        yield return null;

        OnPhaseChanged?.Invoke(CurrentState);

        if (targetState == GameState.Liminal)
        {
            if (useInSceneLiminal)
            {
                Debug.Log("Enter Liminal");

                if (liminalRoomManager != null)
                {
                    liminalRoomManager.EnterLiminal();
                }

                yield break;
            }
            else
            {
                SceneManager.LoadScene(
                    liminalSceneName
                );

                yield break;
            }
        }

        GameObject player =
            GameObject.FindGameObjectWithTag(
                "Player"
            );

        if (player != null)
        {
            if (wakeUpPosition != null)
            {
                player.transform.position =
                    wakeUpPosition.position;

                player.transform.rotation =
                    wakeUpPosition.rotation;
            }

            PlayerMovement move =
                player.GetComponent<PlayerMovement>();

            if (move != null)
            {
                move.enabled = true;
            }

            Collider2D col =
                player.GetComponent<Collider2D>();

            if (col != null)
            {
                col.enabled = true;
            }
        }
        else
        {
            Debug.LogWarning("PLAYER NOT FOUND");
        }
    }

    private IEnumerator InvokePhaseChangedNextFrame(
        GameState state
    )
    {
        yield return null;

        OnPhaseChanged?.Invoke(state);
    }
}