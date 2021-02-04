'use strict';

const { unpack, pack } = require('msgpackr');

const wss = require('./init.js');
const Client = require('./client.js');
const Enemy = require('./enemy.js');
const e = require('express');
const { kill } = require('process');
// const {storeMessage} = require('./storage.js');

var numEnemies = 5;
var readyCount = 0;

var connectedClients = {};
var enemies = {};
var deadEnemies = {};

var events = {};
var len = 0;
var maxLen = 200;

wss.on('connection', socket => {
    console.log("Client connected");

    socket.on('message', data => {
        processMessage(socket, data);
    })
});

function initEnemies() {
    let key = "";
    deadEnemies = {};
    events = {};
    len = 0;
    for (let i = 0; i < numEnemies; i++) {
        key = i.toString();
        const pos = [((Math.floor(Math.random() * 11)-5)) * 4, (Math.floor(Math.random() * 6) * 4)];
        enemies[key] = new Enemy(i, pos, Object.keys(connectedClients).length);
    }
    console.log('Enemies initialized to: ' + JSON.stringify(enemies));
}

function getReplayEvents() {
    let sendEvents = {};
    let playerEvents = {};
    let sendEnemies = {};
    let killTimes = {};
    for (const [key, value] of Object.entries(events)) {
        if (value.type == "enemyKilled") {
            sendEnemies[value.enemyKilled.enemyId] = deadEnemies[value.enemyKilled.enemyId]
            killTimes[key] = value.enemyKilled.enemyId + ":" + value.enemyKilled.id;
        }
    }

    let switchPlayerTime;
    let killEventNum = 0;
    let keys = Object.keys(events);
    let ts = Array.from(Object.keys(killTimes), t => Number(t));
    if (ts.length == 1)
        switchPlayerTime = Number(keys[keys.length-1]);
    else
        switchPlayerTime = (ts[killEventNum] + ts[killEventNum + 1]) / 2;

    let currentKillEvent = killTimes[ts[killEventNum]];
    for (const [key, value] of Object.entries(events)) {
        if (value.type == "remoteState") {
            if (key >= switchPlayerTime) {
                sendEvents[currentKillEvent] = playerEvents;
                playerEvents = {}
                killEventNum++;
                if (killEventNum == ts.length - 1)
                    switchPlayerTime = Number(keys[keys.length-1]);
                else
                    switchPlayerTime = (ts[killEventNum] + ts[killEventNum + 1]) / 2;
                currentKillEvent = killTimes[ts[killEventNum]];
            }
            if (currentKillEvent.substring(2, currentKillEvent.length) == value.remoteState.id) {
                playerEvents[key] = value;
            }
        }
        else
            playerEvents[key] = value;
    }

    sendEvents[currentKillEvent] = playerEvents;

    const replayEvents = {
        type: "replay",
        enemies: sendEnemies,
        events: sendEvents,
        killTimes: killTimes,
    };
    console.log(JSON.stringify(replayEvents));
    return replayEvents;
}

function storeReplayEvent(stateEvent, data) {
    const event = {
        type : (stateEvent ? "remoteState" : "enemyKilled")
    }

    if (stateEvent)
        event.remoteState = {...data};
    else 
        event.enemyKilled = {...data};

    if (len >= maxLen) {
        delete events[Object.keys(events)[0]]
    }
    else {
        len++;
    }
    events[Date.now()] = event;
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
                events = {};
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
            if (data.enemyId in enemies) {
                console.log("Enemy " + data.enemyId + " attacking " + name);
                enemies[data.enemyId].startAttacking(Date.now());
                connectedClients[name].decrementHealth();
                wss.broadcast(pack(connectedClients[name]), socket);
            }
            break;
        case "enemyShot":
            console.log("Enemy " + data.enemyId + " shot by " + name);
            if (data.enemyId in enemies) {
                connectedClients[name].registerShot();
                enemies[data.enemyId].decrementHealth(data.damage);

                if (enemies[data.enemyId].health <= 0) {
                    connectedClients[name].registerKill();
                    console.log("Enemy " + data.enemyId + " killed by " + name);
                    deadEnemies[data.enemyId] = enemies[data.enemyId];
                    delete enemies[data.enemyId];

                    deadEnemies[data.enemyId].position = data.enemyPosition;
                    const enemyKilled = {
                        type: "enemyKilled", 
                        enemyId: data.enemyId, 
                        id: name
                    }
                    wss.broadcast(pack(enemyKilled), socket);
                    
                    storeReplayEvent(false, enemyKilled);
                    if (Object.keys(enemies).length == 0) {
                        //killcam
                        events = getReplayEvents();
                        wss.broadcast(pack(events), null);
                    }
                }
                else {
                    wss.broadcast(pack({type: "enemyShot", enemyId: data.enemyId, damage: data.damage}), socket);
                }
            }
            break;
        case "state":
            connectedClients[name].updatePlayerState(data);
            storeReplayEvent(true, connectedClients[name]);
            wss.broadcast(pack(connectedClients[name]), null);
            break;
    }
}