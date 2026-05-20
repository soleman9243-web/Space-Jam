using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TVRepairMinigame : BaseTask
{
    [Header("UI")]
    [SerializeField] private GameObject puzzleUI;
    [SerializeField] private RectTransform signalBar;
    [SerializeField] private RectTransform safeZone;
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI progressText;

    [Header("Player Lock")]
    [SerializeField] private MonoBehaviour playerMovement;
    [SerializeField] private PlayerInteract2D playerInteract;

    [Header("Signal Base")]
    [SerializeField] private float baseSignalSpeed = 220f;
    [SerializeField] private float signalSpeedPerLoop = 25f;
    [SerializeField] private float maxSignalSpeed = 500f;

    [Header("Safe Zone Base")]
    [SerializeField] private float baseZoneSpeed = 300f;
    [SerializeField] private float zoneSpeedPerLoop = 10f;
    [SerializeField] private float maxZoneSpeed = 450f;

    [Header("Progress Balance Base")]
    [SerializeField] private float startProgress = 0.25f;
    [SerializeField] private float baseGainSpeed = 0.35f;
    [SerializeField] private float baseLossSpeed = 0.15f;
    [SerializeField] private float lossSpeedPerLoop = 0.04f;
    [SerializeField] private float maxLossSpeed = 0.55f;

    // Runtime values (set saat OpenPuzzle)
    private float signalSpeed;
    private float zoneSpeed;
    private float signalRange;
    private float zoneRange;
    private float gainSpeed;
    private float lossSpeed;

    private bool isActive;
    private bool resolving;

    private float progress;
    private int stage;
    private int maxStage = 3;

    private float signalDir = 1f;
    private RectTransform parentRect;

    private void Start()
    {
        puzzleUI.SetActive(false);
        parentRect = signalBar.parent as RectTransform;
        UpdateUI();
        UpdateProgressText();
    }

    private int GetCurrentLoop()
    {
        PhaseLoopManager pm = FindObjectOfType<PhaseLoopManager>();
        return pm != null ? pm.currentLoop : 1;
    }

    public void OpenPuzzle()
    {
        if (completed) return;

        // Scale dari loop
        int loop = GetCurrentLoop();
        signalSpeed = Mathf.Min(baseSignalSpeed + (loop - 1) * signalSpeedPerLoop, maxSignalSpeed);
        zoneSpeed = Mathf.Min(baseZoneSpeed + (loop - 1) * zoneSpeedPerLoop, maxZoneSpeed);
        gainSpeed = baseGainSpeed;
        lossSpeed = Mathf.Min(baseLossSpeed + (loop - 1) * lossSpeedPerLoop, maxLossSpeed);

        signalRange = parentRect.rect.width * 0.5f;
        zoneRange = signalRange;

        isActive = true;
        puzzleUI.SetActive(true);

        stage = 0;
        progress = startProgress;

        UpdateUI();
        UpdateProgressText();

        playerMovement.enabled = false;
        playerInteract.canInteract = false;

        Debug.Log($"[TVRepair] Loop {loop} ? SignalSpeed:{signalSpeed:F0} LossSpeed:{lossSpeed:F2}");
    }

    private void Update()
    {
        if (!isActive) return;

        MoveSignal();
        MoveSafeZone();
        CheckMatch();
    }

    private void MoveSignal()
    {
        Vector2 pos = signalBar.anchoredPosition;
        pos.x += signalDir * signalSpeed * Time.deltaTime;

        if (pos.x > signalRange) signalDir = -1f;
        else if (pos.x < -signalRange) signalDir = 1f;

        signalBar.anchoredPosition = pos;
    }

    private void MoveSafeZone()
    {
        Vector2 pos = safeZone.anchoredPosition;

        pos.x = Input.GetKey(KeyCode.Space)
            ? pos.x + zoneSpeed * Time.deltaTime
            : pos.x - zoneSpeed * Time.deltaTime * 0.6f;

        pos.x = Mathf.Clamp(pos.x, -zoneRange, zoneRange);
        safeZone.anchoredPosition = pos;
    }

    private void CheckMatch()
    {
        if (resolving) return;

        float signalX = signalBar.anchoredPosition.x;
        float zoneX = safeZone.anchoredPosition.x;
        float zoneHalf = safeZone.rect.width * 0.5f;

        bool inside = Mathf.Abs(signalX - zoneX) <= zoneHalf;

        progress += (inside ? gainSpeed : -lossSpeed) * Time.deltaTime;
        progress = Mathf.Clamp01(progress);

        UpdateUI();

        if (progress <= 0f) StartCoroutine(FailStageRoutine());
        else if (progress >= 1f) StartCoroutine(NextStageRoutine());
    }

    private IEnumerator NextStageRoutine()
    {
        resolving = true;
        stage++;
        UpdateProgressText();

        yield return new WaitForSeconds(0.2f);

        if (PlayerStatus.Instance != null)
            PlayerStatus.Instance.IncreaseStability(5f);

        if (stage >= maxStage)
        {
            CompleteTask();
            if (PlayerStatus.Instance != null)
                PlayerStatus.Instance.IncreaseStability(10f);
            ClosePuzzle();
            yield break;
        }

        progress = startProgress;
        resolving = false;
        UpdateUI();
    }

    private IEnumerator FailStageRoutine()
    {
        resolving = true;
        stage = Mathf.Max(0, stage - 1);
        progress = startProgress * 0.8f;

        UpdateProgressText();
        UpdateUI();

        if (PlayerStatus.Instance != null)
            PlayerStatus.Instance.ReduceStability(8f);

        yield return new WaitForSeconds(0.2f);
        resolving = false;
    }

    private void ClosePuzzle()
    {
        isActive = false;
        puzzleUI.SetActive(false);
        playerMovement.enabled = true;
        playerInteract.canInteract = true;
    }

    private void UpdateUI() { if (progressBar != null) progressBar.value = progress; }
    private void UpdateProgressText() { if (progressText != null) progressText.text = stage + " / " + maxStage; }
    public override void ForceStopTask()
    {
        StopAllCoroutines();

        isActive = false;
        resolving = false;

        progress = 0f;
        stage = 0;

        if (puzzleUI != null)
        {
            puzzleUI.SetActive(false);
        }

        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }

        if (playerInteract != null)
        {
            playerInteract.canInteract = true;
        }

        DeactivateTask();
    }
}