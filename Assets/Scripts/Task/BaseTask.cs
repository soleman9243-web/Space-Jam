using UnityEngine;

public class BaseTask : MonoBehaviour
{
    [Header("Task")]
    public string taskText;

    protected bool completed = false;

    public bool IsActive { get; private set; }

    public virtual void ActivateTask()
    {
        IsActive = true;
        completed = false;
    }

    public virtual void DeactivateTask()
    {
        IsActive = false;
    }

    public virtual void ResetTask()
    {
        completed = false;
        IsActive = false;
    }

    public virtual void CompleteTask()
    {
        if (completed)
        {
            return;
        }

        completed = true;
        DeactivateTask();

        FindObjectOfType<PhaseTaskManager>()
            .CompleteTask(this);
    }

    public virtual string GetTaskText()
    {
        return taskText;
    }

}