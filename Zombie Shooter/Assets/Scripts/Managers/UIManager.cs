using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public enum GameStatus
{
    Start = 0,
    Playing = 1,
    Paused = 2
}

public class UIManager : MonoBehaviour
{
    [Header("Start UI")]
    public GameObject startScreen;
    public Button startButton;
    public Text startButtonText;

    [Header("In Game UI")]
    public GameObject inGameScreen;
    public Slider healthBar;
    public Text scoreText;

    [Header("Pause Menu UI")]
    public GameObject pauseScreen;
    public Button resumeButton;
    public Button exitButton;

    [Header("Speech UI")]
    public Image micIndicator;
    private SphinxExample sphinx;

    private GameManager gameManager;
    private GameStatus gameStatus;


    // [0: Start Screen, 1: In-Game Screen, 2: Pause Screen]
    public void SetScreensActive(int screen)
    {
        gameStatus = (GameStatus) screen;

        startScreen.SetActive(gameStatus == GameStatus.Start);
        inGameScreen.SetActive(gameStatus == GameStatus.Playing);
        pauseScreen.SetActive(gameStatus == GameStatus.Paused);
    }

    private IEnumerator Start()
    {
        gameManager = GetComponent<GameManager>();

        SetScreensActive(0);

        resumeButton.onClick.AddListener(ResumeGame);
        exitButton.onClick.AddListener(ReloadGame);

        UpdateIndicator(new Color(1,0,0,1));

        sphinx = FindObjectOfType<SphinxExample>();
        sphinx.OnSpeechRecognized += UpdateSpeechUI;

        while (sphinx.mic == null)
        {
            yield return null;
        }
        MicConnected();
    }

    public void ShowLoading()
    {
        SetScreensActive(0);
    }
    public void ShowStart()
    {
        SetScreensActive(0);
    }

    public void StartGame()
    {
        scoreText.text = "Score: 0";
        SetScreensActive(1);
    }

    public void PauseGame()
    {
        SetScreensActive(2);
        Time.timeScale = 0;
    }

    public void ResumeGame()
    {
        SetScreensActive(1);
        Time.timeScale = 1;
    }

    public void ReloadGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        Time.timeScale = 1;
    }

    public void SetHealth(int value) //between 0 and 100
    {
        healthBar.value = value;
    }

    public void UpdateScore(int newScore)
    {
        scoreText.text = "Score: " + newScore.ToString();
    }

    private void MicConnected()
    {
        Debug.Log($"<color=green><b>Connected to: {sphinx.mic.Name}</b></color>");
        UpdateIndicator(new Color(0,1,0,1));
    }

    private void UpdateSpeechUI(string str)
    {
        StartCoroutine(ProcessVoiceCommand(str));
    }

    private IEnumerator ProcessVoiceCommand(string cmd)
    {
        if (cmd.Contains(" "))
            cmd = cmd.Substring(0,cmd.IndexOf(" "));
        
        Debug.Log($"Voice Command: {cmd.ToUpper()}");
        cmd = cmd.ToLower();

        switch (gameStatus)
        {
            case GameStatus.Start:
                if (cmd == "play") {
                    gameManager.StartGame();
                }
                break;
            case GameStatus.Playing:
                if (cmd == "reload") {
                    // reload code
                } else if (cmd == "pause") {
                    PauseGame();
                    UpdateIndicator(new Color(0,1,0,1));
                }
                break;
            case GameStatus.Paused:
                if (cmd == "resume") {
                    ResumeGame();
                } else if (cmd == "exit") {
                    ReloadGame();
                }
                break;
            default:
                // wtf
                break;
        }

        yield return new WaitForSecondsRealtime(1);
    }

    public void UpdateIndicator(Color col)
    {
        micIndicator.color = col;
    }
}
