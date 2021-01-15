'use strict';

const wss = require('./init.js');
const WebSocket = require('ws');
const Client = require('./client.js');

var updateInterval = 100; 
var numEnemies = 5;

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
        enemies[i] = "" + ((Math.floor(Math.random() * 11)-5)) * 4 + "," + (Math.floor(Math.random() * 6) * 4);
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
        case "start":
            console.log('Game started');
            broadcast(message);
            break;
        case "register":
            console.log("Adding " + name);
    
            if (Object.keys(connectedClients).length == 0){
                initEnemies();
            }
        
            let client = new Client(name);
            connectedClients[name] = client;
        
            let init = {
                type: "initialize",
                playerList: Object.keys(connectedClients),
                enemyPositions: enemies
            }
            broadcast(JSON.stringify(init));
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
            broadcast(JSON.stringify(connectedClients[name].state));
            break;
        case "enemyShot":
            console.log("Enemy " + data.enemyId + " shot by " + name);
            if (data.enemyId in enemies) {
                console.log("Enemy " + data.enemyId + " killed by " + name);
                broadcast(JSON.stringify({type: "enemyKilled", enemyId: data.enemyId, id: name}));
                connectedClients[data.id].registerKill();
                broadcast(JSON.stringify(connectedClients[name].state));
                delete connectedClients[name];
            }
            break;
        case "state":
            connectedClients[data.id].updatePlayerState(data);
            broadcast(JSON.stringify(connectedClients[name].state));
            break;
    }
}