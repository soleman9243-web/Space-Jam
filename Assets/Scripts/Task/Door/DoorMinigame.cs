using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorMinigame : MonoBehaviour
{
    [Header("Doors")]
    [SerializeField] private DoorChoice[] doors;

    [Header("Door Parent")]
    [SerializeField] private GameObject doorParent;

    [Header("Difficulty")]
    [Range(1, 5)]
    [SerializeField] private int difficulty = 1;

    private List<DoorChoice> correctDoors = new List<DoorChoice>();

    private bool finished;

    private void Awake()
    {
        if (doorParent != null)
        {
            doorParent.SetActive(false);
        }
    }

    private void OnEnable()
    {
        SetupDoors();
    }

    public void OpenDoors()
    {
        if (doorParent != null)
        {
            doorParent.SetActive(true);
        }

        SetupDoors();
    }

    private void SetupDoors()
    {
        finished = false;

        correctDoors.Clear();

        for (int i = 0; i < doors.Length; i++)
        {
            doors[i].gameObject.SetActive(true);
        }

        List<DoorChoice> shuffledDoors = new List<DoorChoice>(doors);

        for (int i = 0; i < shuffledDoors.Count; i++)
        {
            int rand = Random.Range(i, shuffledDoors.Count);

            DoorChoice temp = shuffledDoors[i];
            shuffledDoors[i] = shuffledDoors[rand];
            shuffledDoors[rand] = temp;
        }

        int correctCount = GetCorrectDoorCount();

        for (int i = 0; i < shuffledDoors.Count; i++)
        {
            bool isCorrect = i < correctCount;

            shuffledDoors[i].SetDoor(this, isCorrect);

            if (isCorrect)
            {
                correctDoors.Add(shuffledDoors[i]);
            }
        }
    }

    private int GetCorrectDoorCount()
    {
        switch (difficulty)
        {
            case 1:
            case 2:
                return 3;

            case 3:
            case 4:
                return 2;

            case 5:
                return 1;
        }

        return 1;
    }

    private int GetStabilityAmount()
    {
        return 5 + ((difficulty - 1) * 10);
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

            for (int i = 0; i < doors.Length; i++)
            {
                if (doors[i] != chosenDoor)
                {
                    doors[i].gameObject.SetActive(false);
                }
            }

            PlayerStatus.Instance.IncreaseStability(50);

            Debug.Log("Correct Door");
        }
        else
        {
            PlayerStatus.Instance.ReduceStability(GetStabilityAmount());

            Debug.Log("Wrong Door");
        }
    }

    public void SetDifficulty(int newDifficulty)
    {
        difficulty = Mathf.Clamp(newDifficulty, 1, 5);

        SetupDoors();
    }

    private void OnValidate()
    {
        difficulty = Mathf.Clamp(difficulty, 1, 5);

        if (Application.isPlaying)
        {
            SetupDoors();
        }
    }
}