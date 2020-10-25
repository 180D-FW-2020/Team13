using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Constants
{
    //Animator triggers
    public static string TRIGGER_MOVE = "TriggerMove";
    public static string TRIGGER_ATTACK = "TriggerAttack";
    public static string TRIGGER_FALLDOWN = "TriggerFallingDown";
}

public enum MouseControlType
{
    Reticle = 1,
    Camera = 2
}

public enum InputType
{
    Mouse = 1,
    CV = 2
}