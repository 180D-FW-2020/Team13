using JetBrains.Annotations;
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


public class Ping
{
    public long timestamp;
}

//sent from client to server
[Serializable]
public class GameState
{
    public string id;
    public List<float> rotation;
    public bool shooting;
}

//sent from server to client
[Serializable]
public class RemoteState
{
    public string id;
    public int health;
    public int score;
    public int kills;
    public List<float> rotation;
    public int shooting;
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
    public Dictionary<string, string> enemyPositions;
}

[Serializable]
public class Leave
{
    public string id;
    public List<string> playerList;
}

[Serializable]
public class EnemyKilled
{
    public string id;
    public string enemyId;
}

[Serializable]
public class EnemyAttack
{
    public string id;
}