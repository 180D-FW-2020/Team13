using System;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Newtonsoft.Json;

using NativeWebSocket;

public class NetworkConnection
{
    public UnityEvent Opened = new UnityEvent();
    public UnityEvent Closed = new UnityEvent();
    public UnityEvent StartReceived = new UnityEvent();
    public UnityEvent<Ping> PongReceived = new UnityEvent<Ping>();
    public UnityEvent<PlayerList> PlayerListReceived = new UnityEvent<PlayerList>();
    public UnityEvent<EnemyStates> EnemyLocationsReceived = new UnityEvent<EnemyStates>();
    public UnityEvent<Leave> LeaveMessageReceived = new UnityEvent<Leave>();
    public UnityEvent<EnemyKilled> EnemyKilledMessageReceived = new UnityEvent<EnemyKilled>();
    public UnityEvent<RemoteState> RemoteStateUpdateReceived = new UnityEvent<RemoteState>();
    private WebSocket client;

    private Queue<string> messageQueue = new Queue<string>();

    private JsonSerializerSettings settings = new JsonSerializerSettings()
    {
        TypeNameHandling = TypeNameHandling.All
    };


    // Connect to server and initialize async events
    public NetworkConnection()
    {
        //client = new WebSocket("ws://localhost:3000");
        client = new WebSocket("wss://zombie-shooter-server.herokuapp.com/");

        client.OnOpen += () =>
        {
            messageQueue.Enqueue("OPEN");
        };
        client.OnClose += (e) =>
        {
            messageQueue.Enqueue("CLOSE");
        };
        client.OnError += (e) =>
        {
            Debug.LogError("NetworkConnection Error: " + e);
        };


        client.OnMessage += (e) =>
        {
            string message = Encoding.UTF8.GetString(e);
            messageQueue.Enqueue(message);
        };

    }

    public void Update()
    {
        while (messageQueue.Count > 0)
        {
            ProcessMessage(messageQueue.Dequeue());
        }
        client.DispatchMessageQueue();
    }

    public void ProcessMessage(string data)
    {
        if (data == "OPEN")
        {
            Opened.Invoke();
            return;
        }
        else if (data == "CLOSE")
        {
            Closed.Invoke();
            return;
        }

        Message message = JsonConvert.DeserializeObject<Message>(data, settings);
        switch(message.type)
        {
            case "ping":
                PongReceived.Invoke(JsonConvert.DeserializeObject<Ping>(data, settings));
                break;
            case "start":
                StartReceived.Invoke();
                break;
            case "remoteState":
                RemoteStateUpdateReceived.Invoke(JsonConvert.DeserializeObject<RemoteState>(data, settings));
                break;
            case "enemyKilled":
                EnemyKilledMessageReceived.Invoke(JsonConvert.DeserializeObject<EnemyKilled>(data, settings));
                break;
            case "playerList":
                PlayerListReceived.Invoke(JsonConvert.DeserializeObject<PlayerList>(data, settings));
                break;
            case "enemyStates":
                EnemyLocationsReceived.Invoke(JsonConvert.DeserializeObject<EnemyStates>(data, settings));
                break;
            case "leave":
                LeaveMessageReceived.Invoke(JsonConvert.DeserializeObject<Leave>(data, settings));
                break;
        }
    }

    public async Task Send<T>(T message)
    {
        if (client.State == WebSocketState.Open)
        {
            await client.SendText(JsonConvert.SerializeObject(message));
        }
    }

    public async Task Connect()
    {
        await client.Connect();
    }

    public async Task Stop(string playerName)
    {
        Register leaveReq = new Register
        {
            type = "leave",
            id = playerName
        };
        await Send(leaveReq);
        await client.Close();
    }
}