using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PhaseTaskManager : MonoBehaviour
{
    public List<BaseTask> awakeTasks;
    public List<BaseTask> dreamTasks;

    private List<BaseTask> currentTasks;

    public BedInteract bed;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI objectiveText;

    public PhaseLoopManager phaseManager;

    private IEnumerator Start()
    {
        yield return null; // tunggu semua Awake selesai


        if (phaseManager != null)
        {
            phaseManager.OnPhaseChanged.AddListener(OnPhaseChanged);
        }

        SetupTasks();
    }

    private void OnPhaseChanged(GameState state)
    {
        if (phaseManager == null)
        {
            return;
        }

        if (state != phaseManager.CurrentState)
        {
            return;
        }

        SetupTasks();
    }

    public void SetupTasks()
    {
        if (phaseManager == null)
        {
            Debug.LogError("PhaseLoopManager not found!");
            return;
        }

        currentTasks = new List<BaseTask>();

        GameState state = phaseManager.CurrentState;

        // STOP semua task aktif dulu
        foreach (var task in awakeTasks)
        {
            task.DeactivateTask();
            task.ResetTask();
        }

        foreach (var task in dreamTasks)
        {
            task.DeactivateTask();
            task.ResetTask();
        }

        if (state == GameState.Awake)
        {
            currentTasks.AddRange(awakeTasks);
        }
        else if (state == GameState.Dream)
        {
            currentTasks.AddRange(dreamTasks);
        }

        ShuffleTasks();

        bed.canSleep = false;

        if (currentTasks.Count > 0)
        {
            currentTasks[0].ActivateTask();
        }
        else
        {
            bed.canSleep = true;
        }

        UpdateObjectiveUI();

        Debug.Log("TASK RESET COMPLETE: " + state);
    }

    public void CompleteTask(BaseTask task)
    {
        if (currentTasks == null)
        {
            return;
        }

        currentTasks.Remove(task);

        UpdateObjectiveUI();

        if (currentTasks.Count <= 0)
        {
            bed.canSleep = true;

            if (objectiveText != null)
            {
                objectiveText.text = "Go to bed";
            }

            return;
        }

        currentTasks[0].ActivateTask();
    }

    public void UpdateObjectiveUI()
    {
        if (objectiveText == null)
        {
            return;
        }

        if (currentTasks == null || currentTasks.Count <= 0)
        {
            objectiveText.text = "";
            return;
        }

        objectiveText.text = currentTasks[0].taskText;
    }

    private void ShuffleTasks()
    {
        for (int i = 0; i < currentTasks.Count; i++)
        {
            int rand = Random.Range(i, currentTasks.Count);

            BaseTask temp = currentTasks[i];
            currentTasks[i] = currentTasks[rand];
            currentTasks[rand] = temp;
        }
    }
}