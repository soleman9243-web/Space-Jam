using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorMinigame : MonoBehaviour
{
    [SerializeField] private DoorChoice[] doors;
    [SerializeField] private GameObject doorParent;

    public event Action<bool> OnFinished;

    private bool finished;

    public void OpenDoors()
    {
        doorParent.SetActive(true);
        SetupDoors();
    }

    private void SetupDoors()
    {
        finished = false;

        int correctIndex = UnityEngine.Random.Range(0, doors.Length);

        for (int i = 0; i < doors.Length; i++)
        {
            bool isCorrect = (i == correctIndex);
            doors[i].SetDoor(this, isCorrect);
            doors[i].gameObject.SetActive(true);
        }
    }

    public void ChooseDoor(DoorChoice chosenDoor)
    {
        if (finished)
        {
            return;
        }

        if (chosenDoor.IsCorrect)
        {
            finished = true;
            OnFinished?.Invoke(true);
        }
        else
        {
            PlayerStatus.Instance.ReduceStability(15);
            chosenDoor.gameObject.SetActive(false);

            OnFinished?.Invoke(false);
        }
    }

    private void OnDisable()
    {
        OnFinished = null;
    }
}