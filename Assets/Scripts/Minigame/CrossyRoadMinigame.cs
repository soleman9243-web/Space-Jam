using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceJam.Minigames
{
    public class CrossyRoadMinigame : MonoBehaviour
    {
        [Header("Level Length / Height Config")]
        [Tooltip("The total height of the Crossy Road level. Adjust this to make the game shorter or longer.")]
        [SerializeField] private float levelHeight = 30f;
        
        [Tooltip("GameObject of the Finish Line (with a Trigger Collider 2D and CrossyRoadFinish script).")]
        [SerializeField] private Transform finishLineTransform;

        [Header("Arena & Objects Config")]
        [Tooltip("Parent GameObject of the minigame map/arena.")]
        [SerializeField] private GameObject minigameArenaParent;
        
        // Note: No manual deactivation list is needed anymore!
        // All room and quest objects are automatically found and deactivated in code.
        
        [Tooltip("Transform where the player will teleport to start the minigame.")]
        [SerializeField] private Transform playerStartPoint;

        [Header("Obstacle Spawning Config")]
        [Tooltip("Prefab of the obstacle/enemy. Must have a Collider 2D set to Is Trigger.")]
        [SerializeField] private GameObject obstaclePrefab;
        
        [Tooltip("X-coordinate where obstacles spawn on the left side.")]
        [SerializeField] private float leftSpawnX = -8f;
        
        [Tooltip("X-coordinate where obstacles spawn on the right side.")]
        [SerializeField] private float rightSpawnX = 8f;
        
        [Tooltip("Spawn interval in seconds.")]
        [SerializeField] private float spawnInterval = 1.2f;
        
        [Header("Spawn Position Config")]
        [Tooltip("Minimum distance ahead of the player to spawn obstacles.")]
        [SerializeField] private float minSpawnAhead = 3f;
        
        [Tooltip("Maximum distance ahead of the player to spawn obstacles.")]
        [SerializeField] private float maxSpawnAhead = 8f;
        
        [Header("Obstacle Speed Config")]
        [SerializeField] private float obstacleSpeedMin = 3f;
        [SerializeField] private float obstacleSpeedMax = 5.5f;

        [Header("Camera Follow Config")]
        [Tooltip("If checked, the main camera will automatically track the player vertically.")]
        [SerializeField] private bool enableCameraFollow = true;
        
        [Tooltip("How smoothly the camera catches up to the player.")]
        [Range(0.01f, 1f)]
        [SerializeField] private float cameraSmoothSpeed = 0.1f;

        [Header("Debug / Testing")]
        [Tooltip("Check this box in the Inspector during play mode to instantly force-start the minigame for testing.")]
        [SerializeField] private bool debugForceStartMinigame = false;

        // Runtime states
        private PhaseLoopManager phaseLoopManager;
        private bool isMinigameActive = false;
        private Coroutine spawnCoroutine;
        private Transform playerTransform;
        private Camera mainCamera;
        
        private Vector3 originalCameraPos;
        private bool cameraStateSaved = false;
        private List<GameObject> automaticallyDeactivatedObjects = new List<GameObject>();

        private Vector3 originalPlayerPos;
        private Quaternion originalPlayerRot;
        private bool playerStateSaved = false;

        public float LevelHeight { get => levelHeight; set => levelHeight = Mathf.Max(5f, value); }
        public bool IsMinigameActive => isMinigameActive;

        private void Start()
        {
            // Auto hide the minigame arena at game start
            if (minigameArenaParent != null)
            {
                minigameArenaParent.SetActive(false);
            }

            phaseLoopManager = FindObjectOfType<PhaseLoopManager>();
            if (phaseLoopManager != null)
            {
                phaseLoopManager.OnPhaseChanged.AddListener(OnPhaseChanged);
            }

            mainCamera = Camera.main;
        }

        private void OnDestroy()
        {
            if (phaseLoopManager != null)
            {
                phaseLoopManager.OnPhaseChanged.RemoveListener(OnPhaseChanged);
            }
        }

        private void OnPhaseChanged(GameState state)
        {
            if (state == GameState.Liminal)
            {
                if (!isMinigameActive)
                {
                    StartMinigame();
                }
            }
            else
            {
                if (isMinigameActive)
                {
                    StopMinigame(false);
                }
            }
        }

        public void StartMinigame()
        {
            isMinigameActive = true;
            Debug.Log($"[CrossyRoadMinigame] Starting Crossy Road! Level Height: {levelHeight}");

            // 1. Automatically find and deactivate all non-essential objects in the scene (ruangan langsung hilang otomatis!)
            automaticallyDeactivatedObjects.Clear();
            GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var obj in rootObjects)
            {
                // Skip essential objects so they aren't deactivated
                if (obj == gameObject) continue; // Skip CrossyRoadManager
                if (obj == minigameArenaParent) continue; // Skip Arena
                if (obj.CompareTag("Player") || obj.GetComponentInChildren<PlayerMovement>() != null || obj.GetComponentInChildren<PlayerStatus>() != null) continue; // Skip Player
                if (obj.CompareTag("MainCamera") || obj.GetComponentInChildren<Camera>() != null) continue; // Skip Main Camera
                
                // Skip core managers and UI canvases
                string nameLower = obj.name.ToLower();
                if (nameLower.Contains("camera") || 
                    nameLower.Contains("manager") || 
                    nameLower.Contains("canvas") || 
                    nameLower.Contains("event") ||
                    nameLower.Contains("ui") ||
                    obj.GetComponent<Canvas>() != null ||
                    obj.GetComponent<AudioListener>() != null)
                {
                    continue;
                }

                // NEW: Destroy temporary objects like bullets or clones so they don't linger or reactivate!
                if (nameLower.Contains("bullet") || nameLower.Contains("clone") || obj.GetComponentInChildren<RingBullet>() != null)
                {
                    Destroy(obj);
                    continue;
                }

                // Deactivate it and save in list for restoration
                if (obj.activeSelf)
                {
                    obj.SetActive(false);
                    automaticallyDeactivatedObjects.Add(obj);
                }
            }

            // 2. Clean up any leftover obstacles in scene
            CleanupObstacles();

            // 3. Activate minigame arena
            if (minigameArenaParent != null)
            {
                minigameArenaParent.SetActive(true);
            }

            // 4. Teleport Player to start point
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                
                // Save original player position and rotation for restoration later
                originalPlayerPos = playerTransform.position;
                originalPlayerRot = playerTransform.rotation;
                playerStateSaved = true;

                if (playerStartPoint != null)
                {
                    playerTransform.position = playerStartPoint.position;
                    playerTransform.rotation = playerStartPoint.rotation;
                }
                
                // Ensure player's movement and controls are enabled
                PlayerMovement move = player.GetComponent<PlayerMovement>();
                if (move != null) move.enabled = true;
                
                Collider2D col = player.GetComponent<Collider2D>();
                if (col != null) col.enabled = true;
            }
            else
            {
                Debug.LogWarning("[CrossyRoadMinigame] Player or Start Spawn Point is missing!");
            }

            // 5. Position the Finish Line dynamically based on levelHeight
            if (finishLineTransform != null && playerStartPoint != null)
            {
                Vector3 newFinishPos = finishLineTransform.position;
                newFinishPos.y = playerStartPoint.position.y + levelHeight;
                finishLineTransform.position = newFinishPos;
                Debug.Log($"[CrossyRoadMinigame] Dynamically set Finish Line to Y: {newFinishPos.y}");
            }

            // 6. Save original camera position for restoration later
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera != null)
            {
                originalCameraPos = mainCamera.transform.position;
                cameraStateSaved = true;
            }

            // 7. Start dynamic spawning of obstacles
            if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
            spawnCoroutine = StartCoroutine(SpawnObstaclesRoutine());
        }

        public void CompleteMinigame()
        {
            Debug.Log("[CrossyRoadMinigame] Minigame completed! Restoring stability...");

            // Restore Player Stability to Max
            if (PlayerStatus.Instance != null)
            {
                PlayerStatus.Instance.stability = PlayerStatus.Instance.maxStability;
            }

            StopMinigame(true);
        }

        private void StopMinigame(bool transitionToAwake)
        {
            isMinigameActive = false;

            // Stop spawning
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }

            // Clean up obstacles
            CleanupObstacles();

            // Deactivate arena
            if (minigameArenaParent != null)
            {
                minigameArenaParent.SetActive(false);
            }

            // (Standard manual list deactivated - handled automatically below)

            // Reactivate automatically deactivated objects
            foreach (var obj in automaticallyDeactivatedObjects)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                }
            }
            automaticallyDeactivatedObjects.Clear();

            // Restore camera position
            if (enableCameraFollow && cameraStateSaved && mainCamera != null)
            {
                mainCamera.transform.position = originalCameraPos;
                cameraStateSaved = false;
            }

            // Restore Player position and rotation back to their original spots in the room!
            if (playerStateSaved && playerTransform != null)
            {
                playerTransform.position = originalPlayerPos;
                playerTransform.rotation = originalPlayerRot;
                playerStateSaved = false;
            }

            // Transition PhaseLoopManager back to GameState.Dream (returning player to prior phase loop)
            if (transitionToAwake && phaseLoopManager != null)
            {
                phaseLoopManager.StartManualTransition(GameState.Dream);
            }
        }

        private void Update()
        {
            if (debugForceStartMinigame)
            {
                debugForceStartMinigame = false;
                DebugForceStartMinigame();
            }
        }

        [ContextMenu("Force Start Minigame (Play Mode Only)")]
        public void DebugForceStartMinigame()
        {
            if (Application.isPlaying)
            {
                if (phaseLoopManager != null)
                {
                    phaseLoopManager.StartManualTransition(GameState.Liminal);
                }
                else
                {
                    StartMinigame();
                }
            }
            else
            {
                Debug.LogWarning("[CrossyRoadMinigame] Force-start can only be triggered while the game is playing!");
            }
        }

        private void LateUpdate()
        {
            // Smooth Camera Follow along the Y axis
            if (isMinigameActive && enableCameraFollow && mainCamera != null && playerTransform != null)
            {
                Vector3 targetCamPos = mainCamera.transform.position;
                
                // Track player Y, keep original camera X and Z
                targetCamPos.y = playerTransform.position.y;

                // Clamp camera Y so it doesn't go below the start point
                if (playerStartPoint != null)
                {
                    targetCamPos.y = Mathf.Max(targetCamPos.y, playerStartPoint.position.y);
                }

                // Clamp camera Y so it doesn't scroll past the finish line
                if (finishLineTransform != null)
                {
                    targetCamPos.y = Mathf.Min(targetCamPos.y, finishLineTransform.position.y);
                }

                // Interpolate smoothly
                mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetCamPos, cameraSmoothSpeed);
            }
        }

        private IEnumerator SpawnObstaclesRoutine()
        {
            if (obstaclePrefab == null)
            {
                Debug.LogError("[CrossyRoadMinigame] Obstacle Prefab is missing! Spawning cancelled.");
                yield break;
            }

            while (isMinigameActive && playerTransform != null)
            {
                yield return new WaitForSeconds(spawnInterval);

                // Check again to avoid spawning after deactivation
                if (!isMinigameActive || playerTransform == null) yield break;

                // 1. Choose spawn direction (50% left-to-right, 50% right-to-left)
                bool goRight = Random.value > 0.5f;
                float startX = goRight ? leftSpawnX : rightSpawnX;
                float targetX = goRight ? rightSpawnX : leftSpawnX;

                // 2. Select a random Y coordinate in front of the player's view
                float randomYOffset = Random.Range(minSpawnAhead, maxSpawnAhead);
                float targetY = playerTransform.position.y + randomYOffset;

                // Clamp spawn Y so we don't spawn below the starting area
                if (playerStartPoint != null)
                {
                    targetY = Mathf.Max(targetY, playerStartPoint.position.y + 1f);
                }

                // Clamp spawn Y so we don't spawn past the finish line
                if (finishLineTransform != null)
                {
                    targetY = Mathf.Min(targetY, finishLineTransform.position.y - 1f);
                }

                // 3. Setup spawn positions
                Vector3 spawnPos = new Vector3(startX, targetY, 0f);
                Vector3 endPos = new Vector3(targetX, targetY, 0f);

                // 4. Instantiate Obstacle
                GameObject obstacleObj = Instantiate(obstaclePrefab, spawnPos, Quaternion.identity, minigameArenaParent.transform);
                
                CrossyRoadObstacle obstacle = obstacleObj.GetComponent<CrossyRoadObstacle>();
                if (obstacle == null)
                {
                    obstacle = obstacleObj.AddComponent<CrossyRoadObstacle>();
                }

                float randomSpeed = Random.Range(obstacleSpeedMin, obstacleSpeedMax);
                obstacle.Setup(spawnPos, endPos, randomSpeed);
            }
        }

        private void CleanupObstacles()
        {
            CrossyRoadObstacle[] activeObstacles = FindObjectsOfType<CrossyRoadObstacle>();
            foreach (var obstacle in activeObstacles)
            {
                if (obstacle != null)
                {
                    Destroy(obstacle.gameObject);
                }
            }
        }
    }
}
