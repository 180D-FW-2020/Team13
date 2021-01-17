'use strict';

const wss = require('./init.js');
const WebSocket = require('ws');
const Client = require('./client.js');
const Enemy = require('./enemy.js');

var numEnemies = 5;
var readyCount = 0;

var connectedClients = {};
var enemies = {};

wss.on('connection', socket => {
    console.log("Client connected");

    socket.on('message', data => {
        processMessage(socket, data);
    })
});

function initEnemies() {
    for (let i = 0; i < numEnemies; i++) {
        const pos = ((Math.floor(Math.random() * 11)-5)) * 4 + "," + (Math.floor(Math.random() * 6) * 4);
        enemies[i] = new Enemy(i, pos, Object.keys(connectedClients).length);
    }
    console.log('Enemies initialized to: ' + JSON.stringify(enemies));
}

function broadcast(data) {
    wss.clients.forEach(function each(client) {
        if (client.readyState === WebSocket.OPEN) {
            client.send(data);
        }
    });
}

function processMessage(socket, message) {
    const data = JSON.parse(message);
    const name = data.id;

    switch (data.type) {
        case "ping":
            socket.send(message);
            break;
        case "ready":
            readyCount++;
            console.log(name + " is ready");
            if (readyCount == Object.keys(connectedClients).length) {
                console.log('Game started');
                readyCount = 0;
                broadcast(JSON.stringify({type: "start"}));
            }
            break;
        case "register":
            console.log("Adding " + name);
    
            if (Object.keys(connectedClients).length == 0){
                initEnemies();
            }
        
            let client = new Client(name);
            connectedClients[name] = client;
        
            let playerList = {
                type: "playerList",
                playerList: Object.keys(connectedClients)
            }
            broadcast(JSON.stringify(playerList));
            break;
        case "requestEnemies":
            if (Object.keys(enemies).length == 0) {
                initEnemies();
            }
            let states = {
                type: "enemyStates",
                enemies: enemies
            }
            socket.send(JSON.stringify(states));
            break;
        case "leave":
            console.log("Removing " + name);
            delete connectedClients[name];
        
            let leave = {
                type: "leave",
                id: name,
                playerList: Object.keys(connectedClients)
            }
            broadcast(JSON.stringify(leave));
            break;
        case "enemyAttack":
            console.log("Enemy attacking " + name);
            connectedClients[name].decrementHealth();
            broadcast(JSON.stringify(connectedClients[name]));
            break;
        case "enemyShot":
            console.log("Enemy " + data.enemyId + " shot by " + name);
            enemies[data.enemyId].decrementHealth(data.damage);
            connectedClients[name].registerShot();
            if (enemies[data.enemyId].health <= 0) {
                console.log("Enemy " + data.enemyId + " killed by " + name);
                broadcast(JSON.stringify({type: "enemyKilled", enemyId: data.enemyId, id: name}));
                delete enemies[data.enemyId];
            }
            broadcast(JSON.stringify(connectedClients[name]));
            break;
        case "state":
            connectedClients[name].updatePlayerState(data);
            broadcast(JSON.stringify(connectedClients[name]));
            break;
    }
}