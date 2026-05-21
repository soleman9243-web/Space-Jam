// ==============================
// PhaseTaskManager
// FINAL
// ==============================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhaseTaskManager : MonoBehaviour
{
    public List<BaseTask> awakeTasks;
    public List<BaseTask> dreamTasks;

    private List<BaseTask> currentTasks;

    [Header("Bubble")]
    [SerializeField] private TaskBubble taskBubble;

    [Header("Legacy UI (opsional)")]
    [SerializeField] private TMPro.TextMeshProUGUI objectiveText;

    public PhaseLoopManager phaseManager;

    private GameState lastState;

    private IEnumerator Start()
    {
        yield return null;

        if (!enabled)
        {
            yield break;
        }

        lastState = (GameState)(-1);

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

        if (state == GameState.Liminal)
        {
            if (currentTasks != null &&
                currentTasks.Count > 0)
            {
                currentTasks[0].ForceStopTask();
            }

            if (taskBubble != null)
            {
                taskBubble.ClearBubble();
            }

            lastState = state;

            return;
        }

        if (state == lastState)
        {
            return;
        }

        Debug.Log(
            $"[PhaseTaskManager] PHASE CHANGED: {lastState} -> {state}"
        );

        StartCoroutine(
            DelayedSetupTasks(state)
        );
    }

    private IEnumerator DelayedSetupTasks(
        GameState state
    )
    {
        while (phaseManager != null &&
               phaseManager.IsBusy)
        {
            yield return null;
        }

        SetupTasks();

        lastState = state;
    }

    public void SetupTasks()
    {
        if (phaseManager == null)
        {
            Debug.LogError(
                "[PhaseTaskManager] PhaseLoopManager not found!"
            );

            return;
        }

        currentTasks = new List<BaseTask>();

        GameState state =
            phaseManager.CurrentState;

        if (state == GameState.Awake &&
            awakeTasks != null)
        {
            foreach (var t in awakeTasks)
            {
                if (t != null)
                {
                    t.ResetTask();

                    currentTasks.Add(t);
                }
            }
        }
        else if (state == GameState.Dream &&
                 dreamTasks != null)
        {
            foreach (var t in dreamTasks)
            {
                if (t != null)
                {
                    t.ResetTask();

                    currentTasks.Add(t);
                }
            }
        }

        ShuffleTasks();

        if (currentTasks.Count > 0)
        {
            currentTasks[0].ActivateTask();

            ShowCurrentTaskBubble();
        }

        Debug.Log(
            "[PhaseTaskManager] TASK SETUP: " +
            state
        );
    }

    public void CompleteTask(BaseTask task)
    {
        if (currentTasks == null)
        {
            return;
        }

        currentTasks.Remove(task);

        if (currentTasks.Count <= 0)
        {
            RebuildCurrentPhaseTasks();
        }

        if (currentTasks.Count > 0)
        {
            currentTasks[0].ActivateTask();

            ShowCurrentTaskBubble();
        }
    }

    private void RebuildCurrentPhaseTasks()
    {
        currentTasks = new List<BaseTask>();

        GameState state =
            phaseManager.CurrentState;

        if (state == GameState.Awake &&
            awakeTasks != null)
        {
            foreach (var t in awakeTasks)
            {
                if (t != null)
                {
                    t.ResetTask();

                    currentTasks.Add(t);
                }
            }
        }
        else if (state == GameState.Dream &&
                 dreamTasks != null)
        {
            foreach (var t in dreamTasks)
            {
                if (t != null)
                {
                    t.ResetTask();

                    currentTasks.Add(t);
                }
            }
        }

        ShuffleTasks();

        Debug.Log(
            "[PhaseTaskManager] QUEST LOOP RESET"
        );
    }

    public bool AreAllCurrentTasksCompleted()
    {
        return currentTasks != null &&
               currentTasks.Count == 0;
    }

    public void ForceActivateTask(BaseTask task)
    {
        if (currentTasks == null)
        {
            currentTasks =
                new List<BaseTask>();
        }

        if (currentTasks.Count > 0)
        {
            currentTasks[0]
                .DeactivateTask();
        }

        currentTasks.Remove(task);

        currentTasks.Insert(0, task);

        task.ActivateTask();

        ShowCurrentTaskBubble();
    }

    private void ShowCurrentTaskBubble()
    {
        string text = GetCurrentTaskText();

        if (objectiveText != null)
        {
            objectiveText.text = text;
        }

        if (taskBubble == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(text))
        {
            taskBubble.ClearBubble();
        }
        else
        {
            taskBubble.ShowNewTask(text);
        }
    }

    public void UpdateObjectiveUI()
    {
        string text = GetCurrentTaskText();

        if (objectiveText != null)
        {
            objectiveText.text = text;
        }

        if (taskBubble != null)
        {
            taskBubble.RefreshText(text);
        }
    }

    private string GetCurrentTaskText()
    {
        return (currentTasks == null ||
                currentTasks.Count == 0)
            ? ""
            : currentTasks[0].GetTaskText();
    }

    private void ShuffleTasks()
    {
        for (int i = 0;
             i < currentTasks.Count;
             i++)
        {
            int rand =
                Random.Range(
                    i,
                    currentTasks.Count
                );

            BaseTask temp =
                currentTasks[i];

            currentTasks[i] =
                currentTasks[rand];

            currentTasks[rand] = temp;
        }
    }
}