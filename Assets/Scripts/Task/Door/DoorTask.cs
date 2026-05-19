using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorTask : BaseTask
{
    public DoorMinigame minigame;

    public override void ActivateTask()
    {
        base.ActivateTask();

        minigame.gameObject.SetActive(true);
        minigame.OpenDoors();

        minigame.OnFinished -= HandleResult;
        minigame.OnFinished += HandleResult;
    }

    private void HandleResult(bool success)
    {
        if (success)
        {
            PlayerStatus.Instance.IncreaseStability(50);
        }

        CompleteTask();
    }

    public override void DeactivateTask()
    {
        base.DeactivateTask();

        minigame.OnFinished -= HandleResult;
        minigame.gameObject.SetActive(false);
    }

    public override string GetTaskText()
    {
        return "Find the right door";
    }
}