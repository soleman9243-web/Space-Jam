using System.Collections;
using TMPro;
using UnityEngine;

public class ClockPuzzle : BaseTask
{
    [Header("References")]
    [SerializeField] private RectTransform minuteHand;
    [SerializeField] private RectTransform hourHand;

    [SerializeField] private GameObject puzzleUI;
    [SerializeField] private MonoBehaviour playerMovement;
    [SerializeField] private PlayerInteract2D playerInteract;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("Timer")]
    [SerializeField] private float baseTimer = 40f;
    [SerializeField] private float timerReductionPerLoop = 4f;
    [SerializeField] private float minimumTimer = 12f;

    [Header("Stability")]
    [SerializeField] private float baseFailPenalty = 10f;
    [SerializeField] private float penaltyIncreasePerLoop = 2f;

    [SerializeField] private float baseReward = 5f;
    [SerializeField] private float rewardIncreasePerLoop = 1f;

    private int startMinutes;

    private int targetMinutes;

    private int currentMinutes;

    private bool isActive;

    private float currentTimer;

    private void Start()
    {
        puzzleUI.SetActive(false);
    }

    public void ActivateTask()
    {
        currentMinutes = startMinutes;

        UpdateClockVisual();
    }

    private void Update()
    {
        if (!isActive)
        {
            return;
        }

        UpdateTimer();

        if (Input.GetKeyDown(KeyCode.A))
        {
            RotateTime(-5);
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            RotateTime(5);
        }
    }

    public void OpenPuzzle()
    {
        if (completed)
        {
            return;
        }

        GenerateRandomTime();

        isActive = true;

        currentMinutes = startMinutes;

        UpdateClockVisual();

        puzzleUI.SetActive(true);

        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        if (playerInteract != null)
        {
            playerInteract.canInteract = false;
        }

        SetupTimer();

        PhaseTaskManager taskManager =
            FindObjectOfType<PhaseTaskManager>();

        if (taskManager != null)
        {
            taskManager.UpdateObjectiveUI();
        }
    }

    public void ClosePuzzle()
    {
        isActive = false;

        puzzleUI.SetActive(false);

        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }

        if (playerInteract != null)
        {
            playerInteract.canInteract = true;
        }
    }

    private void GenerateRandomTime()
    {
        startMinutes =
            Random.Range(0, 12) * 60;

        int randomAdd;

        do
        {
            randomAdd =
                Random.Range(3, 12) * 5;
        }
        while (randomAdd <= 15);

        targetMinutes =
            startMinutes + randomAdd;

        if (targetMinutes >= 720)
        {
            targetMinutes -= 720;
        }

        taskText =
            "Make the clock to " + FormatTime(targetMinutes);
    }

    private void SetupTimer()
    {
        PhaseLoopManager phaseManager =
            FindObjectOfType<PhaseLoopManager>();

        int loop =
            phaseManager != null
            ? phaseManager.currentLoop
            : 1;

        currentTimer =
            baseTimer - ((loop - 1) * timerReductionPerLoop);

        currentTimer =
            Mathf.Max(currentTimer, minimumTimer);

        UpdateTimerUI();
    }

    private void UpdateTimer()
    {
        currentTimer -= Time.deltaTime;

        if (currentTimer <= 0f)
        {
            currentTimer = 0f;

            UpdateTimerUI();

            FailPuzzle();

            return;
        }

        UpdateTimerUI();
    }

    private void UpdateTimerUI()
    {
        if (timerText == null)
        {
            return;
        }

        timerText.text =
            Mathf.CeilToInt(currentTimer).ToString();
    }

    private void FailPuzzle()
    {
        ClosePuzzle();

        PhaseLoopManager phaseManager =
            FindObjectOfType<PhaseLoopManager>();

        int loop =
            phaseManager != null
            ? phaseManager.currentLoop
            : 1;

        float penalty =
            baseFailPenalty + ((loop - 1) * penaltyIncreasePerLoop);

        if (PlayerStatus.Instance != null)
        {
            PlayerStatus.Instance.ReduceStability(penalty);
        }

        Debug.Log("Clock gagal");
    }

    private void RotateTime(int amount)
    {
        currentMinutes += amount;

        if (currentMinutes < 0)
        {
            currentMinutes += 720;
        }

        if (currentMinutes >= 720)
        {
            currentMinutes -= 720;
        }

        UpdateClockVisual();

        CheckAnswer();
    }

    private void UpdateClockVisual()
    {
        float minuteRotation =
            -(currentMinutes % 60) * 6f;

        float hourRotation =
            -(currentMinutes / 60f) * 30f;

        minuteHand.localRotation =
            Quaternion.Euler(0f, 0f, minuteRotation);

        hourHand.localRotation =
            Quaternion.Euler(0f, 0f, hourRotation);
    }

    private void CheckAnswer()
    {
        if (completed)
        {
            return;
        }

        if (currentMinutes == targetMinutes)
        {
            PhaseLoopManager phaseManager =
                FindObjectOfType<PhaseLoopManager>();

            int loop =
                phaseManager != null
                ? phaseManager.currentLoop
                : 1;

            float reward =
                baseReward + ((loop - 1) * rewardIncreasePerLoop);

            if (PlayerStatus.Instance != null)
            {
                PlayerStatus.Instance.IncreaseStability(reward);
            }

            CompleteTask();

            ClosePuzzle();

            Debug.Log("Clock selesai");
        }
    }

    private string FormatTime(int minutes)
    {
        int hour = minutes / 60;

        int minute = minutes % 60;

        return hour.ToString("00") + ":" + minute.ToString("00");
    }
}