using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    public PlayerController playerController;
    public GameObject playerWeaponObject;

    [Header("Game Scoring Constants")]
    public int healthLossIncrement;
    public int hitScore;
    public int killScore;

    private int health;
    public int Health
    {
        get { return health; }
        set
        {
            health = value;
            uiManager?.UpdateHealth(health);
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

    private int currentAmmo;
    private int currentHeat;
    private int maxHeat;

    public void Awake()
    {
        enemyManager = GetComponent<EnemyManager>();
        inputManager = GetComponent<InputManager>();
        uiManager = GetComponent<UIManager>();
    }

    void Start()
    {
        playerWeaponObject.SetActive(false);

        connection = new NetworkConnection();
        connection.PlayerStateReceived.AddListener(PlayerStateReceived);
        connection.InitializeMessageReceived.AddListener(InitializeMessageReceived);
        connection.EnemyKilledMessageReceived.AddListener(EnemyKilledMessageReceived);
        connection.StartReceived.AddListener(StartReceived);

        uiManager.startButton.onClick.AddListener(Connect);
        uiManager.playButton.onClick.AddListener(SendStart);
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
        playerWeaponObject.SetActive(true);
        playerController.StartGame();
        gameStatus = GameStatus.Playing;
        GameStarted = true;
        uiManager.UpdateAllScores(0);
        uiManager.StartGame();
        Health = 100;
        Debug.Log("Game Started");
    }

    public async void Connect()
    {
        gameStatus = GameStatus.Connecting;
        uiManager.ShowConnecting();
        playerName = uiManager.GetPlayerName();
        gameState.id = playerName;
        await connection.Connect(playerName);
        WaitForPlayers();
    }

    public void WaitForPlayers()
    {
        gameStatus = GameStatus.Waiting;
        uiManager.EnterWaitingRoom();

        var player = Instantiate(crosshair, playersParent);
        player.GetComponent<Text>().text = playerName;
        player.name = playerName;
        allPlayers.Add(playerName, player);
        uiManager.AddPlayer(playerName);
        inputManager.playerReticle = player.transform;
    }

    public void PauseGame()
    {
        gameStatus = GameStatus.Paused;
        uiManager.SetScreensActive(GameStatus.Paused);
        playerWeaponObject.SetActive(false);
    }

    public void ResumeGame()
    {
        gameStatus = GameStatus.Playing;
        uiManager.SetScreensActive(GameStatus.Playing);
        playerWeaponObject.SetActive(true);
    }

    public void ReloadGame()
    {
        gameStatus = GameStatus.Start;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        Time.timeScale = 1;
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
        if (Health <= 0)
        {
            Debug.Log("YOU DEAD AF");
        }
    }

    #endregion

    #region Network callbacks
    public async void SendStart()
    {
        await connection.SendStart();
    }

    private void StartReceived()
    {
        pendingActions.Enqueue(() => StartGame());
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
            uiManager.UpdateScore(enemyKilled.id, killScore);
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
                uiManager.AddPlayer(state.id);
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
            pendingActions.Dequeue()();

        if (GameStarted)
        {
            inputManager.UpdateInput();
            uiManager.UpdateAmmo(inputManager.weaponController.GetCurrentAmmo());
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
