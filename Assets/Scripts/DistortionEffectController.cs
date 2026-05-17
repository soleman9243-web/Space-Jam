using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DistortionEffectController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The Global Volume containing Post-Processing overrides.")]
    public Volume globalVolume;
    [Tooltip("Reference to the Phase Loop Manager to listen to state changes.")]
    public PhaseLoopManager phaseManager;

    [Header("Distortion Settings")]
    public float maxLensDistortion = -0.5f;
    public float maxVignetteIntensity = 0.5f;
    public float maxChromaticAberration = 1f;
    
    [Tooltip("How fast the effects blend in and out.")]
    public float transitionSpeed = 2f;

    // Post-Processing overrides
    private LensDistortion lensDistortion;
    private Vignette vignette;
    private ChromaticAberration chromaticAberration;

    // Value between 0 (no effect) and 1 (max effect)
    private float targetDistortionWeight = 0f;

    private void Start()
    {
        // Grab the overrides from the Volume Profile
        if (globalVolume != null && globalVolume.profile != null)
        {
            globalVolume.profile.TryGet(out lensDistortion);
            globalVolume.profile.TryGet(out vignette);
            globalVolume.profile.TryGet(out chromaticAberration);
        }

        // Subscribe to phase change events (optional, for immediate spikes if desired)
        if (phaseManager != null)
        {
            phaseManager.OnPhaseChanged.AddListener(HandlePhaseChanged);
        }
    }

    private void HandlePhaseChanged(GameState newState)
    {
        // E.g. Spike distortion instantly on confusion
        if (newState == GameState.Confusion)
        {
            // Forces instant distortion, which will then smooth out or hold depending on Update logic
            // (Leaving this here as an example if you want immediate jumps on state change)
        }
    }

    private void Update()
    {
        if (globalVolume == null) return;

        CalculateTargetDistortion();
        ApplyDistortionEffects();
    }

    private void CalculateTargetDistortion()
    {
        float stability = GameManager.Instance != null ? GameManager.Instance.currentStability : 100f;
        GameState currentState = phaseManager != null ? phaseManager.CurrentState : GameState.Awake;

        // Condition 1: High distortion when in Confusion Phase
        if (currentState == GameState.Confusion)
        {
            targetDistortionWeight = 1f;
        }
        // Condition 2: Dynamic distortion when stability drops below 30%
        else if (stability < 30f)
        {
            // Maps 30% -> 0% stability to 0.0 -> 1.0 weight
            targetDistortionWeight = 1f - (stability / 30f);
        }
        else
        {
            targetDistortionWeight = 0f; // Stable
        }
    }

    private void ApplyDistortionEffects()
    {
        // Smoothly lerp towards target intensity for all effects
        if (lensDistortion != null)
        {
            lensDistortion.intensity.value = Mathf.Lerp(
                lensDistortion.intensity.value, 
                targetDistortionWeight * maxLensDistortion, 
                Time.deltaTime * transitionSpeed);
        }

        if (vignette != null)
        {
            vignette.intensity.value = Mathf.Lerp(
                vignette.intensity.value, 
                targetDistortionWeight * maxVignetteIntensity, 
                Time.deltaTime * transitionSpeed);
        }

        if (chromaticAberration != null)
        {
            chromaticAberration.intensity.value = Mathf.Lerp(
                chromaticAberration.intensity.value, 
                targetDistortionWeight * maxChromaticAberration, 
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
