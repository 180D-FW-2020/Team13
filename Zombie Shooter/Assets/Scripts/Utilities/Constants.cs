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
    public const int CAMERA_INPUT_FPS = 20;

    public const string MQTT_BROKER_URL = "broker.emqx.io";
    public const string MQTT_TOPIC = "ece180d/team13/multiplayer";
}

public class GameState
{
    public long timestamp;
    public string id;
    public Vector3 playerPosition;
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