using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhaseTaskManager : MonoBehaviour
{
    public List<BaseTask> awakeTasks;
    public List<BaseTask> dreamTasks;

    private List<BaseTask> currentTasks;

    [Header("Bubble")]
    [Tooltip("Referensi ke TaskBubble yang ada di atas player.")]
    [SerializeField] private TaskBubble taskBubble;

    // objectiveText lama dipertahankan opsional untuk backward compatibility.
    [Header("Legacy UI (opsional)")]
    [SerializeField] private TMPro.TextMeshProUGUI objectiveText;

    public PhaseLoopManager phaseManager;

    private GameState lastState;

    // =========================================================
    // UNITY LIFECYCLE
    // =========================================================

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

    // =========================================================
    // PHASE CHANGED LISTENER
    // =========================================================

    private void OnPhaseChanged(GameState state)
    {
        if (phaseManager == null)
        {
            return;
        }

        // ?? Masuk Liminal ??????????????????????????????????????
        if (state == GameState.Liminal)
        {
            if (currentTasks != null && currentTasks.Count > 0)
            {
                currentTasks[0].ForceStopTask();
            }

            // Sembunyikan bubble saat masuk Liminal
            if (taskBubble != null)
            {
                taskBubble.ClearBubble();
            }

            lastState = state;
            return;
        }

        // ?? Kembali dari Liminal ???????????????????????????????
        if (lastState == GameState.Liminal && state != GameState.Liminal)
        {
            SetupTasks();
            lastState = state;
            return;
        }

        // ?? Normal phase switch (Awake ? Dream) ???????????????
        SetupTasks();
        lastState = state;
    }

    // =========================================================
    // TASK SETUP
    // =========================================================

    public void SetupTasks()
    {
        if (phaseManager == null)
        {
            Debug.LogError("[PhaseTaskManager] PhaseLoopManager not found!");
            return;
        }

        currentTasks = new List<BaseTask>();

        GameState state = phaseManager.CurrentState;

        // Reset semua task agar clean setiap switch phase
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

        // Assign task sesuai phase
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

        // Bubble otomatis muncul dengan task pertama (typewriter dari awal)
        ShowCurrentTaskBubble();

        Debug.Log("[PhaseTaskManager] TASK SETUP: " + state);
    }

    // =========================================================
    // COMPLETE / LOOP
    // =========================================================

    public void CompleteTask(BaseTask task)
    {
        if (currentTasks == null)
        {
            return;
        }

        currentTasks.Remove(task);

        if (currentTasks.Count <= 0)
        {
            LoopCurrentPhaseTasks();
            return;
        }

        // Aktifkan task berikutnya
        currentTasks[0].ActivateTask();

        // Bubble otomatis muncul dengan task baru (typewriter dari awal)
        ShowCurrentTaskBubble();
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

        // Bubble muncul lagi dengan task yang di-loop
        ShowCurrentTaskBubble();
    }

    // =========================================================
    // FORCE ACTIVATE
    // =========================================================

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

        // Bubble muncul dengan task yang di-force
        ShowCurrentTaskBubble();
    }

    // =========================================================
    // BUBBLE & UI
    // =========================================================

    /// <summary>
    /// Tampilkan bubble dengan task aktif saat ini.
    /// Auto-show + typewriter dari awal — dipakai saat task baru di-assign.
    /// </summary>
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
            // ShowNewTask = auto-show + typewriter dari awal
            taskBubble.ShowNewTask(text);
        }
    }

    /// <summary>
    /// Refresh text di UI dan bubble tanpa auto-show dan tanpa typewriter.
    /// Dipakai untuk update progress di tengah task (misal: TrashCleanupTask).
    /// </summary>
    public void UpdateObjectiveUI()
    {
        string text = GetCurrentTaskText();

        if (objectiveText != null)
        {
            objectiveText.text = text;
        }

        // RefreshText: update text kalau bubble terbuka, tidak paksa show
        if (taskBubble != null)
        {
            taskBubble.RefreshText(text);
        }
    }

    private string GetCurrentTaskText()
    {
        return (currentTasks == null || currentTasks.Count == 0)
            ? ""
            : currentTasks[0].GetTaskText();
    }

    // =========================================================
    // UTILITIES
    // =========================================================

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