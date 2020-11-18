﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(EnemyManager))]
[RequireComponent(typeof(UIManager))]
[RequireComponent(typeof(InputManager))]
public class GameManager : MonoBehaviour
{
    public GameObject crosshair;
    public Transform playersParent;

    [Header("Game Scoring Constants")]
    public int healthLossIncrement;
    public int hitScore;
    public int killScore;

    private int health;
    private int score;
    public int Health
    {
        get { return health; }
        set
        {
            health = value;
            uiManager?.UpdateHealth(health);
        }
    }

    public int Score
    {
        get { return score; }
        set
        {
            score = value;
            uiManager?.UpdateScore(score);
        }
    }


    private EnemyManager enemyManager;
    private InputManager inputManager;
    private UIManager uiManager;
    private SphinxExample sphinx;

    private NetworkConnection connection;
    private GameState gameState = new GameState();
    private Queue<Action> pendingActions = new Queue<Action>();
    private Dictionary<string, GameObject> allPlayers = new Dictionary<string, GameObject>();
    private string playerName, room;
    private long serverTimeOffset;

    private GameStatus gameStatus = GameStatus.Start;
    internal bool GameStarted = false;

    public void Awake()
    {
        enemyManager = GetComponent<EnemyManager>();
        inputManager = GetComponent<InputManager>();
        uiManager = GetComponent<UIManager>();
    }

    void Start()
    {
        connection = new NetworkConnection();
        connection.PlayerStateReceived.AddListener(PlayerStateReceived);
        connection.InitializeMessageReceived.AddListener(InitializeMessageReceived);
        connection.EnemyKilledMessageReceived.AddListener(EnemyKilledMessageReceived);

        uiManager.startButton.onClick.AddListener(WaitForPlayers);
        uiManager.playButton.onClick.AddListener(StartGame);
        uiManager.resumeButton.onClick.AddListener(ResumeGame);
        uiManager.exitButton.onClick.AddListener(ReloadGame);
        uiManager.ShowStart();
        Health = 100;

        //sphinx = FindObjectOfType<SphinxExample>();
        //sphinx.OnSpeechRecognized += UpdateSpeechUI;

        //uiManager.UpdateMicIndicator(new Color(0, 1, 0, 1));
        //while (sphinx.mic == null)
        //{
        //    yield return null;
        //}
        //Debug.Log($"<color=green><b>Connected to: {sphinx.mic.Name}</b></color>");
        //uiManager.UpdateMicIndicator(new Color(1, 0, 0, 1));
    }

    #region Game Events
    public void StartGame()
    {
        gameStatus = GameStatus.Playing;
        GameStarted = true;
        uiManager.UpdateScore(0);
        uiManager.StartGame();
        Health = 100;
        Debug.Log("Game Started");
    }

    public async void WaitForPlayers()
    {
        gameStatus = GameStatus.Waiting;
        uiManager.EnterWaitingRoom();
        playerName = uiManager.GetPlayerName();
        gameState.id = playerName;
        await connection.Connect(playerName);

        var player = Instantiate(crosshair, playersParent);
        player.GetComponent<Text>().text = playerName;
        player.name = playerName;
        allPlayers.Add(playerName, player);
        inputManager.playerReticle = player.transform;
    }

    public void PauseGame()
    {
        gameStatus = GameStatus.Paused;
        uiManager.SetScreensActive(GameStatus.Paused);
        Time.timeScale = 0;
    }

    public void ResumeGame()
    {
        gameStatus = GameStatus.Playing;
        uiManager.SetScreensActive(GameStatus.Playing);
        Time.timeScale = 1;
    }

    public void ReloadGame()
    {
        gameStatus = GameStatus.Start;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        Time.timeScale = 1;
    }
    #endregion

    #region Network callbacks
    private void PlayerStateReceived(GameState state)
    {
        pendingActions.Enqueue(() => UpdateRemoteState(state));
    }

    private void InitializeMessageReceived(Initialize init)
    {
        pendingActions.Enqueue(() => uiManager.UpdatePlayerList(init.playerList));
        pendingActions.Enqueue(() => enemyManager.Initialize(init.enemyPositions));
    }
    private void EnemyKilledMessageReceived(EnemyKilled enemyKilled)
    {
        pendingActions.Enqueue(() =>
        {
            if (enemyKilled.id == playerName)
            {
                Score += killScore;
            }
            enemyManager.KillEnemy(enemyKilled.enemyId);
        });
    }
    public void UpdateRemoteState(GameState state)
    {
        if (state.id != playerName)
        {
            Vector3 position = new Vector3(state.playerPosition[0] * Screen.width, state.playerPosition[1] * Screen.height, 0);
            if (allPlayers.ContainsKey(state.id))
            {
                allPlayers[state.id].transform.position = position;
            }
            else
            {
                Debug.Log($"New player joined: {state.id}");
                var newPlayer = Instantiate(crosshair, playersParent);
                newPlayer.GetComponent<Text>().text = state.id;
                newPlayer.transform.position = position;
                newPlayer.name = state.id;
                allPlayers.Add(state.id, newPlayer);
            }
        }
        else // measure latency
        {
            long now = DateTime.Now.Ticks;
            double roundtripLatency = TimeSpan.FromTicks(now - state.timestamp).TotalMilliseconds;
            //serverTimeOffset = (now - state.timestamp)/2 
            uiManager.UpdateLatency(roundtripLatency);
        }
    }
    #endregion

    public async void FixedUpdate()
    {
        if (GameStarted)
        {
            inputManager.UpdateInput();
            gameState.timestamp = DateTime.Now.Ticks;
            gameState.playerPosition = allPlayers[playerName].transform.position.normalizedCoordinates();
            await connection.SendState(gameState);
        }
    }

    private void Update()
    {
        while (pendingActions.Count > 0)
        {
            var action = pendingActions.Dequeue();
            action();
        }
    }

    public async void KillEnemy(GameObject enemy)
    {
        EnemyKilled shotEnemy = new EnemyKilled
        {
            enemyId = enemy.name,
            id = playerName
        };
        await connection.SendEnemyShoot(shotEnemy);
    }

    public void AttackPlayer()
    {
        Health -= healthLossIncrement;
        if (Health <= 0) {
            Debug.Log("YOU DEAD AF");
        }
    }

    #region Speech
    private void UpdateSpeechUI(string str)
    {
        StartCoroutine(ProcessVoiceCommand(str));
    }

    private IEnumerator ProcessVoiceCommand(string cmd)
    {
        if (cmd.Contains(" "))
            cmd = cmd.Substring(0, cmd.IndexOf(" "));

        Debug.Log($"Voice Command: {cmd.ToUpper()}");
        cmd = cmd.ToLower();

        switch (gameStatus)
        {
            case GameStatus.Start:
                if (cmd == "play")
                {
                    StartGame();
                }
                break;
            case GameStatus.Playing:
                if (cmd == "reload")
                {
                    // reload code
                }
                else if (cmd == "pause")
                {
                    PauseGame();
                    uiManager.UpdateMicIndicator(new Color(0, 1, 0, 1));
                }
                break;
            case GameStatus.Paused:
                if (cmd == "resume")
                {
                    ResumeGame();
                }
                else if (cmd == "exit")
                {
                    ReloadGame();
                }
                break;
            default:
                // wtf
                break;
        }

        yield return new WaitForSecondsRealtime(1);
    }
    #endregion

    public async void OnApplicationQuit()
    {
        await connection.Stop(playerName);
    }
}
