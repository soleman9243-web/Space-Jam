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

    [Header("Signal")]
    [SerializeField] private float signalSpeed = 220f;
    [SerializeField] private float signalRange = 400f;

    [Header("Safe Zone")]
    [SerializeField] private float zoneSpeed = 300f;
    [SerializeField] private float zoneRange = 400f;

    [Header("Progress Balance")]
    [SerializeField] private float startProgress = 0.25f;
    [SerializeField] private float gainSpeed = 0.35f;
    [SerializeField] private float lossSpeed = 0.15f;

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
        signalRange = parentRect.rect.width * 0.5f;
        zoneRange = signalRange;

        UpdateUI();
        UpdateProgressText();
    }

    public void OpenPuzzle()
    {
        if (completed)
        {
            return;
        }

        isActive = true;
        puzzleUI.SetActive(true);

        stage = 0;
        progress = startProgress;

        UpdateUI();
        UpdateProgressText();

        playerMovement.enabled = false;
        playerInteract.canInteract = false;
    }

    private void Update()
    {
        if (!isActive)
        {
            return;
        }

        MoveSignal();
        MoveSafeZone();
        CheckMatch();
    }

    // ======================
    // SIGNAL AI (BOUNCE RANDOM)
    // ======================
    private void MoveSignal()
    {
        Vector2 pos = signalBar.anchoredPosition;

        pos.x += signalDir * signalSpeed * Time.deltaTime;

        if (pos.x > signalRange)
        {
            signalDir = -1f;
        }
        else if (pos.x < -signalRange)
        {
            signalDir = 1f;
        }

        signalBar.anchoredPosition = pos;
    }

    // ======================
    // SAFE ZONE (PLAYER CONTROL)
    // ======================
    private void MoveSafeZone()
    {
        Vector2 pos = safeZone.anchoredPosition;

        if (Input.GetKey(KeyCode.Space))
        {
            pos.x += zoneSpeed * Time.deltaTime;
        }
        else
        {
            pos.x -= zoneSpeed * Time.deltaTime * 0.6f;
        }

        pos.x = Mathf.Clamp(pos.x, -zoneRange, zoneRange);

        safeZone.anchoredPosition = pos;
    }

    // ======================
    // CORE STABILITY SYSTEM
    // ======================
    private void CheckMatch()
    {
        if (resolving)
        {
            return;
        }

        float signalX = signalBar.anchoredPosition.x;
        float zoneX = safeZone.anchoredPosition.x;
        float zoneWidth = safeZone.rect.width * 0.5f;

        bool inside = Mathf.Abs(signalX - zoneX) <= zoneWidth;

        if (inside)
        {
            progress += gainSpeed * Time.deltaTime;
        }
        else
        {
            progress -= lossSpeed * Time.deltaTime;
        }

        progress = Mathf.Clamp01(progress);

        UpdateUI();

        if (progress <= 0f)
        {
            StartCoroutine(FailStageRoutine());
        }
        else if (progress >= 1f)
        {
            StartCoroutine(NextStageRoutine());
        }
    }

    private IEnumerator NextStageRoutine()
    {
        resolving = true;

        stage++;

        UpdateProgressText();

        yield return new WaitForSeconds(0.2f);

        // ?? NEW: reward stability tiap stage sukses
        if (PlayerStatus.Instance != null)
        {
            PlayerStatus.Instance.IncreaseStability(5f);
        }

        if (stage >= maxStage)
        {
            CompleteTask();

            // ?? bonus reward kalau full clear
            if (PlayerStatus.Instance != null)
            {
                PlayerStatus.Instance.IncreaseStability(10f);
            }

            ClosePuzzle();
            yield break;
        }

        progress = startProgress;

        UpdateUI();

        resolving = false;
    }

    private IEnumerator FailStageRoutine()
    {
        resolving = true;

        // ? tidak ulang stage lagi
        stage = Mathf.Max(0, stage - 1);

        progress = startProgress * 0.8f;

        UpdateProgressText();
        UpdateUI();

        // ?? NEW: langsung kena stability damage
        if (PlayerStatus.Instance != null)
        {
            PlayerStatus.Instance.ReduceStability(8f);
        }

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

    private void UpdateUI()
    {
        if (progressBar != null)
        {
            progressBar.value = progress;
        }
    }

    private void UpdateProgressText()
    {
        if (progressText == null)
        {
            return;
        }

        progressText.text = stage + " / " + maxStage;
    }
}