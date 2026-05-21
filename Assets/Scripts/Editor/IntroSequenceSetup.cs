using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Editor: Tools > Space-Jam > Setup Intro Sequence
/// Sekali klik — buat semua object + assign semua field.
/// 
/// Membuat Canvas TERPISAH (Screen Space Overlay, sort order tinggi)
/// supaya fog/dialogue PASTI render di atas segalanya.
/// </summary>
public class IntroSequenceSetup
{
    [MenuItem("Tools/Space-Jam/Setup Intro Sequence")]
    public static void Setup()
    {
        // =========================================================
        // CLEANUP
        // =========================================================
        var existingManager = Object.FindObjectOfType<IntroSequenceManager>();
        if (existingManager != null)
        {
            if (!EditorUtility.DisplayDialog(
                "Intro Setup",
                "IntroManager sudah ada. Hapus dan buat ulang?",
                "Ya", "Batal"))
                return;

            Undo.DestroyObjectImmediate(existingManager.gameObject);
        }

        // Hapus object lama dari setup sebelumnya
        string[] oldNames = {
            "IntroCanvas", "ZzzText", "WakeUpPrompt",
            "SpawnNearBed", "EnemySpawnPoint", "EnemyTargetPoint",
            "IntroManager", "__IntroBedOutline"
        };
        foreach (string n in oldNames) DestroyByName(n);

        // =========================================================
        // FIND EXISTING SCENE OBJECTS
        // =========================================================

        // Player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            var pm = Object.FindObjectOfType<PlayerMovement>();
            if (pm != null) player = pm.gameObject;
        }

        PlayerMovement playerMovement = null;
        SpriteRenderer playerSR = null;
        if (player != null)
        {
            playerMovement = player.GetComponent<PlayerMovement>();
            playerSR = player.GetComponent<SpriteRenderer>();
            if (playerSR == null) playerSR = player.GetComponentInChildren<SpriteRenderer>();
        }

        // Bed — cari by name (TIDAK menyentuh InteractObject2D)
        Transform bedTransform = null;
        // 1. Cari exact name "Bed" atau "Kasur" (case-insensitive)
        foreach (var go in Object.FindObjectsOfType<GameObject>(true))
        {
            string n = go.name.ToLower();
            if (n == "bed" || n == "kasur")
            {
                // Pastikan ini bukan UI element
                if (go.GetComponent<RectTransform>() != null) continue;
                // Pastikan ini bukan child dari player
                if (player != null && go.transform.IsChildOf(player.transform)) continue;
                bedTransform = go.transform;
                break;
            }
        }

        // 2. Jika tidak ketemu, cari name containing "bed" atau "kasur" tapi hindari spawn points, outlines, dll.
        if (bedTransform == null)
        {
            foreach (var go in Object.FindObjectsOfType<GameObject>(true))
            {
                string n = go.name.ToLower();
                if ((n.Contains("bed") || n.Contains("kasur")) && 
                    !n.Contains("spawn") && !n.Contains("outline") && !n.Contains("text") && !n.Contains("prompt") && !n.Contains("canvas") && !n.Contains("manager"))
                {
                    // Pastikan ini bukan UI element
                    if (go.GetComponent<RectTransform>() != null) continue;
                    // Pastikan ini bukan child dari player
                    if (player != null && go.transform.IsChildOf(player.transform)) continue;
                    bedTransform = go.transform;
                    break;
                }
            }
        }

        if (bedTransform == null)
        {
            string prefabPath = "Assets/Prefabs/Bed.prefab";
            var bedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (bedPrefab == null)
            {
                bedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CODINGAN/Scenes/bed.prefab");
            }

            if (bedPrefab != null)
            {
                GameObject bedGO = (GameObject)PrefabUtility.InstantiatePrefab(bedPrefab);
                bedGO.name = "Bed";
                bedGO.transform.position = new Vector3(-2.54f, -2.78f, 0f);
                Undo.RegisterCreatedObjectUndo(bedGO, "Instantiate Bed Prefab");
                bedTransform = bedGO.transform;
                Debug.Log("[IntroSetup] Bed prefab instantiated automatically at default position (-2.54, -2.78, 0).");
            }
        }

        // PhaseLoopManager
        PhaseLoopManager phaseLoop = Object.FindObjectOfType<PhaseLoopManager>();

        // Enemy Prefab
        GameObject enemyPrefab = FindEnemyPrefab();

        // Camera
        Camera mainCam = Camera.main;
        float camY = mainCam != null ? mainCam.transform.position.y : 0f;
        float camHeight = mainCam != null ? mainCam.orthographicSize : 5f;

        Vector3 bedPos = bedTransform != null
            ? bedTransform.position
            : new Vector3(-3f, -2f, 0f);

        // =========================================================
        // CREATE INTRO CANVAS (TERPISAH, PASTI DI ATAS)
        // =========================================================
        GameObject introCanvasGO = new GameObject("IntroCanvas");
        Canvas introCanvas = introCanvasGO.AddComponent<Canvas>();
        introCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        introCanvas.sortingOrder = 100; // Di atas semua canvas lain

        CanvasScaler scaler = introCanvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        introCanvasGO.AddComponent<GraphicRaycaster>();
        introCanvasGO.layer = LayerMask.NameToLayer("UI");

        Undo.RegisterCreatedObjectUndo(introCanvasGO, "Create IntroCanvas");

        // =========================================================
        // FOG OVERLAY (Image hitam fullscreen + CanvasGroup)
        // =========================================================
        GameObject fogGO = CreateUIChild("FogOverlay", introCanvasGO.transform);

        RectTransform fogRT = fogGO.GetComponent<RectTransform>();
        fogRT.anchorMin = Vector2.zero;
        fogRT.anchorMax = Vector2.one;
        fogRT.sizeDelta = Vector2.zero;
        fogRT.anchoredPosition = Vector2.zero;

        Image fogImg = fogGO.AddComponent<Image>();
        fogImg.color = new Color(0f, 0f, 0f, 0.95f); // Hitam transparan di editor agar tidak putih mencolok
        fogImg.raycastTarget = true;

        CanvasGroup fogCG = fogGO.AddComponent<CanvasGroup>();
        fogCG.alpha = 0.97f;
        fogCG.blocksRaycasts = true;

        // --- "Tap to Play" text ---
        GameObject tapGO = CreateUIChild("TapToPlayText", fogGO.transform);

        RectTransform tapRT = tapGO.GetComponent<RectTransform>();
        tapRT.anchorMin = new Vector2(0.5f, 0.35f);
        tapRT.anchorMax = new Vector2(0.5f, 0.35f);
        tapRT.sizeDelta = new Vector2(600, 80);
        tapRT.anchoredPosition = Vector2.zero;

        TextMeshProUGUI tapTMP = tapGO.AddComponent<TextMeshProUGUI>();
        tapTMP.text = "Tap to Play";
        tapTMP.fontSize = 20; // Elegan, tidak terlalu besar
        tapTMP.alignment = TextAlignmentOptions.Center;
        tapTMP.color = new Color(0.75f, 0.8f, 1f, 1f);
        tapTMP.fontStyle = FontStyles.Normal;

        // =========================================================
        // DIALOGUE PANEL
        // =========================================================
        GameObject dialoguePanelGO = CreateUIChild("DialoguePanel", introCanvasGO.transform);

        RectTransform dRT = dialoguePanelGO.GetComponent<RectTransform>();
        dRT.anchorMin = new Vector2(0.5f, 0f);
        dRT.anchorMax = new Vector2(0.5f, 0f);
        dRT.pivot = new Vector2(0.5f, 0f);
        dRT.sizeDelta = new Vector2(650, 160);
        dRT.anchoredPosition = new Vector2(0, 60);

        Image dBG = dialoguePanelGO.AddComponent<Image>();
        dBG.color = new Color(0.02f, 0.02f, 0.06f, 0.88f);

        // Dialogue Text
        GameObject dtGO = CreateUIChild("DialogueText", dialoguePanelGO.transform);
        RectTransform dtRT = dtGO.GetComponent<RectTransform>();
        dtRT.anchorMin = Vector2.zero;
        dtRT.anchorMax = Vector2.one;
        dtRT.sizeDelta = new Vector2(-40, -45);
        dtRT.anchoredPosition = new Vector2(0, 8);

        TextMeshProUGUI dtTMP = dtGO.AddComponent<TextMeshProUGUI>();
        dtTMP.text = "";
        dtTMP.fontSize = 16; // Elegan & premium
        dtTMP.fontStyle = FontStyles.Italic;
        dtTMP.alignment = TextAlignmentOptions.Center;
        dtTMP.color = Color.white;
        dtTMP.enableWordWrapping = true;

        // Continue Text
        GameObject ctGO = CreateUIChild("ContinueText", dialoguePanelGO.transform);
        RectTransform ctRT = ctGO.GetComponent<RectTransform>();
        ctRT.anchorMin = new Vector2(1f, 0f);
        ctRT.anchorMax = new Vector2(1f, 0f);
        ctRT.pivot = new Vector2(1f, 0f);
        ctRT.sizeDelta = new Vector2(250, 30);
        ctRT.anchoredPosition = new Vector2(-15, 12);

        TextMeshProUGUI ctTMP = ctGO.AddComponent<TextMeshProUGUI>();
        ctTMP.text = "Press any key...";
        ctTMP.fontSize = 11; // Sangat proporsional
        ctTMP.alignment = TextAlignmentOptions.Right;
        ctTMP.color = new Color(0.6f, 0.6f, 0.7f, 1f);

        // =========================================================
        // SCREEN FADER
        // =========================================================
        CanvasGroup screenFaderCG = null;

        // Cek apakah PhaseLoopManager punya screenFader
        if (phaseLoop != null && phaseLoop.screenFader != null)
        {
            screenFaderCG = phaseLoop.screenFader;
        }

        if (screenFaderCG == null)
        {
            GameObject sfGO = CreateUIChild("ScreenFader", introCanvasGO.transform);
            RectTransform sfRT = sfGO.GetComponent<RectTransform>();
            sfRT.anchorMin = Vector2.zero;
            sfRT.anchorMax = Vector2.one;
            sfRT.sizeDelta = Vector2.zero;
            sfRT.anchoredPosition = Vector2.zero;

            Image sfImg = sfGO.AddComponent<Image>();
            sfImg.color = Color.black;
            sfImg.raycastTarget = true;

            screenFaderCG = sfGO.AddComponent<CanvasGroup>();
            screenFaderCG.alpha = 0f;
            screenFaderCG.blocksRaycasts = false;
        }

        // =========================================================
        // WORLD SPACE OBJECTS
        // =========================================================

        // Zzz Text (world space TMP)
        GameObject zzzGO = new GameObject("ZzzText");
        zzzGO.transform.localScale = new Vector3(0.29f, 0.29f, 1f); // Skala diatur jadi 0.29 sesuai request
        TextMeshPro zzzTMP = zzzGO.AddComponent<TextMeshPro>();
        zzzTMP.text = "Z z z . . .";
        zzzTMP.fontSize = 18; // Crisp & resolusi tinggi!
        zzzTMP.alignment = TextAlignmentOptions.Center;
        zzzTMP.color = new Color(0.6f, 0.8f, 1f, 0.85f);
        zzzGO.transform.position = bedPos + new Vector3(0f, 0.65f, 0f);

        var zzzMR = zzzGO.GetComponent<MeshRenderer>();
        if (zzzMR != null) zzzMR.sortingOrder = 20;

        Undo.RegisterCreatedObjectUndo(zzzGO, "Create ZzzText");

        // "[E] Wake Up" prompt (world space TMP)
        GameObject wakeUpGO = new GameObject("WakeUpPrompt");
        wakeUpGO.transform.localScale = new Vector3(0.29f, 0.29f, 1f); // Skala diatur jadi 0.29 sesuai request
        TextMeshPro wakeUpTMP = wakeUpGO.AddComponent<TextMeshPro>();
        wakeUpTMP.text = "[E] Wake Up";
        wakeUpTMP.fontSize = 16; // Crisp!
        wakeUpTMP.alignment = TextAlignmentOptions.Center;
        wakeUpTMP.color = new Color(1f, 1f, 0.8f, 1f);
        wakeUpGO.transform.position = bedPos + new Vector3(0f, 1.1f, 0f);

        var wakeUpMR = wakeUpGO.GetComponent<MeshRenderer>();
        if (wakeUpMR != null) wakeUpMR.sortingOrder = 20;

        Undo.RegisterCreatedObjectUndo(wakeUpGO, "Create WakeUpPrompt");

        // SpawnNearBed
        GameObject snb = new GameObject("SpawnNearBed");
        snb.transform.position = bedPos + new Vector3(0.9f, -0.2f, 0f);
        Undo.RegisterCreatedObjectUndo(snb, "Create SpawnNearBed");

        // EnemySpawnPoint (off-screen bawah)
        GameObject esp = new GameObject("EnemySpawnPoint");
        esp.transform.position = new Vector3(bedPos.x + 1.5f, camY - camHeight - 2.5f, 0f);
        Undo.RegisterCreatedObjectUndo(esp, "Create EnemySpawnPoint");

        // EnemyTargetPoint (tengah scene)
        GameObject etp = new GameObject("EnemyTargetPoint");
        etp.transform.position = new Vector3(bedPos.x + 1.5f, camY - 0.5f, 0f);
        Undo.RegisterCreatedObjectUndo(etp, "Create EnemyTargetPoint");

        // =========================================================
        // CREATE INTRO MANAGER + WIRE FIELDS
        // =========================================================
        GameObject introGO = new GameObject("IntroManager");
        IntroSequenceManager mgr = introGO.AddComponent<IntroSequenceManager>();
        Undo.RegisterCreatedObjectUndo(introGO, "Create IntroManager");

        SerializedObject so = new SerializedObject(mgr);

        SetRef(so, "fogOverlay", fogCG);
        SetRef(so, "tapToPlayText", tapTMP);
        SetRef(so, "playerObject", player);
        SetRef(so, "playerMovement", playerMovement);
        SetRef(so, "playerSpriteRenderer", playerSR);
        SetRef(so, "spawnNearBed", snb.transform);
        SetRef(so, "bedTransform", bedTransform);
        SetRef(so, "zzzTextObject", zzzGO);
        SetRef(so, "wakeUpPrompt", wakeUpGO);
        SetRef(so, "enemyPrefab", enemyPrefab);
        SetRef(so, "enemySpawnPoint", esp.transform);
        SetRef(so, "enemyTargetPoint", etp.transform);
        SetRef(so, "dialoguePanel", dialoguePanelGO);
        SetRef(so, "dialogueText", dtTMP);
        SetRef(so, "continueText", ctTMP);
        SetRef(so, "phaseLoopManager", phaseLoop);
        SetRef(so, "screenFader", screenFaderCG);
        SetRef(so, "introCanvasObject", introCanvasGO);

        so.ApplyModifiedProperties();

        // =========================================================
        // DISABLE OLD SYSTEMS
        // =========================================================
        var oldMenu = Object.FindObjectOfType<SeamlessMainMenu>();
        if (oldMenu != null)
        {
            Undo.RecordObject(oldMenu, "Disable SeamlessMainMenu");
            oldMenu.enabled = false;
        }

        // =========================================================
        // FINALIZE
        // =========================================================
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Selection.activeGameObject = introGO;

        // Report
        string r = "INTRO SEQUENCE SETUP SELESAI!\n\n";
        r += Stat("Player", player);
        r += Stat("PlayerMovement", playerMovement);
        r += Stat("SpriteRenderer", playerSR);
        r += Stat("Bed (kasur)", bedTransform != null ? bedTransform.gameObject : null);
        r += Stat("PhaseLoopManager", phaseLoop);
        r += Stat("Enemy Prefab", enemyPrefab);
        r += Stat("Screen Fader", screenFaderCG);
        r += "\nPosisi SpawnNearBed, EnemySpawnPoint, EnemyTargetPoint\n";
        r += "bisa digeser di Scene View.\n\n";

        bool ok = player != null && playerMovement != null
            && bedTransform != null && phaseLoop != null && enemyPrefab != null;

        r += ok
            ? "SEMUA TERISI! Langsung Play."
            : "Ada field [X] belum terisi — assign manual di Inspector.";

        EditorUtility.DisplayDialog("Intro Sequence Setup", r, "OK");
        Debug.Log("[IntroSetup] " + r);
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private static GameObject CreateUIChild(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.layer = LayerMask.NameToLayer("UI");
        return go;
    }

    private static void SetRef(SerializedObject so, string prop, Object value)
    {
        var p = so.FindProperty(prop);
        if (p != null) p.objectReferenceValue = value;
        else Debug.LogWarning($"[IntroSetup] Property '{prop}' not found!");
    }

    private static string Stat(string label, Object obj)
    {
        return $"  [{(obj != null ? "OK" : "X ")}] {label}: " +
               $"{(obj != null ? obj.name : "NOT FOUND")}\n";
    }

    private static GameObject FindEnemyPrefab()
    {
        string[] paths = {
            "Assets/Enemy.prefab",
            "Assets/ShadowEnemy.prefab",
            "Assets/Prefabs/Enemy.prefab"
        };

        foreach (var path in paths)
        {
            var p = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (p != null) return p;
        }

        foreach (var guid in AssetDatabase.FindAssets("Enemy t:Prefab"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.ToLower().Contains("enemy"))
                return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        return null;
    }

    private static void DestroyByName(string name)
    {
        foreach (var go in Object.FindObjectsOfType<GameObject>(true))
            if (go.name == name) Undo.DestroyObjectImmediate(go);
    }
}
