// ==============================
// PhaseLoopManager
// FINAL
// ==============================

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

    // =====================================
    // BUSY STATE
    // =====================================

    public bool IsBusy
    {
        get;
        private set;
    }

    public void SetBusy(bool busy)
    {
        IsBusy = busy;
    }

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

        if (FindObjectOfType<IntroSequenceManager>() != null)
        {
            return;
        }

        float stability =
            PlayerStatus.Instance != null
            ? PlayerStatus.Instance.stability
            : 100f;

        if (stability <= 30f && !isTransitioning)
        {
            PreviousState = CurrentState;

            StartManualTransition(GameState.Liminal);
        }
    }

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

    private bool isTransitioning = false;

    public void StartManualTransition(
        GameState targetState,
        Transform wakeUpPosition = null
    )
    {
        if (isTransitioning)
        {
            return;
        }

        isTransitioning = true;

        Debug.Log(
            ">>> MANUAL TRANSITION REQUEST: " +
            targetState
        );

        if (CurrentState == GameState.Dream && targetState == GameState.Awake)
        {
            currentLoop++;
            if (dayNightCycle != null)
            {
                dayNightCycle.TimeOfDay = 0.25f; // Set ke Pagi (0.25)
            }
            Debug.Log($"[PhaseLoopManager] Loop ke-{currentLoop} (Manual Skip)");
        }

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

                if (screenFader != null)
                {
                    float elapsed = 0f;

                    while (elapsed < fadeDuration)
                    {
                        elapsed += Time.deltaTime;

                        screenFader.alpha =
                            1f - (elapsed / fadeDuration);

                        yield return null;
                    }

                    screenFader.alpha = 0f;

                    screenFader.blocksRaycasts = false;
                }

                isTransitioning = false;

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

        if (screenFader != null)
        {
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;

                screenFader.alpha =
                    1f - (elapsed / fadeDuration);

                yield return null;
            }

            screenFader.alpha = 0f;

            screenFader.blocksRaycasts = false;
        }

        isTransitioning = false;
    }

    private IEnumerator InvokePhaseChangedNextFrame(
        GameState state
    )
    {
        yield return null;

        OnPhaseChanged?.Invoke(state);

        if (AudioManager.Instance != null)
        {
            switch (state)
            {
                case GameState.Awake:
                    AudioManager.Instance.PlayBGM(BGMType.Awake);
                    break;

                case GameState.Dream:
                    AudioManager.Instance.PlayBGM(BGMType.Dream);
                    break;

                case GameState.Liminal:
                    AudioManager.Instance.PlayBGM(BGMType.Liminal);
                    break;
            }
        }
    }
}