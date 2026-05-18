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
    [Header("Phase Progression Settings")]
    [Tooltip("Jika true, fase akan berganti otomatis berdasarkan durasi. Jika false, perpindahan fase dipicu manual (misal: tidur di kasur).")]
    public bool autoLoopPhases = false;
    
    [Tooltip("Nama Scene untuk fase 3 (Confusion). Pastikan sudah dimasukkan di Build Settings.")]
    public string phase3SceneName = "ConfusionScene";
    
    [Header("Phase Durations (Seconds) - Only if Auto Loop is True")]
    public float awakeDuration = 10f;
    public float dreamDuration = 20f;
    public float confusionDuration = 15f;

    [Header("UI & Visuals")]
    [Tooltip("CanvasGroup used for fading the screen in and out. Attach a full-screen black panel with a CanvasGroup here.")]
    public CanvasGroup screenFader;
    [Tooltip("How long the fade to black and fade from black takes.")]
    public float fadeDuration = 1f;
    
    private bool isTransitioning = false;

    [Header("Events")]
    public UnityEvent<GameState> OnPhaseChanged;

    [Header("Debug (Informasi Live Saat Play)")]
    [Tooltip("Cek di sini untuk melihat fase game sudah berganti atau belum.")]
    public GameState inspectorCurrentState;

    public GameState CurrentState { get; private set; }

    private void Start()
    {
        CurrentState = GameState.Awake;
        inspectorCurrentState = CurrentState;
        
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

        if (autoLoopPhases)
        {
            StartCoroutine(PhaseLoopRoutine());
        }
    }

    private void Update()
    {
        // Update terus tulisan di Inspector agar kamu gampang ngeceknya
        inspectorCurrentState = CurrentState;

        // Pengecekan khusus untuk pindah ke Fase 3 (Confusion) saat di Fase 2 (Dream)
        if (CurrentState == GameState.Dream && !isTransitioning)
        {
            float currentStability = GameManager.Instance != null ? GameManager.Instance.currentStability : 100f;
            if (PlayerStatus.Instance != null)
            {
                currentStability = PlayerStatus.Instance.stability;
            }

            if (currentStability <= 30f)
            {
                // Stability drop di bawah 30, paksa masuk fase 3 (pindah scene)
                StartManualTransition(GameState.Confusion);
            }
        }
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
        while (autoLoopPhases)
        {
            OnPhaseChanged?.Invoke(CurrentState);

            yield return new WaitForSeconds(GetDurationForState(CurrentState));

            // Fade out ke layar hitam
            yield return StartCoroutine(FadeScreen(1f));

            TransitionToNextState();
            inspectorCurrentState = CurrentState;

            OnPhaseChanged?.Invoke(CurrentState);

            // Fade in kembali ke game
            yield return StartCoroutine(FadeScreen(0f));
        }
    }

    // Fungsi transisi instan yang dipanggil saat klik kasur
    public void StartManualTransition(GameState targetState, Transform wakeUpPosition = null)
    {
        // Langsung ganti status
        CurrentState = targetState;
        inspectorCurrentState = CurrentState; // Update Inspector
        OnPhaseChanged?.Invoke(CurrentState);

        // Pindah posisi player jika ada (fase Dream)
        if (targetState == GameState.Dream && wakeUpPosition != null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                player.transform.position = wakeUpPosition.position;
                player.transform.rotation = wakeUpPosition.rotation;
            }
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
