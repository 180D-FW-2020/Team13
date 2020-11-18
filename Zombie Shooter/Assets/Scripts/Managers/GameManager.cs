using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;
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

    private NetworkConnection connection;
    private GameState gameState = new GameState();
    private Queue<Action> pendingActions = new Queue<Action>();
    private Dictionary<string, GameObject> allPlayers = new Dictionary<string, GameObject>();
    private string playerName, room;
    private long serverTimeOffset;

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
        uiManager.ShowStart();
    }

    public async void WaitForPlayers()
    {
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

    public void StartGame()
    {
        GameStarted = true;
        uiManager.UpdateScore(0);
        uiManager.StartGame();
        Health = 100;
        Debug.Log("Game Started");
    }

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

    public async void OnApplicationQuit()
    {
        await connection.Stop(playerName);
    }
}
