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

    public const float RUN_SPEED = 3.7f;
    public const float WALK_SPEED = 0.266f;
}




public class Message
{
    public string type;
}

////[MessagePackObject(keyAsPropertyName: true)]
public class Ping
{
    public string type = "ping";
    public long timestamp;
}

//sent from client to server
//[MessagePackObject(keyAsPropertyName: true)]
public class GameState
{
    public string type = "state";
    public string id;
    public List<float> rotation;
    public int shooting;
}

//sent from server to client
//[MessagePackObject(keyAsPropertyName: true)]
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

//[MessagePackObject(keyAsPropertyName: true)]
public class Register
{
    public string type = "register";
    public string id;
}

//[MessagePackObject(keyAsPropertyName: true)]
public class Ready
{
    public string type = "ready";
    public string id;
}

//[MessagePackObject(keyAsPropertyName: true)]
public class ReplayEvents
{
    public string type = "replay";
    public Dictionary<string, EnemyState> enemies;
    public Dictionary<string, string> killTimes;
    public Dictionary<string, Dictionary<string, ReplayEvent>> events;
}

//[MessagePackObject(keyAsPropertyName: true)]
public class ReplayEvent
{
    public string type = "remoteState";
    public RemoteState remoteState;
    public EnemyKilled enemyKilled;
}

//[MessagePackObject(keyAsPropertyName: true)]
public class PlayerList
{
    public string type = "playerList";
    public List<string> playerList;
}

//[MessagePackObject(keyAsPropertyName: true)]
public class EnemyStates
{
    public string type = "enemyStates";
    public Dictionary<string, EnemyState> enemies;
}

//[MessagePackObject(keyAsPropertyName: true)]
public class EnemyState
{
    public string type = "enemyState";
    public int enemyId;
    public List<float> position;
    public int health;
    public int target;
    public int running;
    public long attacking;
}

//[MessagePackObject(keyAsPropertyName: true)]
public class EnemiesRequest
{
    public string type = "requestEnemies";
}

//[MessagePackObject(keyAsPropertyName: true)]
public class Leave
{
    public string type = "leave";
    public string id;
    public List<string> playerList;
}

//[MessagePackObject(keyAsPropertyName: true)]
public class EnemyKilled
{
    public string type = "enemyKilled";
    public string id;
    public string enemyId;
}

//[MessagePackObject(keyAsPropertyName: true)]
public class EnemyShot
{
    public string type = "enemyShot";
    public string id;
    public string enemyId;
    public int damage;
    public List<float> enemyPosition;
}

//[MessagePackObject(keyAsPropertyName: true)]
public class EnemyAttack
{
    public string type = "enemyAttack";
    public string enemyId;
    public string id;
}