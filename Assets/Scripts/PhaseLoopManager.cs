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
        
        // 1. Cari otomatis Screen Fader jika belum dimasukkan di Inspector
        if (screenFader == null)
        {
            GameObject faderObj = GameObject.Find("Screen Fader") ?? GameObject.Find("ScreenFader") ?? GameObject.Find("Fader") ?? GameObject.Find("Panel");
            if (faderObj != null)
            {
                screenFader = faderObj.GetComponent<CanvasGroup>();
            }
        }

        // 2. Pastikan layar mulai transparan dan tidak menghalangi klik
        if (screenFader != null)
        {
            screenFader.alpha = 0f;
            screenFader.blocksRaycasts = false;
        }

        // 3. Cek otomatis penyebab layar hitam di Game View
        CheckCommonBlackScreenIssues();

        StartCoroutine(PhaseLoopRoutine());
    }

    private void CheckCommonBlackScreenIssues()
    {
        // Cek 1: Posisi Z Kamera
        Camera mainCam = Camera.main;
        if (mainCam != null && mainCam.transform.position.z >= 0f)
        {
            Debug.LogError("⚠️ [Penyebab Layar Hitam] Posisi Z Main Camera Anda berada di " + mainCam.transform.position.z + "! Ubah posisi Z kamera di Inspector menjadi -10 agar bisa melihat karakter 2D Anda.");
        }

        // Cek 2: Ketersediaan Cahaya 2D di URP
        var lights = FindObjectsOfType<UnityEngine.Rendering.Universal.Light2D>();
        if (lights == null || lights.Length == 0)
        {
            Debug.LogWarning("⚠️ [Penyebab Layar Hitam] Tidak ada objek cahaya (Light 2D) di scene Anda! Di project URP 2D, tanpa Global Light 2D, semua Sprite akan menjadi gelap/hitam di Game View.");
        }
    }

    private IEnumerator PhaseLoopRoutine()
    {
        while (true)
        {
            OnPhaseChanged?.Invoke(CurrentState);

            yield return new WaitForSeconds(GetDurationForState(CurrentState));

            // Fade out ke layar hitam
            yield return StartCoroutine(FadeScreen(1f));

            TransitionToNextState();

            OnPhaseChanged?.Invoke(CurrentState);

            // Fade in kembali ke game
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

        if (targetAlpha > 0f) screenFader.blocksRaycasts = true;

        float startAlpha = screenFader.alpha;
        float time = 0;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            screenFader.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            yield return null;
        }

        screenFader.alpha = targetAlpha;
        if (targetAlpha == 0f) screenFader.blocksRaycasts = false;
    }
}
