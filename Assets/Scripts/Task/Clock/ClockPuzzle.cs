using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClockPuzzle : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform minuteHand;
    [SerializeField] private RectTransform hourHand;

    [SerializeField] private GameObject puzzleUI;
    [SerializeField] private MonoBehaviour playerMovement;
    [SerializeField] private PlayerInteract2D playerInteract;

    private int currentMinutes;
    private bool isActive;


    private void Start()
    {
        puzzleUI.SetActive(false);

        UpdateClockVisual();
    }

    private void Update()
    {
        if (!isActive)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            RotateTime(-5);
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            RotateTime(5);
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ClosePuzzle();
        }
    }

    public void OpenPuzzle()
    {
        isActive = true;

        puzzleUI.SetActive(true);

        playerMovement.enabled = false;

        playerInteract.canInteract = false;
    }

    public void ClosePuzzle()
    {

        isActive = false;

        puzzleUI.SetActive(false);

        playerMovement.enabled = true;
        playerInteract.canInteract = true;
    }
    private void RotateTime(int amount)
    {
        currentMinutes += amount;

        if (currentMinutes < 0)
        {
            currentMinutes += 720;
        }

        if (currentMinutes >= 720)
        {
            currentMinutes -= 720;
        }

        UpdateClockVisual();
    }

    private void UpdateClockVisual()
    {
        float minuteRotation =
            -(currentMinutes % 60) * 6f;

        float hourRotation =
            -(currentMinutes / 60f) * 30f;

        minuteHand.localRotation =
            Quaternion.Euler(0f, 0f, minuteRotation);

        hourHand.localRotation =
            Quaternion.Euler(0f, 0f, hourRotation);
    }
}