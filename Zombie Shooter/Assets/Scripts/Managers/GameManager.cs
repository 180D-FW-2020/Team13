using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public List<LevelController> levels;
    public VehicleController vehicle;
    public float cameraMovementSpeed;

    public Transform mainCamera;
    public GameObject player;

    public float pingInterval;
    public int maxBufferSize;

    private EnemyManager enemyManager;
    private InputManager inputManager;
    private UIManager uiManager;
    private SphinxExample sphinx;

    private NetworkConnection connection;
    private GameState gameState = new GameState();
    private Queue<Action> pendingActions = new Queue<Action>();
    private Dictionary<string, PlayerController> allPlayers = new Dictionary<string, PlayerController>();
    private string playerName;
    private PlayerController mainPlayer;

    private GameStatus gameStatus = GameStatus.Start;
    private int currentLevel = 0;

    public void Awake()
    {
        enemyManager = GetComponent<EnemyManager>();
        inputManager = GetComponent<InputManager>();
        uiManager = GetComponent<UIManager>();
    }

    void Start()
    {
        // connect all event listeners to the proper networking and UI events
        connection = new NetworkConnection();
        connection.PlayerListReceived.AddListener(PlayerListReceived);
        connection.EnemyLocationsReceived.AddListener(EnemyLocationsReceived);
        connection.LeaveMessageReceived.AddListener(LeaveMessageReceived);
        connection.EnemyKilledMessageReceived.AddListener(EnemyKilledMessageReceived);
        connection.EnemyShotMessageReceived.AddListener(EnemyShotMessageReceived);
        connection.RemoteStateUpdateReceived.AddListener(RemoteStateUpdateReceived);
        connection.StartReceived.AddListener(StartReceived);
        connection.PongReceived.AddListener(PongReceived);
        connection.Opened.AddListener(ConnectionOpened);

        uiManager.startButton.onClick.AddListener(Connect);
        uiManager.readyButton.onClick.AddListener(SendReady);
        uiManager.resumeButton.onClick.AddListener(ResumeGame);
        uiManager.exitButton.onClick.AddListener(ReloadGame);
        uiManager.ShowStart();

        StartCoroutine(InitMicrophone());
        StartCoroutine(MeasureLatency());
    }

    private IEnumerator MeasureLatency()
    {
        while (true)
        {
            yield return new WaitForSeconds(pingInterval);
            Ping ping = new Ping
            {
                timestamp = DateTime.Now.Ticks
            };
            Task.Run(async () => await connection.Send(ping));
        }
    }

    #region Game Events

    public IEnumerator MoveToLevel()
    {
        Debug.Log("Move to level");
        uiManager.StartGame();
        gameStatus = GameStatus.Moving;
        uiManager.SetScreensActive(gameStatus);

        if (currentLevel < levels.Count - 1) {
            SendEnemyPositionRequest();
        }
        Debug.Log($"Moving to Level {currentLevel} of {levels.Count}");
        List<Transform> waypoints = levels[currentLevel].GetWaypoints();
        vehicle.SetWaypoints(waypoints);
        while (!vehicle.IsStopped())
            yield return null;

        if (currentLevel == levels.Count - 1)
            EndGame();
        else
            StartCoroutine(TransitionToLevel());
    }

    public IEnumerator TransitionToLevel()
    {
        Debug.Log("Transition to level");
        gameStatus = GameStatus.Transitioning;
        uiManager.SetScreensActive(gameStatus);

        yield return null;

        for (int i = 0; i < allPlayers.Count; i++)
        {
            Transform pad = levels[currentLevel].GetPlayerPads()[i];
            string key = allPlayers.Keys.ElementAt(i);
            StartCoroutine(allPlayers[key].WalkToPad(pad));
        }
        while (!allPlayers.All(p => !p.Value.IsWalking()))
        {
            yield return null;
        }
        while (mainCamera.position != mainPlayer.playerCamera.position || mainCamera.eulerAngles != mainPlayer.playerCamera.eulerAngles)
        {
            mainCamera.position = Vector3.MoveTowards(mainCamera.position, mainPlayer.playerCamera.position, Time.deltaTime * cameraMovementSpeed);
            mainCamera.eulerAngles = Vector3.MoveTowards(mainCamera.eulerAngles, mainPlayer.playerCamera.eulerAngles, Time.deltaTime * cameraMovementSpeed);
            yield return mainCamera;
        }
        mainCamera.SetParent(mainPlayer.playerCamera, true);
        SendReady();
        mainPlayer.ResetRotation();
    }

    public void StartLevel()
    {
        Debug.Log("Start level");
        mainPlayer.EnableShooting(true);
        gameStatus = GameStatus.Playing;
        uiManager.SetScreensActive(gameStatus);
        enemyManager.StartGame();
    }
    public IEnumerator TransitionFromLevel()
    {
        Debug.Log("Transition from level");
        gameStatus = GameStatus.Transitioning;
        uiManager.SetScreensActive(gameStatus);

        while (mainCamera.position != vehicle.vehicleCamera.position || mainCamera.eulerAngles != vehicle.vehicleCamera.eulerAngles)
        {
            mainCamera.position = Vector3.MoveTowards(mainCamera.position, vehicle.vehicleCamera.position, Time.deltaTime * cameraMovementSpeed);
            mainCamera.eulerAngles = Vector3.MoveTowards(mainCamera.eulerAngles, vehicle.vehicleCamera.eulerAngles, Time.deltaTime * cameraMovementSpeed);
            yield return mainCamera;
        }
        mainCamera.SetParent(vehicle.vehicleCamera, true);

        for (int i = 0; i < allPlayers.Count; i++)
        {
            Transform pad = vehicle.playerPads[i];
            string key = allPlayers.Keys.ElementAt(i);
            StartCoroutine(allPlayers[key].WalkToPad(pad));
        }
        while (!allPlayers.All(p => !p.Value.IsWalking()))
        {
            yield return null;
        }
        StartCoroutine(MoveToLevel());
    }

    public void EndLevel()
    {
        mainPlayer.EnableShooting(false);
        currentLevel++;
        Debug.Log("End level");
        StartCoroutine(TransitionFromLevel());
    }

    public async void Connect()
    {
        gameStatus = GameStatus.Connecting;
        uiManager.ShowConnecting();
        playerName = uiManager.GetPlayerName();
        gameState.id = playerName;
        await connection.Connect();
    }

    public void WaitForPlayers()
    {
        gameStatus = GameStatus.Waiting;
        uiManager.EnterWaitingRoom();
    }

    public void PauseGame()
    {
        gameStatus = GameStatus.Paused;
        uiManager.SetScreensActive(gameStatus);
        mainPlayer.EnableShooting(false);
    }

    public void ResumeGame()
    {
        gameStatus = GameStatus.Playing;
        uiManager.SetScreensActive(gameStatus);
        mainPlayer.EnableShooting(true);
    }

    public void ReloadGame()
    {
        gameStatus = GameStatus.Start;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        Time.timeScale = 1;
    }

    public void EndGame()
    {
        gameStatus = GameStatus.Ended;
        uiManager.SetScreensActive(gameStatus);
        mainPlayer.EnableShooting(false);
        Debug.Log("End Game");
    }

    public async void RegisterShot(GameObject enemy, int damage, bool killed)
    {
        EnemyKilled shotEnemy = new EnemyKilled
        {
            enemyId = enemy.name,
            id = playerName,
            damage = damage
        };
        if (killed)
            enemyManager.KillEnemy(enemy.name);
        await connection.Send(shotEnemy);
    }

    public async void AttackPlayer()
    {
        EnemyAttack attack = new EnemyAttack
        {
            id = playerName
        };
        await connection.Send(attack);
    }

    public async void SendEnemyPositionRequest()
    {
        EnemiesRequest request = new EnemiesRequest();
        await connection.Send(request);
    }

    public void Shoot(int weapon)
    {
        gameState.shooting = weapon;
    }

    #endregion

    #region Speech
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
                if (cmd == "ready")
                {
                    SendReady();
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

    #region Network callbacks
    public async void ConnectionOpened()
    {
        Debug.Log("Connected to server");
        Register register = new Register
        {
            id = playerName
        };
        await connection.Send(register);
        WaitForPlayers();
    }

    public void ConnectionClosed()
    {
        Debug.Log("Disconnected from server");
    }

    public async void SendReady()
    {
        uiManager.readyButton.interactable = false;
        Ready ready = new Ready
        {
            id = playerName
        };
        await connection.Send(ready);
    }

    private void StartReceived()
    {
        pendingActions.Enqueue(() =>
        {
            if (gameStatus == GameStatus.Waiting) //game just started
                StartCoroutine(TransitionFromLevel());
            else
                StartLevel();
        });
    }

    private void RemoteStateUpdateReceived(RemoteState state)
    {
        pendingActions.Enqueue(() =>
        {
            uiManager.UpdateScore(state.id, state.score);
            uiManager.UpdateHealth(state.id, state.health);

            if (state.id != playerName)
            {
                if (state.shooting != (int)GestureType.None)
                {
                    allPlayers[state.id].SwitchWeapon((GestureType)state.shooting);
                    allPlayers[state.id].Shoot();
                }

                Vector3 rotation = new Vector3(state.rotation[0], state.rotation[1], state.rotation[2]);
                allPlayers[state.id].transform.eulerAngles = rotation;
            }
        });
    }

    private void PlayerListReceived(PlayerList list)
    {
        pendingActions.Enqueue(() => {
            uiManager.UpdatePlayerList(list.playerList);

            for (int i = 0; i < list.playerList.Count; i++)
            {
                var name = list.playerList[i];
                if (allPlayers.ContainsKey(name))
                    continue;

                var parent = vehicle.GetPlayerPads()[i];
                Debug.Log($"New player joined: {name}");
                var newPlayer = Instantiate(player, parent);
                newPlayer.name = name;
                var newPlayerController = newPlayer.GetComponent<PlayerController>();
                newPlayerController.SetInputManager(inputManager);
                if (name == playerName)
                {
                    newPlayerController.mainPlayer = true;
                    mainPlayer = newPlayerController;
                }
                allPlayers.Add(name, newPlayerController);
                uiManager.AddPlayer(name);
            }
        });
    }

    private void EnemyLocationsReceived(EnemyStates states)
    {
        pendingActions.Enqueue(() => enemyManager.Initialize(states.enemies, levels[currentLevel].transform, levels[currentLevel].GetPlayerPads()));
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

    private void EnemyShotMessageReceived(EnemyKilled enemyShot)
    {
        pendingActions.Enqueue(() =>
        {
            enemyManager.ShootEnemy(enemyShot.enemyId, enemyShot.damage);
        });
    }
    public void PongReceived(Ping pong)
    {
        long now = DateTime.Now.Ticks;
        double roundtripLatency = TimeSpan.FromTicks(now - pong.timestamp).TotalMilliseconds;
        //oneWayLatency = roundtripLatency/2
        pendingActions.Enqueue(() =>
        {
            uiManager.UpdateLatency(roundtripLatency);
        });
    }
    #endregion

    public async void FixedUpdate()
    {
        if (gameStatus == GameStatus.Playing)
        {
            uiManager.UpdateAmmo(mainPlayer.GetCurrentAmmo());
            gameState.rotation = allPlayers[playerName].transform.eulerAngles.coordinates();
            await connection.Send(gameState);
            gameState.shooting = 0;

            if (enemyManager.GetEnemyCount() == 0)
                EndLevel();
        }
    }

    private void Update()
    {
        connection.Update();
        while (pendingActions.Count > 0)
        {
            pendingActions.Dequeue()();
        }

        if (gameStatus == GameStatus.Calibrating)
        {
            inputManager.UpdateCalibration();
        }

        if (gameStatus == GameStatus.Playing)
        {
            mainPlayer.UpdateInput();
        }
    }

    public async void OnApplicationQuit()
    {
        await connection.Stop(playerName);
    }
}
