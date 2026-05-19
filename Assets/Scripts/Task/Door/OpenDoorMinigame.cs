using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenDoorMinigame : MonoBehaviour
{
    [SerializeField] private DoorMinigame doorMinigame;

    private void Update()
    {
        if(Input.GetKeyUp(KeyCode.N))
        {
            Open();
        }
    }
    public void Open()
    {
        doorMinigame.OpenDoors();
    }
}