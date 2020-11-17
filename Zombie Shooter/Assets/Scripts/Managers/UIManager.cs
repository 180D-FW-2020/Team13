using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

    public void SetScreensActive(bool startScreenActive, bool waitingScreenActive, bool inGameScreenActive)
    {
        startScreen.SetActive(startScreenActive);
        waitingScreen.SetActive(waitingScreenActive);
        inGameScreen.SetActive(inGameScreenActive);
    }

    public void Start()
    {
        ShowStart();
    }

    public void ShowStart()
    {
        SetScreensActive(true, false, false);
    }
    public void EnterWaitingRoom()
    {
        SetScreensActive(false, true, false);
    }

    public void StartGame()
    {
        SetScreensActive(false, false, true);
    }

    #region Start UI
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
}
