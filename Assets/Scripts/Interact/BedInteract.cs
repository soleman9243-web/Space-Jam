using System.Collections;
using UnityEngine;

public class BedInteract : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Posisi di mana karakter akan terbangun (misal: di sebelah kasur).")]
    public Transform wakeUpPosition;
    [Tooltip("Referensi ke PhaseLoopManager. Akan dicari otomatis jika kosong.")]
    public PhaseLoopManager phaseManager;

    private bool hasSlept = false;

    // Fungsi ini bisa dipanggil dari UnityEvent di InteractObject2D
    public void SleepOnBed()
    {
        if (hasSlept) return; // Mencegah spam klik yang bikin nge-bug
        
        hasSlept = true;

<<<<<<< Updated upstream
        // Minta PhaseLoopManager untuk pindah ke fase Dream SECARA INSTAN
        if (phaseManager == null) phaseManager = FindObjectOfType<PhaseLoopManager>();
        if (phaseManager != null)
        {
            phaseManager.StartManualTransition(GameState.Dream, wakeUpPosition);
        }
        else
        {
            Debug.LogError("PhaseLoopManager tidak ditemukan di scene!");
=======
        // hasSlept = true;

        // 1. Pindahkan posisi dan rotasi player agar tiduran mengikuti arah kasur
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.transform.position = sleepPosition != null ? sleepPosition.position : transform.position;
            
            // Samakan rotasi karakter dengan rotasi titik tidur (sleepPosition)
            player.transform.rotation = sleepPosition != null ? sleepPosition.rotation : transform.rotation;

            // Nonaktifkan movement
            var movement = player.GetComponent<PlayerMovement>();
            if (movement != null)
            {
                movement.enabled = false;
            }

            // Stop velocity
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
            }

            // Matikan collider sementara supaya tidak mental dari kasur
            var collider = player.GetComponent<Collider2D>();
            if (collider != null)
            {
                collider.enabled = false;
            }
        }

        // 2. Tentukan target fase
        if (phaseManager == null)
        {
            phaseManager = FindObjectOfType<PhaseLoopManager>();
        }

        if (phaseManager != null)
        {
            GameState targetState = GameState.Dream;

            // Jika sekarang sudah di Dream, balik ke Awake
            if (phaseManager.CurrentState == GameState.Dream)
            {
                targetState = GameState.Awake;
            }

            phaseManager.StartManualTransition(targetState, sleepDuration, wakeUpPosition);
>>>>>>> Stashed changes
        }
    }
}
