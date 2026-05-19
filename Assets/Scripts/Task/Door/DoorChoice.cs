using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class DoorChoice : MonoBehaviour
{
    private DoorMinigame manager;

    public bool IsCorrect { get; private set; }

    public void SetDoor(DoorMinigame doorManager, bool correct)
    {
        manager = doorManager;
        IsCorrect = correct;
    }

    public void Choose()
    {
        manager.ChooseDoor(this);
    }
}