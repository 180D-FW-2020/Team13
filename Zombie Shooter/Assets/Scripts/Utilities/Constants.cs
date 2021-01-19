using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Constants
{
    //Animator triggers
    public const string TRIGGER_RUN = "TriggerRun";
    public const string TRIGGER_WALK = "TriggerWalk";
    public const string TRIGGER_ATTACK = "TriggerAttack";
    public const string TRIGGER_FALLDOWN = "TriggerFallingDown";

    public const int CAMERA_INPUT_WIDTH = 640;
    public const int CAMERA_INPUT_HEIGHT = 360;
    public const int CAMERA_INPUT_FPS = 20;

    public const int MAX_NAME_LENGTH = 10;
}




public class Message
{
    public string type;
}


public class Ping
{
    public string type = "ping";
    public long timestamp;
}

//sent from client to server

public class GameState
{
    public string type = "state";
    public string id;
    public List<float> rotation;
    public int shooting;
}

//sent from server to client

public class RemoteState
{
    public string type = "remoteState";
    public string id;
    public int health;
    public int score;
    public int kills;
    public List<float> rotation;
    public int shooting;
}


public class Register
{
    public string type = "register";
    public string id;
}


public class Ready
{
    public string type = "ready";
    public string id;
}



public class PlayerList
{
    public string type = "playerList";
    public List<string> playerList;
}


public class EnemyStates
{
    public string type = "enemyStates";
    public Dictionary<string, EnemyState> enemies;
}

public class EnemyState
{
    public string type = "enemyState";
    public string enemyId;
    public string initialPosition;
    public int health;
    public int target;
    public int running;
}


public class EnemiesRequest
{
    public string type = "requestEnemies";
}


public class Leave
{
    public string type = "leave";
    public string id;
    public List<string> playerList;
}


public class EnemyKilled
{
    public string type = "enemyShot";
    public string id;
    public string enemyId;
    public int damage;
}


public class EnemyAttack
{
    public string type = "enemyAttack";
    public string id;
}