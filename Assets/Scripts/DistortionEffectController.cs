using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DistortionEffectController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The Global Volume containing Post-Processing overrides. Akan dicari otomatis jika kosong.")]
    public Volume globalVolume;
    [Tooltip("Reference to the Phase Loop Manager to listen to state changes.")]
    public PhaseLoopManager phaseManager;

    [Header("Audio Settings")]
    [Tooltip("Sound effects to play when stability drops below 30%. Assign audio clips in Inspector.")]
    public AudioClip[] glitchSounds;
    private AudioSource audioSource;
    private float audioTimer = 0f;
    private float nextAudioInterval = 3f;

    [Header("Distortion Settings")]
    public float maxLensDistortion = -0.6f;
    public float maxVignetteIntensity = 0.5f;
    public float maxChromaticAberration = 1f;
    public float transitionSpeed = 10f; // Kecepatan lerp dinaikkan agar instan
    [Tooltip("Jika dicentang, UI Canvas diubah ke Camera Mode agar ikut terdistorsi. Jika UI jadi terlalu zoom, matikan centang ini.")]
    public bool distortUICanvases = false;

    [Header("Debug (Informasi Live Saat Play)")]
    public float debugCurrentStability;
    public float debugFinalDistortionWeight;
    public bool isPostProcessingCameraActive;
    public bool isVolumeActive;

    // Post-Processing overrides
    private LensDistortion lensDistortion;
    private Vignette vignette;
    private ChromaticAberration chromaticAberration;

    // Distortion calculation variables
    private float baseDistortionWeight = 0f;
    private float glitchDistortionWeight = 0f;
    private float glitchTimer = 0f;
    private float glitchHoldTimer = 0f;
    private float nextGlitchInterval = 3f;

    private void Start()
    {
        // 1. Setup AudioSource
        if (!TryGetComponent(out audioSource))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.spatialBlend = 0f; // 2D sound

        // 2. Pastikan Main Camera menyalakan Post-Processing
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            if (mainCam.TryGetComponent(out UniversalAdditionalCameraData camData))
            {
                camData.renderPostProcessing = true;
                isPostProcessingCameraActive = true;
            }
        }

        // 4. Cari Global Volume otomatis jika belum diisi di Inspector
        if (globalVolume == null)
        {
            globalVolume = FindObjectOfType<Volume>();
        }

        // 4. Pasang Override Post-Processing jika belum ada di Volume Profile
        if (globalVolume != null && globalVolume.profile != null)
        {
            isVolumeActive = true;

            if (!globalVolume.profile.TryGet(out lensDistortion))
            {
                lensDistortion = globalVolume.profile.Add<LensDistortion>();
            }
            if (!globalVolume.profile.TryGet(out vignette))
            {
                vignette = globalVolume.profile.Add<Vignette>();
            }
            if (!globalVolume.profile.TryGet(out chromaticAberration))
            {
                chromaticAberration = globalVolume.profile.Add<ChromaticAberration>();
            }

            // Aktifkan semua parameter
            if (lensDistortion != null) lensDistortion.intensity.overrideState = true;
            if (vignette != null) vignette.intensity.overrideState = true;
            if (chromaticAberration != null) chromaticAberration.intensity.overrideState = true;
        }

        if (phaseManager != null)
        {
            phaseManager.OnPhaseChanged.AddListener(HandlePhaseChanged);
        }
    }

    private void HandlePhaseChanged(GameState newState)
    {
        if (newState == GameState.Confusion)
        {
            TriggerGlitch(0.8f, 0.5f);
        }
    }

    private void Update()
    {
        // Prioritaskan membaca langsung dari PlayerStatus bar yang ada di UI, dengan fallback ke GameManager
        float stability = PlayerStatus.Instance != null ? PlayerStatus.Instance.stability : 
                         (GameManager.Instance != null ? GameManager.Instance.currentStability : 100f);
        debugCurrentStability = stability;

        if (globalVolume == null) return;

        GameState currentState = phaseManager != null ? phaseManager.CurrentState : GameState.Awake;

        HandleStabilityEffects(stability, currentState);
        ApplyDistortionEffects();
    }

    private void TriggerGlitch(float weight, float holdTime)
    {
        glitchDistortionWeight = weight;
        glitchHoldTimer = holdTime;
    }

    private void HandleStabilityEffects(float stability, GameState currentState)
    {
        if (glitchHoldTimer > 0f)
        {
            glitchHoldTimer -= Time.deltaTime;
        }
        else if (glitchDistortionWeight > 0f)
        {
            glitchDistortionWeight = Mathf.MoveTowards(glitchDistortionWeight, 0f, Time.deltaTime * 2f);
        }

        // 1. TIER 1: STABILITY <= 50% (Distorsi Keluar Jarang)
        if (stability <= 50f && stability > 30f)
        {
            baseDistortionWeight = 0.15f;
            glitchTimer += Time.deltaTime;
            if (glitchTimer >= nextGlitchInterval)
            {
                glitchTimer = 0f;
                nextGlitchInterval = Random.Range(2.5f, 5f);
                TriggerGlitch(0.65f, 0.4f); // Glitch ditahan 0.4 detik agar sangat jelas terlihat
            }
        }
        // 2. TIER 2: STABILITY <= 30% (Suara & Distorsi Sering)
        else if (stability <= 30f && stability > 20f)
        {
            baseDistortionWeight = 0.4f;
            glitchTimer += Time.deltaTime;
            if (glitchTimer >= nextGlitchInterval)
            {
                glitchTimer = 0f;
                nextGlitchInterval = Random.Range(1.5f, 3f);
                TriggerGlitch(0.85f, 0.4f);
            }

            HandleAudioGlitch(2f, 4f);
        }
        // 3. TIER 3: STABILITY <= 20% (Distorsi Brutal & Suara Intens)
        else if (stability <= 20f)
        {
            float brutalWave = Mathf.PingPong(Time.time * 5f, 0.5f);
            baseDistortionWeight = 0.7f + brutalWave;

            glitchTimer += Time.deltaTime;
            if (glitchTimer >= 0.8f)
            {
                glitchTimer = 0f;
                TriggerGlitch(1f, 0.25f);
            }

            HandleAudioGlitch(0.5f, 1.5f);
        }
        else
        {
            if (currentState == GameState.Confusion)
            {
                baseDistortionWeight = 0.6f;
            }
            else if (currentState == GameState.Dream)
            {
                // Memberikan efek vignette dan distorsi sangat ringan untuk nuansa "Mimpi"
                baseDistortionWeight = 0.15f; 
            }
            else
            {
                baseDistortionWeight = 0f;
            }
        }

        debugFinalDistortionWeight = Mathf.Clamp01(baseDistortionWeight + glitchDistortionWeight);
    }

    private void HandleAudioGlitch(float minInterval, float maxInterval)
    {
        if (glitchSounds == null || glitchSounds.Length == 0 || audioSource == null) return;

        audioTimer += Time.deltaTime;
        if (audioTimer >= nextAudioInterval)
        {
            audioTimer = 0f;
            nextAudioInterval = Random.Range(minInterval, maxInterval);

            AudioClip clip = glitchSounds[Random.Range(0, glitchSounds.Length)];
            if (clip != null)
            {
                audioSource.pitch = Random.Range(0.7f, 1.3f);
                audioSource.PlayOneShot(clip, Random.Range(0.6f, 1f));
            }
        }
    }

    private void ApplyDistortionEffects()
    {
        float finalWeight = debugFinalDistortionWeight;

        if (lensDistortion != null)
        {
            lensDistortion.intensity.overrideState = true;
            lensDistortion.intensity.value = Mathf.Lerp(
                lensDistortion.intensity.value, 
                finalWeight * maxLensDistortion, 
                Time.deltaTime * transitionSpeed);
        }

        if (vignette != null)
        {
            vignette.intensity.overrideState = true;
            vignette.intensity.value = Mathf.Lerp(
                vignette.intensity.value, 
                finalWeight * maxVignetteIntensity, 
                Time.deltaTime * transitionSpeed);
        }

        if (chromaticAberration != null)
        {
            chromaticAberration.intensity.overrideState = true;
            chromaticAberration.intensity.value = Mathf.Lerp(
                chromaticAberration.intensity.value, 
                finalWeight * maxChromaticAberration, 
                Time.deltaTime * transitionSpeed);
        }
    }

    private void OnDestroy()
    {
        if (phaseManager != null)
        {
            phaseManager.OnPhaseChanged.RemoveListener(HandlePhaseChanged);
        }
    }
}
