using System;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using System.Dynamic;
using System.Reflection;
using System.Linq;

using NativeWebSocket;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

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
    public UnityEvent<EnemyShot> EnemyShotMessageReceived = new UnityEvent<EnemyShot>();
    public UnityEvent<RemoteState> RemoteStateUpdateReceived = new UnityEvent<RemoteState>();
    public UnityEvent<ReplayEvents> KillCamEventsReceived = new UnityEvent<ReplayEvents>();
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

        dynamic message = Unpack<dynamic>(data);
        switch((string)message.type)
        {
            case "ping":
                PongReceived.Invoke(Unpack<Ping>(data));
                break;
            case "start":
                StartReceived.Invoke();
                break;
            case "remoteState":
                RemoteStateUpdateReceived.Invoke(Unpack<RemoteState>(data));
                break;
            case "enemyKilled":
                EnemyKilledMessageReceived.Invoke(Unpack<EnemyKilled>(data));
                break;
            case "enemyShot":
                EnemyShotMessageReceived.Invoke(Unpack<EnemyShot>(data));
                break;
            case "playerList":
                PlayerListReceived.Invoke(Unpack<PlayerList>(data));
                break;
            case "enemyStates":
                EnemyLocationsReceived.Invoke(Unpack<EnemyStates>(data));
                break;
            case "replay":
                KillCamEventsReceived.Invoke(Unpack<ReplayEvents>(data));
                break;
            case "leave":
                LeaveMessageReceived.Invoke(Unpack<Leave>(data));
                break;
        }
    }

    public T Unpack<T>(string data)
    {
        return JsonConvert.DeserializeObject<T>(data, settings);
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