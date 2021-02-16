using System.Collections.Specialized;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Runtime.CompilerServices;
using System;

public enum GameStatus
{
    MainMenu = 0,
    ControlsMenu = 1,
    SettingsMenu = 2,
    Start = 3,
    Connecting = 4,
    Waiting = 5,
    Playing = 6,
    Moving = 7,
    Transitioning = 8,
    KillCam = 9,
    Paused = 10,
    Ended = 11,
    Calibrating = 12,
}

// UIManager coordinates the changing of screens and UI element initialization
// In addition, the value of input fields (i.e. player name) can be retrived and
// in-game elements can be dynamically updated (i.e. player list and scores)
public class UIManager : MonoBehaviour
{
    [Header("Main Menu UI")]
    public GameObject mainMenuScreen;
    public GameObject controlsScreen;
    public GameObject settingsScreen;
    public Button mainStartButton;
    public Button controlsButton;
    public Button settingsButton;
    public Button backButton_C;
    public Button backButton_S;

    [Header("Start UI")]
    public GameObject startScreen;
    public Button startButton;
    public Button backButton;
    public InputField playerName;

    [Header("End UI")]
    public GameObject endScreen;

    [Header("Calibration UI")]
    public GameObject calibrationScreen;
    public Text calibrationText;

    [Header("Connecting UI")]
    public GameObject connectingScreen;

    [Header("Waiting Room UI")]
    public GameObject waitingScreen;
    public Button readyButton;
    public Text playerListText;

    [Header("In Game UI")]
    public GameObject inGameScreen;
    public Text currentAmmo;
    public GameObject scoreCard;
    public Text latencyText;
    private OrderedDictionary playerScores = new OrderedDictionary();
    private OrderedDictionary playerHealthBars = new OrderedDictionary();

    [Header("Killcam UI")]
    public GameObject killcamScreen;

    [Header("Pause Menu UI")]
    public GameObject pauseScreen;
    public Button resumeButton;
    public Button exitButton;

    [Header("Speech UI")]
    public Image micIndicator;
    public bool micConnected = false;

    private GameManager gameManager;

    private void Start()
    {
        playerName.characterLimit = Constants.MAX_NAME_LENGTH;
        SetScreensActive(GameStatus.MainMenu);
    }

    // Set the correct UI screen active based on current game state
    public void SetScreensActive(GameStatus gameStatus)
    {
        mainMenuScreen.SetActive(IsStatus(gameStatus, GameStatus.MainMenu));
        controlsScreen.SetActive(IsStatus(gameStatus, GameStatus.ControlsMenu));
        settingsScreen.SetActive(IsStatus(gameStatus, GameStatus.SettingsMenu));
        startScreen.SetActive(IsStatus(gameStatus, GameStatus.Start));
        endScreen.SetActive(IsStatus(gameStatus, GameStatus.Ended));
        connectingScreen.SetActive(IsStatus(gameStatus, GameStatus.Connecting));
        inGameScreen.SetActive(IsStatus(gameStatus, GameStatus.Playing, GameStatus.Moving, GameStatus.Transitioning));
        killcamScreen.SetActive(IsStatus(gameStatus, GameStatus.KillCam));
        waitingScreen.SetActive(IsStatus(gameStatus, GameStatus.Waiting));
        pauseScreen.SetActive(IsStatus(gameStatus, GameStatus.Paused));
        calibrationScreen.SetActive(IsStatus(gameStatus, GameStatus.Calibrating));
    }

    private bool IsStatus(GameStatus state, params GameStatus[] states)
    {
        return states.Contains(state);
    }

    public void ShowMainMenu()
    {
        SetScreensActive(GameStatus.MainMenu);
    }

    public void ShowControls()
    {
        SetScreensActive(GameStatus.ControlsMenu);
    }

    public void ShowSettings()
    {
        SetScreensActive(GameStatus.SettingsMenu);
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

    public void StartCalibration()
    {
        SetScreensActive(GameStatus.Calibrating);
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
    public void UpdateAmmo(int value)
    {
        currentAmmo.text = $"Ammo: {value}";
    }
    public void UpdateHealth(string playerName, int value) //between 0 and 100
    {
        if (!playerHealthBars.Contains(playerName)) return;
        Slider healthBar = (Slider)playerHealthBars[playerName];
        healthBar.value = value / 100f;
    }

    // dynamically add new score card
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
            rectTransform.anchoredPosition = new Vector3(((playerScores.Count - 1) * -80f) - prev.rect.width - padding, -padding, 0);
        }
        Text scoreText = newPlayerCard.GetComponentsInChildren<Text>().Where(text => text.gameObject.name == "Score").FirstOrDefault();
        Slider healthBar = newPlayerCard.GetComponentsInChildren<Slider>().FirstOrDefault();
        Text playerNameText = newPlayerCard.GetComponentsInChildren<Text>().Where(text => text.gameObject.name == "Name").FirstOrDefault();
        playerNameText.text = playerName;
        healthBar.value = 1;
        playerScores.Add(playerName, scoreText);
        playerHealthBars.Add(playerName, healthBar);
    }

    public void UpdateScore(string playerName, int newScore)
    {
        if (!playerScores.Contains(playerName)) return;
        Text score = (Text)playerScores[playerName];
        score.text = newScore.ToString();
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
