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

        // Minta PhaseLoopManager untuk pindah ke fase Dream SECARA INSTAN
        if (phaseManager == null) phaseManager = FindObjectOfType<PhaseLoopManager>();
        if (phaseManager != null)
        {
            phaseManager.StartManualTransition(GameState.Dream, wakeUpPosition);
        }
        else
        {
            Debug.LogError("PhaseLoopManager tidak ditemukan di scene!");
        }
    }
}
