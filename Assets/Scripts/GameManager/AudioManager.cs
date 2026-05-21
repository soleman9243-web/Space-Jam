using UnityEngine;
using System.Collections.Generic;

public enum BGMType
{
    None,
    Awake,
    Dream,
    Liminal
}

public enum SFXType
{
    Footstep,
    Interact,
    Glitch,
    WakeUp,
    TaskUpdate
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource footstepSource;

    [Header("BGM Clips")]
    [SerializeField] private AudioClip bgmAwake;
    [SerializeField] private AudioClip bgmDream;
    [SerializeField] private AudioClip bgmLiminal;

    [Header("SFX Clips")]
    [SerializeField] private AudioClip sfxInteract;
    [SerializeField] private AudioClip sfxGlitch;
    [SerializeField] private AudioClip sfxWakeUp;
    [SerializeField] private AudioClip sfxTaskUpdate;
    [SerializeField] private AudioClip sfxFootstep;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ================== BGM METHODS ================== //

    public void PlayBGM(BGMType type)
    {
        if (bgmSource == null) return;

        AudioClip clipToPlay = null;

        switch (type)
        {
            case BGMType.Awake:
                clipToPlay = bgmAwake;
                break;
            case BGMType.Dream:
                clipToPlay = bgmDream;
                break;
            case BGMType.Liminal:
                clipToPlay = bgmLiminal;
                break;
        }

        if (clipToPlay == null)
        {
            bgmSource.Stop();
            return;
        }

        if (bgmSource.clip == clipToPlay && bgmSource.isPlaying)
        {
            return; // Sedang diputar
        }

        bgmSource.clip = clipToPlay;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        if (bgmSource != null)
        {
            bgmSource.Stop();
        }
    }

    // ================== SFX METHODS ================== //

    public void PlaySFX(SFXType type)
    {
        if (sfxSource == null) return;

        AudioClip clipToPlay = null;

        switch (type)
        {
            case SFXType.Interact:
                clipToPlay = sfxInteract;
                break;
            case SFXType.Glitch:
                clipToPlay = sfxGlitch;
                break;
            case SFXType.WakeUp:
                clipToPlay = sfxWakeUp;
                break;
            case SFXType.TaskUpdate:
                clipToPlay = sfxTaskUpdate;
                break;
            case SFXType.Footstep:
                // Footstep dipisah logic-nya karena loop
                SetFootstep(true);
                return;
        }

        if (clipToPlay != null)
        {
            sfxSource.PlayOneShot(clipToPlay);
        }
    }

    public void SetFootstep(bool isWalking)
    {
        if (footstepSource == null || sfxFootstep == null) return;

        if (isWalking)
        {
            if (!footstepSource.isPlaying)
            {
                footstepSource.clip = sfxFootstep;
                footstepSource.loop = true;
                footstepSource.Play();
            }
        }
        else
        {
            if (footstepSource.isPlaying)
            {
                footstepSource.Pause();
            }
        }
    }
}
