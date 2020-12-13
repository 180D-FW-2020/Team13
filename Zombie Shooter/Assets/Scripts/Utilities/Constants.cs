using System;
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
    public const int CAMERA_INPUT_HEIGHT = 360;
    public const int CAMERA_INPUT_FPS = 20;

    public const int MAX_NAME_LENGTH = 10;
}

[Serializable]
public class GameState
{
    public long timestamp;
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
    // public Dictionary<string, float> enemyPositions;
    public Dictionary<string, string> enemyPositions;
}

[Serializable]
public class EnemyKilled
{
    public string id;
    public string enemyId;
}