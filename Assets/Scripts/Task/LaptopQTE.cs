using UnityEngine;
using TMPro; // Membutuhkan TextMeshPro
using UnityEngine.UI; // Membutuhkan UI untuk Slider
using System.Collections.Generic;
using System.Collections;

public class LaptopQTE : MonoBehaviour
{
    [Header("Difficulty Settings")]
    [Tooltip("Jumlah tombol yang harus ditekan")]
    public int sequenceLength = 6;
    [Tooltip("Waktu maksimal (detik) untuk menyelesaikan urutan")]
    public float timeLimit = 5f;
    [Tooltip("Pengurangan stability saat gagal")]
    public float stabilityPenalty = -15f;

    [Header("UI References")]
    [Tooltip("Canvas/Panel keseluruhan untuk Laptop UI")]
    public GameObject laptopUIPanel;
    [Tooltip("Teks untuk menampilkan deretan huruf. Wajib TextMeshPro")]
    public TextMeshProUGUI sequenceText;
    [Tooltip("Slider UI untuk menampilkan sisa waktu")]
    public Slider timerSlider;
    
    [Header("Overload/Fail UI")]
    [Tooltip("Panel layar rusak/merah saat gagal")]
    public GameObject overloadPanel;
    [Tooltip("Teks tulisan 'SYSTEM OVERLOAD' yang akan berkedip")]
    public TextMeshProUGUI overloadText; 

    [Header("Player Reference")]
    public PlayerMovement playerMovement;

    [Header("Feedback Settings")]
    [Tooltip("Komponen AudioSource untuk suara (bisa tambahkan di objek yang sama)")]
    public AudioSource audioSource;
    [Tooltip("Suara yang dimainkan saat salah pencet")]
    public AudioClip errorSound;

    [Header("Random QTE (Fase 2)")]
    [Tooltip("Jika dicentang, QTE akan terpicu secara acak khusus di Fase 2 (Dream)")]
    public bool enableRandomTriggers = true;
    [Tooltip("Waktu minimum jeda antar pemicuan acak (dalam detik)")]
    public float minTimeBetweenTriggers = 10f;
    [Tooltip("Waktu maksimum jeda antar pemicuan acak (dalam detik)")]
    public float maxTimeBetweenTriggers = 25f;

    private PhaseLoopManager phaseLoopManager;
    private Coroutine randomQTECoroutine;

    private List<KeyCode> currentSequence = new List<KeyCode>();
    private int currentKeyIndex = 0;
    private float timeRemaining;
    private bool isMinigameActive = false;
    private bool isOverloading = false;

    // Daftar semua huruf dari A-Z untuk diacak
    private KeyCode[] possibleKeys = {
        KeyCode.A, KeyCode.B, KeyCode.C, KeyCode.D, KeyCode.E, KeyCode.F, 
        KeyCode.G, KeyCode.H, KeyCode.I, KeyCode.J, KeyCode.K, KeyCode.L, 
        KeyCode.M, KeyCode.N, KeyCode.O, KeyCode.P, KeyCode.Q, KeyCode.R, 
        KeyCode.S, KeyCode.T, KeyCode.U, KeyCode.V, KeyCode.W, KeyCode.X, 
        KeyCode.Y, KeyCode.Z
    };

    void Start()
    {
        // Otomatis matikan UI saat game baru mulai
        if (laptopUIPanel != null) laptopUIPanel.SetActive(false);
        if (overloadPanel != null) overloadPanel.SetActive(false);

        // Cari PhaseLoopManager di scene
        phaseLoopManager = FindObjectOfType<PhaseLoopManager>();
        if (phaseLoopManager != null)
        {
            phaseLoopManager.OnPhaseChanged.AddListener(OnPhaseChanged);
        }
    }

    private void OnDisable()
    {
        if (phaseLoopManager != null)
        {
            phaseLoopManager.OnPhaseChanged.RemoveListener(OnPhaseChanged);
        }
    }

    private void OnPhaseChanged(GameState newState)
    {
        if (!enableRandomTriggers) return;

        if (newState == GameState.Dream)
        {
            if (randomQTECoroutine != null) StopCoroutine(randomQTECoroutine);
            randomQTECoroutine = StartCoroutine(RandomQTERoutine());
        }
        else
        {
            if (randomQTECoroutine != null)
            {
                StopCoroutine(randomQTECoroutine);
                randomQTECoroutine = null;
            }
            
            // Tutup QTE jika sedang aktif saat berganti fase
            if (isMinigameActive)
            {
                isMinigameActive = false;
                if (laptopUIPanel != null) laptopUIPanel.SetActive(false);
                if (playerMovement != null) playerMovement.enabled = true;
            }
        }
    }

    private IEnumerator RandomQTERoutine()
    {
        while (phaseLoopManager != null && phaseLoopManager.CurrentState == GameState.Dream)
        {
            float waitTime = Random.Range(minTimeBetweenTriggers, maxTimeBetweenTriggers);
            yield return new WaitForSeconds(waitTime);

            if (phaseLoopManager.CurrentState == GameState.Dream && !isMinigameActive && !isOverloading)
            {
                Debug.Log("[LaptopQTE] Triggering Random QTE in Dream Phase!");
                StartMinigame();
            }
        }
    }

    void Update()
    {
        if (!isMinigameActive || isOverloading) return;

        // Hitung mundur Timer
        timeRemaining -= Time.deltaTime;
        if (timerSlider != null)
        {
            timerSlider.value = timeRemaining;
        }

        // Jika waktu habis
        if (timeRemaining <= 0)
        {
            FailMinigame();
            return;
        }

        // Deteksi input keyboard
        if (Input.anyKeyDown)
        {
            // Cek seluruh tombol keyboard
            foreach (KeyCode vKey in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(vKey))
                {
                    // Abaikan klik mouse agar tidak terhitung sebagai tombol QTE
                    if (vKey == KeyCode.Mouse0 || vKey == KeyCode.Mouse1 || vKey == KeyCode.Mouse2) continue;

                    // Jika tombol yang ditekan SAMA dengan tombol yang diminta saat ini
                    if (vKey == currentSequence[currentKeyIndex])
                    {
                        // Benar! Lanjut ke huruf berikutnya
                        currentKeyIndex++;
                        UpdateSequenceUI();

                        // Jika semua tombol sudah berhasil ditekan
                        if (currentKeyIndex >= currentSequence.Count)
                        {
                            WinMinigame();
                        }
                    }
                    else
                    {
                        // SALAH PENCET! Ulang dari huruf paling awal
                        currentKeyIndex = 0;
                        UpdateSequenceUI();
                        
                        // Mainkan efek suara salah
                        if (audioSource != null && errorSound != null)
                        {
                            audioSource.PlayOneShot(errorSound);
                        }

                        // Berikan efek getaran pada UI
                        StartCoroutine(ShakeUIRoutine());
                    }
                }
            }
        }
    }

    // Fungsi ini dipanggil dari InteractObject2D (Event OnInteract)
    public void StartMinigame()
    {
        if (isMinigameActive || isOverloading) return;

        laptopUIPanel.SetActive(true);
        if (overloadPanel != null) overloadPanel.SetActive(false);
        
        isMinigameActive = true;
        isOverloading = false;
        
        // Kunci player agar tidak bisa gerak saat main QTE
        if (playerMovement != null) playerMovement.enabled = false;

        GenerateSequence();
        timeRemaining = timeLimit;

        if (timerSlider != null)
        {
            timerSlider.maxValue = timeLimit;
            timerSlider.value = timeLimit;
        }
    }

    private void GenerateSequence()
    {
        currentSequence.Clear();
        currentKeyIndex = 0;

        // Acak huruf sebanyak `sequenceLength`
        for (int i = 0; i < sequenceLength; i++)
        {
            currentSequence.Add(possibleKeys[Random.Range(0, possibleKeys.Length)]);
        }

        UpdateSequenceUI();
    }

    private void UpdateSequenceUI()
    {
        if (sequenceText == null) return;

        string displayText = "";
        for (int i = 0; i < currentSequence.Count; i++)
        {
            if (i < currentKeyIndex)
            {
                // Huruf yang SUDAH ditekan: Warna Abu-abu dan dicoret (strikethrough)
                displayText += "<color=#888888><s>" + currentSequence[i].ToString() + "</s></color> ";
            }
            else if (i == currentKeyIndex)
            {
                // Huruf yang HARUS DITEKAN SEKARANG: Warna Hijau dan ditebalkan (bold)
                displayText += "<color=#00FF00><b>" + currentSequence[i].ToString() + "</b></color> ";
            }
            else
            {
                // Huruf sisanya yang belum ditekan: Warna Putih
                displayText += "<color=#FFFFFF>" + currentSequence[i].ToString() + "</color> ";
            }
        }
        sequenceText.text = displayText;
    }

    private void WinMinigame()
    {
        isMinigameActive = false;
        laptopUIPanel.SetActive(false);
        
        // Kembalikan pergerakan player
        if (playerMovement != null) playerMovement.enabled = true;

        Debug.Log("QTE BERHASIL!");
        // Jika ada event / task selesai, panggil di sini
    }

    private void FailMinigame()
    {
        isMinigameActive = false;
        
        // Mulai layar kedip merah rusak
        StartCoroutine(OverloadRoutine());

        // Kurangi stability langsung lewat PlayerStatus
        if (PlayerStatus.Instance != null)
        {
            PlayerStatus.Instance.ReduceStability(Mathf.Abs(stabilityPenalty));
        }
        else if (GameManager.Instance != null)
        {
            GameManager.Instance.ModifyStability(-Mathf.Abs(stabilityPenalty));
        }
    }

    private IEnumerator OverloadRoutine()
    {
        isOverloading = true;
        
        if (overloadPanel != null) overloadPanel.SetActive(true);
        if (overloadText != null) overloadText.text = "SYSTEM OVERLOAD";

        // Durasi layar rusak dipercepat jadi 1.5 detik
        float blinkDuration = 1.5f; 
        float blinkTimer = 0f;
        bool isVisible = true;

        while (blinkTimer < blinkDuration)
        {
            if (overloadText != null)
            {
                overloadText.gameObject.SetActive(isVisible);
                overloadText.color = Color.red;
                isVisible = !isVisible;
            }
            
            // Kedip lebih cepat
            yield return new WaitForSeconds(0.08f);
            blinkTimer += 0.08f;
        }

        if (overloadText != null) overloadText.gameObject.SetActive(true);

        // Setelah 3 detik, matikan laptop dan kembalikan kontrol player
        if (overloadPanel != null) overloadPanel.SetActive(false);
        if (laptopUIPanel != null) laptopUIPanel.SetActive(false);
        
        if (playerMovement != null) playerMovement.enabled = true;
        isOverloading = false;
        
        Debug.Log("QTE GAGAL. Stability berkurang.");
    }

    private IEnumerator ShakeUIRoutine()
    {
        if (laptopUIPanel == null) yield break;

        RectTransform panelRect = laptopUIPanel.GetComponent<RectTransform>();
        if (panelRect == null) yield break;

        Vector2 originalPos = panelRect.anchoredPosition;
        float elapsed = 0f;
        float duration = 0.3f; // Durasi getaran

        while (elapsed < duration)
        {
            float xOffset = Random.Range(-15f, 15f);
            float yOffset = Random.Range(-15f, 15f);

            panelRect.anchoredPosition = originalPos + new Vector2(xOffset, yOffset);
            
            yield return null;
            elapsed += Time.deltaTime;
        }

        // Kembalikan UI ke posisi semula
        panelRect.anchoredPosition = originalPos;
    }
}
