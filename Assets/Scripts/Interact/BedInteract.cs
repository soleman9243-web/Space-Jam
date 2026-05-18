using System.Collections;
using UnityEngine;

public class BedInteract : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Posisi di mana karakter akan tertidur (di atas kasur).")]
    public Transform sleepPosition;
    [Tooltip("Posisi di mana karakter akan bangun (di samping kasur). Biarkan kosong jika bangun di tempat yang sama.")]
    public Transform wakeUpPosition;
    [Tooltip("Referensi ke PhaseLoopManager. Akan dicari otomatis jika kosong.")]
    public PhaseLoopManager phaseManager;

    [Header("Settings")]
    [Tooltip("Berapa lama layar akan hitam gelap sebelum bangun di dunia mimpi.")]
    public float sleepDuration = 3f;

    private bool hasSlept = false;

    // Fungsi ini bisa dipanggil dari UnityEvent di InteractObject2D
    public void SleepOnBed()
    {
        // if (hasSlept) return; // Saya matikan sementara agar kamu bisa tes berkali-kali
        
        // Asumsi quest sudah selesai (kamu bisa tambahkan pengecekan quest di sini nanti)
        // if (!QuestManager.Instance.IsAllQuestsDone) return;

        // hasSlept = true;

        // 1. Pindahkan posisi dan rotasi player agar tiduran mengikuti arah kasur
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.transform.position = sleepPosition != null ? sleepPosition.position : transform.position;
            
            // Samakan rotasi karakter dengan rotasi titik tidur (sleepPosition)
            player.transform.rotation = sleepPosition != null ? sleepPosition.rotation : transform.rotation;
            
            // Nonaktifkan script pergerakan agar beneran 'diem'
            var movement = player.GetComponent<PlayerMovement>();
            if (movement != null) movement.enabled = false;

            // Stop sisa gaya geser/gerak (Velocity) jika ada
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.velocity = Vector2.zero;
        }

        // 2. Minta PhaseLoopManager untuk pindah ke fase Dream dengan durasi tidur
        if (phaseManager == null) phaseManager = FindObjectOfType<PhaseLoopManager>();
        if (phaseManager != null)
        {
            // Panggil transisi dengan jeda waktu layar hitam dan posisi bangun
            phaseManager.StartManualTransition(GameState.Dream, sleepDuration, wakeUpPosition);
        }
        else
        {
            Debug.LogError("PhaseLoopManager tidak ditemukan di scene!");
        }
    }
}
