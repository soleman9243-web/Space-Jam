using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [SerializeField] private GameOver gameOver;

    void Awake()
    {
        Instance = this;
    }

    public void GameOver()
    {
        gameOver.Show();
    }
}