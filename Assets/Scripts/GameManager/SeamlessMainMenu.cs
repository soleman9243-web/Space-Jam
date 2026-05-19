using UnityEngine;
using UnityEngine.UI;

public class SeamlessMainMenu : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("Masukkan Panel/Canvas UI Main Menu di sini")]
    public GameObject mainMenuUI;
    
    [Header("Player Dependencies")]
    [Tooltip("Masukkan script PlayerMovement karakter kamu")]
    public PlayerMovement playerMovement;

    [Tooltip("Set true jika ingin seluruh waktu game berhenti (Time.timeScale = 0) saat di menu. Set false jika ingin animasi seperti nafas/rumput tetap bergerak.")]
    public bool freezeTime = false;

    private void Start()
    {
        // 1. Tampilkan Main Menu UI
        if (mainMenuUI != null)
        {
            mainMenuUI.SetActive(true);
        }
        
        // 2. Kunci Player agar tidak bisa bergerak
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        // 3. Pause game jika freezeTime diaktifkan (misal agar musuh tidak bergerak/menyerang)
        if (freezeTime)
        {
            Time.timeScale = 0f;
        }
    }

    public void StartGame()
    {
        // 1. Sembunyikan UI Main Menu
        if (mainMenuUI != null)
        {
            mainMenuUI.SetActive(false);
        }
        
        // 2. Kembalikan kontrol Player
        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }

        // 3. Kembalikan waktu normal jika sebelumnya di-freeze
        if (freezeTime)
        {
            Time.timeScale = 1f;
        }

        Debug.Log("Seamless Start: Game dimulai tanpa loading!");
    }
}
