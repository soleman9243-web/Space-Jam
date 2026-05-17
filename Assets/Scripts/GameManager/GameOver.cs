using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOver : MonoBehaviour
{
    public static GameOver Instance;

    [SerializeField] private GameObject panel;

    private void Awake()
    {
        Instance = this;

        panel.SetActive(false);
    }

    public void Show()
    {
        Time.timeScale = 0f;

        panel.SetActive(true);
    }

    public void Retry()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}