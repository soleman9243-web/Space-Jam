using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static UnityEditorInternal.VersionControl.ListControl;

public class PhaseTaskManager : MonoBehaviour
{
    public List<BaseTask> awakeTasks;
    public List<BaseTask> dreamTasks;

    private List<BaseTask> currentTasks;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI objectiveText;

    public PhaseLoopManager phaseManager;

    private GameState lastState;
    private IEnumerator Start()
    {
        yield return null;

        lastState = phaseManager.CurrentState;
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

        // LIMINAL
        if (state == GameState.Liminal)
        {
            if (currentTasks != null && currentTasks.Count > 0)
            {
                currentTasks[0].ForceStopTask();
            }

            lastState = state;
            return;
        }

        // RETURN FROM LIMINAL ONLY
        if (lastState == GameState.Liminal && state != GameState.Liminal)
        {
            SetupTasks();
            lastState = state;
            return;
        }

        // NORMAL PHASE SWITCH (Awake ? Dream)
        SetupTasks();
        lastState = state;
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

        // reset semua task biar clean setiap switch phase
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

        // assign task sesuai phase
        if (state == GameState.Awake)
        {
            currentTasks.AddRange(awakeTasks);
        }
        else if (state == GameState.Dream)
        {
            currentTasks.AddRange(dreamTasks);
        }

        ShuffleTasks();

        if (currentTasks.Count > 0)
        {
            currentTasks[0].ActivateTask();
        }

        UpdateObjectiveUI();

        Debug.Log("[PhaseTaskManager] TASK SETUP: " + state);
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
            LoopCurrentPhaseTasks();
            return;
        }

        currentTasks[0].ActivateTask();
    }

    private void LoopCurrentPhaseTasks()
    {
        GameState state = phaseManager.CurrentState;

        List<BaseTask> source =
            state == GameState.Awake ? awakeTasks : dreamTasks;

        foreach (var task in source)
        {
            task.DeactivateTask();
            task.ResetTask();
        }

        currentTasks = new List<BaseTask>(source);

        ShuffleTasks();

        if (currentTasks.Count > 0)
        {
            currentTasks[0].ActivateTask();
        }

        UpdateObjectiveUI();
    }

    public void ForceActivateTask(BaseTask task)
    {
        if (currentTasks == null)
        {
            currentTasks = new List<BaseTask>();
        }

        if (currentTasks.Count > 0)
        {
            currentTasks[0].DeactivateTask();
        }

        currentTasks.Remove(task);
        currentTasks.Insert(0, task);

        task.ActivateTask();

        UpdateObjectiveUI();
    }

    public void UpdateObjectiveUI()
    {
        if (objectiveText == null)
        {
            return;
        }

        objectiveText.text =
            (currentTasks == null || currentTasks.Count == 0)
            ? ""
            : currentTasks[0].GetTaskText();
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