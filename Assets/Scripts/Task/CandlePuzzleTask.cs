using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceJam.Minigames
{
    /// <summary>
    /// Task lilin: Player harus menyalakan semua lilin dalam urutan yang benar.
    /// Lilin dibuat OTOMATIS saat runtime melingkari posisi player.
    /// Salah urutan = semua mati, coba lagi (pola tetap sama).
    /// Pola berubah setiap loop fase baru.
    /// </summary>
    public class CandlePuzzleTask : BaseTask
    {
        [Header("Puzzle Settings")]
        [Tooltip("Jumlah lilin dalam puzzle.")]
        [SerializeField] private int candleCount = 5;

        [Tooltip("Jarak radius lingkaran lilin dari player.")]
        [SerializeField] private float circleRadius = 3f;

        [Tooltip("Radius interaksi setiap lilin.")]
        [SerializeField] private float interactRadius = 1.2f;

        [Header("Visual Settings")]
        [SerializeField] private int sortingOrder = 5;

        // Runtime state
        private List<CandleInteractable> candles = new List<CandleInteractable>();
        private int[] correctPattern;
        private int currentStep = 0;
        private bool puzzleActive = false;
        private int lastPatternSeed = -1;
        private Sprite runtimeSprite;

        private void Start()
        {
            // Auto-register to PhaseTaskManager if present
            PhaseTaskManager ptm = FindObjectOfType<PhaseTaskManager>();
            if (ptm != null)
            {
                if (ptm.dreamTasks == null)
                    ptm.dreamTasks = new List<BaseTask>();
                if (!ptm.dreamTasks.Contains(this))
                {
                    ptm.dreamTasks.Add(this);
                    Debug.Log("[CandlePuzzleTask] Auto-registered to PhaseTaskManager Dream Tasks!");
                }
            }

            // Auto-subscribe to PhaseLoopManager
            PhaseLoopManager phaseLoop = FindObjectOfType<PhaseLoopManager>();
            if (phaseLoop != null)
            {
                phaseLoop.OnPhaseChanged.AddListener(OnPhaseChanged);

                if (ptm == null && phaseLoop.CurrentState == GameState.Dream)
                {
                    Debug.Log("[CandlePuzzleTask] Startup detected GameState.Dream. Auto-activating!");
                    ActivateTask();
                }
            }
        }

        private void OnDestroy()
        {
            PhaseLoopManager phaseLoop = FindObjectOfType<PhaseLoopManager>();
            if (phaseLoop != null)
            {
                phaseLoop.OnPhaseChanged.RemoveListener(OnPhaseChanged);
            }

            // Bersihkan lilin runtime
            DestroyCandleObjects();
        }

        private void OnPhaseChanged(GameState state)
        {
            PhaseTaskManager taskManager = FindObjectOfType<PhaseTaskManager>();
            if (taskManager == null)
            {
                if (state == GameState.Dream)
                    ActivateTask();
                else
                    DeactivateTask();
            }
        }

        public override void ActivateTask()
        {
            base.ActivateTask();
            completed = false;
            currentStep = 0;
            puzzleActive = true;

            // Buat lilin kalau belum ada
            if (candles.Count == 0)
            {
                CreateCandlesAroundPlayer();
            }
            else
            {
                RepositionCandlesAroundPlayer();
            }

            // Generate pola baru setiap loop fase
            PhaseLoopManager phaseLoop = FindObjectOfType<PhaseLoopManager>();
            int currentSeed = phaseLoop != null ? phaseLoop.currentLoop : 0;

            if (correctPattern == null || currentSeed != lastPatternSeed)
            {
                GeneratePattern(currentSeed);
                lastPatternSeed = currentSeed;
                Debug.Log($"[CandlePuzzleTask] Pola baru: [{string.Join(", ", correctPattern)}]");
            }

            ExtinguishAll();
            SetCandlesVisible(true);

            Debug.Log($"[CandlePuzzleTask] Aktif! {candles.Count} lilin melingkari player. Temukan urutan yang benar!");
        }

        public override void DeactivateTask()
        {
            base.DeactivateTask();
            puzzleActive = false;
            SetCandlesVisible(false);
        }

        public override void ResetTask()
        {
            base.ResetTask();
            puzzleActive = false;
            currentStep = 0;
            ExtinguishAll();
            SetCandlesVisible(false);
        }

        public override void CompleteTask()
        {
            completed = true;
            puzzleActive = false;

            PhaseTaskManager taskManager = FindObjectOfType<PhaseTaskManager>();
            if (taskManager != null)
            {
                taskManager.CompleteTask(this);
            }
            else
            {
                Debug.Log("[CandlePuzzleTask] Semua lilin menyala! Quest selesai (standalone mode).");
            }

            DeactivateTask();
        }

        // ===================== INTERAKSI =====================

        public void OnCandleInteracted(int candleIndex)
        {
            if (!IsActive || completed || !puzzleActive) return;
            if (candleIndex < 0 || candleIndex >= candles.Count) return;
            if (candles[candleIndex].IsLit) return;

            if (correctPattern != null && currentStep < correctPattern.Length && correctPattern[currentStep] == candleIndex)
            {
                // BENAR!
                candles[candleIndex].SetLit(true);
                currentStep++;
                Debug.Log($"[CandlePuzzleTask] Benar! Langkah {currentStep}/{candles.Count}");

                if (currentStep >= candles.Count)
                {
                    Debug.Log("[CandlePuzzleTask] Semua benar! Puzzle selesai!");
                    StartCoroutine(CompletePuzzleRoutine());
                }
            }
            else
            {
                // SALAH!
                Debug.Log("[CandlePuzzleTask] Salah! Semua lilin mati. Coba lagi!");
                StartCoroutine(WrongAnswerRoutine());
            }
        }

        private IEnumerator WrongAnswerRoutine()
        {
            puzzleActive = false;
            yield return new WaitForSeconds(0.4f);
            ExtinguishAll();
            currentStep = 0;
            yield return new WaitForSeconds(0.3f);
            puzzleActive = true;
        }

        private IEnumerator CompletePuzzleRoutine()
        {
            puzzleActive = false;
            yield return new WaitForSeconds(1f);
            CompleteTask();
        }

        // ===================== PEMBUATAN LILIN RUNTIME =====================

        private Sprite GetRuntimeSprite()
        {
            if (runtimeSprite == null)
            {
                Texture2D tex = new Texture2D(4, 4);
                Color[] colors = new Color[16];
                for (int i = 0; i < 16; i++) colors[i] = Color.white;
                tex.SetPixels(colors);
                tex.Apply();
                runtimeSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            }
            return runtimeSprite;
        }

        private void CreateCandlesAroundPlayer()
        {
            DestroyCandleObjects();

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            Vector3 center = player != null ? player.transform.position : transform.position;

            Sprite sprite = GetRuntimeSprite();

            for (int i = 0; i < candleCount; i++)
            {
                float angle = (2f * Mathf.PI * i) / candleCount;
                float x = Mathf.Cos(angle) * circleRadius;
                float y = Mathf.Sin(angle) * circleRadius;
                Vector3 pos = center + new Vector3(x, y, 0f);

                GameObject candle = CreateSingleCandle(i, pos, sprite);
                CandleInteractable ci = candle.GetComponent<CandleInteractable>();
                candles.Add(ci);
            }

            Debug.Log($"[CandlePuzzleTask] {candleCount} lilin dibuat melingkari player di radius {circleRadius}!");
        }

        private GameObject CreateSingleCandle(int index, Vector3 position, Sprite sprite)
        {
            // === ROOT ===
            GameObject candle = new GameObject($"Candle_{index}");
            candle.transform.SetParent(transform);
            candle.transform.position = position;

            // === BADAN LILIN (coklat tua) ===
            GameObject body = new GameObject("Body");
            body.transform.SetParent(candle.transform);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(0.3f, 0.7f, 1f);

            SpriteRenderer bodySR = body.AddComponent<SpriteRenderer>();
            bodySR.sprite = sprite;
            bodySR.color = new Color(0.35f, 0.22f, 0.1f, 1f);
            bodySR.sortingOrder = sortingOrder;

            // === SUMBU (hitam kecil di atas) ===
            GameObject wick = new GameObject("Wick");
            wick.transform.SetParent(candle.transform);
            wick.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            wick.transform.localScale = new Vector3(0.06f, 0.12f, 1f);

            SpriteRenderer wickSR = wick.AddComponent<SpriteRenderer>();
            wickSR.sprite = sprite;
            wickSR.color = new Color(0.15f, 0.1f, 0.05f, 1f);
            wickSR.sortingOrder = sortingOrder + 1;

            // === API / FLAME (kuning-oranye, hidden by default) ===
            GameObject flame = new GameObject("Flame");
            flame.transform.SetParent(candle.transform);
            flame.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            flame.transform.localScale = new Vector3(0.25f, 0.35f, 1f);

            SpriteRenderer flameSR = flame.AddComponent<SpriteRenderer>();
            flameSR.sprite = sprite;
            flameSR.color = new Color(1f, 0.7f, 0.1f, 0.95f);
            flameSR.sortingOrder = sortingOrder + 3;

            // === GLOW (cahaya lembut di sekitar api) ===
            GameObject glow = new GameObject("Glow");
            glow.transform.SetParent(flame.transform);
            glow.transform.localPosition = Vector3.zero;
            glow.transform.localScale = new Vector3(3f, 3f, 1f);

            SpriteRenderer glowSR = glow.AddComponent<SpriteRenderer>();
            glowSR.sprite = sprite;
            glowSR.color = new Color(1f, 0.6f, 0f, 0.12f);
            glowSR.sortingOrder = sortingOrder + 2;

            flame.SetActive(false); // Api mati default

            // === COLLIDER INTERAKSI ===
            CircleCollider2D col = candle.AddComponent<CircleCollider2D>();
            col.radius = interactRadius;
            col.isTrigger = true;

            // === CANDLE INTERACTABLE ===
            CandleInteractable ci = candle.AddComponent<CandleInteractable>();
            ci.Initialize(this, index, flame, bodySR);

            // === INTERACT OBJECT (native bubble system) ===
            InteractObject2D interact = candle.AddComponent<InteractObject2D>();
            interact.requireActiveTask = true;
            interact.linkedTask = this;
            interact.onInteract.AddListener(ci.TryLight);

            return candle;
        }

        private void RepositionCandlesAroundPlayer()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            Vector3 center = player != null ? player.transform.position : transform.position;

            for (int i = 0; i < candles.Count; i++)
            {
                if (candles[i] == null) continue;

                float angle = (2f * Mathf.PI * i) / candles.Count;
                float x = Mathf.Cos(angle) * circleRadius;
                float y = Mathf.Sin(angle) * circleRadius;

                candles[i].transform.position = center + new Vector3(x, y, 0f);
            }
        }

        private void DestroyCandleObjects()
        {
            foreach (var candle in candles)
            {
                if (candle != null)
                    Destroy(candle.gameObject);
            }
            candles.Clear();
        }

        // ===================== UTILITAS =====================

        private void GeneratePattern(int seed)
        {
            correctPattern = new int[candleCount];

            List<int> indices = new List<int>();
            for (int i = 0; i < candleCount; i++) indices.Add(i);

            Random.State oldState = Random.state;
            Random.InitState(seed + System.DateTime.Now.Millisecond);

            for (int i = indices.Count - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, i + 1);
                int temp = indices[i];
                indices[i] = indices[randomIndex];
                indices[randomIndex] = temp;
            }

            Random.state = oldState;

            for (int i = 0; i < candleCount; i++)
            {
                correctPattern[i] = indices[i];
            }
        }

        private void ExtinguishAll()
        {
            foreach (var candle in candles)
            {
                if (candle != null)
                    candle.SetLit(false);
            }
        }

        private void SetCandlesVisible(bool visible)
        {
            foreach (var candle in candles)
            {
                if (candle != null)
                    candle.gameObject.SetActive(visible);
            }
        }

        [ContextMenu("Force Activate Candle Puzzle (Play Mode Only)")]
        public void DebugForceActivate()
        {
            if (Application.isPlaying)
            {
                StartCoroutine(ForceActivateRoutine());
            }
            else
            {
                Debug.LogWarning("[CandlePuzzleTask] Force activate hanya bisa di Play Mode!");
            }
        }

        private IEnumerator ForceActivateRoutine()
        {
            gameObject.SetActive(true);

            PhaseLoopManager phaseLoop = FindObjectOfType<PhaseLoopManager>();
            if (phaseLoop != null && phaseLoop.CurrentState != GameState.Dream)
            {
                phaseLoop.StartManualTransition(GameState.Dream);
            }

            yield return null;
            yield return null;

            ActivateTask();
        }
    }
}
