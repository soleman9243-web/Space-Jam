using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Intro Sequence Manager — Mengontrol seluruh intro SampleScene.
/// 
/// Flow:
/// 1. Layar gelap (fog) + "Tap to Play" → tekan key
/// 2. Fog fade out → ruangan terlihat (TIDAK DIUBAH)
/// 3. Ghost MC muncul transparan biru dekat kasur + "Zzz" di kasur
/// 4. MC gerak, deket kasur → outline + "[E] Wake Up" muncul
/// 5. Interact kasur → enemy ghost jalan dari bawah
/// 6. Dialogue 2 paragraf → tekan key
/// 7. Fade to black → masuk Awake phase
/// 
/// PENTING: Script ini TIDAK mengubah object ruangan apapun secara permanen.
///          Hanya overlay UI + spawn ghost + sleeping player body.
///          Dilengkapi runtime Auto-Healing yang sangat kuat untuk mencegah crash jika referensi kosong.
/// </summary>
public class IntroSequenceManager : MonoBehaviour
{
    public enum IntroState
    {
        WaitingToStart,
        FadingIn,
        Exploring,
        GhostAppearing,
        Dialogue,
        TransitionToAwake
    }

    // =====================================================================
    // INSPECTOR FIELDS
    // =====================================================================

    [Header("=== FOG OVERLAY ===")]
    [Tooltip("CanvasGroup pada Image hitam yang menutupi layar.")]
    [SerializeField] private CanvasGroup fogOverlay;

    [Tooltip("Teks 'Tap to Play' yang muncul di layar gelap.")]
    [SerializeField] private TextMeshProUGUI tapToPlayText;

    [Header("=== PLAYER (Ghost MC) ===")]
    [SerializeField] private GameObject playerObject;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private SpriteRenderer playerSpriteRenderer;
    [SerializeField] private Transform spawnNearBed;

    [Header("Ghost Appearance")]
    [SerializeField] private Color ghostColor = new Color(0.3f, 0.55f, 1f, 0.45f);

    [Header("=== BED (KASUR) ===")]
    [Tooltip("Transform kasur di scene. TIDAK AKAN DIUBAH.")]
    [SerializeField] private Transform bedTransform;

    [Tooltip("Jarak player ke kasur untuk bisa interact.")]
    [SerializeField] private float interactRange = 1.8f;

    [Header("=== ZZZ & WAKE UP ===")]
    [Tooltip("GameObject teks Zzz (world space) di atas kasur.")]
    [SerializeField] private GameObject zzzTextObject;

    [Tooltip("GameObject teks '[E] Wake Up' (world space) di atas kasur.")]
    [SerializeField] private GameObject wakeUpPrompt;

    [Header("=== ENEMY GHOST ===")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform enemySpawnPoint;
    [SerializeField] private Transform enemyTargetPoint;
    [SerializeField] private float ghostWalkDuration = 2.5f;

    [Header("=== DIALOGUE ===")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private TextMeshProUGUI continueText;

    [Header("=== TRANSITION ===")]
    [SerializeField] private PhaseLoopManager phaseLoopManager;
    [SerializeField] private CanvasGroup screenFader;
    [SerializeField] private float fadeDuration = 2f;
    [SerializeField] private GameObject introCanvasObject;

    // =====================================================================
    // PRIVATE STATE
    // =====================================================================

    private IntroState currentState = IntroState.WaitingToStart;
    private Coroutine tapPulseCoroutine;
    private Coroutine zzzAnimCoroutine;
    private Coroutine continuePulseCoroutine;
    private Coroutine outlinePulseCoroutine;
    private GameObject spawnedEnemy;
    private GameObject bedOutlineGO;
    private InteractObject2D bedInteractObj;
    private int currentDialogueIndex = 0;
    private bool canAdvanceDialogue = false;
    private Color originalPlayerColor = Color.white;
    private List<Canvas> disabledCanvases = new List<Canvas>();
    private GameObject sleepingBody;
    private SpriteRenderer mainBedSR;
    
    private TextMeshProUGUI gameTitleText;
    private RectTransform leftDoor;
    private RectTransform rightDoor;

    private readonly string[] dialogueLines = new string[]
    {
        "\"Remember where you are.\"",
        "\"Try to defeat yourself, even though you will never win.\""
    };

    // =====================================================================
    // UNITY LIFECYCLE & AUTO-HEALING
    // =====================================================================

    private void Awake()
    {
        // Jalankan Auto-Healing untuk menjamin referensi tidak pernah null
        AutoHealReferences();
    }

    private void Start()
    {
        // Proteksi jika player tidak ada sama sekali di scene
        if (playerObject == null)
        {
            Debug.LogError("[IntroSequenceManager] Player tidak ditemukan! Intro dibatalkan demi keamanan permainan.");
            ForceBypassIntro();
            return;
        }

        // Disable gameplay systems yang tidak perlu saat intro
        DisableGameplaySystems();

        // Setup initial state
        SetupInitialState();

    }

    private void Update()
    {
        // =====================================================================
        // FORCE BED & ROOM VISIBILITY (Mencegah script lain mematikan kasur/ruangan)
        // =====================================================================
        if (bedTransform != null)
        {
            if (!bedTransform.gameObject.activeInHierarchy)
            {
                bedTransform.gameObject.SetActive(true);
                Transform current = bedTransform.parent;
                while (current != null)
                {
                    current.gameObject.SetActive(true);
                    current = current.parent;
                }
            }
            
            // Paksa nyalakan semua child dari kasur (seperti child "Bed" dan "Outline" yang memegang SpriteRenderer)
            foreach (Transform child in bedTransform)
            {
                if (child.name != "__IntroBedOutline" && !child.gameObject.activeSelf)
                {
                    child.gameObject.SetActive(true);
                }
            }

            // Paksa nyalakan SpriteRenderer utama kasur
            if (mainBedSR != null && !mainBedSR.enabled)
            {
                mainBedSR.enabled = true;
            }
        }

        switch (currentState)
        {
            case IntroState.WaitingToStart:
                if (Input.anyKeyDown || Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
                {
                    BeginIntro();
                }
                break;

            case IntroState.Exploring:
                UpdateBedProximity();
                break;

            case IntroState.Dialogue:
                if (canAdvanceDialogue && (Input.anyKeyDown || Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)))
                {
                    AdvanceDialogue();
                }
                break;
        }
    }

    // =====================================================================
    // DYNAMIC AUTO-HEALING (BULLETPROOF FALLBACKS)
    // =====================================================================

    private void AutoHealReferences()
    {
        Debug.Log("[IntroSequenceManager] Memulai pemeriksaan referensi & Auto-Healing...");

        // 1. Player
        if (playerObject == null)
        {
            var pm = FindObjectOfType<PlayerMovement>(true);
            if (pm != null)
            {
                playerObject = pm.gameObject;
            }
            else
            {
                playerObject = GameObject.FindGameObjectWithTag("Player");
            }
        }
        if (playerObject != null)
        {
            if (playerMovement == null) playerMovement = playerObject.GetComponent<PlayerMovement>();
            if (playerSpriteRenderer == null)
            {
                playerSpriteRenderer = playerObject.GetComponent<SpriteRenderer>();
                if (playerSpriteRenderer == null) playerSpriteRenderer = playerObject.GetComponentInChildren<SpriteRenderer>();
            }
        }

        // 2. Bed (Kasur)
        if (bedTransform == null)
        {
            var bedInteract = FindObjectOfType<BedInteract>(true);
            if (bedInteract != null)
            {
                bedTransform = bedInteract.transform;
            }
            else
            {
                // 1. Cari exact name "Bed" atau "Kasur" (case-insensitive)
                foreach (var go in FindObjectsOfType<GameObject>(true))
                {
                    string n = go.name.ToLower();
                    if (n == "bed" || n == "kasur")
                    {
                        if (go.GetComponent<RectTransform>() != null) continue;
                        if (playerObject != null && go.transform.IsChildOf(playerObject.transform)) continue;
                        bedTransform = go.transform;
                        break;
                    }
                }

                // 2. Jika tidak ketemu, cari name containing "bed" atau "kasur" tapi hindari spawn points, outlines, dll.
                if (bedTransform == null)
                {
                    foreach (var go in FindObjectsOfType<GameObject>(true))
                    {
                        string n = go.name.ToLower();
                        if ((n.Contains("bed") || n.Contains("kasur")) && 
                            !n.Contains("spawn") && !n.Contains("outline") && !n.Contains("text") && !n.Contains("prompt") && !n.Contains("canvas") && !n.Contains("manager"))
                        {
                            if (go.GetComponent<RectTransform>() != null) continue;
                            if (playerObject != null && go.transform.IsChildOf(playerObject.transform)) continue;
                            bedTransform = go.transform;
                            break;
                        }
                    }
                }
            }

            // Fallback: Jika setelah pencarian scene tetap null, instantiate dari Resources!
            if (bedTransform == null)
            {
                GameObject bedRes = Resources.Load<GameObject>("Bed");
                if (bedRes != null)
                {
                    GameObject bedGO = Instantiate(bedRes, new Vector3(-2.54f, -2.78f, 0f), Quaternion.identity);
                    bedGO.name = "Bed";
                    bedTransform = bedGO.transform;
                    Debug.Log("[IntroSequenceManager] Bed prefab instantiated dynamically from Resources at runtime!");
                }
                else
                {
                    Debug.LogWarning("[IntroSequenceManager] Bed prefab could not be loaded from Resources!");
                }
            }
        }

        // 3. PhaseLoopManager
        if (phaseLoopManager == null)
        {
            phaseLoopManager = FindObjectOfType<PhaseLoopManager>(true);
        }

        // 4. Intro Canvas
        if (introCanvasObject == null)
        {
            introCanvasObject = GameObject.Find("IntroCanvas");
        }
        if (introCanvasObject == null)
        {
            introCanvasObject = new GameObject("IntroCanvas");
            Canvas canvas = introCanvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            introCanvasObject.layer = LayerMask.NameToLayer("UI");
        }
        else
        {
            Canvas canvas = introCanvasObject.GetComponent<Canvas>();
            if (canvas == null) canvas = introCanvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
        }

        RectTransform canvasRT = introCanvasObject.GetComponent<RectTransform>();
        if (canvasRT == null) canvasRT = introCanvasObject.AddComponent<RectTransform>();
        // Do NOT touch Canvas localScale, Unity manages it!

        // Always heal the CanvasScaler on the IntroCanvas to guarantee correct size!
        CanvasScaler scaler = introCanvasObject.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = introCanvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GraphicRaycaster raycaster = introCanvasObject.GetComponent<GraphicRaycaster>();
        if (raycaster == null) raycaster = introCanvasObject.AddComponent<GraphicRaycaster>();

        // 5. Fog Overlay (Search first for any child containing "fog" or "vignette" to heal existing)
        if (fogOverlay == null && introCanvasObject != null)
        {
            Transform fogTrans = null;
            foreach (Transform child in introCanvasObject.transform)
            {
                string nameLower = child.name.ToLower();
                if (nameLower.Contains("fog") || nameLower.Contains("vignette"))
                {
                    fogTrans = child;
                    break;
                }
            }
            GameObject fogGO = fogTrans != null ? fogTrans.gameObject : null;
            if (fogGO == null)
            {
                fogGO = new GameObject("FogOverlay", typeof(RectTransform));
                fogGO.transform.SetParent(introCanvasObject.transform, false);
                fogGO.layer = LayerMask.NameToLayer("UI");

                Image img = fogGO.AddComponent<Image>();
                img.color = Color.white;
                img.raycastTarget = true;

                fogOverlay = fogGO.AddComponent<CanvasGroup>();
            }
            else
            {
                fogOverlay = fogGO.GetComponent<CanvasGroup>();
                if (fogOverlay == null) fogOverlay = fogGO.AddComponent<CanvasGroup>();
            }
        }
        if (fogOverlay != null)
        {
            RectTransform rt = fogOverlay.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.localScale = Vector3.one;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;
                rt.anchoredPosition = Vector2.zero;
            }
            Image img = fogOverlay.GetComponent<Image>();
            if (img == null) img = fogOverlay.gameObject.AddComponent<Image>();
            img.raycastTarget = true;
        }

        // 6. Tap To Play Text
        if (tapToPlayText == null && fogOverlay != null)
        {
            Transform tapTrans = fogOverlay.transform.Find("TapToPlayText");
            GameObject tapGO = tapTrans != null ? tapTrans.gameObject : null;
            if (tapGO == null)
            {
                tapGO = new GameObject("TapToPlayText", typeof(RectTransform));
                tapGO.transform.SetParent(fogOverlay.transform, false);
                tapGO.layer = LayerMask.NameToLayer("UI");
                tapToPlayText = tapGO.AddComponent<TextMeshProUGUI>();
            }
            else
            {
                tapToPlayText = tapGO.GetComponent<TextMeshProUGUI>();
            }
        }
        if (tapToPlayText != null)
        {
            RectTransform rt = tapToPlayText.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.localScale = Vector3.one;
                rt.anchorMin = new Vector2(0.5f, 0.35f);
                rt.anchorMax = new Vector2(0.5f, 0.35f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(600, 80);
                rt.anchoredPosition = Vector2.zero;
            }
            tapToPlayText.text = "Tap to Play";
            tapToPlayText.fontSize = 20; // Ukuran elegan, tidak raksasa
            tapToPlayText.alignment = TextAlignmentOptions.Center;
            tapToPlayText.color = new Color(0.75f, 0.8f, 1f, 1f);
            tapToPlayText.enableWordWrapping = false;
        }

        // 6.5. Game Title Text (BRING ME ALIVE)
        if (gameTitleText == null && fogOverlay != null)
        {
            Transform titleTrans = fogOverlay.transform.Find("GameTitleText");
            GameObject titleGO = titleTrans != null ? titleTrans.gameObject : null;
            if (titleGO == null)
            {
                titleGO = new GameObject("GameTitleText", typeof(RectTransform));
                titleGO.transform.SetParent(fogOverlay.transform, false);
                titleGO.layer = LayerMask.NameToLayer("UI");
                gameTitleText = titleGO.AddComponent<TextMeshProUGUI>();
            }
            else
            {
                gameTitleText = titleGO.GetComponent<TextMeshProUGUI>();
            }
        }
        if (gameTitleText != null)
        {
            RectTransform rt = gameTitleText.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.localScale = Vector3.one;
                rt.anchorMin = new Vector2(0.5f, 0.72f);
                rt.anchorMax = new Vector2(0.5f, 0.72f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(800, 100);
                rt.anchoredPosition = Vector2.zero;
            }
            gameTitleText.text = "BRING ME ALIVE";
            gameTitleText.fontSize = 55; // Font besar dan mendominasi
            gameTitleText.fontStyle = FontStyles.Bold;
            gameTitleText.alignment = TextAlignmentOptions.Center;
            gameTitleText.color = new Color(0.9f, 0.9f, 1f, 0.95f); // Putih agak kebiruan
            gameTitleText.enableWordWrapping = false;
        }

        // 7. Dialogue Panel
        if (dialoguePanel == null && introCanvasObject != null)
        {
            Transform panelTrans = introCanvasObject.transform.Find("DialoguePanel");
            if (panelTrans != null)
            {
                dialoguePanel = panelTrans.gameObject;
            }
            else
            {
                dialoguePanel = new GameObject("DialoguePanel", typeof(RectTransform));
                dialoguePanel.transform.SetParent(introCanvasObject.transform, false);
                dialoguePanel.layer = LayerMask.NameToLayer("UI");
                Image bg = dialoguePanel.AddComponent<Image>();
                bg.color = new Color(0.02f, 0.02f, 0.06f, 0.88f);
            }
        }
        if (dialoguePanel != null)
        {
            RectTransform rt = dialoguePanel.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.localScale = Vector3.one;
                rt.anchorMin = new Vector2(0.5f, 0.15f);
                rt.anchorMax = new Vector2(0.5f, 0.15f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(750, 160);
                rt.anchoredPosition = Vector2.zero;
            }
            Image bg = dialoguePanel.GetComponent<Image>();
            if (bg == null) bg = dialoguePanel.AddComponent<Image>();
            bg.color = new Color(0.02f, 0.02f, 0.06f, 0.88f);
        }

        // 8. Dialogue Text
        if (dialogueText == null && dialoguePanel != null)
        {
            Transform txtTrans = dialoguePanel.transform.Find("DialogueText");
            GameObject txtGO = txtTrans != null ? txtTrans.gameObject : null;
            if (txtGO == null)
            {
                txtGO = new GameObject("DialogueText", typeof(RectTransform));
                txtGO.transform.SetParent(dialoguePanel.transform, false);
                txtGO.layer = LayerMask.NameToLayer("UI");
                dialogueText = txtGO.AddComponent<TextMeshProUGUI>();
            }
            else
            {
                dialogueText = txtGO.GetComponent<TextMeshProUGUI>();
            }
        }
        if (dialogueText != null)
        {
            RectTransform rt = dialogueText.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.localScale = Vector3.one;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(-40, -45);
                rt.anchoredPosition = new Vector2(0, 8);
            }
            dialogueText.fontSize = 16; // Ukuran elegan, premium
            dialogueText.fontStyle = FontStyles.Italic;
            dialogueText.alignment = TextAlignmentOptions.Center;
            dialogueText.color = Color.white;
            dialogueText.enableWordWrapping = true;
        }

        // 9. Continue Text
        if (continueText == null && dialoguePanel != null)
        {
            Transform cTrans = dialoguePanel.transform.Find("ContinueText");
            GameObject cGO = cTrans != null ? cTrans.gameObject : null;
            if (cGO == null)
            {
                cGO = new GameObject("ContinueText", typeof(RectTransform));
                cGO.transform.SetParent(dialoguePanel.transform, false);
                cGO.layer = LayerMask.NameToLayer("UI");
                continueText = cGO.AddComponent<TextMeshProUGUI>();
            }
            else
            {
                continueText = cGO.GetComponent<TextMeshProUGUI>();
            }
        }
        if (continueText != null)
        {
            RectTransform rt = continueText.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.localScale = Vector3.one;
                rt.anchorMin = new Vector2(1f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(1f, 0f);
                rt.sizeDelta = new Vector2(250, 30);
                rt.anchoredPosition = new Vector2(-15, 12);
            }
            continueText.text = "Press any key...";
            continueText.fontSize = 11; // Sangat proporsional
            continueText.alignment = TextAlignmentOptions.Right;
            continueText.color = new Color(0.6f, 0.6f, 0.7f, 1f);
        }

        // 10. Screen Fader & Sliding Doors
        if (introCanvasObject != null)
        {
            Transform sfTrans = introCanvasObject.transform.Find("ScreenFader");
            if (sfTrans != null)
            {
                screenFader = sfTrans.GetComponent<CanvasGroup>();
                
                Transform lTrans = sfTrans.Find("LeftDoor");
                if (lTrans != null)
                {
                    leftDoor = lTrans.GetComponent<RectTransform>();
                    // Pastikan state TERBUKA di awal (menyembunyikan diri)
                    leftDoor.localScale = new Vector3(0, 1, 1);
                }

                Transform rTrans = sfTrans.Find("RightDoor");
                if (rTrans != null)
                {
                    rightDoor = rTrans.GetComponent<RectTransform>();
                    // Pastikan state TERBUKA di awal (menyembunyikan diri)
                    rightDoor.localScale = new Vector3(0, 1, 1);
                }
            }
        }

        // 11. Spawn Points (relative to bed position)
        Vector3 bedPos = bedTransform != null ? bedTransform.position : new Vector3(-3f, -2f, 0f);

        if (spawnNearBed == null)
        {
            GameObject snbGO = GameObject.Find("SpawnNearBed");
            if (snbGO == null)
            {
                snbGO = new GameObject("SpawnNearBed");
                snbGO.transform.position = bedPos + new Vector3(0.9f, -0.2f, 0f);
            }
            spawnNearBed = snbGO.transform;
        }

        if (enemySpawnPoint == null)
        {
            GameObject espGO = GameObject.Find("EnemySpawnPoint");
            if (espGO == null)
            {
                Camera mainCam = Camera.main;
                float camY = mainCam != null ? mainCam.transform.position.y : 0f;
                float camHeight = mainCam != null ? mainCam.orthographicSize : 5f;
                espGO = new GameObject("EnemySpawnPoint");
                espGO.transform.position = new Vector3(bedPos.x + 1.5f, camY - camHeight - 2.5f, 0f);
            }
            enemySpawnPoint = espGO.transform;
        }

        if (enemyTargetPoint == null)
        {
            GameObject etpGO = GameObject.Find("EnemyTargetPoint");
            if (etpGO == null)
            {
                Camera mainCam = Camera.main;
                float camY = mainCam != null ? mainCam.transform.position.y : 0f;
                etpGO = new GameObject("EnemyTargetPoint");
                etpGO.transform.position = new Vector3(bedPos.x + 1.5f, camY - 0.5f, 0f);
            }
            enemyTargetPoint = etpGO.transform;
        }

        // 12. World Space Zzz & WakeUp (relative to bed position)
        if (zzzTextObject == null && bedTransform != null)
        {
            zzzTextObject = GameObject.Find("ZzzText");
            if (zzzTextObject == null)
            {
                zzzTextObject = new GameObject("ZzzText");
                zzzTextObject.AddComponent<TextMeshPro>();
                zzzTextObject.transform.position = bedPos + new Vector3(0f, 0.65f, 0f);
            }
        }
        if (zzzTextObject != null)
        {
            zzzTextObject.transform.localScale = new Vector3(0.29f, 0.29f, 1f); // Skala diperbesar
            TextMeshPro tmp = zzzTextObject.GetComponent<TextMeshPro>();
            if (tmp != null)
            {
                tmp.text = "Z z z . . .";
                tmp.fontSize = 18; // Sangat crisp & resolusi tinggi!
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = new Color(0.6f, 0.8f, 1f, 0.85f);
            }
            var mr = zzzTextObject.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = 20;
        }

        if (wakeUpPrompt == null && bedTransform != null)
        {
            wakeUpPrompt = GameObject.Find("WakeUpPrompt");
            if (wakeUpPrompt == null)
            {
                wakeUpPrompt = new GameObject("WakeUpPrompt");
                wakeUpPrompt.AddComponent<TextMeshPro>();
                wakeUpPrompt.transform.position = bedPos + new Vector3(0f, 1.1f, 0f);
            }
        }
        if (wakeUpPrompt != null)
        {
            wakeUpPrompt.transform.localScale = new Vector3(0.29f, 0.29f, 1f); // Skala diperbesar
            TextMeshPro tmp = wakeUpPrompt.GetComponent<TextMeshPro>();
            if (tmp != null)
            {
                tmp.text = "[E] Wake Up";
                tmp.fontSize = 16; // Sangat crisp!
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = new Color(1f, 1f, 0.8f, 1f);
            }
            var mr = wakeUpPrompt.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = 20;
        }

        // 13. Enemy Prefab Fallback (jika null, cari di scene atau gunakan playerObject yang di-tint gelap)
        if (enemyPrefab == null)
        {
            var existingEnemy = GameObject.FindWithTag("Enemy");
            if (existingEnemy != null)
            {
                enemyPrefab = existingEnemy;
            }
        }

        Debug.Log("[IntroSequenceManager] Pemeriksaan referensi selesai. Status Auto-Healing: BERHASIL.");
    }

    /// <summary>
    /// Fail-safe bypass: Jika ada error fatal di Start, langsung aktifkan gameplay phase 1 agar game tidak stuck.
    /// </summary>
    private void ForceBypassIntro()
    {
        Debug.LogWarning("[IntroSequenceManager] Menjalankan Force-Bypass Intro Sequence...");
        
        // Kembalikan semua canvases
        foreach (var canvas in FindObjectsOfType<Canvas>(true))
        {
            if (canvas.gameObject.name != "IntroCanvas") canvas.enabled = true;
        }

        // Re-enable player movement & gameplay systems
        if (playerObject != null) playerObject.SetActive(true);
        if (playerMovement != null) playerMovement.enabled = true;

        var playerStatus = FindObjectOfType<PlayerStatus>();
        if (playerStatus != null) playerStatus.enabled = true;

        var playerInteract = FindObjectOfType<PlayerInteract2D>();
        if (playerInteract != null) playerInteract.canInteract = true;

        var phaseTaskManager = FindObjectOfType<PhaseTaskManager>(true);
        if (phaseTaskManager != null)
        {
            phaseTaskManager.enabled = true;
            phaseTaskManager.SetupTasks();
        }

        if (phaseLoopManager != null)
            phaseLoopManager.StartManualTransition(GameState.Awake);

        // Hancurkan intro Canvas & Manager
        if (introCanvasObject != null) Destroy(introCanvasObject);
        Destroy(gameObject);
    }

    // =====================================================================
    // SETUP SYSTEMS
    // =====================================================================

    private void DisableGameplaySystems()
    {
        // Disable menu lama
        var oldMenu = FindObjectOfType<SeamlessMainMenu>();
        if (oldMenu != null) oldMenu.enabled = false;

        // Disable stability drain
        var playerStatus = FindObjectOfType<PlayerStatus>();
        if (playerStatus != null) playerStatus.enabled = false;

        // Disable player interact (kita pakai sistem sendiri)
        var playerInteract = FindObjectOfType<PlayerInteract2D>();
        if (playerInteract != null) playerInteract.canInteract = false;

        // Disable PhaseTaskManager agar tidak memicu taskbubble sebelum waktunya
        var phaseTaskManager = FindObjectOfType<PhaseTaskManager>();
        if (phaseTaskManager != null) phaseTaskManager.enabled = false;

        // Disable other canvases in the scene to hide all unrelated gameplay UI
        disabledCanvases.Clear();
        foreach (var canvas in FindObjectsOfType<Canvas>(true))
        {
            if (canvas != null && canvas.gameObject.name != "IntroCanvas" && canvas.enabled)
            {
                canvas.enabled = false;
                disabledCanvases.Add(canvas);
            }
        }
    }

    private void SetupInitialState()
    {
        currentState = IntroState.WaitingToStart;

        // ENSURE NORMAL WORLD IS ACTIVE & LIMINAL IS INACTIVE AT START
        LiminalRoomManager liminalManager = null;
        var allLiminalManagers = FindObjectsOfType<LiminalRoomManager>(true);
        if (allLiminalManagers != null && allLiminalManagers.Length > 0)
        {
            liminalManager = allLiminalManagers[0];
        }

        if (liminalManager != null)
        {
            var type = liminalManager.GetType();
            var normalWorldField = type.GetField("normalWorld", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var liminalWorldField = type.GetField("liminalWorld", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (normalWorldField != null)
            {
                var normalWorld = normalWorldField.GetValue(liminalManager) as GameObject;
                if (normalWorld != null) normalWorld.SetActive(true);
            }
            if (liminalWorldField != null)
            {
                var liminalWorld = liminalWorldField.GetValue(liminalManager) as GameObject;
                if (liminalWorld != null) liminalWorld.SetActive(false);
            }
        }

        // Sembunyikan player (MC berupa arwah, belum muncul di awal)
        if (playerObject != null)
        {
            if (playerSpriteRenderer != null)
                originalPlayerColor = playerSpriteRenderer.color;
            playerObject.SetActive(false);
        }
        if (playerMovement != null) playerMovement.enabled = false;

        // FOG: setup vignette sprite dan flicker
        SetupVignetteSprite();
        if (fogOverlay != null)
        {
            fogOverlay.gameObject.SetActive(true);
            fogOverlay.alpha = 0.97f;
            fogOverlay.blocksRaycasts = true;
            fogOverlay.interactable = true;
            StartCoroutine(FlickerVignette());
        }

        // Sembunyikan UI
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (zzzTextObject != null) zzzTextObject.SetActive(false);
        if (wakeUpPrompt != null) wakeUpPrompt.SetActive(false);

        // Screen fader transparan
        if (screenFader != null)
        {
            screenFader.alpha = 0f;
            screenFader.blocksRaycasts = false;
        }

        // Pulse "Tap to Play"
        if (tapToPlayText != null)
        {
            tapToPlayText.gameObject.SetActive(true);
            tapPulseCoroutine = StartCoroutine(PulseTextAlpha(tapToPlayText));
        }

        // AKTIFKAN KASUR (BED) DAN SEMUA PARENT-NYA AGAR PASTI KELIHATAN
        if (bedTransform != null)
        {
            bedTransform.gameObject.SetActive(true);
            Transform current = bedTransform.parent;
            while (current != null)
            {
                current.gameObject.SetActive(true);
                current = current.parent;
            }

            mainBedSR = bedTransform.GetComponent<SpriteRenderer>();
            if (mainBedSR == null) mainBedSR = bedTransform.GetComponentInChildren<SpriteRenderer>(true);
            if (mainBedSR != null) mainBedSR.enabled = true;
        }

        // Buat body MC yang sedang tidur di kasur
        CreateSleepingBody();
    }

    private void SetupVignetteSprite()
    {
        if (fogOverlay == null) return;
        Image img = fogOverlay.GetComponent<Image>();
        if (img == null) img = fogOverlay.gameObject.GetComponentInChildren<Image>();
        if (img != null)
        {
            img.sprite = CreateVignetteSprite();
            img.color = Color.white; // Render sprite dengan warna aslinya
        }
    }

    private Sprite CreateVignetteSprite()
    {
        int width = 256;
        int height = 256;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Konversi koordinat ke rentang [-1, 1]
                float dx = (x - width * 0.5f) / (width * 0.5f);
                float dy = (y - height * 0.5f) / (height * 0.5f);
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                
                // Vignette tebal: tengah hampir pekat (alpha 0.98f), sudut benar-benar hitam (alpha 1.0f)
                // Ini memastikan ruangan SAMA SEKALI tidak kelihatan sebelum diklik (hanya kabut tebal).
                float alpha = Mathf.Clamp01(Mathf.Lerp(0.98f, 1.0f, dist * dist));
                
                tex.SetPixel(x, y, new Color(0f, 0f, 0f, alpha));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
    }

    private IEnumerator FlickerVignette()
    {
        if (fogOverlay == null) yield break;
        
        while (currentState == IntroState.WaitingToStart)
        {
            // Base alpha yang tebal
            float targetAlpha = Random.Range(0.92f, 1.0f);
            
            // Random glitch cepat lampu rusak creepy
            if (Random.value < 0.12f)
            {
                float duration = Random.Range(0.05f, 0.15f);
                float elapsed = 0f;
                float glitchAlpha = Random.Range(0.5f, 0.8f);
                while (elapsed < duration && currentState == IntroState.WaitingToStart)
                {
                    elapsed += Time.deltaTime;
                    if (fogOverlay != null) fogOverlay.alpha = Mathf.Lerp(glitchAlpha, targetAlpha, elapsed / duration);
                    yield return null;
                }
            }
            else
            {
                // breathing effect lambat
                float duration = Random.Range(0.1f, 0.3f);
                float elapsed = 0f;
                float startAlpha = fogOverlay != null ? fogOverlay.alpha : 0.97f;
                while (elapsed < duration && currentState == IntroState.WaitingToStart)
                {
                    elapsed += Time.deltaTime;
                    if (fogOverlay != null) fogOverlay.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
                    yield return null;
                }
            }
            
            yield return new WaitForSeconds(Random.Range(0.05f, 0.2f));
        }
    }

    // =====================================================================
    // STATE 1: WAITING TO START — GELAP + "TAP TO PLAY"
    // =====================================================================

    private void BeginIntro()
    {
        currentState = IntroState.FadingIn;

        if (tapPulseCoroutine != null) StopCoroutine(tapPulseCoroutine);
        if (tapToPlayText != null) tapToPlayText.gameObject.SetActive(false);

        StartCoroutine(FadeInSequence());
    }

    /// <summary>
    /// Pulse alpha teks naik-turun (efek breathing).
    /// </summary>
    private IEnumerator PulseTextAlpha(TextMeshProUGUI text)
    {
        if (text == null) yield break;
        Color baseColor = text.color;

        while (true)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 1.2f;
                text.color = new Color(baseColor.r, baseColor.g, baseColor.b,
                    Mathf.Lerp(0.15f, 1f, t));
                yield return null;
            }

            t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 1.2f;
                text.color = new Color(baseColor.r, baseColor.g, baseColor.b,
                    Mathf.Lerp(1f, 0.15f, t));
                yield return null;
            }
        }
    }

    // =====================================================================
    // STATE 2: FADING IN — FOG HILANG, GHOST MC MUNCUL
    // =====================================================================

    private IEnumerator FadeInSequence()
    {
        // Fade out fog overlay (gelap → transparan)
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            t = 1f - Mathf.Pow(1f - t, 2.5f); // Ease-out
            if (fogOverlay != null)
                fogOverlay.alpha = Mathf.Lerp(0.97f, 0f, t);
            yield return null;
        }

        if (fogOverlay != null)
        {
            fogOverlay.alpha = 0f;
            fogOverlay.blocksRaycasts = false;
            fogOverlay.interactable = false;
            fogOverlay.gameObject.SetActive(false); // DEAKTIFKAN agar mutlak tidak menutupi gameplay viewport!
        }

        // Disable GraphicRaycaster on IntroCanvas to prevent blocking clicks during exploration
        if (introCanvasObject != null)
        {
            GraphicRaycaster raycaster = introCanvasObject.GetComponent<GraphicRaycaster>();
            if (raycaster != null) raycaster.enabled = false;
        }

        yield return new WaitForSeconds(0.5f);

        // ---- SPAWN GHOST MC ----
        if (playerObject != null)
        {
            if (spawnNearBed != null)
                playerObject.transform.position = spawnNearBed.position;

            // Mulai dari invisible
            if (playerSpriteRenderer != null)
                playerSpriteRenderer.color = new Color(ghostColor.r, ghostColor.g, ghostColor.b, 0f);

            playerObject.SetActive(true);

            // Fade in ghost
            yield return StartCoroutine(FadeSpriteAlpha(
                playerSpriteRenderer, 0f, ghostColor.a, 1f));

            // Set final ghost color
            if (playerSpriteRenderer != null)
                playerSpriteRenderer.color = ghostColor;
        }

        // ---- ZZZ TEXT ----
        if (zzzTextObject != null)
        {
            zzzTextObject.SetActive(true);
            zzzAnimCoroutine = StartCoroutine(AnimateZzzText());
        }

        yield return new WaitForSeconds(0.3f);

        // Aktifkan movement
        if (playerMovement != null)
            playerMovement.enabled = true;

        currentState = IntroState.Exploring;
    }

    private IEnumerator FadeSpriteAlpha(SpriteRenderer sr, float from, float to, float duration)
    {
        if (sr == null) yield break;
        Color c = sr.color;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(from, to, elapsed / duration);
            sr.color = c;
            yield return null;
        }
        c.a = to;
        sr.color = c;
    }

    // =====================================================================
    // ZZZ ANIMATION
    // =====================================================================

    private IEnumerator AnimateZzzText()
    {
        if (zzzTextObject == null) yield break;
        Vector3 startPos = zzzTextObject.transform.localPosition;

        while (zzzTextObject != null && zzzTextObject.activeSelf)
        {
            float bob = Mathf.Sin(Time.time * 1.8f) * 0.12f;
            zzzTextObject.transform.localPosition = startPos + new Vector3(0f, bob, 0f);

            // Alpha wave
            var tmp = zzzTextObject.GetComponent<TextMeshPro>();
            if (tmp != null)
            {
                float a = Mathf.Lerp(0.4f, 1f, (Mathf.Sin(Time.time * 2.5f) + 1f) / 2f);
                Color c = tmp.color;
                c.a = a;
                tmp.color = c;
            }

            yield return null;
        }
    }

    // =====================================================================
    // STATE 3: EXPLORING — JARAK KE KASUR + INTERACT
    // =====================================================================

    private void UpdateBedProximity()
    {
        if (bedTransform == null || playerObject == null) return;

        float dist = Vector2.Distance(
            playerObject.transform.position,
            bedTransform.position);

        bool isNear = dist <= interactRange;

        // Show/hide "[E] Wake Up" prompt
        if (wakeUpPrompt != null)
            wakeUpPrompt.SetActive(isNear);

        // Highlight outline menggunakan script InteractObject2D asli dari user
        if (bedInteractObj != null)
        {
            bedInteractObj.SetHighlight(isNear);
        }

        // Interact dengan E
        if (isNear && Input.GetKeyDown(KeyCode.E))
        {
            OnBedInteracted();
        }
    }



    // =====================================================================
    // STATE 4: GHOST APPEARING — ENEMY MUNCUL
    // =====================================================================

    private void OnBedInteracted()
    {
        if (currentState != IntroState.Exploring) return;
        currentState = IntroState.GhostAppearing;

        // Lock player
        if (playerMovement != null)
        {
            playerMovement.StopMovement();
            playerMovement.enabled = false;
        }

        // Sembunyikan prompt & outline
        if (wakeUpPrompt != null) wakeUpPrompt.SetActive(false);
        if (bedInteractObj != null) bedInteractObj.SetHighlight(false);

        StartCoroutine(GhostEnemySequence());
    }

    private IEnumerator GhostEnemySequence()
    {
        yield return new WaitForSeconds(0.7f);

        // Spawn enemy ghost
        if (enemySpawnPoint != null)
        {
            if (enemyPrefab != null)
            {
                spawnedEnemy = Instantiate(enemyPrefab, enemySpawnPoint.position, Quaternion.identity);
            }
            else if (playerObject != null)
            {
                // Fallback: Jika enemyPrefab null, kita clone playerObject untuk representasi visual!
                Debug.LogWarning("[IntroSequenceManager] enemyPrefab kosong! Membuat bayangan shadow MC sebagai fallback.");
                
                bool originalActive = playerObject.activeSelf;
                playerObject.SetActive(false);
                spawnedEnemy = Instantiate(playerObject, enemySpawnPoint.position, Quaternion.identity);
                playerObject.SetActive(originalActive);
                
                // Warnai jadi bayangan gelap
                var sr = spawnedEnemy.GetComponent<SpriteRenderer>();
                if (sr == null) sr = spawnedEnemy.GetComponentInChildren<SpriteRenderer>();
                if (sr != null) sr.color = new Color(0.1f, 0.1f, 0.2f, 0.8f);
            }

            if (spawnedEnemy != null)
            {
                // Disable SEMUA gameplay script (AI, combat, damage) pada spawned enemy
                foreach (var mb in spawnedEnemy.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    // Jangan matikan Animator atau SpriteRenderer
                    if (mb is Animator || mb is SpriteRenderer) continue;
                    mb.enabled = false;
                }

                // Disable colliders
                foreach (var col in spawnedEnemy.GetComponentsInChildren<Collider2D>(true))
                    col.enabled = false;

                // Re-enable Animator untuk animasi jalan
                Animator anim = spawnedEnemy.GetComponentInChildren<Animator>();
                if (anim == null) anim = spawnedEnemy.GetComponent<Animator>();
                if (anim != null)
                {
                    anim.enabled = true;
                    SafeSetAnimFloat(anim, "MoveX", 0f);
                    SafeSetAnimFloat(anim, "MoveY", 1f);
                    SafeSetAnimBool(anim, "isMoving", true);
                }

                // Re-enable SpriteRenderers
                foreach (var sr in spawnedEnemy.GetComponentsInChildren<SpriteRenderer>(true))
                    sr.enabled = true;
            }
        }

        // Gerakkan ghost ke target
        if (spawnedEnemy != null && enemyTargetPoint != null)
        {
            Vector3 start = spawnedEnemy.transform.position;
            Vector3 end = enemyTargetPoint.position;
            float elapsed = 0f;

            while (elapsed < ghostWalkDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / ghostWalkDuration;
                t = t * t * (3f - 2f * t); // Smoothstep
                if (spawnedEnemy != null) spawnedEnemy.transform.position = Vector3.Lerp(start, end, t);
                yield return null;
            }
            if (spawnedEnemy != null) spawnedEnemy.transform.position = end;
        }

        // Stop animasi jalan
        if (spawnedEnemy != null)
        {
            Animator anim = spawnedEnemy.GetComponentInChildren<Animator>();
            if (anim == null) anim = spawnedEnemy.GetComponent<Animator>();
            if (anim != null)
            {
                SafeSetAnimBool(anim, "isMoving", false);
                SafeSetAnimFloat(anim, "MoveX", 0f);
                SafeSetAnimFloat(anim, "MoveY", -1f);
            }
        }

        yield return new WaitForSeconds(0.8f);

        // Re-enable GraphicRaycaster for dialogue interaction
        if (introCanvasObject != null)
        {
            GraphicRaycaster raycaster = introCanvasObject.GetComponent<GraphicRaycaster>();
            if (raycaster != null) raycaster.enabled = true;
        }

        // Mulai dialogue
        currentState = IntroState.Dialogue;
        currentDialogueIndex = 0;
        ShowDialogueLine(dialogueLines[0]);
    }

    private void SafeSetAnimFloat(Animator anim, string param, float value)
    {
        foreach (var p in anim.parameters)
            if (p.name == param && p.type == AnimatorControllerParameterType.Float)
            { anim.SetFloat(param, value); return; }
    }

    private void SafeSetAnimBool(Animator anim, string param, bool value)
    {
        foreach (var p in anim.parameters)
            if (p.name == param && p.type == AnimatorControllerParameterType.Bool)
            { anim.SetBool(param, value); return; }
    }

    // =====================================================================
    // STATE 5: DIALOGUE
    // =====================================================================

    private void ShowDialogueLine(string text)
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        if (dialogueText != null) dialogueText.text = text;

        if (continueText != null)
        {
            continueText.gameObject.SetActive(true);
            continueText.text = "Press any key...";
            if (continuePulseCoroutine != null) StopCoroutine(continuePulseCoroutine);
            continuePulseCoroutine = StartCoroutine(PulseTextAlpha(continueText));
        }

        StartCoroutine(EnableDialogueAdvance(0.5f));
    }

    private IEnumerator EnableDialogueAdvance(float delay)
    {
        canAdvanceDialogue = false;
        yield return new WaitForSeconds(delay);
        canAdvanceDialogue = true;
    }

    private void AdvanceDialogue()
    {
        canAdvanceDialogue = false;
        currentDialogueIndex++;

        if (currentDialogueIndex < dialogueLines.Length)
        {
            ShowDialogueLine(dialogueLines[currentDialogueIndex]);
        }
        else
        {
            if (continuePulseCoroutine != null) StopCoroutine(continuePulseCoroutine);
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            StartCoroutine(TransitionToAwakeSequence());
        }
    }

    // =====================================================================
    // STATE 6: TRANSITION TO AWAKE (FIXED CANVASES ACTIVATION ORDER)
    // =====================================================================

    private IEnumerator TransitionToAwakeSequence()
    {
        currentState = IntroState.TransitionToAwake;

        // ANIMASI SLIDING DOORS (MENUJU TENGAH / MENUTUP)
        if (screenFader != null && leftDoor != null && rightDoor != null)
        {
            screenFader.gameObject.SetActive(true);
            screenFader.blocksRaycasts = true;
            screenFader.alpha = 1f;

            // Pastikan posisi anchor normal sebelum mulai scale
            leftDoor.anchorMin = new Vector2(0f, 0f);
            leftDoor.anchorMax = new Vector2(0.5f, 1f);
            leftDoor.sizeDelta = Vector2.zero;
            leftDoor.anchoredPosition = Vector2.zero;

            rightDoor.anchorMin = new Vector2(0.5f, 0f);
            rightDoor.anchorMax = new Vector2(1f, 1f);
            rightDoor.sizeDelta = Vector2.zero;
            rightDoor.anchoredPosition = Vector2.zero;

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;
                t = 1f - Mathf.Pow(1f - t, 3f);
                
                // Menutup dari pinggir ke tengah
                leftDoor.localScale = new Vector3(t, 1f, 1f);
                rightDoor.localScale = new Vector3(t, 1f, 1f);
                yield return null;
            }
            leftDoor.localScale = new Vector3(1f, 1f, 1f);
            rightDoor.localScale = new Vector3(1f, 1f, 1f);
        }

        yield return new WaitForSeconds(0.5f);

        // ---- CLEANUP WORLD INTRO OBJECTS ----
        if (sleepingBody != null) Destroy(sleepingBody);
        if (spawnedEnemy != null) Destroy(spawnedEnemy);
        if (zzzAnimCoroutine != null) StopCoroutine(zzzAnimCoroutine);
        if (zzzTextObject != null) zzzTextObject.SetActive(false);
        if (bedOutlineGO != null) Destroy(bedOutlineGO);

        // Reset player ke warna normal (MC bangun, bukan ghost lagi)
        if (playerSpriteRenderer != null)
            playerSpriteRenderer.color = originalPlayerColor;

        // ---- RE-ENABLE SYSTEMS IN THE CORRECT ORDER TO PREVENT TYPEWRITER CRASH ----

        // 1. RE-ENABLE GAMEPLAY CANVASES DULU!
        // Ini MUTLAK dilakukan sebelum SetupTasks() agar TaskBubble (GameObject Canvas) active di scene,
        // sehingga StartCoroutine typewriter tidak memicu error Unity:
        // "Coroutine couldn't be started because the game object 'Canvas' is inactive!"
        foreach (var canvas in disabledCanvases)
        {
            if (canvas != null) canvas.enabled = true;
        }
        disabledCanvases.Clear();

        // 2. RE-ENABLE PHASE TASK MANAGER & MULAI TASKS (Sekarang aman dari crash!)
        var phaseTaskManager = FindObjectOfType<PhaseTaskManager>(true);
        if (phaseTaskManager != null)
        {
            phaseTaskManager.enabled = true;
            phaseTaskManager.SetupTasks();
        }

        // 3. RE-ENABLE OTHER GAMEPLAY SYSTEMS
        var playerStatus = FindObjectOfType<PlayerStatus>();
        if (playerStatus != null) playerStatus.enabled = true;

        var playerInteract = FindObjectOfType<PlayerInteract2D>();
        if (playerInteract != null) playerInteract.canInteract = true;

        // Transition ke Awake phase via manager
        if (phaseLoopManager != null)
            phaseLoopManager.StartManualTransition(GameState.Awake);

        if (playerMovement != null)
            playerMovement.enabled = true;

        yield return new WaitForSeconds(0.3f);

        // ANIMASI SLIDING DOORS (MEMBUKA / SPLIT KE KIRI-KANAN)
        if (screenFader != null && leftDoor != null && rightDoor != null)
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;
                t = t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
                
                // Buka dari tengah ke pinggir
                float scale = 1f - t;
                leftDoor.localScale = new Vector3(scale, 1f, 1f);
                rightDoor.localScale = new Vector3(scale, 1f, 1f);
                yield return null;
            }
            leftDoor.localScale = new Vector3(0f, 1f, 1f);
            rightDoor.localScale = new Vector3(0f, 1f, 1f);
            screenFader.blocksRaycasts = false;
        }

        Debug.Log("[IntroSequence] Intro selesai — masuk Awake Phase!");
        
        // Hancurkan Canvas Intro secara permanen agar bersih dan floor terlihat sempurna
        if (introCanvasObject != null) Destroy(introCanvasObject);
        
        // Hancurkan IntroManager script & object ini
        Destroy(gameObject);
    }

    private void CreateSleepingBody()
    {
        if (playerObject == null || bedTransform == null) return;
        if (sleepingBody != null) Destroy(sleepingBody);
        
        // Deactivate playerObject source temporarily to prevent Awake() on clone scripts immediately (fixes singleton destruction bug)
        bool originalActive = playerObject.activeSelf;
        playerObject.SetActive(false);
        
        // Buat clone dari player di posisi kasur
        sleepingBody = Instantiate(playerObject, bedTransform.position, Quaternion.Euler(0f, 0f, -90f));
        sleepingBody.name = "__SleepingPlayerBody";
        
        // Restore original active state of source playerObject
        playerObject.SetActive(originalActive);
        
        // Hapus SEMUA component scripting agar tidak bergerak/interact/run logikanya SECARA INSTAN
        // Menggunakan DestroyImmediate pada non-active clone mencegah component Awake() berjalan saat SetActive(true)
        foreach (var comp in sleepingBody.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (comp != null) DestroyImmediate(comp);
        }
        
        var rb = sleepingBody.GetComponent<Rigidbody2D>();
        if (rb != null) DestroyImmediate(rb);
        
        var col = sleepingBody.GetComponent<Collider2D>();
        if (col != null) DestroyImmediate(col);
        
        // Pastikan SpriteRenderer diaktifkan dan valid
        var sr = sleepingBody.GetComponent<SpriteRenderer>();
        if (sr == null) sr = sleepingBody.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            // Pastikan sprite renderer dan hierarchy-nya diaktifkan
            sr.gameObject.SetActive(true);
            Transform t = sr.transform;
            while (t != null && t != sleepingBody.transform)
            {
                t.gameObject.SetActive(true);
                t = t.parent;
            }
            
            sr.color = Color.white;
            sr.enabled = true;
            if (sr.sprite == null && playerSpriteRenderer != null)
            {
                sr.sprite = playerSpriteRenderer.sprite;
            }
            
            SpriteRenderer bedSR = bedTransform.GetComponent<SpriteRenderer>();
            if (bedSR == null) bedSR = bedTransform.GetComponentInChildren<SpriteRenderer>();
            if (bedSR != null)
            {
                sr.sortingLayerName = bedSR.sortingLayerName;
                sr.sortingOrder = bedSR.sortingOrder + 1;
            }
            else
            {
                sr.sortingLayerName = "Default";
                sr.sortingOrder = 5;
            }
        }
        
        // Aktifkan Animator untuk play animasi tidur (idle biasa)
        var anim = sleepingBody.GetComponent<Animator>();
        if (anim == null) anim = sleepingBody.GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.enabled = true;
            SafeSetAnimBool(anim, "isMoving", false);
            SafeSetAnimFloat(anim, "MoveX", 0f);
            SafeSetAnimFloat(anim, "MoveY", -1f);
        }

        sleepingBody.SetActive(true);
    }
}
