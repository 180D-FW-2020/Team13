'use strict';

// const { unpack, pack } = require('msgpackr');

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
var currentPosition = 0;

// connect to the redis database

if (process.env.REDISTOGO_URL) {
    var rtg = require("url").parse(process.env.REDISTOGO_URL);
    var redis = require("redis").createClient(rtg.port, rtg.hostname);
    redis.auth(rtg.auth.split(":")[1]);
} else {
    var redis = require("redis").createClient();
}

// report connection status

redis.on('connect', function () {
    console.log('Redis database connected');
});

// connect to the server

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

    var myMap = new Map();

    var keyArray = [];

    var counter = 0;

    for (let i = 0; i <= currentPosition - 1; i++) {
        keyArray.push(JSON.stringify(i));
    }

    if (len < maxLen) {
	counter = currentPosition;
    }
    else {
        counter = maxLen;
    }
    
    function broadcastFirst(callback) {
        keyArray.forEach(
            function (message, index) {
                redis.get(message, (err, data) => {
                    if (err)
                        return callback(err);
                    data = JSON.parse(data);
                    myMap.set(data.time, data.content);
                    counter--;
                    if (counter == 0)
                        callback(null, myMap)
                });
            }
        );
        
    }

    broadcastFirst(function (err, myMap) {
        if (err)
            console.log('error!');
        events = myMap;
        console.log(events);
         /*
	// The original function
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

    console.log("KILLCAM: " + JSON.stringify(killTimes));

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
                console.log("Recorded events for " + currentKillEvent);
                sendEvents[currentKillEvent] = playerEvents;
                playerEvents = {}
                killEventNum++;
                if (killEventNum == ts.length - 1)
                    switchPlayerTime = Number(keys[keys.length-1]);
                else
                    switchPlayerTime = (ts[killEventNum] + ts[killEventNum + 1]) / 2;
                currentKillEvent = killTimes[ts[Math.min(killEventNum, ts.length - 1)]];
            }
            if (currentKillEvent.substring(2, currentKillEvent.length) == value.remoteState.id) {
                playerEvents[key] = value;
            }
        }
        else
            playerEvents[key] = value;
    }

    console.log("Recorded events for " + currentKillEvent);
    sendEvents[currentKillEvent] = playerEvents;

    const replayEvents = {
        type: "replay",
        enemies: sendEnemies,
        events: sendEvents,
        killTimes: killTimes,
    };
    
    return replayEvents;

	// The original function ends
*/
    });
    
}

function storeReplayEvent(stateEvent, data) {
    
    var timeNow = Date.now();

    var event = {
        type : (stateEvent ? "remoteState" : "enemyKilled")
    }

    if (stateEvent)
        event.remoteState = {...data};
    else 
        event.enemyKilled = {...data};

    var eventNew = {'content': event, 'time': timeNow };

    /*
    if (len >= maxLen) {
        delete events[Object.keys(events)[0]]
    }
    else {
        len++;
    }
    events[Date.now()] = event;
    */

    event = JSON.stringify(eventNew);

    if (len < maxLen) {
        redis.set(JSON.stringify(currentPosition), event);
        currentPosition++;
        len++;    }
    else { 
        if (currentPosition == maxLen) {
            currentPosition = 0;
            redis.set(JSON.stringify(currentPosition), event);
            currentPosition++;
        }
        else { 
            redis.set(JSON.stringify(currentPosition), event);
            currentPosition++;
        }
    }

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
                events = {};
                wss.broadcast(JSON.stringify({type: "start"}), null);
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
            wss.broadcast(JSON.stringify(playerList), null);
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
            wss.broadcast(JSON.stringify(leave), null);
            break;
        case "enemyAttack":
            if (data.enemyId in enemies) {
                console.log("Enemy " + data.enemyId + " attacking " + name);
                enemies[data.enemyId].startAttacking(Date.now());
                connectedClients[name].decrementHealth();
                wss.broadcast(JSON.stringify(connectedClients[name]), socket);
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
                    wss.broadcast(JSON.stringify(enemyKilled), socket);
                    
                    storeReplayEvent(false, enemyKilled);
                    if (Object.keys(enemies).length == 0) {
                        events = getReplayEvents();
                        wss.broadcast(JSON.stringify(events), null);
                    }

                }
                else {
                    wss.broadcast(message, socket);
                }
            }
            break;
        case "state":
            connectedClients[name].updatePlayerState(data);
            storeReplayEvent(true, connectedClients[name]);
            wss.broadcast(JSON.stringify(connectedClients[name]), null);
            break;

        case "test":
            storeReplayEvent(true, data);
	    getReplayEvents();
    }
}