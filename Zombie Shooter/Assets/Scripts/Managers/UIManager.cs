using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public Slider healthBar;

    public void SetHealth(int value) //between 0 and 100
    {
        healthBar.value = value;
    }
}
