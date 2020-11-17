﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Constants
{
    //Animator triggers
    public const string TRIGGER_MOVE = "TriggerMove";
    public const string TRIGGER_ATTACK = "TriggerAttack";
    public const string TRIGGER_FALLDOWN = "TriggerFallingDown";

    public const int CAMERA_INPUT_WIDTH = 640;
    public const int CAMERA_INPUT_FPS = 20;
}

[Serializable]
public class GameState
{
    public long timestamp;
    public long serverTimestamp;
    public string id;
    public List<float> playerPosition;
}

[Serializable]
public class Register
{
    public string id;
}

[Serializable]
public class Initialize
{
    public List<string> playerList;
    public Dictionary<string, float> enemyPositions;
}

[Serializable]
public class EnemyKilled
{
    public string id;
    public string enemyId;
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