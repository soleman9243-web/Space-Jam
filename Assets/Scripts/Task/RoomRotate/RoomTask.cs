using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomTask : BaseTask
{
    [Header("Room System")]
    public RandomWASDRotate roomController;

    [Header("Reward")]
    public float rewardStability = 30f;

    public override void ActivateTask()
    {
        base.ActivateTask();

        if (roomController != null)
        {
            roomController.enabled = true;
        }
    }

    public override void DeactivateTask()
    {
        base.DeactivateTask();

        if (roomController != null)
        {
            roomController.enabled = false;
        }
    }

    public override void CompleteTask()
    {
        if (completed)
        {
            return;
        }

        completed = true;
        DeactivateTask();

        if (PlayerStatus.Instance != null)
        {
            PlayerStatus.Instance.IncreaseStability(rewardStability);
        }

        FindObjectOfType<PhaseTaskManager>()
            .CompleteTask(this);
    }

    public override string GetTaskText()
    {
        return "Escape the shifting room";
    }
}