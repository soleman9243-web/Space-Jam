using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public enum GameState
{
    Awake,
    Dream,
    Confusion
}

public class PhaseLoopManager : MonoBehaviour
{
    [Header("Phase Durations (Seconds)")]
    [Tooltip("Duration of the Awake phase in seconds.")]
    public float awakeDuration = 10f;
    [Tooltip("Duration of the Dream phase in seconds.")]
    public float dreamDuration = 20f;
    [Tooltip("Duration of the Confusion phase in seconds.")]
    public float confusionDuration = 15f;

    [Header("UI & Visuals")]
    [Tooltip("CanvasGroup used for fading the screen in and out. Attach a full-screen black panel with a CanvasGroup here.")]
    public CanvasGroup screenFader;
    [Tooltip("How long the fade to black and fade from black takes.")]
    public float fadeDuration = 1f;

    [Header("Events")]
    public UnityEvent<GameState> OnPhaseChanged;

    public GameState CurrentState { get; private set; }

    private void Start()
    {
        CurrentState = GameState.Awake;
        
        // Ensure screen starts fully visible
        if (screenFader != null) screenFader.alpha = 0f;

        StartCoroutine(PhaseLoopRoutine());
    }

    private IEnumerator PhaseLoopRoutine()
    {
        while (true)
        {
            // Trigger phase change event for the active phase
            OnPhaseChanged?.Invoke(CurrentState);

            // Wait for current phase to finish
            yield return new WaitForSeconds(GetDurationForState(CurrentState));

            // Fade out (Screen becomes black)
            yield return StartCoroutine(FadeScreen(1f));

            // Switch to the next state logically
            TransitionToNextState();

            // Trigger phase change event so systems know we're technically in the new phase
            OnPhaseChanged?.Invoke(CurrentState);

            // Fade in (Screen becomes visible again)
            yield return StartCoroutine(FadeScreen(0f));
        }
    }

    private float GetDurationForState(GameState state)
    {
        switch (state)
        {
            case GameState.Awake: return awakeDuration;
            case GameState.Dream: return dreamDuration;
            case GameState.Confusion: return confusionDuration;
            default: return 10f;
        }
    }

    private void TransitionToNextState()
    {
        switch (CurrentState)
        {
            case GameState.Awake:
                CurrentState = GameState.Dream;
                break;
            case GameState.Dream:
                CurrentState = GameState.Confusion;
                break;
            case GameState.Confusion:
                CurrentState = GameState.Awake;
                break;
        }
    }

    private IEnumerator FadeScreen(float targetAlpha)
    {
        if (screenFader == null) yield break;

        float startAlpha = screenFader.alpha;
        float time = 0;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            screenFader.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            yield return null;
        }

        // Ensure it exactly hits the target
        screenFader.alpha = targetAlpha;
    }
}
