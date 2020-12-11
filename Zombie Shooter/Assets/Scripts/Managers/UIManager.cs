﻿using System.Collections.Specialized;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;

public enum GameStatus
{
    Start = 0,
    Connecting = 1,
    Waiting = 2,
    Playing = 3,
    Paused = 4
}

public class UIManager : MonoBehaviour
{
    [Header("Start UI")]
    public GameObject startScreen;
    public Button startButton;
    public InputField playerName;

    [Header("Connecting UI")]
    public GameObject connectingScreen;

    [Header("Waiting Room UI")]
    public GameObject waitingScreen;
    public Button playButton;
    public Text playerListText;

    [Header("In Game UI")]
    public GameObject inGameScreen;
    public Slider healthBar;
    public GameObject scoreCard;
    public Text latencyText;
    private OrderedDictionary playerScores = new OrderedDictionary();

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
        connectingScreen.SetActive(gameStatus == GameStatus.Connecting);
        inGameScreen.SetActive(gameStatus == GameStatus.Playing);
        waitingScreen.SetActive(gameStatus == GameStatus.Waiting);
        pauseScreen.SetActive(gameStatus == GameStatus.Paused);
    }

    private void Start()
    {
        playerName.characterLimit = Constants.MAX_NAME_LENGTH;
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

    public void ShowConnecting()
    {
        SetScreensActive(GameStatus.Connecting);
    }

    public void StartGame()
    {
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

    public void AddPlayer(string playerName)
    {
        int padding = 5;
        GameObject newPlayerCard = Instantiate(scoreCard, inGameScreen.transform);
        RectTransform rectTransform = newPlayerCard.GetComponent<RectTransform>();
        if (playerScores.Count == 0)
            rectTransform.anchoredPosition = new Vector3(-padding, -padding, 0);
        else
        {
            RectTransform prev = ((Text)playerScores[playerScores.Count - 1]).rectTransform;
            rectTransform.anchoredPosition = new Vector3(prev.anchoredPosition.x - prev.rect.width - padding, -padding, 0);
        }
        Text scoreText = newPlayerCard.GetComponentsInChildren<Text>().Where(text => text.gameObject.name == "Score").FirstOrDefault();
        Text playerNameText = newPlayerCard.GetComponentsInChildren<Text>().Where(text => text.gameObject.name == "Name").FirstOrDefault();
        playerNameText.text = playerName;
        playerScores.Add(playerName, scoreText);
    }

    public void UpdateScore(string playerName, int newScore)
    {
        if (!playerScores.Contains(playerName)) return;
        Text score = (Text)playerScores[playerName];
        score.text = (int.Parse(score.text) + newScore).ToString();
    }

    public void UpdateAllScores(int newScore)
    {
        foreach (string player in playerScores.Keys)
            UpdateScore(player, newScore);
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
