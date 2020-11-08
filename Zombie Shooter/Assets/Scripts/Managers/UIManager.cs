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

    [Header("Speech UI")]
    public Image micIndicator;
    private SphinxExample sphinx;


    public void SetScreensActive(bool startScreenActive, bool inGameScreenActive)
    {
        startScreen.SetActive(startScreenActive);
        inGameScreen.SetActive(inGameScreenActive);
    }

    private IEnumerator Start()
    {
        SetScreensActive(true, false);

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
        SetScreensActive(true, false);
    }
    public void ShowStart()
    {
        SetScreensActive(true, false);
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

    private void MicConnected()
    {
        Debug.Log($"<color=green><b>Connected to: {sphinx.mic.Name}</b></color>");
        UpdateIndicator(new Color(0,1,0,1));
    }

    private void UpdateSpeechUI(string str)
    {
        StartCoroutine(PrintVoiceCommand(str));
    }

    private IEnumerator PrintVoiceCommand(string str)
    {
        Debug.Log($"Voice Command: {str.ToUpper()}");
        yield return new WaitForSeconds(1);
    }

    public void UpdateIndicator(Color col)
    {
        micIndicator.color = col;
    }
}
