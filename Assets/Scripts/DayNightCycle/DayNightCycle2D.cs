using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;

namespace SpaceJam.Environment
{
    public enum DayPhase
    {
        Morning,
        Afternoon,
        Night
    }

    [ExecuteAlways]
    [SelectionBase]
    [DisallowMultipleComponent]
    public class DayNightCycle2D : MonoBehaviour
    {
        [Header("Cycle Configuration")]
        [Tooltip("Duration of a full day-night cycle in real-world seconds.")]
        [SerializeField] private float cycleDuration = 60f;
        
        [Tooltip("Current time of day represented as a value between 0.0 (midnight) and 1.0 (next midnight).")]
        [Range(0f, 1f)]
        [SerializeField] private float timeOfDay = 0.35f; // Starts at 6 AM (0.25)
        
        [Tooltip("If true, the cycle will advance automatically over time.")]
        [SerializeField] private bool autoAdvance = true;
        
        [Tooltip("Multiply time progression speed (e.g. 2 for twice as fast).")]
        [SerializeField] private float speedMultiplier = 1f;

        [Header("Phase Transitions (0.0 to 1.0)")]
        [Range(0f, 1f)] [SerializeField] private float morningStart = 0.22f;   // Approx 5:15 AM
        [Range(0f, 1f)] [SerializeField] private float afternoonStart = 0.62f; // Approx 3:00 PM
        [Range(0f, 1f)] [SerializeField] private float nightStart = 0.77f;     // Approx 6:30 PM

        [Header("Target Lights")]
        [Tooltip("The main 2D global light used to set the ambient tone of the environment.")]
        [SerializeField] private Light2D globalLight;
        
        [Tooltip("A directional, sprite, or parametric light representing light coming through a window.")]
        [SerializeField] private Light2D windowLight;

        [Header("Ambient Lighting Presets")]
        [SerializeField] private Gradient globalColorGradient;
        [SerializeField] private AnimationCurve globalIntensityCurve;

        [Header("Window Lighting Presets")]
        [SerializeField] private Gradient windowColorGradient;
        [SerializeField] private AnimationCurve windowIntensityCurve;
        
        [Tooltip("Controls the Z-rotation angle of the Window Light GameObject based on the time of day.")]
        [SerializeField] private AnimationCurve windowRotationCurve;

        [Header("Secondary/Indoor Lights")]
        [Tooltip("Indoor lights (lamps, monitors, signs) that should turn ON at night and OFF during the day.")]
        [SerializeField] private Light2D[] secondaryLights;
        [Tooltip("Defines how secondary lights fade in/out based on time of day.")]
        [SerializeField] private AnimationCurve secondaryLightsIntensityCurve;
        [Tooltip("Maximum intensity multiplier for secondary lights.")]
        [SerializeField] private float secondaryMaxIntensity = 1f;

        [Header("Events & Callbacks")]
        public UnityEvent<DayPhase> OnPhaseChanged;
        public UnityEvent<float> OnTimeUpdated; // Passes normalized time (0.0 - 1.0)

        // Runtime status
        private DayPhase currentPhase = DayPhase.Morning;
        private DayPhase lastFiredPhase = DayPhase.Morning;

        // Getters/Setters
        public float TimeOfDay
        {
            get => timeOfDay;
            set => timeOfDay = Mathf.Clamp01(value);
        }

        public DayPhase CurrentPhase => currentPhase;
        public float CycleDuration { get => cycleDuration; set => cycleDuration = Mathf.Max(0.1f, value); }
        public bool AutoAdvance { get => autoAdvance; set => autoAdvance = value; }

        private void Reset()
        {
            InitializePresets();
        }

        private void Start()
        {
            if (globalColorGradient == null || globalColorGradient.colorKeys.Length <= 1)
            {
                InitializePresets();
            }
            
            UpdateLighting(timeOfDay);
            UpdatePhase(timeOfDay, true);
        }

        private void Update()
        {
            if (Application.isPlaying && autoAdvance)
            {
                float newTime = timeOfDay + (Time.deltaTime / cycleDuration) * speedMultiplier;

                // Tahan waktu agar tidak ganti fase jika quest di fase sekarang belum selesai
                PhaseTaskManager ptm = FindObjectOfType<PhaseTaskManager>();
                if (ptm != null && !ptm.AreAllCurrentTasksCompleted())
                {
                    // Tahan sore sebelum malam
                    if (timeOfDay < nightStart && newTime >= nightStart)
                    {
                        newTime = nightStart - 0.0001f;
                    }
                    // Tahan malam sebelum pagi
                    else if (timeOfDay < morningStart && newTime >= morningStart)
                    {
                        newTime = morningStart - 0.0001f;
                    }
                }

                // Loop around when reaching 1.0
                if (newTime >= 1f)
                {
                    newTime -= 1f;
                }

                timeOfDay = newTime;
            }

            // Sync lights and phase states
            UpdateLighting(timeOfDay);
            UpdatePhase(timeOfDay, false);

            // Handle testing inputs in PlayMode
            if (Application.isPlaying)
            {
                //HandleDebugInputs();
            }
        }

        private void OnValidate()
        {
            // Allows real-time previewing in the scene view when dragging the slider in inspector
            UpdateLighting(timeOfDay);
            UpdatePhase(timeOfDay, true);
        }

        private void UpdateLighting(float time)
        {
            // 1. Update Ambient Global Light
            if (globalLight != null)
            {
                globalLight.color = globalColorGradient.Evaluate(time);
                globalLight.intensity = globalIntensityCurve.Evaluate(time);
            }

            // 2. Update Window Light
            if (windowLight != null)
            {
                windowLight.color = windowColorGradient.Evaluate(time);
                windowLight.intensity = windowIntensityCurve.Evaluate(time);

                // Sweep the rotation (Z-axis) of the window light to simulate sun movement
                float zRotation = windowRotationCurve.Evaluate(time);
                windowLight.transform.rotation = Quaternion.Euler(0f, 0f, zRotation);
            }

            // 3. Update Secondary Lights
            if (secondaryLights != null && secondaryLights.Length > 0)
            {
                float intensityMultiplier = secondaryLightsIntensityCurve.Evaluate(time);
                float finalIntensity = intensityMultiplier * secondaryMaxIntensity;

                foreach (var light in secondaryLights)
                {
                    if (light != null)
                    {
                        light.intensity = finalIntensity;
                        
                        // Optimize rendering: disable light completely if intensity is 0
                        light.enabled = finalIntensity > 0.001f;
                    }
                }
            }
        }

        private void UpdatePhase(float time, bool forceUpdate)
        {
            DayPhase newPhase;

            if (time >= nightStart || time < morningStart)
            {
                newPhase = DayPhase.Night;
            }
            else if (time >= afternoonStart)
            {
                newPhase = DayPhase.Afternoon;
            }
            else
            {
                newPhase = DayPhase.Morning;
            }

            currentPhase = newPhase;

            // Trigger events if phase changed or forced (like on initialization)
            if (currentPhase != lastFiredPhase || forceUpdate)
            {
                lastFiredPhase = currentPhase;
                OnPhaseChanged?.Invoke(currentPhase);
                Debug.Log($"[DayNightCycle2D] Transitioned to Phase: {currentPhase}");
            }

            OnTimeUpdated?.Invoke(time);
        }

        private void HandleDebugInputs()
        {
            // Convenient keyboard hotkeys for rapid testing/debugging in-game
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                SetTime(0.35f); // Jump to Mid-Morning
                Debug.Log("[DayNightCycle2D] Keyboard Hotkey: Jumped to Morning!");
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                SetTime(0.70f); // Jump to Mid-Afternoon (Sunset)
                Debug.Log("[DayNightCycle2D] Keyboard Hotkey: Jumped to Afternoon!");
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                SetTime(0.85f); // Jump to Night
                Debug.Log("[DayNightCycle2D] Keyboard Hotkey: Jumped to Night!");
            }
            else if (Input.GetKeyDown(KeyCode.P))
            {
                autoAdvance = !autoAdvance;
                Debug.Log($"[DayNightCycle2D] Cycle Auto Advance: {autoAdvance}");
            }
        }

        /// <summary>
        /// Explicitly jump to a specific time of day (0.0 to 1.0) with smooth transition.
        /// </summary>
        public void SetTime(float targetTime)
        {
            timeOfDay = Mathf.Clamp01(targetTime);
            UpdateLighting(timeOfDay);
            UpdatePhase(timeOfDay, true);
        }

        /// <summary>
        /// Instantly sets the cycle to Morning phase.
        /// </summary>
        [ContextMenu("Debug: Set Morning")]
        public void SetMorning()
        {
            SetTime((morningStart + afternoonStart) * 0.5f);
        }

        /// <summary>
        /// Instantly sets the cycle to Afternoon phase.
        /// </summary>
        [ContextMenu("Debug: Set Afternoon")]
        public void SetAfternoon()
        {
            SetTime((afternoonStart + nightStart) * 0.5f);
        }

        /// <summary>
        /// Instantly sets the cycle to Night phase.
        /// </summary>
        [ContextMenu("Debug: Set Night")]
        public void SetNight()
        {
            SetTime((nightStart + 1.0f) * 0.5f);
        }

        /// <summary>
        /// Populates default gradients and curves that look beautiful and dynamic out-of-the-box.
        /// </summary>
        [ContextMenu("Reset to Beautiful Presets")]
        public void InitializePresets()
        {
            // 1. Setup Ambient Global Light Colors
            globalColorGradient = new Gradient();
            globalColorGradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(0.04f, 0.06f, 0.15f), 0.00f), // 12 AM (Midnight dark deep blue)
                    new GradientColorKey(new Color(0.06f, 0.08f, 0.20f), 0.20f), // 5 AM (Pre-dawn twilight)
                    new GradientColorKey(new Color(0.95f, 0.85f, 0.70f), 0.28f), // 6:45 AM (Soft golden sunrise)
                    new GradientColorKey(new Color(0.98f, 0.98f, 0.95f), 0.50f), // 12 PM (Bright clean day light)
                    new GradientColorKey(new Color(0.90f, 0.45f, 0.25f), 0.70f), // 5 PM (Warm sunset amber)
                    new GradientColorKey(new Color(0.25f, 0.15f, 0.35f), 0.78f), // 6:45 PM (Dusk deep violet)
                    new GradientColorKey(new Color(0.04f, 0.06f, 0.15f), 1.00f)  // 12 AM (Midnight loop)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1.0f, 0.0f),
                    new GradientAlphaKey(1.0f, 1.0f)
                }
            );

            // Ambient Global Intensity Curve
            globalIntensityCurve = new AnimationCurve();
            globalIntensityCurve.AddKey(new Keyframe(0.00f, 0.30f)); // Midnight (low ambient light - raised from 0.15f for better gameplay visibility)
            globalIntensityCurve.AddKey(new Keyframe(0.20f, 0.30f)); // Dawn start (raised from 0.15f)
            globalIntensityCurve.AddKey(new Keyframe(0.35f, 0.90f)); // Mid-morning peak
            globalIntensityCurve.AddKey(new Keyframe(0.50f, 1.00f)); // Noon peak
            globalIntensityCurve.AddKey(new Keyframe(0.68f, 0.75f)); // Sunset golden intensity
            globalIntensityCurve.AddKey(new Keyframe(0.77f, 0.35f)); // Dusk drop (raised from 0.20f)
            globalIntensityCurve.AddKey(new Keyframe(1.00f, 0.30f)); // Midnight (raised from 0.15f)

            // Ensure smooth tangent interpolations
            for (int i = 0; i < globalIntensityCurve.length; i++)
            {
                globalIntensityCurve.SmoothTangents(i, 0f);
            }

            // 2. Setup Window Light Colors
            windowColorGradient = new Gradient();
            windowColorGradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(0.35f, 0.50f, 0.85f), 0.00f), // Midnight (soft pale blue moonlight)
                    new GradientColorKey(new Color(1.00f, 0.92f, 0.75f), 0.28f), // Morning (warm gold beam)
                    new GradientColorKey(new Color(1.00f, 0.98f, 0.92f), 0.50f), // Midday (crisp, bright white sunray)
                    new GradientColorKey(new Color(0.95f, 0.35f, 0.10f), 0.70f), // Sunset (intense reddish-orange)
                    new GradientColorKey(new Color(0.50f, 0.30f, 0.70f), 0.78f), // Dusk (fading purple)
                    new GradientColorKey(new Color(0.35f, 0.50f, 0.85f), 1.00f)  // Midnight
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1.0f, 0.0f),
                    new GradientAlphaKey(1.0f, 1.0f)
                }
            );

            // Window Beam Intensity Curve
            windowIntensityCurve = new AnimationCurve();
            windowIntensityCurve.AddKey(new Keyframe(0.00f, 0.15f)); // Midnight (faint moonlight beam)
            windowIntensityCurve.AddKey(new Keyframe(0.20f, 0.00f)); // Fade completely right before dawn
            windowIntensityCurve.AddKey(new Keyframe(0.32f, 1.20f)); // Morning sunlight peak
            windowIntensityCurve.AddKey(new Keyframe(0.50f, 0.60f)); // Overhead noon (usually window doesn't catch direct rays, so lower)
            windowIntensityCurve.AddKey(new Keyframe(0.68f, 1.80f)); // Low-angle sunset (intense, dramatic long beam)
            windowIntensityCurve.AddKey(new Keyframe(0.77f, 0.00f)); // Sunset fades out
            windowIntensityCurve.AddKey(new Keyframe(0.85f, 0.25f)); // Moonlight beams emerge
            windowIntensityCurve.AddKey(new Keyframe(1.00f, 0.15f));

            for (int i = 0; i < windowIntensityCurve.length; i++)
            {
                windowIntensityCurve.SmoothTangents(i, 0f);
            }

            // Window Z-Rotation Angle Curve (Sun movement sweep)
            // Rotates the light beam from pointing right-down (-55 degrees) to left-down (55 degrees)
            windowRotationCurve = new AnimationCurve();
            windowRotationCurve.AddKey(new Keyframe(0.00f, 0.00f));   // Night (moon stationary or straight)
            windowRotationCurve.AddKey(new Keyframe(0.20f, -55.0f));  // Morning starts: beam points from left to right (rotated -55)
            windowRotationCurve.AddKey(new Keyframe(0.50f, 0.00f));   // Midday: beam points straight down (rotated 0)
            windowRotationCurve.AddKey(new Keyframe(0.78f, 55.0f));   // Afternoon/Sunset: beam points from right to left (rotated +55)
            windowRotationCurve.AddKey(new Keyframe(0.82f, 0.00f));   // Resets to moon angle
            windowRotationCurve.AddKey(new Keyframe(1.00f, 0.00f));

            for (int i = 0; i < windowRotationCurve.length; i++)
            {
                windowRotationCurve.SmoothTangents(i, 0f);
            }

            // 3. Setup Secondary/Indoor Light Intensity Curve
            // Turn lights ON at Night, and smoothly fade them OFF at Day break
            secondaryLightsIntensityCurve = new AnimationCurve();
            secondaryLightsIntensityCurve.AddKey(new Keyframe(0.00f, 1.00f)); // Night: Full ON
            secondaryLightsIntensityCurve.AddKey(new Keyframe(0.22f, 0.80f)); // Morning start: still slightly on
            secondaryLightsIntensityCurve.AddKey(new Keyframe(0.28f, 0.00f)); // Day: Fade to zero
            secondaryLightsIntensityCurve.AddKey(new Keyframe(0.70f, 0.00f)); // Day: Remains zero
            secondaryLightsIntensityCurve.AddKey(new Keyframe(0.77f, 0.60f)); // Sunset/Dusk: Fades ON
            secondaryLightsIntensityCurve.AddKey(new Keyframe(0.82f, 1.00f)); // Night: Full ON
            secondaryLightsIntensityCurve.AddKey(new Keyframe(1.00f, 1.00f));

            for (int i = 0; i < secondaryLightsIntensityCurve.length; i++)
            {
                secondaryLightsIntensityCurve.SmoothTangents(i, 0f);
            }
        }
    }
}
