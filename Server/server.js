'use strict';

const { unpack, pack } = require('msgpackr');

const wss = require('./init.js');
const Client = require('./client.js');
const Enemy = require('./enemy.js');

var numEnemies = 5;
var readyCount = 0;

var connectedClients = {};
var enemies = {};
var deadEnemies = {};

var finalShotPlayerId = "";

wss.on('connection', socket => {
    console.log("Client connected");

    socket.on('message', data => {
        processMessage(socket, data);
    })
});

function initEnemies() {
    let key = "";
    for (let i = 0; i < numEnemies; i++) {
        key = i.toString();
        const pos = [((Math.floor(Math.random() * 11)-5)) * 4, (Math.floor(Math.random() * 6) * 4)];
        enemies[key] = new Enemy(i, pos, Object.keys(connectedClients).length);
    }
    console.log('Enemies initialized to: ' + JSON.stringify(enemies));
}

function sendReplayEvents() {
    // const event;
    // let sendEvents = [];
    // if (event.id == finalShotPlayerId)
    //     sendEvents.push(event);
}

function processMessage(socket, message) {
    const data = unpack(message);
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
                wss.broadcast(pack({type: "start"}), null);
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
            wss.broadcast(pack(playerList), null);
            break;
        case "requestEnemies":
            if (Object.keys(enemies).length == 0) {
                initEnemies();
            }
            let states = {
                type: "enemyStates",
                enemies: enemies
            }
            socket.send(pack(states));
            break;
        case "leave":
            console.log("Removing " + name);
            delete connectedClients[name];
        
            let leave = {
                type: "leave",
                id: name,
                playerList: Object.keys(connectedClients)
            }
            wss.broadcast(pack(leave), null);
            break;
        case "enemyAttack":
            console.log("Enemy attacking " + name);
            connectedClients[name].decrementHealth();
            wss.broadcast(pack(connectedClients[name]), socket);
            break;
        case "enemyShot":
            console.log("Enemy " + data.enemyId + " shot by " + name);
            if (data.enemyId in enemies) {
                connectedClients[name].registerShot();
                enemies[data.enemyId].decrementHealth(data.damage);

                if (enemies[data.enemyId].health <= 0) {
                    finalShotPlayerId = name;
                    connectedClients[name].registerKill();
                    console.log("Enemy " + data.enemyId + " killed by " + name);
                    deadEnemies[data.enemyId] = enemies[data.enemyId];
                    delete enemies[data.enemyId];

                    deadEnemies[data.enemyId].initialPosition = data.enemyPosition;
                    const enemyKilled = {
                        type: "enemyKilled", 
                        enemyId: data.enemyId, 
                        id: name
                    }
                    wss.broadcast(pack(enemyKilled), socket);

                    if (Object.keys(enemies).length == 0) {
                        //killcam
                        sendReplayEvents();
                    }
                }
                else {
                    wss.broadcast(pack({type: "enemyShot", enemyId: data.enemyId, damage: data.damage}), socket);
                }
            }
            break;
        case "state":
            connectedClients[name].updatePlayerState(data);
            wss.broadcast(pack(connectedClients[name]), null);
            break;
    }
}