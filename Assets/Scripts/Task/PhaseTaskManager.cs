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

        // If transitioning to Confusion, do not reset tasks, just temporarily deactivate the active one!
        if (state == GameState.Confusion)
        {
            if (currentTasks != null && currentTasks.Count > 0)
            {
                currentTasks[0].DeactivateTask();
            }
            return;
        }

        // If transitioning back to Dream from Confusion, resume the current tasks!
        if (state == GameState.Dream && currentTasks != null && currentTasks.Count > 0)
        {
            currentTasks[0].ActivateTask();
            UpdateObjectiveUI();
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

    public void ForceActivateTask(BaseTask task)
    {
        if (currentTasks == null)
        {
            currentTasks = new List<BaseTask>();
        }

        // Deactivate currently active task
        if (currentTasks.Count > 0)
        {
            currentTasks[0].DeactivateTask();
        }

        // Ensure this task is in the list and moved to the front
        currentTasks.Remove(task);
        currentTasks.Insert(0, task);

        // Activate it!
        task.ActivateTask();
        UpdateObjectiveUI();
        
        // Ensure bed is locked since we have active tasks
        bed.canSleep = false;
        
        Debug.Log("[PhaseTaskManager] Force activated task: " + task.taskText);
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