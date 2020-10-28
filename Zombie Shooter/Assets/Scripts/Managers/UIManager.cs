using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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


    public void SetScreensActive(bool startScreenActive, bool inGameScreenActive)
    {
        startScreen.SetActive(startScreenActive);
        inGameScreen.SetActive(inGameScreenActive);
    }

    public void Start()
    {
        SetScreensActive(true, false);
    }

    public void ShowLoading()
    {
        SetScreensActive(true, false);
        startButton.interactable = false;
        startButtonText.text = "Loading";
    }
    public void ShowStart()
    {
        SetScreensActive(true, false);
        startButton.interactable = true;
        startButtonText.text = "Start";
    }

    public void StartGame()
    {
        scoreText.text = "Score: 0";
        SetScreensActive(false, true);
    }

    public void SetHealth(int value) //between 0 and 100
    {
        healthBar.value = value;
    }

    public void UpdateScore(int newScore)
    {
        scoreText.text = "Score: " + newScore.ToString();
    }
}
