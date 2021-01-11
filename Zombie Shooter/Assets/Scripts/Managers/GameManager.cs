using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(EnemyManager))]
[RequireComponent(typeof(UIManager))]
[RequireComponent(typeof(InputManager))]
// GameManager performs all game logic, from determining the game state to correctly parsing
// networking messages. This script manages all data being sent/received over the network, as well
// as work with the UIManager to coordinate UI events
public class GameManager : MonoBehaviour
{
    public GameObject player;
    public GameObject mainPlayer;
    public Transform playersParent;
    public GameObject playerWeaponObject;

    //[Header("Game Scoring Constants")]
    //public int healthLossIncrement;
    //public int hitScore;
    //public int killScore;

    //private int health;
    //public int Health
    //{
    //    get { return health; }
    //    set
    //    {
    //        health = value;
    //        uiManager?.UpdateHealth(health);
    //    }
    //}


    private EnemyManager enemyManager;
    private InputManager inputManager;
    private UIManager uiManager;
    private SphinxExample sphinx;

    private NetworkConnection connection;
    private GameState gameState = new GameState();
    private Queue<Action> pendingActions = new Queue<Action>();
    private Dictionary<string, GameObject> allPlayers = new Dictionary<string, GameObject>();
    private string playerName;

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
        playerWeaponObject.SetActive(false);

        // connect all event listeners to the proper networking and UI events
        connection = new NetworkConnection();
        connection.PlayerStateReceived.AddListener(PlayerStateReceived);
        connection.WeaponShootReceived.AddListener(WeaponShootReceived);
        connection.InitializeMessageReceived.AddListener(InitializeMessageReceived);
        connection.LeaveMessageReceived.AddListener(LeaveMessageReceived);
        connection.EnemyKilledMessageReceived.AddListener(EnemyKilledMessageReceived);
        connection.GameValuesUpdateReceived.AddListener(GameValuesUpdateReceived);
        connection.StartReceived.AddListener(StartReceived);

        uiManager.startButton.onClick.AddListener(Calibrate);
        uiManager.playButton.onClick.AddListener(SendStart);
        uiManager.resumeButton.onClick.AddListener(ResumeGame);
        uiManager.exitButton.onClick.AddListener(ReloadGame);
        uiManager.ShowStart();

        StartCoroutine(InitMicrophone());
    }

    private IEnumerator InitMicrophone()
    {
        sphinx = FindObjectOfType<SphinxExample>();
        sphinx.OnSpeechRecognized += UpdateSpeechUI;

        uiManager.UpdateMicIndicator(new Color(1, 0, 0, 1));
        while (sphinx.mic == null)
        {
           yield return null;
        }
        Debug.Log($"<color=green><b>Connected to: {sphinx.mic.Name}</b></color>");
        uiManager.micConnected = true;
        uiManager.UpdateMicIndicator(new Color(0, 1, 0, 1));
    }

    #region Game Events

    public void StartGame()
    {
        playerWeaponObject.SetActive(true);
        mainPlayer.GetComponent<PlayerController>().StartGame();
        gameStatus = GameStatus.Playing;
        GameStarted = true;
        uiManager.StartGame();
        Debug.Log("Game Started");
    }

    public void Calibrate()
    {
        if (inputManager.aimInputType == AimInputType.Finger) {
            uiManager.calibrationText.text = "Cover all boxes with hand/object." + Environment.NewLine + "Press 'C' to calibrate.";
            uiManager.StartCalibration();
            gameStatus = GameStatus.Calibrating;
        } else
            Connect();
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
        enemyManager.KillEnemy(enemy.name);
        await connection.SendEnemyShoot(shotEnemy);
    }

    public async void AttackPlayer()
    {
        EnemyAttack attack = new EnemyAttack
        {
            id = playerName
        };
        await connection.SendEnemyAttack(attack);
    }

    public async void Shoot(int weapon)
    {
        WeaponShoot shoot = new WeaponShoot
        {
            id = playerName,
            weapon = weapon
        };
        await connection.SendShoot(shoot);
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

    private void GameValuesUpdateReceived(GameValues values)
    {
        Debug.Log($"{values.id}\tScore: {values.score}, Health: {values.health}, Kills: {values.kills}");
        pendingActions.Enqueue(() =>
        {
            uiManager.UpdateScore(values.id, values.score);
            uiManager.UpdateHealth(values.id, values.health);
        });
    }

    private void InitializeMessageReceived(Initialize init)
    {
        pendingActions.Enqueue(() => {
            uiManager.UpdatePlayerList(init.playerList);

            for (int i = allPlayers.Count; i < init.playerList.Count; i++)
            {
                var name = init.playerList[i];
                var parent = playersParent.Find(allPlayers.Count.ToString());
                if (name == playerName)
                {
                    mainPlayer.transform.SetParent(parent, false);
                    allPlayers.Add(playerName, mainPlayer);
                }
                else
                {
                    Debug.Log($"New player joined: {name}");
                    var newPlayer = Instantiate(player, parent);
                    newPlayer.name = name;
                    allPlayers.Add(name, newPlayer);
                }
                uiManager.AddPlayer(name);
            }
        });
        pendingActions.Enqueue(() => enemyManager.Initialize(init.enemyPositions));
    }

    private void LeaveMessageReceived(Leave leave)
    {
        Debug.Log($"{leave.id} leaving");
        pendingActions.Enqueue(() => {
            uiManager.UpdatePlayerList(leave.playerList);

            Destroy(allPlayers[leave.id]);
            allPlayers.Remove(leave.id);
        });
    }

    private void EnemyKilledMessageReceived(EnemyKilled enemyKilled)
    {
        Debug.Log($"Enemy {enemyKilled.enemyId} killed by {enemyKilled.id}");
        pendingActions.Enqueue(() =>
        {
            enemyManager.KillEnemy(enemyKilled.enemyId);
        });
    }

    private void WeaponShootReceived(WeaponShoot shoot)
    {
        if (shoot.id != playerName)
        {
            if (shoot.weapon != (int)GestureType.None)
            {
                pendingActions.Enqueue(() =>
                {
                    WeaponController weaponController = allPlayers[shoot.id].GetComponentInChildren<WeaponController>();
                    weaponController.SwitchWeapon((GestureType)shoot.weapon);
                    weaponController.Shoot();
                });
            }
        }
    }

    public void UpdateRemoteState(GameState state)
    {
        if (state.id != playerName)
        {
            Vector3 rotation = new Vector3(state.rotation[0], state.rotation[1], state.rotation[2]);
            allPlayers[state.id].transform.eulerAngles = rotation;
        }
        else // measure latency
        {
            long now = DateTime.Now.Ticks;
            double roundtripLatency = TimeSpan.FromTicks(now - state.timestamp).TotalMilliseconds;
            //oneWayLatency = roundtripLatency/2 
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
            gameState.rotation = allPlayers[playerName].transform.eulerAngles.coordinates();
            await connection.SendState(gameState);
        }
    }

    private void Update()
    {
        while (pendingActions.Count > 0)
            pendingActions.Dequeue()();

        if (gameStatus == GameStatus.Calibrating)
        {
            inputManager.UpdateCalibration();
        }

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
        Debug.Log(gameStatus);
        cmd = cmd.ToLower();

        switch (gameStatus)
        {
            case GameStatus.Start:
                // idk
                break;
            case GameStatus.Waiting:
                if (cmd == "play")
                {
                    SendStart();
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
