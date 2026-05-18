using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorDifficultyTester : MonoBehaviour
{
    [SerializeField] private DoorMinigame minigame;

    [Header("Debug")]
    [Range(1, 5)]
    [SerializeField] private int difficulty = 1;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            difficulty++;

            if (difficulty > 5)
            {
                difficulty = 5;
            }

            ApplyDifficulty();
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            difficulty--;

            if (difficulty < 1)
            {
                difficulty = 1;
            }

            ApplyDifficulty();
        }
    }

    private void Start()
    {
        ApplyDifficulty();
    }

    private void ApplyDifficulty()
    {
        minigame.SetDifficulty(difficulty);

        Debug.Log("Difficulty: " + difficulty);
    }
}