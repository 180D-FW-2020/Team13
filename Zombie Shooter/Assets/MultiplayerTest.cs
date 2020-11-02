using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class MultiplayerTest : MonoBehaviour
{
    public string playerName;
    public Text latencyText;

    public GameObject player;
    private Dictionary<string, GameObject> allPlayers = new Dictionary<string, GameObject>();

    private MQTTConnnection mqttConnection;
    private GameState gameState = new GameState();
    private Queue<GameState> pendingMessages = new Queue<GameState>();
    private double latency = 0f;

    void Start()
    {
        gameState.id = playerName;
        mqttConnection = new MQTTConnnection();
        mqttConnection.MessageReceived.AddListener(PlayerStateReceived);
    }

    private void PlayerStateReceived(GameState state)
    {
        pendingMessages.Enqueue(state);
    }


    void FixedUpdate()
    {
        gameState.timestamp = DateTime.Now.Ticks;
        gameState.playerPosition = player.transform.position;
        mqttConnection.Publish(JsonUtility.ToJson(gameState));
    }

    private void Update()
    {
        while (pendingMessages.Count > 0)
        {
            var state = pendingMessages.Dequeue();
            if (state.id != playerName)
            {
                if (allPlayers.ContainsKey(state.id))
                {
                    allPlayers[state.id].transform.position = state.playerPosition;
                }
                else
                {
                    Debug.Log($"New player joined: {state.id}");
                    var newPlayer = Instantiate(player, transform);
                    newPlayer.transform.position = state.playerPosition;
                    newPlayer.name = state.id;
                    newPlayer.GetComponent<PlayerMove>().enabled = false;
                    allPlayers.Add(state.id, newPlayer);
                }
            }
            else // measure latency
            {
                double ms = TimeSpan.FromTicks(DateTime.Now.Ticks - state.timestamp).TotalMilliseconds;
                latencyText.text = string.Format("Latency: {0} ms", ms);
            }
        }
    }
}
