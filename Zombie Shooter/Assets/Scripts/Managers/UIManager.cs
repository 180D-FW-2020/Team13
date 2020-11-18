using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public enum GameStatus
{
    Start = 0,
    Waiting = 1,
    Playing = 2,
    Paused = 3
}

public class UIManager : MonoBehaviour
{
    [Header("Start UI")]
    public GameObject startScreen;
    public Button startButton;
    public InputField playerName;

    [Header("Waiting Room UI")]
    public GameObject waitingScreen;
    public Button playButton;
    public Text playerListText;

    [Header("In Game UI")]
    public GameObject inGameScreen;
    public Slider healthBar;
    public Text scoreText;
    public Text latencyText;

    [Header("Pause Menu UI")]
    public GameObject pauseScreen;
    public Button resumeButton;
    public Button exitButton;

    [Header("Speech UI")]
    public Image micIndicator;

    private GameManager gameManager;

    public void SetScreensActive(GameStatus gameStatus)
    {
        startScreen.SetActive(gameStatus == GameStatus.Start);
        inGameScreen.SetActive(gameStatus == GameStatus.Playing);
        waitingScreen.SetActive(gameStatus == GameStatus.Waiting);
        pauseScreen.SetActive(gameStatus == GameStatus.Paused);
    }

    private void Start()
    {
        SetScreensActive(GameStatus.Start);
    }

    public void ShowStart()
    {
        SetScreensActive(GameStatus.Start);
    }
    public void EnterWaitingRoom()
    {
        SetScreensActive(GameStatus.Waiting);
    }

    public void StartGame()
    {
        scoreText.text = "Score: 0";
        SetScreensActive(GameStatus.Playing);
    }

    #region Start Screen UI
    public string GetPlayerName()
    {
        return playerName.text;
    }
    #endregion

    #region Waiting Room UI
    public void UpdatePlayerList(List<string> list)
    {
        playerListText.text = string.Join("\n", list);
    }
    #endregion


    #region In Game UI
    public void UpdateHealth(int value) //between 0 and 100
    {
        healthBar.value = value;
    }

    public void UpdateScore(int newScore)
    {
        scoreText.text = "Score: " + newScore.ToString();
    }

    public void UpdateLatency(double latency)
    {
        latencyText.text = "Latency: " + latency.ToString() + "ms";
    }
    #endregion

    public void UpdateMicIndicator(Color col)
    {
        micIndicator.color = col;
    }
}
