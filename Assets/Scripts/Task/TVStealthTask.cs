using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SpaceJam.Minigames;

namespace SpaceJam.Minigames
{
    public enum TVDifficultyPreset
    {
        Easy,
        Normal,
        Hard,
        Extreme,
        Custom
    }

    public class TVStealthTask : BaseTask
    {
        [Header("Vision Cone Settings")]
        [Tooltip("The sweep rotation speed.")]
        [SerializeField] private float sweepSpeed = 3f;

        [Tooltip("The maximum angle of the sweep (left and right of base angle).")]
        [SerializeField] private float maxSweepAngle = 45f;

        [Tooltip("The base orientation angle of the television's vision cone (0 = pointing straight up).")]
        [SerializeField] private float baseAngle = 180f;

        [Tooltip("The range/length of the triangular vision cone.")]
        [SerializeField] private float visionRange = 5.5f;

        [Tooltip("The field of view (angle) of the vision cone triangle.")]
        [SerializeField] private float visionFovAngle = 40f;

        [Header("Stealth Gameplay & Difficulty")]
        [Tooltip("Difficulty level for the TV quest. Easy=15 stability damage, Normal=35, Hard=60, Extreme=95. Select Custom to set your own value below!")]
        [SerializeField] private TVDifficultyPreset difficulty = TVDifficultyPreset.Normal;

        [Tooltip("How much stability is deducted from the player if they touch the light (Overridden by preset unless set to Custom).")]
        [SerializeField] private float stabilityPenalty = 35f;

        [Tooltip("Wait time in seconds after getting caught before another warning is triggered.")]
        [SerializeField] private float caughtCooldown = 1.5f;

        [Tooltip("Transform representing the safe position where the player teleports when caught by the TV light.")]
        [SerializeField] private Transform safeSpawnPoint;

        [Header("Interaction Settings")]
        [Tooltip("The key to press to turn off the TV.")]
        [SerializeField] private KeyCode interactKey = KeyCode.F;

        [Header("Visual & Rendering Settings")]
        [Tooltip("The Sorting Layer name for the vision cone. Change this if the cone is covered by the floor!")]
        [SerializeField] private string coneSortingLayerName = "Default";

        [Tooltip("The Sorting Order for the vision cone. Increase this (e.g. 5, 10) if the floor is rendering on top of the light cone!")]
        [SerializeField] private int coneSortingOrder = 5;

        [Header("Visual References")]
        [Tooltip("The vision cone child GameObject.")]
        [SerializeField] private GameObject visionConeObject;

        [Tooltip("The Renderer of the vision cone (to highlight alerts).")]
        [SerializeField] private Renderer visionConeRenderer;

        [Tooltip("The visual object representing the TV screen when turned ON.")]
        [SerializeField] private GameObject tvOnVisual;

        [Tooltip("The visual object representing the TV screen when turned OFF.")]
        [SerializeField] private GameObject tvOffVisual;

        [Header("Colors")]
        [SerializeField] private Color normalConeColor = new Color(1f, 0.9f, 0.1f, 0.25f); // Soft yellow
        [SerializeField] private Color alertConeColor = new Color(1f, 0.1f, 0.1f, 0.45f); // Warning red

        // Runtime states
        private bool isPlayerInRange = false;
        private bool isCooldown = false;
        private bool isAlerted = false;
        private float alertEndTime = 0f;

        public bool IsCompleted => completed;

        private void Awake()
        {
            // Automatically register this stealth task to the PhaseTaskManager's Dream tasks list at startup!
            PhaseTaskManager taskManager = GetPhaseTaskManager();
            if (taskManager != null)
            {
                if (taskManager.dreamTasks == null)
                {
                    taskManager.dreamTasks = new List<BaseTask>();
                }
                if (!taskManager.dreamTasks.Contains(this))
                {
                    taskManager.dreamTasks.Add(this);
                    Debug.Log("[TVStealthTask] Successfully auto-registered itself to PhaseTaskManager's Dream Tasks!");
                }
            }
        }

        private void Start()
        {
            ApplyDifficultyPreset();
            GenerateVisionConeGeometry();
            InitializeVisionConeScript();
            EnsureTVSolidBody();

            // Dynamically add InteractObject2D if not present (for old builds that don't have it)
            InteractObject2D interactObj = GetComponent<InteractObject2D>();
            if (interactObj == null)
            {
                interactObj = gameObject.AddComponent<InteractObject2D>();
                Debug.Log("[TVStealthTask] InteractObject2D was missing, added dynamically!");
            }
            interactObj.requireActiveTask = true;
            interactObj.linkedTask = this;
            interactObj.onInteract.RemoveListener(TurnOffTV); // Avoid duplicates
            interactObj.onInteract.AddListener(TurnOffTV);

            // Ensure interaction collider radius is large enough
            CircleCollider2D interactCol = GetComponent<CircleCollider2D>();
            if (interactCol == null)
            {
                interactCol = gameObject.AddComponent<CircleCollider2D>();
                interactCol.isTrigger = true;
            }
            if (interactCol.radius < 2.2f) interactCol.radius = 2.2f;

            // Auto-subscribe to PhaseLoopManager if running in standalone mode (no PhaseTaskManager in scene)
            PhaseLoopManager phaseLoop = GetPhaseLoopManager();
            if (phaseLoop != null)
            {
                phaseLoop.OnPhaseChanged.AddListener(OnPhaseChanged);

                // CRITICAL FIX: If we are already in GameState.Dream at startup, activate immediately!
                PhaseTaskManager taskManager = FindObjectOfType<PhaseTaskManager>();
                if (taskManager == null && phaseLoop.CurrentState == GameState.Dream)
                {
                    Debug.Log("[TVStealthTask] Startup detected GameState.Dream. Automatically activating TV quest!");
                    ActivateTask();
                }
            }

            // Initial state: hide vision cone until the task is active
            if (!IsActive)
            {
                UpdateTVState(false);
            }
        }

        private void InitializeVisionConeScript()
        {
            if (visionConeObject == null) return;

            TVVisionCone cone = visionConeObject.GetComponent<TVVisionCone>();
            if (cone == null)
            {
                cone = visionConeObject.AddComponent<TVVisionCone>();
                Debug.Log("[TVStealthTask] TVVisionCone script was missing, added dynamically!");
            }
            cone.Initialize(this);
        }

        private void EnsureTVSolidBody()
        {
            // Add a solid BoxCollider2D so the player cannot walk through the TV
            BoxCollider2D solidCol = GetComponent<BoxCollider2D>();
            if (solidCol == null)
            {
                solidCol = gameObject.AddComponent<BoxCollider2D>();
            }
            solidCol.isTrigger = false; // SOLID - blocks player movement
            solidCol.size = new Vector2(1.4f, 1.1f); // Match TV body size
            solidCol.offset = Vector2.zero;
        }

        private void OnDestroy()
        {
            PhaseLoopManager phaseLoop = GetPhaseLoopManager();
            if (phaseLoop != null)
            {
                phaseLoop.OnPhaseChanged.RemoveListener(OnPhaseChanged);
            }
        }

        private void OnPhaseChanged(GameState state)
        {
            // Standalone mode: if there's no PhaseTaskManager in the scene,
            // we activate/deactivate the TV quest based directly on the PhaseLoopManager states!
            PhaseTaskManager taskManager = FindObjectOfType<PhaseTaskManager>();
            if (taskManager == null)
            {
                if (state == GameState.Dream)
                {
                    ActivateTask();
                }
                else
                {
                    DeactivateTask();
                }
            }
        }

        public override void ActivateTask()
        {
            base.ActivateTask();
            completed = false;
            ApplyDifficultyPreset();
            ApplyRenderingSettings();
            UpdateTVState(true);
            isPlayerInRange = false;
            isCooldown = false;
            isAlerted = false;

            SetConeColor(normalConeColor);

            Debug.Log($"[TVStealthTask] TV Stealth Task Activated! Find the TV and turn it off using '{interactKey}' without touching the light!");
        }

        public override void DeactivateTask()
        {
            base.DeactivateTask();
            UpdateTVState(false);
        }

        public override void ResetTask()
        {
            base.ResetTask();
            UpdateTVState(false);
            isPlayerInRange = false;
            isCooldown = false;
            isAlerted = false;
        }

        public override void CompleteTask()
        {
            completed = true;
            DeactivateTask();

            PhaseTaskManager taskManager = GetPhaseTaskManager();
            if (taskManager != null)
            {
                taskManager.CompleteTask(this);
            }
            else
            {
                Debug.Log("[TVStealthTask] TV successfully turned off! Quest completed in standalone Phase-Loop mode.");
            }
        }

        private void Update()
        {
            if (!IsActive || completed) return;

            // 1. Sweep the vision cone back and forth smoothly
            if (visionConeObject != null)
            {
                // Smooth Lerp Ping-Pong angle calculation
                float t = Mathf.PingPong(Time.time * sweepSpeed, 1f);
                float angle = Mathf.Lerp(-maxSweepAngle, maxSweepAngle, t);
                visionConeObject.transform.localRotation = Quaternion.Euler(0f, 0f, baseAngle + angle);
            }

            // 2. Handle interaction when player is in range and presses key
            if (isPlayerInRange && Input.GetKeyDown(interactKey))
            {
                TurnOffTV();
            }

            // 3. Reset alert color back to normal after duration
            if (isAlerted && Time.time > alertEndTime)
            {
                isAlerted = false;
                SetConeColor(normalConeColor);
            }
        }

        public void OnPlayerDetected()
        {
            if (!IsActive || completed || isCooldown) return;

            Debug.LogWarning("[TVStealthTask] Player caught in TV light! Stability reduced and teleporting to safety.");

            // 1. Trigger cooldown
            StartCoroutine(TriggerCooldownRoutine());

            // 2. Alert Visuals
            isAlerted = true;
            alertEndTime = Time.time + 1f;
            SetConeColor(alertConeColor);

            // 3. Apply Stability Penalty
            if (PlayerStatus.Instance != null)
            {
                PlayerStatus.Instance.ReduceStability(stabilityPenalty);
            }

            // 4. Teleport Player to safeSpawnPoint
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null && safeSpawnPoint != null)
            {
                player.transform.position = safeSpawnPoint.position;
                player.transform.rotation = safeSpawnPoint.rotation;

                Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
                if (playerRb != null)
                {
                    playerRb.velocity = Vector2.zero;
                }
                Debug.Log("[TVStealthTask] Player successfully teleported to safe spawn point.");
            }
        }

        private IEnumerator TriggerCooldownRoutine()
        {
            isCooldown = true;
            yield return new WaitForSeconds(caughtCooldown);
            isCooldown = false;
        }

        public void TurnOffTV()
        {
            Debug.Log("[TVStealthTask] TV successfully turned off! Task completed.");
            completed = true;
            UpdateTVState(false);
            
            // Invoke the base class completion
            CompleteTask();
        }

        private void UpdateTVState(bool active)
        {
            if (visionConeObject != null)
            {
                visionConeObject.SetActive(active);
            }

            if (tvOnVisual != null)
            {
                tvOnVisual.SetActive(active);
            }

            if (tvOffVisual != null)
            {
                tvOffVisual.SetActive(!active);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (IsPlayer(other))
            {
                isPlayerInRange = true;
                Debug.Log("[TVStealthTask] Player entered interaction zone!");
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (IsPlayer(other))
            {
                isPlayerInRange = false;
                Debug.Log("[TVStealthTask] Player exited interaction zone.");
            }
        }

        private bool IsPlayer(Collider2D other)
        {
            if (other == null) return false;
            if (other.CompareTag("Player")) return true;
            if (other.GetComponent<PlayerMovement>() != null || other.GetComponentInParent<PlayerMovement>() != null) return true;
            if (other.GetComponent<PlayerStatus>() != null || other.GetComponentInParent<PlayerStatus>() != null) return true;
            return false;
        }

        private void SetConeColor(Color color)
        {
            if (visionConeRenderer == null) return;

            // Update material color
            visionConeRenderer.material.color = color;

            // Also update vertex colors on the mesh (required for Sprites/Default shader)
            if (visionConeRenderer is MeshRenderer mr)
            {
                MeshFilter mf = mr.GetComponent<MeshFilter>();
                if (mf != null && mf.mesh != null)
                {
                    Color32[] vertexColors = new Color32[mf.mesh.vertexCount];
                    for (int i = 0; i < vertexColors.Length; i++)
                    {
                        vertexColors[i] = color;
                    }
                    mf.mesh.colors32 = vertexColors;
                }
            }
        }

        private void OnValidate()
        {
            ApplyDifficultyPreset();
            ApplyRenderingSettings();
            if (visionConeObject != null)
            {
                GenerateVisionConeGeometry();
            }
        }

        public void GenerateVisionConeGeometry()
        {
            if (visionConeObject == null) return;

            // 1. Ensure Kinematic Rigidbody2D on visionConeObject to guarantee physics triggers always work
            Rigidbody2D visionConeRb = visionConeObject.GetComponent<Rigidbody2D>();
            if (visionConeRb == null)
            {
                visionConeRb = visionConeObject.AddComponent<Rigidbody2D>();
            }
            visionConeRb.bodyType = RigidbodyType2D.Kinematic;
            visionConeRb.simulated = true;
            visionConeRb.useFullKinematicContacts = true;

            // 2. Update PolygonCollider2D on visionConeObject
            PolygonCollider2D polyCol = visionConeObject.GetComponent<PolygonCollider2D>();
            if (polyCol == null)
            {
                polyCol = visionConeObject.AddComponent<PolygonCollider2D>();
            }
            polyCol.isTrigger = true;

            float fovRad = visionFovAngle * Mathf.Deg2Rad;
            float halfWidth = visionRange * Mathf.Tan(fovRad / 2f);

            Vector2[] trianglePoints = new Vector2[3];
            trianglePoints[0] = Vector2.zero;
            trianglePoints[1] = new Vector2(-halfWidth, visionRange);
            trianglePoints[2] = new Vector2(halfWidth, visionRange);
            polyCol.points = trianglePoints;

            // 3. Update Mesh on ConeVisual child
            Transform visualTransform = visionConeObject.transform.Find("ConeVisual");
            GameObject coneVisual;
            if (visualTransform == null)
            {
                coneVisual = new GameObject("ConeVisual");
                coneVisual.transform.SetParent(visionConeObject.transform);
            }
            else
            {
                coneVisual = visualTransform.gameObject;
            }

            // CRITICAL: Remove old SpriteRenderer from previous builds (it renders a RECTANGLE over the mesh triangle!)
            SpriteRenderer oldSR = coneVisual.GetComponent<SpriteRenderer>();
            if (oldSR != null)
            {
                Debug.Log("[TVStealthTask] Destroying old SpriteRenderer on ConeVisual (was causing rectangle visual instead of triangle)!");
                if (Application.isPlaying)
                    Destroy(oldSR);
                else
                    DestroyImmediate(oldSR);
            }

            coneVisual.transform.localPosition = Vector3.zero;
            coneVisual.transform.localRotation = Quaternion.identity;
            coneVisual.transform.localScale = Vector3.one;

            MeshFilter meshFilter = coneVisual.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = coneVisual.AddComponent<MeshFilter>();
            }

            MeshRenderer meshRenderer = coneVisual.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                meshRenderer = coneVisual.AddComponent<MeshRenderer>();
            }

            // Create/update procedural mesh
            Mesh mesh = new Mesh();
            mesh.name = "TVVisionConeMesh";

            Vector3[] vertices = new Vector3[3];
            vertices[0] = Vector3.zero;
            vertices[1] = new Vector3(-halfWidth, visionRange, 0f);
            vertices[2] = new Vector3(halfWidth, visionRange, 0f);

            int[] triangles = new int[6];
            // Both faces so the triangle is visible from any camera angle
            triangles[0] = 0;
            triangles[1] = 1;
            triangles[2] = 2;
            triangles[3] = 0;
            triangles[4] = 2;
            triangles[5] = 1;

            Vector2[] uvs = new Vector2[3];
            uvs[0] = new Vector2(0.5f, 0f);
            uvs[1] = new Vector2(0f, 1f);
            uvs[2] = new Vector2(1f, 1f);

            // CRITICAL: Set vertex colors! Without this, Sprites/Default shader renders invisible!
            Color coneColor = isAlerted ? alertConeColor : normalConeColor;
            Color32[] vertexColors = new Color32[3];
            vertexColors[0] = coneColor;
            vertexColors[1] = coneColor;
            vertexColors[2] = coneColor;

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.colors32 = vertexColors;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            meshFilter.mesh = mesh;

            // Robust shader fallback chain (URP, Built-in, etc.)
            Material coneMat = new Material(Shader.Find("Sprites/Default"));
            // Try multiple shaders in case Sprites/Default is not available
            string[] shaderFallbacks = new string[]
            {
                "Sprites/Default",
                "Universal Render Pipeline/2D/Sprite-Unlit-Default",
                "Unlit/Transparent",
                "Unlit/Color",
                "UI/Default"
            };
            foreach (string shaderName in shaderFallbacks)
            {
                Shader s = Shader.Find(shaderName);
                if (s != null)
                {
                    coneMat = new Material(s);
                    Debug.Log($"[TVStealthTask] Vision cone using shader: {shaderName}");
                    break;
                }
            }
            coneMat.color = coneColor;
            meshRenderer.material = coneMat;
            meshRenderer.sortingLayerName = coneSortingLayerName;
            meshRenderer.sortingOrder = coneSortingOrder;

            visionConeRenderer = meshRenderer;
            Debug.Log($"[TVStealthTask] Vision cone mesh generated! Vertices: {mesh.vertexCount}, Material: {meshRenderer.material.shader.name}, Color: {coneColor}");
        }

        public void ApplyDifficultyPreset()
        {
            switch (difficulty)
            {
                case TVDifficultyPreset.Easy:
                    stabilityPenalty = 15f;
                    break;
                case TVDifficultyPreset.Normal:
                    stabilityPenalty = 35f;
                    break;
                case TVDifficultyPreset.Hard:
                    stabilityPenalty = 60f;
                    break;
                case TVDifficultyPreset.Extreme:
                    stabilityPenalty = 95f;
                    break;
                case TVDifficultyPreset.Custom:
                    break;
            }
        }

        public void ApplyRenderingSettings()
        {
            if (visionConeRenderer != null)
            {
                visionConeRenderer.sortingLayerName = coneSortingLayerName;
                visionConeRenderer.sortingOrder = coneSortingOrder;
            }
        }

        // Editor helper methods for dynamic building
        public void Configure(float speed, float angle, float baseDir, TVDifficultyPreset diffPreset, float penalty, string sortingLayer, int sortingOrder, Transform safePoint, GameObject cone, Renderer sr, GameObject onVis, GameObject offVis)
        {
            sweepSpeed = speed;
            maxSweepAngle = angle;
            baseAngle = baseDir;
            difficulty = diffPreset;
            stabilityPenalty = penalty;
            coneSortingLayerName = sortingLayer;
            coneSortingOrder = sortingOrder;
            safeSpawnPoint = safePoint;
            visionConeObject = cone;
            visionConeRenderer = sr;
            tvOnVisual = onVis;
            tvOffVisual = offVis;
            ApplyRenderingSettings();
        }

        [ContextMenu("Force Activate TV Quest (Play Mode Only)")]
        public void DebugForceActivate()
        {
            if (Application.isPlaying)
            {
                StartCoroutine(ForceActivateRoutine());
            }
            else
            {
                Debug.LogWarning("[TVStealthTask] Force activation can only be done while the game is playing!");
            }
        }

        private PhaseTaskManager GetPhaseTaskManager()
        {
            PhaseTaskManager manager = FindObjectOfType<PhaseTaskManager>();
            if (manager != null) return manager;

            // Try to find inactive one in the scene (useful if its GameObject is disabled)
            PhaseTaskManager[] allManagers = Resources.FindObjectsOfTypeAll<PhaseTaskManager>();
            foreach (var m in allManagers)
            {
                if (m.gameObject.scene.name != null) // In a scene, not a prefab
                {
                    Debug.LogWarning($"[TVStealthTask] Found inactive PhaseTaskManager on GameObject '{m.gameObject.name}'. Activating it!");
                    m.gameObject.SetActive(true);
                    return m;
                }
            }
            return null;
        }

        private PhaseLoopManager GetPhaseLoopManager()
        {
            PhaseLoopManager manager = FindObjectOfType<PhaseLoopManager>();
            if (manager != null) return manager;

            // Try to find inactive one in the scene
            PhaseLoopManager[] allLoops = Resources.FindObjectsOfTypeAll<PhaseLoopManager>();
            foreach (var l in allLoops)
            {
                if (l.gameObject.scene.name != null) // In a scene, not a prefab
                {
                    Debug.LogWarning($"[TVStealthTask] Found inactive PhaseLoopManager on GameObject '{l.gameObject.name}'. Activating it!");
                    l.gameObject.SetActive(true);
                    return l;
                }
            }
            return null;
        }

        private IEnumerator ForceActivateRoutine()
        {
            gameObject.SetActive(true);

            // Double check registration to PhaseTaskManager
            PhaseTaskManager taskManager = GetPhaseTaskManager();
            if (taskManager != null)
            {
                if (taskManager.dreamTasks == null)
                {
                    taskManager.dreamTasks = new List<BaseTask>();
                }
                if (!taskManager.dreamTasks.Contains(this))
                {
                    taskManager.dreamTasks.Add(this);
                    Debug.Log("[TVStealthTask] Dyn-registered to Dream Tasks list on force activation.");
                }
            }

            PhaseLoopManager phaseLoop = GetPhaseLoopManager();
            if (phaseLoop != null && phaseLoop.CurrentState != GameState.Dream)
            {
                phaseLoop.StartManualTransition(GameState.Dream);
            }

            // Wait 2 frames so that PhaseLoopManager and PhaseTaskManager have fully completed their SetupTasks()!
            yield return null;
            yield return null;

            // Refetch or use the cached taskManager
            if (taskManager == null)
            {
                taskManager = GetPhaseTaskManager();
            }

            if (taskManager != null)
            {
                taskManager.ForceActivateTask(this);
            }
            else
            {
                Debug.LogWarning("[TVStealthTask] PhaseTaskManager was not found in scene! Activating TV Stealth Quest directly in standalone mode.");
                ActivateTask();
            }
        }
    }
}
