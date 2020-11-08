using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIHandler : MonoBehaviour
{
    private SphinxExample sphinx;
    public Image micIndicator;
    private UIManager uiManager;

    private IEnumerator Start()
    {
        uiManager = GetComponent<UIManager>();

        updateIndicator(new Color(1,0,0,1));

        sphinx = FindObjectOfType<SphinxExample>();
        sphinx.OnSpeechRecognized += UpdateUI;

        // Wait until the mic is initialized before we setup the UI.
        while(sphinx.mic == null)
        {
            yield return null;
        }
        SetupUI();
    }

    private void SetupUI()
    {
        Debug.Log($"<color=green><b>Connected to: {sphinx.mic.Name}</b></color>");
        updateIndicator(new Color(0,1,0,1));
    }

    private void UpdateUI(string str)
    {
        StartCoroutine(UpdateUIEnum(str));
    }

    private IEnumerator UpdateUIEnum(string str)
    {
        Debug.Log($"Voice Command: {str.ToUpper()}");
        yield return new WaitForSeconds(1);
    }

    public void updateIndicator(Color col)
    {
        micIndicator.color = col;
    }
}
