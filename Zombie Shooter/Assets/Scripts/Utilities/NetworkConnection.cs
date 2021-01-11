using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using SocketIOClient;
using Newtonsoft.Json;

public class NetworkConnection
{
    public UnityEvent StartReceived = new UnityEvent();
    public UnityEvent<WeaponShoot> WeaponShootReceived = new UnityEvent<WeaponShoot>();
    public UnityEvent<GameState> PlayerStateReceived = new UnityEvent<GameState>();
    public UnityEvent<Initialize> InitializeMessageReceived = new UnityEvent<Initialize>();
    public UnityEvent<Leave> LeaveMessageReceived = new UnityEvent<Leave>();
    public UnityEvent<EnemyKilled> EnemyKilledMessageReceived = new UnityEvent<EnemyKilled>();
    public UnityEvent<GameValues> GameValuesUpdateReceived = new UnityEvent<GameValues>();
    private SocketIO client;


    // Connect to server and initialize async events
    public NetworkConnection()
    {
        client = new SocketIO("https://zombie-shooter-server.herokuapp.com/");

        client.OnConnected += (s, e) => Debug.Log("Connected to server");
        client.OnDisconnected += (s, e) => Debug.Log("Disconnected from server");

        var settings = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.All
        };

        client.On("start", response =>
        {
            StartReceived.Invoke();
        });
        client.On("remote_state", response =>
        {
            PlayerStateReceived.Invoke(JsonConvert.DeserializeObject<GameState>(response.GetValue<string>(), settings));
        });
        client.On("remote_shoot", response =>
        {
            WeaponShootReceived.Invoke(JsonConvert.DeserializeObject<WeaponShoot>(response.GetValue<string>(), settings));
        });
        client.On("enemy_killed", response =>
        {
            EnemyKilledMessageReceived.Invoke(JsonConvert.DeserializeObject<EnemyKilled>(response.GetValue<string>(), settings));
        });
        client.On("update_values", response =>
        {
            GameValuesUpdateReceived.Invoke(JsonConvert.DeserializeObject<GameValues>(response.GetValue<string>(), settings));
        });
        client.On("initialize", response =>
        {
            InitializeMessageReceived.Invoke(JsonConvert.DeserializeObject<Initialize>(response.GetValue<string>(), settings));
        });
        client.On("leave", response =>
        {
            LeaveMessageReceived.Invoke(JsonConvert.DeserializeObject<Leave>(response.GetValue<string>(), settings));
        });
    }

    public async Task Connect(string playerName)
    {
        await client.ConnectAsync();
        Register register = new Register
        {
            id = playerName
        };
        await client.EmitAsync("register", JsonConvert.SerializeObject(register));
    }

    public async Task SendStart()
    {
        if (client.Connected)
            await client.EmitAsync("start");
    }

    public async Task SendState(GameState state)
    {
        if (client.Connected)
            await client.EmitAsync("state", JsonConvert.SerializeObject(state));
    }

    public async Task SendShoot(WeaponShoot shoot)
    {
        if (client.Connected)
            await client.EmitAsync("shoot", JsonConvert.SerializeObject(shoot));
    }

    public async Task SendEnemyShoot(EnemyKilled enemyKilled)
    {
        if (client.Connected)
            await client.EmitAsync("shoot_enemy", JsonConvert.SerializeObject(enemyKilled));
    }

    public async Task SendLeave(Register req)
    {
        if (client.Connected)
            await client.EmitAsync("leave", JsonConvert.SerializeObject(req));
    }

    public async Task SendEnemyAttack(EnemyAttack attack)
    {
        if (client.Connected)
            await client.EmitAsync("enemy_attack", JsonConvert.SerializeObject(attack));
    }

    public async Task Stop(string playerName)
    {
        Register leaveReq = new Register
        {
            id = playerName
        };
        await SendLeave(leaveReq);
        await client?.DisconnectAsync();
    }
}