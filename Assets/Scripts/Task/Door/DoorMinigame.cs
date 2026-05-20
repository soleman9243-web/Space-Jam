using System;
using UnityEngine;

public class DoorMinigame : MonoBehaviour
{
    [SerializeField] private DoorChoice[] doors;
    [SerializeField] private GameObject doorParent;

    [Header("Penalty Scaling")]
    [SerializeField] private float basePenalty = 15f;
    [SerializeField] private float penaltyPerLoop = 5f;
    [SerializeField] private float maxPenalty = 50f;

    public event Action<bool> OnFinished;

    private bool finished;
    private float currentPenalty;

    private int GetCurrentLoop()
    {
        PhaseLoopManager pm = FindObjectOfType<PhaseLoopManager>();
        return pm != null ? pm.currentLoop : 1;
    }

    public void OpenDoors()
    {
        int loop = GetCurrentLoop();
        currentPenalty = Mathf.Min(basePenalty + (loop - 1) * penaltyPerLoop, maxPenalty);

        doorParent.SetActive(true);
        SetupDoors();

        Debug.Log($"[DoorMinigame] Loop {loop} ? Penalty salah: {currentPenalty}");
    }

    private void SetupDoors()
    {
        finished = false;
        int correctIndex = UnityEngine.Random.Range(0, doors.Length);

        for (int i = 0; i < doors.Length; i++)
        {
            doors[i].SetDoor(this, i == correctIndex);
            doors[i].gameObject.SetActive(true);
        }
    }

    private void HideAllDoors()
    {
        foreach (var door in doors)
            door.gameObject.SetActive(false);
    }

    public void ChooseDoor(DoorChoice chosenDoor)
    {
        if (finished) return;

        if (chosenDoor.IsCorrect)
        {
            finished = true;
            HideAllDoors();
            OnFinished?.Invoke(true);
        }
        else
        {
            if (PlayerStatus.Instance != null)
                PlayerStatus.Instance.ReduceStability(currentPenalty);

            chosenDoor.gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        OnFinished = null;
    }
}