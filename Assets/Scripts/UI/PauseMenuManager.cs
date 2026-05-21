using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    [Tooltip("Panel UI keseluruhan untuk Pause Menu")]
    public GameObject pausePanel; 
    
    [Header("Menu Buttons (Images)")]
    [Tooltip("Isi dengan gambar tombol. Index 0 = Resume, Index 1 = Main Menu")]
    public Image[] menuButtons; 
    public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow;
    
    [Header("Settings")]
    public string mainMenuSceneName = "MainMenu";
    
    private bool isPaused = false;
    private int selectedIndex = 0;
    
    void Start()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }
    
    void Update()
    {
        // Toggle Pause dengan ESC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }
        
        if (!isPaused) return;
        
        // Navigasi W / S
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            selectedIndex--;
            if (selectedIndex < 0) selectedIndex = menuButtons.Length - 1;
            UpdateSelectionUI();
        }
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            selectedIndex++;
            if (selectedIndex >= menuButtons.Length) selectedIndex = 0;
            UpdateSelectionUI();
        }
        
        // Accept dengan F
        if (Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Return))
        {
            ExecuteAction(selectedIndex);
        }
    }
    
    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }
        
        selectedIndex = 0;
        UpdateSelectionUI();
    }
    
    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }
    
    private void UpdateSelectionUI()
    {
        if (menuButtons == null || menuButtons.Length == 0) return;
        
        for (int i = 0; i < menuButtons.Length; i++)
        {
            if (menuButtons[i] != null)
            {
                if (i == selectedIndex)
                {
                    menuButtons[i].color = selectedColor; // Ubah warna
                    menuButtons[i].transform.localScale = new Vector3(1.1f, 1.1f, 1f); // Sedikit membesar
                }
                else
                {
                    menuButtons[i].color = normalColor; // Kembali ke normal
                    menuButtons[i].transform.localScale = Vector3.one; // Kembali ukuran asli
                }
            }
        }
    }
    
    private void ExecuteAction(int index)
    {
        if (index == 0) // Tombol Resume
        {
            ResumeGame();
        }
        else if (index == 1) // Tombol Main Menu
        {
            // Kembalikan waktu normal
            Time.timeScale = 1f; 
            
            // Hancurkan manager yang DontDestroyOnLoad agar game benar-benar ter-reset ke 0
            PhaseLoopManager phaseLoop = FindObjectOfType<PhaseLoopManager>();
            if (phaseLoop != null) Destroy(phaseLoop.gameObject);
            
            GameManager gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null) Destroy(gameManager.gameObject);

            AudioManager audioManager = FindObjectOfType<AudioManager>();
            if (audioManager != null) Destroy(audioManager.gameObject);
            
            // Load ulang scene saat ini (Hard Reset)
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
