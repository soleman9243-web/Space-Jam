using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MathTaskManager : MonoBehaviour
{
    [Header("UI Root")]
    [SerializeField] private GameObject parentUI;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI questionText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TMP_InputField answerInput;

    [Header("Feedback UI")]
    [SerializeField] private RectTransform uiPanel;
    [SerializeField] private Image flashImage;

    [Header("Settings")]
    [SerializeField] private int difficulty = 1;
    [SerializeField] private int totalQuestions = 5;

    [Header("Player")]
    [SerializeField] private PlayerMovement playerMovement;


    private int currentQuestionIndex;
    private int correctAnswer;
    private float timeLeft;
    private bool isRunning;

    private int wrongCount;

    public System.Action OnTaskFinished;

    private void Start()
    {
        parentUI.SetActive(false);
        flashImage.color = new Color(0, 1, 0, 0);
    }

    private void Update()
    {
        if (!isRunning)
        {
            return;
        }

        HandleEnter();
        ForceInputFocus();

        timeLeft -= Time.deltaTime;

        if (timeLeft <= 0)
        {
            OnFail();
            return;
        }

        UpdateTimerUI();
    }

    private void HandleEnter()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            SubmitAnswer();
        }
    }

    private void ForceInputFocus()
    {
        if (EventSystem.current.currentSelectedGameObject != answerInput.gameObject)
        {
            EventSystem.current.SetSelectedGameObject(answerInput.gameObject);
            answerInput.ActivateInputField();
        }
    }

    public void OpenTask()
    {
        parentUI.SetActive(true);

        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        currentQuestionIndex = 0;
        wrongCount = 0;

        StartTimer(GetTimerByDifficulty(difficulty));
        GenerateQuestion();
        UpdateProgressUI();

        answerInput.text = "";

        // pastikan hanya angka (TMP InputField setting)
        answerInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        answerInput.ForceLabelUpdate();

        EventSystem.current.SetSelectedGameObject(answerInput.gameObject);
        answerInput.ActivateInputField();

        isRunning = true;
    }

    private void CloseTask()
    {
        isRunning = false;

        parentUI.SetActive(false);

        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }

        OnTaskFinished?.Invoke();
    }
    public void SubmitAnswer()
    {
        if (!isRunning)
        {
            return;
        }

        if (!int.TryParse(answerInput.text, out int playerAnswer))
        {
            answerInput.text = "";
            return;
        }

        if (playerAnswer == correctAnswer)
        {
            StartCoroutine(CorrectFeedback());

            currentQuestionIndex++;

            if (currentQuestionIndex >= totalQuestions)
            {
                OnWin();
                return;
            }

            GenerateQuestion();
        }
        else
        {
            wrongCount++;

            float penalty = GetPenaltyByDifficulty(difficulty);

            if (PlayerStatus.Instance != null)
            {
                PlayerStatus.Instance.ReduceStability(penalty);
            }

            timeLeft -= 2f;
            StartCoroutine(ShakeUI());
        }

        answerInput.text = "";
        answerInput.ActivateInputField();

        UpdateProgressUI();
    }

    private void OnWin()
    {
        if (PlayerStatus.Instance != null)
        {
            PlayerStatus.Instance.IncreaseStability(GetRewardByDifficulty(difficulty));
        }

        Debug.Log("TASK COMPLETE");
        CloseTask();
    }

    private void OnFail()
    {
        Debug.Log("TASK FAILED");
        CloseTask();
    }

    // ================= rest unchanged =================
    private float GetPenaltyByDifficulty(int diff)
    {
        if (diff <= 2) return 2f;
        if (diff <= 4) return 4f;
        if (diff <= 6) return 7f;
        if (diff <= 8) return 10f;
        return 15f;
    }

    private float GetRewardByDifficulty(int diff)
    {
        if (diff <= 2) return 5f;
        if (diff <= 4) return 8f;
        if (diff <= 6) return 12f;
        if (diff <= 8) return 18f;
        return 25f;
    }

    private void GenerateQuestion()
    {
        int range = GetRangeByDifficulty(difficulty);

        int a = Random.Range(1, range);
        int b = Random.Range(1, range);

        correctAnswer = a + b;
        questionText.text = a + " + " + b + " = ?";
    }

    private void StartTimer(float duration)
    {
        timeLeft = duration;
    }

    private float GetTimerByDifficulty(int diff)
    {
        if (diff <= 2) return 40f;
        if (diff <= 4) return 35f;
        if (diff <= 6) return 30f;
        if (diff <= 8) return 25f;
        return 20f;
    }

    private int GetRangeByDifficulty(int diff)
    {
        if (diff <= 2) return 10;
        if (diff <= 4) return 20;
        if (diff <= 6) return 50;
        if (diff <= 8) return 100;
        return 200;
    }

    private void UpdateProgressUI()
    {
        progressText.text = currentQuestionIndex + "/" + totalQuestions;
    }

    private void UpdateTimerUI()
    {
        timerText.text = Mathf.Ceil(timeLeft) + "s";
    }

    private IEnumerator CorrectFeedback()
    {
        flashImage.color = Color.green;
        flashImage.canvasRenderer.SetAlpha(0.3f);

        yield return new WaitForSeconds(0.15f);

        flashImage.canvasRenderer.SetAlpha(0f);
    }

    private IEnumerator ShakeUI()
    {
        Vector3 originalPos = uiPanel.localPosition;

        float elapsed = 0f;
        float duration = 0.2f;
        float strength = 10f;

        while (elapsed < duration)
        {
            float x = Random.Range(-strength, strength);
            float y = Random.Range(-strength, strength);

            uiPanel.localPosition = originalPos + new Vector3(x, y, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        uiPanel.localPosition = originalPos;
    }
}